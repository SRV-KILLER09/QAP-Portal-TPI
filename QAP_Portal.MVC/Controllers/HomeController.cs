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

            // 1. Try Admin database login first
            var adminResult = !string.IsNullOrWhiteSpace(password) 
                ? await _api.LoginAdminAsync(email, password) 
                : null;

            if (adminResult != null)
            {
                HttpContext.Session.SetString("Role", nameof(UserRole.Admin));
                HttpContext.Session.SetString("Email", email);
                HttpContext.Session.SetString("AdminName", adminResult.AdminName);
                HttpContext.Session.SetString("AdminId", adminResult.AdminId);
                HttpContext.Session.SetString("DisplayName", adminResult.AdminName);
                TempData["Success"] = $"Welcome back, Admin {adminResult.AdminName}!";
                return RedirectToAction(nameof(Dashboard));
            }

            // 2. Try Initiator/User database login next
            var userResult = !string.IsNullOrWhiteSpace(password) 
                ? await _api.LoginQapUserAsync(email, password) 
                : null;

            if (userResult != null)
            {
                HttpContext.Session.SetString("Role", userResult.Role); // Fetches correct role from database (Admin or Initiator)
                HttpContext.Session.SetString("Email", userResult.Email);
                HttpContext.Session.SetString("DisplayName", userResult.DisplayName);
                TempData["Success"] = $"Welcome back, {userResult.DisplayName}!";
                return RedirectToAction(nameof(Dashboard));
            }

            // 3. Fallback for passwordless / dev bypass (only if password is empty)
            if (string.IsNullOrWhiteSpace(password))
            {
                var admins = await _api.GetAdminsAsync();
                bool isDbAdmin = admins.Any(a => a.Email.ToLower() == email.ToLower());
                string resolvedRole = isDbAdmin ? nameof(UserRole.Admin) : nameof(UserRole.Initiator);
                
                HttpContext.Session.SetString("Role", resolvedRole);
                HttpContext.Session.SetString("Email", email);
                
                var parts = email.Split('@');
                var displayName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[0].Replace(".", " "));
                HttpContext.Session.SetString("DisplayName", displayName);
                
                TempData["Success"] = $"Access Granted! Welcome to GAIL QAP System.";
                return RedirectToAction(nameof(Dashboard));
            }

            TempData["Error"] = "Invalid credentials or account inactive.";
            return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> Register(string email, string displayName, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "All fields are required for registration.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _api.CreateQapUserAsync(email, displayName, role ?? "Initiator", password);
            if (success)
            {
                TempData["Success"] = "Account created successfully! You can now log in.";
            }
            else
            {
                TempData["Error"] = "Failed to create account. User might already exist.";
            }

            return RedirectToAction(nameof(Index));
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