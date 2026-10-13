using Microsoft.AspNetCore.Mvc;
using NoiseVault.Application.Posts.Interfaces;

namespace NoiseVault.Api.Controllers
{
    [Route("api/comments")]
    public class CommentsController : ApiBaseController
    {
        private readonly ICommentAppService _commentAppService;

        public CommentsController(ICommentAppService commentAppService)
        {
            _commentAppService = commentAppService;
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _commentAppService.Delete(id);
            return ToResponse(result);
        }
    }
}
