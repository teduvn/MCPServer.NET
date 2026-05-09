using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Auth.Command.Login;
using OrderManagement.Application.Auth.Command.Register;

namespace OrderManagement.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(ISender sender) : BaseApiController(sender)
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? CreatedAtAction(nameof(Register), new { id = result.Value }, null)
                : BadRequest(new ProblemDetails { Detail = result.Error.Description });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Ok(result.Value)
                : Unauthorized(new ProblemDetails { Detail = result.Error.Description });
        }
    }

}
