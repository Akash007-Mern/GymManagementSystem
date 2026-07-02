using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.Controllers
{
    public class MembershipPlanController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
