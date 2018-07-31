using Microsoft.AspNetCore.Mvc;

namespace PluginArchitectureSample.Controllers
{
    public class BlogController : Controller
    {
        public IActionResult List()
        {
            return View("Plugins/Plugin1/Views/Blog/List.cshtml");
        }
    }
}
