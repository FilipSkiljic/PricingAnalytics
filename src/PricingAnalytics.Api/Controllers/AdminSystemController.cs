using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PricingAnalytics.Api.Authentication;
using PricingAnalytics.Application.Services;

namespace PricingAnalytics.Api.Controllers;

/// <summary>Administration of the system: registered accounts, overall figures, corpus import.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("admin")]
public sealed class AdminSystemController(AdminService adminService) : ControllerBase
{
    /// <summary>GET /admin/statistics - corpus size, catalog size and coverage.</summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<SystemStatistics>> GetStatistics(CancellationToken cancellationToken)
    {
        return Ok(await adminService.GetStatisticsAsync(cancellationToken));
    }

    /// <summary>GET /admin/businesses - registered accounts with their price list sizes.</summary>
    [HttpGet("businesses")]
    public async Task<ActionResult<IReadOnlyList<BusinessView>>> GetBusinesses(CancellationToken cancellationToken)
    {
        return Ok(await adminService.GetBusinessesAsync(cancellationToken));
    }

    /// <summary>DELETE /admin/businesses/{id} - removes an account and its price list.</summary>
    [HttpDelete("businesses/{id:int}")]
    public async Task<IActionResult> DeleteBusiness(int id, CancellationToken cancellationToken)
    {
        try
        {
            await adminService.DeleteBusinessAsync(id, User.GetBusinessId(), cancellationToken);
            return NoContent();
        }
        catch (AdminOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /admin/corpus/import - replaces the whole market corpus from an uploaded
    /// "Name","Price" export.
    /// </summary>
    [HttpPost("corpus/import")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<CorpusImportResult>> ImportCorpus(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Datoteka nije priložena ili je prazna." });
        }

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var csvText = await reader.ReadToEndAsync(cancellationToken);

        try
        {
            return Ok(await adminService.ImportCorpusAsync(csvText, cancellationToken));
        }
        catch (AdminOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
