using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediVerify.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        [Route("Error")]
        public IActionResult Index(int? code)
        {
            var status = code ?? 500;
            Response.StatusCode = status;
            ViewData["StatusCode"] = status;
            ViewData["Title"] = status == 404 ? "Page not found" : "Something went wrong";
            ViewData["Message"] = status switch
            {
                404 => "The page you are looking for could not be found.",
                403 => "You do not have permission to access this page.",
                _ => "An unexpected error occurred. Please try again."
            };
            return View();
        }
    }
}
