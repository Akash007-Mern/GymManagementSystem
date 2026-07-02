using GymManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;


namespace GymManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
