using Microsoft.AspNetCore.Mvc;

namespace ThaiBeer.Controllers;

[Route("gioi-thieu")]
public class GioiThieuController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
