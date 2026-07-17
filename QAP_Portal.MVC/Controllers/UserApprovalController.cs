using Microsoft.AspNetCore.Mvc;
using QAP_Portal.MVC.Services;
using System.Threading.Tasks;

namespace QAP_Portal.MVC.Controllers
{
    public class UserApprovalController : Controller
    {
        private readonly IQapApiService _api;

        public UserApprovalController(IQapApiService api)
        {
            _api = api;
        }

        private string? CurrentRole => HttpContext.Session.GetString("Role");

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (CurrentRole != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var pendingUsers = await _api.GetPendingUsersAsync();
            return View(pendingUsers);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(string email)
        {
            if (CurrentRole != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email is required.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _api.ApproveUserAsync(email);
            if (success)
            {
                TempData["Success"] = $"Account for {email} has been approved and activated.";
            }
            else
            {
                TempData["Error"] = $"Failed to approve account for {email}.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(string email)
        {
            if (CurrentRole != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email is required.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _api.RejectUserAsync(email);
            if (success)
            {
                TempData["Success"] = $"Registration request for {email} has been rejected.";
            }
            else
            {
                TempData["Error"] = $"Failed to reject request for {email}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
