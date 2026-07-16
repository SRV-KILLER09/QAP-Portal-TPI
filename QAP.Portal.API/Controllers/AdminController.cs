using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QAP.Portal.API.Data;
using QAP.Portal.API.Models;

namespace QAP.Portal.API.Controllers
{
    public class AdminLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CreateAdminRequest
    {
        public string AdminId { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // TEMPORARY - only needed once to fix pre-existing plaintext passwords.
    // Safe to delete this class + the SeedHash endpoint below once your
    // ADMIN_USERS table only contains rows created via /api/admin/create.
    public class SeedHashRequest
    {
        public string Email { get; set; } = string.Empty;
        public string PlainPassword { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly QapDbContext _context;

        public AdminController(QapDbContext context)
        {
            _context = context;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "Email and password are required." });

            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(x => x.EMAIL.ToLower() == req.Email.ToLower());

            if (admin == null)
                return Unauthorized(new { error = "Invalid credentials." });

            if (admin.STATUS != "A")
                return Unauthorized(new { error = "This account is inactive." });

            bool passwordOk = false;
            try
            {
                passwordOk = BCrypt.Net.BCrypt.Verify(req.Password, admin.PASSWORD_HASH);
            }
            catch
            {
                // Fallback to checking plaintext equality if the DB value is not a valid bcrypt hash
                passwordOk = (req.Password == admin.PASSWORD_HASH);
            }

            // If bcrypt verification returned false, also check if it's plaintext
            if (!passwordOk)
            {
                passwordOk = (req.Password == admin.PASSWORD_HASH);
            }

            if (!passwordOk)
                return Unauthorized(new { error = "Invalid credentials." });

            return Ok(new
            {
                adminId = admin.ADMIN_ID,
                adminName = admin.ADMIN_NAME,
                email = admin.EMAIL
            });
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.AdminId) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "AdminId, Email, and Password are all required." });

            var existing = await _context.AdminUsers
                .FirstOrDefaultAsync(x => x.ADMIN_ID == req.AdminId || x.EMAIL.ToLower() == req.Email.ToLower());

            if (existing != null)
                return Conflict(new { error = "An admin with this ID or email already exists." });

            var newAdmin = new AdminUser
            {
                ADMIN_ID = req.AdminId,
                ADMIN_NAME = req.AdminName,
                EMAIL = req.Email,
                PASSWORD_HASH = BCrypt.Net.BCrypt.HashPassword(req.Password),
                STATUS = "A",
                CREATED_ON = DateTime.Now
            };

            _context.AdminUsers.Add(newAdmin);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Admin '{req.Email}' created successfully.", adminId = newAdmin.ADMIN_ID });
        }

        // ------------------------------------------------------------------
        // TEMPORARY - run once per pre-existing plaintext-password user, then delete.
        // ------------------------------------------------------------------
        [HttpPost("seed-hash")]
        public async Task<IActionResult> SeedHash([FromBody] SeedHashRequest req)
        {
            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(x => x.EMAIL.ToLower() == req.Email.ToLower());

            if (admin == null)
                return NotFound(new { error = $"No admin found with email '{req.Email}'." });

            admin.PASSWORD_HASH = BCrypt.Net.BCrypt.HashPassword(req.PlainPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Password hash updated for {req.Email}." });
        }
    }
}