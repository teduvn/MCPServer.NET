using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.WebAPI.Controllers
{
    /// <summary>
    /// Base class cho tất cả API controller.
    /// Inject ISender một lần — các controller con không cần khai báo lại.
    /// </summary>
    [ApiController]
    public abstract class BaseApiController(ISender sender) : ControllerBase
    {
        protected readonly ISender Sender = sender;
    }
}
