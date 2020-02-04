using Microsoft.AspNetCore.Mvc;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Tests.SPA.Controllers
{
    public class SenseNetController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Index")]
        public IActionResult IndexPost()
        {
            // use this controller action to perform test operations

            return View();
        }
    }
}