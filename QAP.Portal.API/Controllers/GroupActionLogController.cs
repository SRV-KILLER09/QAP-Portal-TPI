using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QAP.Portal.API.Data;
using QAP.Portal.API.Models;

namespace QAP.Portal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupActionLogController : ControllerBase
    {
        private readonly QapDbContext _context;

        public GroupActionLogController(QapDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var logs = await _context.GroupActionLogs.ToListAsync();
            return Ok(logs);
        }

        [HttpGet("{seqNo}")]
        public async Task<IActionResult> GetById(int seqNo)
        {
            var log = await _context.GroupActionLogs.FindAsync(seqNo);
            if (log == null) return NotFound();
            return Ok(log);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GroupActionLog log)
        {
            _context.GroupActionLogs.Add(log);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { seqNo = log.SeqNo }, log);
        }

        [HttpPut("{seqNo}")]
        public async Task<IActionResult> Update(int seqNo, [FromBody] GroupActionLog updated)
        {
            var existing = await _context.GroupActionLogs.FindAsync(seqNo);
            if (existing == null) return NotFound();

            existing.GroupId = updated.GroupId;
            existing.Stage = updated.Stage;
            existing.ActionOn = updated.ActionOn;
            existing.ActionBy = updated.ActionBy;
            existing.Remarks = updated.Remarks;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{seqNo}")]
        public async Task<IActionResult> Delete(int seqNo)
        {
            var existing = await _context.GroupActionLogs.FindAsync(seqNo);
            if (existing == null) return NotFound();

            _context.GroupActionLogs.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}