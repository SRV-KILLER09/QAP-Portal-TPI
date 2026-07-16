using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QAP.Portal.API.Data;
using QAP.Portal.API.Models;

namespace QAP.Portal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PoDocumentsController : ControllerBase
    {
        private readonly QapDbContext _context;

        public PoDocumentsController(QapDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var docs = await _context.PoDocuments.ToListAsync();
            return Ok(docs);
        }

        [HttpGet("{po}")]
        public async Task<IActionResult> GetById(string po)
        {
            var doc = await _context.PoDocuments.FindAsync(po);
            if (doc == null) return NotFound();
            return Ok(doc);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PoDocument doc)
        {
            _context.PoDocuments.Add(doc);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { po = doc.Po }, doc);
        }

        [HttpPut("{po}")]
        public async Task<IActionResult> Update(string po, [FromBody] PoDocument updated)
        {
            var existing = await _context.PoDocuments.FindAsync(po);
            if (existing == null) return NotFound();

            existing.TechSpec = updated.TechSpec;
            existing.PoCopy = updated.PoCopy;
            existing.UpdatedOn = updated.UpdatedOn;
            existing.UpdatedBy = updated.UpdatedBy;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{po}")]
        public async Task<IActionResult> Delete(string po)
        {
            var existing = await _context.PoDocuments.FindAsync(po);
            if (existing == null) return NotFound();

            _context.PoDocuments.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{po}/upload-tech-spec")]
        public async Task<IActionResult> UploadTechSpec(string po, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            var doc = await _context.PoDocuments.FindAsync(po);
            if (doc == null) return NotFound(new { error = $"No PO_DOCUMENTS record found for PO '{po}'." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            doc.TechSpec = ms.ToArray();
            doc.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { po, fileName = file.FileName, sizeBytes = file.Length, message = "Technical spec uploaded successfully." });
        }

        [HttpPost("{po}/upload-po-copy")]
        public async Task<IActionResult> UploadPoCopy(string po, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            var doc = await _context.PoDocuments.FindAsync(po);
            if (doc == null) return NotFound(new { error = $"No PO_DOCUMENTS record found for PO '{po}'." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            doc.PoCopy = ms.ToArray();
            doc.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { po, fileName = file.FileName, sizeBytes = file.Length, message = "PO copy uploaded successfully." });
        }
    }
}