using Microsoft.AspNetCore.Mvc;
using QAP_Portal.MVC.Models;
using QAP_Portal.MVC.Services;

namespace QAP_Portal.MVC.Controllers
{
    // Initiator (Mechanical / Initiator Department) side of the QAP module.
    public class QapController : Controller
    {
        private readonly IQapApiService _api;

        public QapController(IQapApiService api)
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

        private string CurrentEmail => HttpContext.Session.GetString("Email") ?? "initiator@gail.in";

        [HttpGet]
        public IActionResult Create() => View(new CreateQapViewModel());

        // AJAX: GET api/PurchaseOrders/{po} via the service, feeds Step 1 result + Step 2 line item table
        [HttpGet]
        public async Task<IActionResult> SearchPo(string poNumber)
        {
            if (string.IsNullOrWhiteSpace(poNumber))
                return Json(new { success = false, message = "Enter a PO number." });

            var po = await _api.SearchPurchaseOrderAsync(poNumber);
            if (po is null)
                return Json(new { success = false, message = $"No purchase order found for '{poNumber}'." });

            return Json(new { success = true, po });
        }

        // Final submit: POST api/QapCreation, then per-group and PO-level document uploads.
        // NOTE: the API auto-submits on creation - there is no "Save Draft" path today.
        [HttpPost]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Create(CreateQapViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.PoNumber))
                ModelState.AddModelError(nameof(model.PoNumber), "PO Number is required.");

            if (model.Groups is null || model.Groups.Count == 0 || model.Groups.All(g => g.LineItems.Count == 0))
                ModelState.AddModelError(string.Empty, "Select at least one line item and group it.");

            if (model.TechnicalSpecificationFile is null || model.PurchaseOrderCopyFile is null)
                ModelState.AddModelError(string.Empty, "Technical Specification and Purchase Order Copy are mandatory.");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the highlighted errors before submitting.";
                return View(model);
            }

            var createdGroupIds = await _api.CreateQapAsync(model, CurrentEmail);
            if (createdGroupIds.Count == 0)
            {
                TempData["Error"] = "Could not create the QAP. Please try again.";
                return View(model);
            }

            TempData["Success"] = createdGroupIds.Count == 1
                ? "QAP submitted successfully."
                : $"{createdGroupIds.Count} QAPs submitted successfully (one per line item group).";

            // Multiple groups can come out of a single wizard run - land on the first one.
            return RedirectToAction(nameof(Details), new { id = createdGroupIds[0] });
        }

        [HttpGet]
        public async Task<IActionResult> MyQaps(QapStatus? status)
        {
            ViewBag.ActiveStatus = status;
            var qaps = await _api.GetQapGroupsForInitiatorAsync(CurrentEmail, status);
            return View(qaps);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var qap = await _api.GetQapGroupDetailAsync(id);
            if (qap is null) return NotFound();
            return View(qap);
        }

        // Rejected -> Draft, so the initiator can edit and resubmit (per the spec's rejection flow)
        [HttpPost]
        public async Task<IActionResult> Reopen(int id)
        {
            var ok = await _api.ReopenAsync(id, CurrentEmail);
            TempData[ok ? "Success" : "Error"] = ok
                ? "QAP reopened for editing."
                : "Could not reopen the QAP.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Draft -> Submitted (used after a Reopen, since the create endpoint only runs once)
        [HttpPost]
        public async Task<IActionResult> Resubmit(int id)
        {
            var ok = await _api.SubmitAsync(id, CurrentEmail);
            TempData[ok ? "Success" : "Error"] = ok
                ? "QAP resubmitted for review."
                : "Could not resubmit the QAP.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Document downloads - the API stores raw bytes with no filename/content-type,
        // so these come back as a generic octet-stream.
        [HttpGet]
        public async Task<IActionResult> DownloadQapDocument(int id)
        {
            var bytes = await _api.GetQapDocumentBytesAsync(id);
            if (bytes is null || bytes.Length == 0) return NotFound();
            return File(bytes, "application/octet-stream", $"qap-document-group-{id}");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDrawing(int id)
        {
            var bytes = await _api.GetDrawingBytesAsync(id);
            if (bytes is null || bytes.Length == 0) return NotFound();
            return File(bytes, "application/octet-stream", $"drawing-group-{id}");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTechSpec(string po)
        {
            var bytes = await _api.GetTechSpecBytesAsync(po);
            if (bytes is null || bytes.Length == 0) return NotFound();
            return File(bytes, "application/octet-stream", $"technical-specification-{po}");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPoCopy(string po)
        {
            var bytes = await _api.GetPoCopyBytesAsync(po);
            if (bytes is null || bytes.Length == 0) return NotFound();
            return File(bytes, "application/octet-stream", $"po-copy-{po}");
        }
    }
}