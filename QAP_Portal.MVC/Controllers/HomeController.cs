using Microsoft.AspNetCore.Mvc;
using QAP_Portal.MVC.Models;
using QAP_Portal.MVC.Services;
using Microsoft.AspNetCore.SignalR;
using QAP_Portal.MVC.Hubs;

namespace QAP_Portal.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IQapApiService _api;
        private readonly IHubContext<NotificationHub> _hubContext;

        public HomeController(IQapApiService api, IHubContext<NotificationHub> hubContext)
        {
            _api = api;
            _hubContext = hubContext;
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
                HttpContext.Session.SetString("DisplayName", adminResult.AdminName);
                TempData["Success"] = $"Welcome back, Admin {adminResult.AdminName}!";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(password))
                {
                    var userResult = await _api.LoginQapUserAsync(email, password);
                    if (userResult == null)
                    {
                        TempData["Error"] = "Invalid initiator credentials or account inactive.";
                        return RedirectToAction(nameof(Index));
                    }

                    HttpContext.Session.SetString("Role", userResult.Role);
                    HttpContext.Session.SetString("Email", userResult.Email);
                    HttpContext.Session.SetString("DisplayName", userResult.DisplayName);
                    TempData["Success"] = $"Welcome back, {userResult.DisplayName}!";
                }
                else
                {
                    HttpContext.Session.SetString("Role", role);
                    HttpContext.Session.SetString("Email", email);
                    
                    var parts = email.Split('@');
                    var displayName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[0].Replace(".", " "));
                    HttpContext.Session.SetString("DisplayName", displayName);
                    
                    TempData["Success"] = $"Access Granted! Welcome to GAIL QAP System.";
                }
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
        public async Task<IActionResult> PushNotification(string title, string message)
        {
            if (CurrentRole != nameof(UserRole.Admin))
            {
                return Json(new { success = false, message = "Access Denied: Only Admin can broadcast notifications." });
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, message = "Title and message are required." });
            }

            var timestamp = DateTime.Now.ToString("dd/MM/yyyy, hh:mm:ss tt");
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", title, message, timestamp);

            return Json(new { success = true });
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