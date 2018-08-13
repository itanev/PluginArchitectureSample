using Microsoft.AspNetCore.Mvc;

namespace Plugin1.Controllers
{
    public class BlogController : Controller
    {
        public IActionResult List()
        {
            return View("Plugins/Plugin1/Views/Blog/List.cshtml");
        }
    }
}
