using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PricingAnalytics.Application.Services;
using PricingAnalytics.Domain.Matching;

namespace PricingAnalytics.Api.Controllers;

public sealed record CreateCanonicalServiceRequest(
    [Required, MinLength(2), MaxLength(100)] string Slug,
    [Required, MinLength(2), MaxLength(200)] string DisplayName,
    [Required, MinLength(2), MaxLength(300)] string RequiredPhrase,
    [MaxLength(500)] string? ExcludedWords);

public sealed record UpdateCanonicalServiceRequest(
    [Required, MinLength(2), MaxLength(200)] string DisplayName,
    [Required, MinLength(2), MaxLength(300)] string RequiredPhrase,
    [MaxLength(500)] string? ExcludedWords);

public sealed record PreviewRuleRequest(
    [Required, MinLength(2), MaxLength(300)] string RequiredPhrase,
    [MaxLength(500)] string? ExcludedWords,
    int? EditingId);

/// <summary>
/// Management of the canonical service catalog. Administrators only: these rules decide
/// how every market entry is interpreted, so a bad edit changes the analysis for everyone.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("admin/catalog")]
public sealed class AdminCatalogController(CatalogService catalogService) : ControllerBase
{
    /// <summary>GET /admin/catalog - all rules with how many market entries each one claims.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CatalogEntryView>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await catalogService.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CatalogEntryView>> Create(
        CreateCanonicalServiceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await catalogService.CreateAsync(
                request.Slug, request.DisplayName, request.RequiredPhrase, request.ExcludedWords, cancellationToken);

            return Created($"/admin/catalog/{created.Id}", created);
        }
        catch (CatalogConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateCanonicalServiceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await catalogService.UpdateAsync(
                id, request.DisplayName, request.RequiredPhrase, request.ExcludedWords, cancellationToken);

            return NoContent();
        }
        catch (CatalogNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await catalogService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (CatalogNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /admin/catalog/preview - what a rule would claim, without saving it.
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<RulePreview>> Preview(
        PreviewRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await catalogService.PreviewAsync(
                request.RequiredPhrase, request.ExcludedWords, request.EditingId, cancellationToken);

            return Ok(preview);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>GET /admin/catalog/unmatched - corpus names no rule currently claims.</summary>
    [HttpGet("unmatched")]
    public async Task<ActionResult<IReadOnlyList<UnmatchedName>>> GetUnmatched(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return Ok(await catalogService.GetUnmatchedNamesAsync(Math.Clamp(limit, 1, 500), cancellationToken));
    }

    /// <summary>
    /// GET /admin/catalog/explain?name=... - why a name matches the rule it does,
    /// and why every other rule passed on it.
    /// </summary>
    [HttpGet("explain")]
    public async Task<ActionResult<MatchExplanation>> Explain(
        [FromQuery, Required] string name,
        CancellationToken cancellationToken)
    {
        return Ok(await catalogService.ExplainAsync(name, cancellationToken));
    }
}
