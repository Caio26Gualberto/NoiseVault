using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Headbanger.Api.Controllers
{
    [ApiController]
    [Authorize]
    public abstract class ApiBaseController : BaseController
    {
    }
}
