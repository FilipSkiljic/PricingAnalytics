using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PricingAnalytics.Application.Services;
using PricingAnalytics.Domain.Recommendation;

namespace PricingAnalytics.Api.Controllers;

/// <summary>
/// Market analysis endpoints. Behind authentication like the rest of the application -
/// the market corpus is the product here, so it is not served to anonymous callers.
/// </summary>
[ApiController]
[Authorize]
public sealed class AnalyticsController(PriceAnalysisService priceAnalysisService) : ControllerBase
{
    /// <summary>GET /analyze?service=...&amp;price=... - is the given price fair for that service?</summary>
    [HttpGet("/analyze")]
    public async Task<ActionResult<PriceRecommendation>> Analyze(
        [FromQuery] string service,
        [FromQuery] decimal price,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await priceAnalysisService.AnalyzeAsync(service, price, cancellationToken);
            return Ok(result);
        }
        catch (UnknownServiceException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InsufficientMarketDataException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>GET /services - canonical services the engine can analyze.</summary>
    [HttpGet("/services")]
    public async Task<ActionResult<IReadOnlyList<AvailableService>>> GetServices(CancellationToken cancellationToken) =>
        Ok(await priceAnalysisService.GetAvailableServicesAsync(cancellationToken));
}
