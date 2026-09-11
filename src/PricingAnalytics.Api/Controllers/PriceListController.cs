using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PricingAnalytics.Api.Authentication;
using PricingAnalytics.Application.Services;

namespace PricingAnalytics.Api.Controllers;

public sealed record AddServiceRequest(
    [Required, MinLength(2)] string Name,
    [Range(0, 1_000_000)] decimal Price);

/// <summary>
/// The signed-in business's own price list. Every endpoint scopes its work to the
/// business id carried by the access token, so one account can never read or modify
/// another account's rows.
/// </summary>
[ApiController]
[Authorize]
[Route("my/services")]
public sealed class PriceListController(PriceListService priceListService) : ControllerBase
{
    /// <summary>GET /my/services - the price list, each row analyzed against the market.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PriceListItem>>> Get(CancellationToken cancellationToken)
    {
        var items = await priceListService.GetAnalyzedAsync(User.GetBusinessId(), cancellationToken);
        return Ok(items);
    }

    /// <summary>POST /my/services - adds a single service to the price list.</summary>
    [HttpPost]
    public async Task<IActionResult> Add(AddServiceRequest request, CancellationToken cancellationToken)
    {
        var service = await priceListService.AddAsync(
            User.GetBusinessId(), request.Name, request.Price, cancellationToken);

        return Created($"/my/services/{service.Id}", new { service.Id, service.Name, service.Price });
    }

    /// <summary>DELETE /my/services/{id} - removes one row.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await priceListService.DeleteAsync(User.GetBusinessId(), id, cancellationToken);
            return NoContent();
        }
        catch (PriceListNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /my/services/import - replaces the whole price list from an uploaded
    /// "Name","Price" CSV export.
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ImportResult>> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Datoteka nije priložena ili je prazna." });
        }

        // UTF8 with BOM detection: real POS exports carry a BOM often enough that
        // reading without it corrupts the first column name.
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var csvText = await reader.ReadToEndAsync(cancellationToken);

        try
        {
            var result = await priceListService.ImportFromCsvAsync(User.GetBusinessId(), csvText, cancellationToken);
            return Ok(result);
        }
        catch (PriceListNotFoundException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
