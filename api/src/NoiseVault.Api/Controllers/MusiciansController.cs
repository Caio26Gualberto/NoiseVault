using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Musicians.Dtos;
using NoiseVault.Application.Musicians.Interfaces;

namespace NoiseVault.Api.Controllers
{
    [Route("api/musicians")]
    public class MusiciansController : ApiBaseController
    {
        private readonly IMusicianAppService _musicianAppService;

        public MusiciansController(IMusicianAppService musicianAppService)
        {
            _musicianAppService = musicianAppService;
        }

        [EnableRateLimiting(ApiRateLimitPolicies.UserSearch)]
        [HttpGet]
        public async Task<ActionResult> Search([FromQuery] MusicianSearchQueryDto query)
        {
            var result = await _musicianAppService.Search(query);
            return ToResponse(result);
        }

        [HttpGet("me")]
        public async Task<ActionResult> GetMine()
        {
            var result = await _musicianAppService.GetMine();
            return ToResponse(result);
        }

        [HttpPut("me")]
        public async Task<ActionResult> SaveMine([FromBody] MusicianInputDto input)
        {
            var result = await _musicianAppService.SaveMine(input);
            return ToResponse(result);
        }

        [HttpPost("me/looking")]
        public async Task<ActionResult> SetLooking([FromBody] MusicianLookingInputDto input)
        {
            var result = await _musicianAppService.SetLooking(input);
            return ToResponse(result);
        }

        [HttpGet("{nickname}")]
        public async Task<ActionResult> GetByNickname(string nickname)
        {
            var result = await _musicianAppService.GetByNickname(nickname);
            return ToResponse(result);
        }
    }
}
