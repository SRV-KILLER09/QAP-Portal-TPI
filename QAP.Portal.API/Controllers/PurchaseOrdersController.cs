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

        [HttpGet("all-numbers")]
        public async Task<IActionResult> GetAllPoNumbers()
        {
            var numbers = await _context.SapPoMasters.Select(x => x.PoNumber).ToListAsync();
            return Ok(numbers);
        }

        [HttpGet("db-inspect")]
        public async Task<IActionResult> DbInspect()
        {
            var users = new System.Collections.Generic.List<object>();
            var constraints = new System.Collections.Generic.List<object>();
            using (var conn = _context.Database.GetDbConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM QAP_USERS";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new
                            {
                                Email = reader["EMAIL"]?.ToString(),
                                DisplayName = reader["DISPLAY_NAME"]?.ToString(),
                                Role = reader["ROLE"]?.ToString(),
                                PasswordHash = reader["PASSWORD_HASH"]?.ToString(),
                                IsActive = reader["IS_ACTIVE"]?.ToString(),
                                CreatedOn = reader["CREATED_ON"]?.ToString()
                            });
                        }
                    }
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT constraint_name, constraint_type, 
                               xmlcast(xmlquery('/ROW/SEARCH_CONDITION/text()' passing dbms_xmlgen.getxmltype('select search_condition from user_constraints where constraint_name = ''' || constraint_name || '''') returning content) as varchar2(4000)) as search_condition_str
                        FROM user_constraints 
                        WHERE table_name = 'QAP_USERS'";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            constraints.Add(new
                            {
                                Name = reader.IsDBNull(0) ? null : reader.GetString(0),
                                Type = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Condition = reader.IsDBNull(2) ? null : reader.GetString(2)
                            });
                        }
                    }
                }
            }
            return Ok(new { users, constraints });
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

        [HttpPost]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePoRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.PoNumber) || string.IsNullOrWhiteSpace(req.VendorCode))
                return BadRequest(new { error = "PO Number and Vendor Code are required." });

            var cleanPoNumber = req.PoNumber.Trim().ToUpper();

            // Check if PO already exists
            var exists = await _context.SapPoMasters.FindAsync(cleanPoNumber) != null;
            if (exists)
                return Conflict(new { error = $"Purchase Order '{cleanPoNumber}' already exists." });

            // Create SapPoMaster header
            var master = new Models.SapPoMaster
            {
                PoNumber = cleanPoNumber,
                PoDescription = req.PoDescription?.Trim(),
                VendorCode = req.VendorCode.Trim().ToUpper(),
                PoDate = req.PoDate ?? DateTime.Now,
                PoValue = req.PoValue,
                PlantCode = req.PlantCode?.Trim(),
                ContactPerson = req.ContactPerson?.Trim(),
                Email = req.Email?.Trim(),
                MobileNo = req.MobileNo?.Trim()
            };

            _context.SapPoMasters.Add(master);

            // Create MbaPoDetails line items
            foreach (var item in req.LineItems)
            {
                var details = new Models.MbaPoDetails
                {
                    PurchaseOrder = cleanPoNumber,
                    Item = item.Item,
                    Line = item.Line,
                    LineDescription = item.LineDescription?.Trim(),
                    CreationDate = req.PoDate ?? DateTime.Now,
                    QtyOrdered = item.QtyOrdered,
                    Uom = item.Uom?.Trim() ?? "NOS",
                    QtyDelivered = 0,
                    UnitPrice = item.UnitPrice,
                    VendorCode = req.VendorCode.Trim().ToUpper()
                };
                _context.MbaPoDetails.Add(details);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                var innerEx = dbEx.InnerException ?? dbEx;
                return BadRequest(new { error = $"Database constraint failure: {innerEx.Message}" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = $"An error occurred: {ex.Message}" });
            }

            return CreatedAtAction(nameof(GetPoWithLineItems), new { po = cleanPoNumber }, new { message = "Purchase Order created successfully.", po = cleanPoNumber });
        }
    }

    public class CreatePoRequest
    {
        public string PoNumber { get; set; } = null!;
        public string? PoDescription { get; set; }
        public string VendorCode { get; set; } = null!;
        public DateTime? PoDate { get; set; }
        public decimal? PoValue { get; set; }
        public string? PlantCode { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? MobileNo { get; set; }
        public System.Collections.Generic.List<CreatePoLineItemRequest> LineItems { get; set; } = new();
    }

    public class CreatePoLineItemRequest
    {
        public int Item { get; set; }
        public int Line { get; set; }
        public string? LineDescription { get; set; }
        public decimal? QtyOrdered { get; set; }
        public string? Uom { get; set; }
        public decimal? UnitPrice { get; set; }
    }
}