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

        [HttpGet]
        [EnableRateLimiting(ApiRateLimitPolicies.CitySearch)]
        public async Task<ActionResult> Search([FromQuery] CitySearchQueryDto query)
        {
            var result = await _cityAppService.Search(query);
            return ToResponse(result);
        }
    }
}
