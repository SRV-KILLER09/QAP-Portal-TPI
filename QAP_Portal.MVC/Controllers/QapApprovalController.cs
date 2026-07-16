using Microsoft.AspNetCore.Mvc;
using QAP_Portal.MVC.Models;
using QAP_Portal.MVC.Services;

namespace QAP_Portal.MVC.Controllers
{
    // Admin (Inspection Department) side: "QAP Approvals" module.
    // Approve = PUT api/QapLineGroups/{id}/approve { ActionBy } - no remarks field on the API.
    // Reject  = PUT api/QapLineGroups/{id}/reject  { ActionBy, Remarks } - remarks mandatory.
    public class QapApprovalController : Controller
    {
        private readonly IQapApiService _api;

        public QapApprovalController(IQapApiService api)
        {
            _api = api;
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            var role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
            {
                context.Result = RedirectToAction("Index", "Home");
            }
            base.OnActionExecuting(context);
        }

        private string CurrentEmail => HttpContext.Session.GetString("Email") ?? "admin@gail.in";

        [HttpGet]
        public async Task<IActionResult> Index(QapStatus? status)
        {
            ViewBag.ActiveStatus = status;
            var qaps = await _api.GetAllQapGroupsAsync(status);
            return View(qaps);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var qap = await _api.GetQapGroupDetailAsync(id);
            if (qap is null) return NotFound();
            return View(qap);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var ok = await _api.ApproveAsync(id, CurrentEmail);
            TempData[ok ? "Success" : "Error"] = ok
                ? "QAP approved. It is now eligible for an inspection request."
                : "Could not approve the QAP. Only a Submitted QAP can be approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Reject(RejectQapViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Remarks))
            {
                TempData["Error"] = "Remarks are mandatory when rejecting a QAP.";
                return RedirectToAction(nameof(Details), new { id = model.GroupId });
            }

            var ok = await _api.RejectAsync(model.GroupId, CurrentEmail, model.Remarks);
            TempData[ok ? "Success" : "Error"] = ok
                ? $"QAP {model.QapNumber} rejected and sent back to the initiator for editing."
                : "Could not reject the QAP. Only a Submitted QAP can be rejected.";
            return RedirectToAction(nameof(Details), new { id = model.GroupId });
        }
    }
}