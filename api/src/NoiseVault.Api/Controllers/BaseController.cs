using Microsoft.AspNetCore.Mvc;
using NoiseVault.Api.ApiResponse;
using NoiseVault.Api.Diagnostics;
using NoiseVault.Application.Response;

namespace NoiseVault.Api.Controllers
{
    public class BaseController : ControllerBase
    {
        protected ActionResult MapError(Error error)
        {
            var response = new ApiResponse<object>
            {
                IsSuccess = false,
                Code = error.Code,
                Message = error.Message,
            };

            if (error.Type is not (ErrorType.Validation or ErrorType.NotFound
                or ErrorType.Unauthorized or ErrorType.Forbidden or ErrorType.Conflict))
                HttpContext.Items[RequestDescriptor.ErrorCodeItemKey] = error.Code;

            return error.Type switch
            {
                ErrorType.Validation => BadRequest(response),
                ErrorType.NotFound => NotFound(response),
                ErrorType.Unauthorized => Unauthorized(response),
                ErrorType.Forbidden => StatusCode(403, response),
                ErrorType.Conflict => Conflict(response),
                _ => StatusCode(500, response)
            };
        }

        protected ActionResult ToResponse<T>(Result<T> result)
        {
            if (result.IsFailure)
                return MapError(result.Error);

            return Ok(new ApiResponse<T>
            {
                IsSuccess = true,
                Code = result.Code,
                Message = result.Message,
                Data = result.Data
            });
        }

        protected ActionResult ToResponse(Result result)
        {
            if (result.IsFailure)
                return MapError(result.Error);

            return Ok(new ApiResponse<object>
            {
                IsSuccess = true,
                Code = result.Code,
                Message = result.Message,
                Data = null
            });
        }
    }
}
