using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Cities.Dtos;
using NoiseVault.Application.Cities.Interfaces;

namespace NoiseVault.Api.Controllers
{
    [Route("api/cities")]
    public class CitiesController : ApiBaseController
    {
        private readonly ICityAppService _cityAppService;

        public CitiesController(ICityAppService cityAppService)
        {
            _cityAppService = cityAppService;
        }

        [AllowAnonymous]
        [HttpGet]
        [EnableRateLimiting(ApiRateLimitPolicies.CitySearch)]
        public async Task<ActionResult> Search([FromQuery] CitySearchQueryDto query)
        {
            var result = await _cityAppService.Search(query);
            return ToResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("featured")]
        public async Task<ActionResult> Featured()
        {
            var result = await _cityAppService.Featured();
            return ToResponse(result);
        }
    }
}
