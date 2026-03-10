using System.Diagnostics;
using CMCS_ST10026321.Helpers;
using CMCS_ST10026321.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMCS_ST10026321.Controllers
{
    [Authorize(Roles = Roles.Employee + "," + Roles.HR + "," + Roles.Admin)]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var claims = _context.Claims.ToList();
            Console.WriteLine($"Claims found: {claims.Count}"); // Fixed the syntax error
            return View(claims);
        }
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}