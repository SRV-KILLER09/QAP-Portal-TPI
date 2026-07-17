using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QAP.Portal.API.Data;
using QAP.Portal.API.Models;
using QAP.Portal.API.Models.Dtos;

namespace QAP.Portal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QapCreationController : ControllerBase
    {
        private readonly QapDbContext _context;

        public QapCreationController(QapDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateFullQap([FromBody] CreateQapRequest request)
        {
            // Basic validation (Section 13 rules)
            if (string.IsNullOrWhiteSpace(request.Po))
                return BadRequest(new { error = "PO is mandatory." });

            if (request.Groups == null || request.Groups.Count == 0)
                return BadRequest(new { error = "At least one line item group is mandatory." });

            foreach (var g in request.Groups)
            {
                if (g.LineItems == null || g.LineItems.Count == 0)
                    return BadRequest(new { error = "Each group must have at least one line item." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Ensure PO_DOCUMENTS row exists for this PO (created empty for now - file upload comes later)
                var poDoc = await _context.PoDocuments.FindAsync(request.Po);
                if (poDoc == null)
                {
                    poDoc = new PoDocument
                    {
                        Po = request.Po,
                        UpdatedOn = DateTime.Now,
                        UpdatedBy = "SYSTEM"
                    };
                    _context.PoDocuments.Add(poDoc);
                }

                var createdGroups = new List<QapLineGroup>();

                foreach (var groupReq in request.Groups)
                {
                    var group = new QapLineGroup
                    {
                        Status = "D",
                        AssignedAdmin = request.AssignedAdmin
                    };
                    _context.QapLineGroups.Add(group);
                    await _context.SaveChangesAsync(); // save now so group.GroupId is generated

                    // Give it a display QAP number now that we have the real ID
                    group.QapNumber = $"GAIL-QAP-{DateTime.Now.Year}-{group.GroupId:D4}";

                    foreach (var item in groupReq.LineItems)
                    {
                        _context.QapGroupItems.Add(new QapGroupItem
                        {
                            Po = request.Po,
                            Line = item.Line,
                            ItemNo = item.ItemNo,
                            GroupId = group.GroupId,
                            UpdatedOn = DateTime.Now,
                            UpdatedBy = "SYSTEM"
                        });
                    }

                    // Submit immediately (Draft -> Submitted), matching "Submit QAP" button
                    group.Status = "S";
                    _context.GroupActionLogs.Add(new GroupActionLog
                    {
                        GroupId = group.GroupId,
                        Stage = "I",
                        ActionOn = DateTime.Now,
                        ActionBy = request.InitiatorEmail ?? "unknown",
                        Remarks = "QAP created and submitted"
                    });

                    createdGroups.Add(group);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    po = request.Po,
                    groupsCreated = createdGroups.Select(g => new { g.GroupId, g.QapNumber, g.Status })
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { error = "QAP creation failed.", detail = innerMsg });
            }
        }
    }
}