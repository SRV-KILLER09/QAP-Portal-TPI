using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QAP.Portal.API.Data;
using QAP.Portal.API.Models;

namespace QAP.Portal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QapGroupItemsController : ControllerBase
    {
        private readonly QapDbContext _context;

        public QapGroupItemsController(QapDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.QapGroupItems.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{po}/{line}/{itemNo}")]
        public async Task<IActionResult> GetById(string po, int line, int itemNo)
        {
            var item = await _context.QapGroupItems
                .FirstOrDefaultAsync(x => x.Po == po && x.Line == line && x.ItemNo == itemNo);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] QapGroupItem item)
        {
            _context.QapGroupItems.Add(item);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById),
                new { po = item.Po, line = item.Line, itemNo = item.ItemNo }, item);
        }

        [HttpPut("{po}/{line}/{itemNo}")]
        public async Task<IActionResult> Update(string po, int line, int itemNo, [FromBody] QapGroupItem updated)
        {
            var existing = await _context.QapGroupItems
                .FirstOrDefaultAsync(x => x.Po == po && x.Line == line && x.ItemNo == itemNo);
            if (existing == null) return NotFound();

            existing.GroupId = updated.GroupId;
            existing.UpdatedOn = updated.UpdatedOn;
            existing.UpdatedBy = updated.UpdatedBy;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{po}/{line}/{itemNo}")]
        public async Task<IActionResult> Delete(string po, int line, int itemNo)
        {
            var existing = await _context.QapGroupItems
                .FirstOrDefaultAsync(x => x.Po == po && x.Line == line && x.ItemNo == itemNo);
            if (existing == null) return NotFound();

            _context.QapGroupItems.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}