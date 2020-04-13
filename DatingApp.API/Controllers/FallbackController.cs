using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    // by default every user will have to authenticate to every single method,
    // but for this controller we need allow anonymous 
    // because we always want our users to be able to get to the Index.html page
    [AllowAnonymous]
    public class FallbackController : Controller
    {
        public IActionResult Index()
        {
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot", "index.html"), "text/HTML");
        }
    }
}