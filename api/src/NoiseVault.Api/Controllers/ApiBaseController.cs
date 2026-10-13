using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NoiseVault.Api.Controllers
{
    [ApiController]
    [Authorize]
    public abstract class ApiBaseController : BaseController
    {
    }
}
