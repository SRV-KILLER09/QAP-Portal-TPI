using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QAP.Portal.API.Data;
using QAP.Portal.API.Models;

namespace QAP.Portal.API.Controllers
{
    public class RejectRequest
    {
        public string ActionBy { get; set; } = null!;
        public string Remarks { get; set; } = null!;
    }

    public class ActionRequest
    {
        public string ActionBy { get; set; } = null!;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class QapLineGroupsController : ControllerBase
    {
        private readonly QapDbContext _context;

        public QapLineGroupsController(QapDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var groups = await _context.QapLineGroups.ToListAsync();
            return Ok(groups);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var group = await _context.QapLineGroups.FindAsync(id);
            if (group == null) return NotFound();
            return Ok(group);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] QapLineGroup group)
        {
            group.Status = "D"; // every new group always starts as Draft
            _context.QapLineGroups.Add(group);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = group.GroupId }, group);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] QapLineGroup updated)
        {
            var existing = await _context.QapLineGroups.FindAsync(id);
            if (existing == null) return NotFound();

            // Status is NOT editable here - only via Submit/Approve/Reject/Reopen endpoints
            existing.QapNumber = updated.QapNumber;
            existing.QapDocument = updated.QapDocument;
            existing.DrawingDocument = updated.DrawingDocument;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.QapLineGroups.FindAsync(id);
            if (existing == null) return NotFound();

            _context.QapLineGroups.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id}/submit")]
        public async Task<IActionResult> Submit(int id, [FromBody] ActionRequest req)
        {
            var group = await _context.QapLineGroups.FindAsync(id);
            if (group == null) return NotFound();

            if (group.Status != "D")
                return BadRequest(new { error = $"Cannot submit from status '{group.Status}'. Only Draft (D) can be submitted." });

            group.Status = "S";
            _context.GroupActionLogs.Add(new GroupActionLog
            {
                GroupId = id,
                Stage = "I",
                ActionOn = DateTime.Now,
                ActionBy = req.ActionBy,
                Remarks = "Submitted for review"
            });

            await _context.SaveChangesAsync();
            return Ok(group);
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] ActionRequest req)
        {
            var group = await _context.QapLineGroups.FindAsync(id);
            if (group == null) return NotFound();

            if (group.Status != "S")
                return BadRequest(new { error = $"Cannot approve from status '{group.Status}'. Only Submitted (S) can be approved." });

            group.Status = "A";
            _context.GroupActionLogs.Add(new GroupActionLog
            {
                GroupId = id,
                Stage = "R",
                ActionOn = DateTime.Now,
                ActionBy = req.ActionBy,
                Remarks = "Approved"
            });

            await _context.SaveChangesAsync();
            return Ok(group);
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectRequest req)
        {
            var group = await _context.QapLineGroups.FindAsync(id);
            if (group == null) return NotFound();

            if (group.Status != "S")
                return BadRequest(new { error = $"Cannot reject from status '{group.Status}'. Only Submitted (S) can be rejected." });

            if (string.IsNullOrWhiteSpace(req.Remarks))
                return BadRequest(new { error = "Remarks are mandatory when rejecting." });

            group.Status = "R";
            _context.GroupActionLogs.Add(new GroupActionLog
            {
                GroupId = id,
                Stage = "R",
                ActionOn = DateTime.Now,
                ActionBy = req.ActionBy,
                Remarks = req.Remarks
            });

            await _context.SaveChangesAsync();
            return Ok(group);
        }

        [HttpPut("{id}/reopen")]
        public async Task<IActionResult> Reopen(int id, [FromBody] ActionRequest req)
        {
            var group = await _context.QapLineGroups.FindAsync(id);
            if (group == null) return NotFound();

            if (group.Status != "R")
                return BadRequest(new { error = $"Cannot reopen from status '{group.Status}'. Only Rejected (R) can be reopened to Draft." });

            group.Status = "D";
            _context.GroupActionLogs.Add(new GroupActionLog
            {
                GroupId = id,
                Stage = "I",
                ActionOn = DateTime.Now,
                ActionBy = req.ActionBy,
                Remarks = "Reopened for editing after rejection"
            });

            await _context.SaveChangesAsync();
            return Ok(group);
        }

        [HttpPost("{id}/upload-qap-document")]
        public async Task<IActionResult> UploadQapDocument(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            var group = await _context.QapLineGroups.FindAsync(id);
            if (group == null) return NotFound(new { error = $"No QAP_LINE_GROUPS record found for id '{id}'." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            group.QapDocument = ms.ToArray();

            await _context.SaveChangesAsync();
            return Ok(new { groupId = id, fileName = file.FileName, sizeBytes = file.Length, message = "QAP document uploaded successfully." });
        }

        [HttpPost("{id}/upload-drawing")]
        public async Task<IActionResult> UploadDrawing(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            var group = await _context.QapLineGroups.FindAsync(id);
            if (group == null) return NotFound(new { error = $"No QAP_LINE_GROUPS record found for id '{id}'." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            group.DrawingDocument = ms.ToArray();

            await _context.SaveChangesAsync();
            return Ok(new { groupId = id, fileName = file.FileName, sizeBytes = file.Length, message = "Drawing uploaded successfully." });
        }
    }
}