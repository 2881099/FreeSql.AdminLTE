using Microsoft.AspNetCore.Mvc;

namespace net80_blazor.Controllers
{
	[Route("/webapi")]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public string Index()
        {
            return "ok";
        }
    }
}
