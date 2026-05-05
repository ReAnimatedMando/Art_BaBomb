using Microsoft.AspNetCore.Mvc;

namespace Art_BaBomb.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

    }
}
