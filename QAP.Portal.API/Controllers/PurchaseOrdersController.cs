using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QAP.Portal.API.Data;

namespace QAP.Portal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly QapDbContext _context;

        public PurchaseOrdersController(QapDbContext context)
        {
            _context = context;
        }

        [HttpGet("{po}")]
        public async Task<IActionResult> GetPoWithLineItems(string po)
        {
            var header = await _context.SapPoMasters.FindAsync(po);
            if (header == null) return NotFound(new { error = $"PO '{po}' not found in SAP_PO_MASTER." });

            var lineItems = await _context.MbaPoDetails
                .Where(x => x.PurchaseOrder == po)
                .ToListAsync();

            return Ok(new { header, lineItems });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchByVendor([FromQuery] string? vendorCode)
        {
            var query = _context.SapPoMasters.AsQueryable();
            if (!string.IsNullOrWhiteSpace(vendorCode))
                query = query.Where(x => x.VendorCode == vendorCode);

            var results = await query.Take(20).ToListAsync();
            return Ok(results);
        }
    }
}