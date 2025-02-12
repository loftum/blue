using Microsoft.AspNetCore.Mvc;

namespace Blue.Web.Controllers;

[Route("")]
public class HomeController : Controller
{
    [HttpGet("")]
    public object Index()
    {
        return View();
    }
}