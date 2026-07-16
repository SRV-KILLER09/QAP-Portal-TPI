using Microsoft.AspNetCore.Mvc;
using QAP_Portal.MVC.Models;
using QAP_Portal.MVC.Services;

namespace QAP_Portal.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IQapApiService _api;

        public HomeController(IQapApiService api)
        {
            _api = api;
        }

        private string? CurrentRole => HttpContext.Session.GetString("Role");

        private string CurrentEmail =>
            HttpContext.Session.GetString("Email") ??
            (CurrentRole == nameof(UserRole.Admin)
                ? "admin@gail.in"
                : "initiator@gail.in");

        public IActionResult Index()
        {
            if (!string.IsNullOrEmpty(CurrentRole))
                return RedirectToAction(nameof(Dashboard));

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SetRole(string role, string email, string? password)
        {
            email = string.IsNullOrWhiteSpace(email)
                ? (role == nameof(UserRole.Admin)
                    ? "admin@gail.in"
                    : "initiator@gail.in")
                : email.Trim();

            // Allow only valid registered database admin accounts to access Admin role
            if (role == nameof(UserRole.Admin))
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    TempData["Error"] = "Password is required for Admin login.";
                    return RedirectToAction(nameof(Index));
                }

                var adminResult = await _api.LoginAdminAsync(email, password);
                if (adminResult == null)
                {
                    TempData["Error"] = "Invalid admin credentials or account inactive.";
                    return RedirectToAction(nameof(Index));
                }

                HttpContext.Session.SetString("Role", role);
                HttpContext.Session.SetString("Email", email);
                HttpContext.Session.SetString("AdminName", adminResult.AdminName);
                HttpContext.Session.SetString("AdminId", adminResult.AdminId);
                TempData["Success"] = $"Welcome back, Admin {adminResult.AdminName}!";
            }
            else
            {
                HttpContext.Session.SetString("Role", role);
                HttpContext.Session.SetString("Email", email);
                TempData["Success"] = $"Access Granted! Welcome to GAIL QAP System.";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (string.IsNullOrEmpty(CurrentRole))
                return RedirectToAction(nameof(Index));

            var vm = new DashboardViewModel
            {
                Role = CurrentRole!,
                Email = CurrentEmail
            };

            var groups = CurrentRole == nameof(UserRole.Admin)
                ? await _api.GetAllQapGroupsAsync()
                : await _api.GetQapGroupsForInitiatorAsync(CurrentEmail);

            vm.Total = groups.Count;
            vm.DraftCount = groups.Count(q => q.Status == QapStatus.Draft);
            vm.SubmittedCount = groups.Count(q => q.Status == QapStatus.Submitted);
            vm.ApprovedCount = groups.Count(q => q.Status == QapStatus.Approved);
            vm.RejectedCount = groups.Count(q => q.Status == QapStatus.Rejected);

            vm.RecentQaps = groups
                .OrderByDescending(q => q.InitiatedOn)
                .Take(5)
                .ToList();

            return View(vm);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}