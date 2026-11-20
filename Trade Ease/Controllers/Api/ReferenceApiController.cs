using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeEase.Models.Dtos;
using TradeEase.Services;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ReferenceController : ControllerBase
{
    private readonly IWorldGeoService _geo;

    public ReferenceController(IWorldGeoService geo) => _geo = geo;

    [HttpGet("countries")]
    [AllowAnonymous]
    public ActionResult<object> Countries()
    {
        var list = PakistanTradeReference.SortCountries(_geo.GetCountries(), c => c.Code).ToList();
        return Ok(new
        {
            total = list.Count,
            source = _geo.Source,
            lastRefreshUtc = _geo.LastRefreshUtc,
            countries = list,
        });
    }

    [HttpGet("country/{code}")]
    [Authorize]
    public ActionResult<CountryDetailDto> Country(string code)
    {
        var country = _geo.GetCountry(code);
        return country is null ? NotFound(new { message = "Country not found." }) : Ok(country);
    }

    [HttpGet("ports")]
    [Authorize]
    public ActionResult<IEnumerable<PortDetailDto>> Ports([FromQuery] string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            return BadRequest(new { message = "Country code is required." });

        var ports = _geo.GetPorts(country);
        if (ports.Count == 0)
            return NotFound(new { message = "No ports found for country." });

        return Ok(ports);
    }
}
