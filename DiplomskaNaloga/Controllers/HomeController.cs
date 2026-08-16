using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DiplomskaNaloga.Controllers {
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger) {
            _logger = logger;
        }

        public IActionResult Index() {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            ViewData["RequestId"] = requestId;
            return View();
        }
    }
}