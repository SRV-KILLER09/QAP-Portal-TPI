using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QAP.Portal.API.Data;
using QAP.Portal.API.Models;

namespace QAP.Portal.API.Controllers
{
    public class UserLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/user")]
    public class QapUserController : ControllerBase
    {
        private readonly QapDbContext _context;

        public QapUserController(QapDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "Email and password are required." });

            var user = await _context.QapUsers
                .FirstOrDefaultAsync(x => x.Email.ToLower() == req.Email.ToLower());

            if (user == null)
                return Unauthorized(new { error = "Invalid credentials." });

            if (user.IsActive == 0)
                return Unauthorized(new { error = "PENDING_APPROVAL" });

            if (user.IsActive != 1)
                return Unauthorized(new { error = "This account is inactive." });

            bool passwordOk = false;
            try
            {
                passwordOk = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            }
            catch
            {
                passwordOk = (req.Password == user.PasswordHash);
            }

            if (!passwordOk)
            {
                passwordOk = (req.Password == user.PasswordHash);
            }

            if (!passwordOk)
                return Unauthorized(new { error = "Invalid credentials." });

            return Ok(new
            {
                email = user.Email,
                displayName = user.DisplayName,
                role = user.Role
            });
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.DisplayName) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "Email, DisplayName, and Password are all required." });

            var existing = await _context.QapUsers
                .FirstOrDefaultAsync(x => x.Email.ToLower() == req.Email.ToLower());

            if (existing != null)
                return Conflict(new { error = "A user with this email already exists." });

            var newUser = new QapUser
            {
                Email = req.Email.Trim(),
                DisplayName = req.DisplayName.Trim(),
                Role = string.IsNullOrWhiteSpace(req.Role) ? "Initiator" : System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(req.Role.Trim().ToLower()),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                IsActive = 0, // 0 = Pending Admin Approval
                CreatedOn = DateTime.Now
            };

            _context.QapUsers.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User '{req.Email}' created successfully." });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingUsers()
        {
            var pending = await _context.QapUsers
                .Where(x => x.IsActive == 0)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();
            return Ok(pending);
        }

        [HttpPost("approve")]
        public async Task<IActionResult> ApproveUser([FromBody] UserApprovalRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { error = "Email is required." });

            var user = await _context.QapUsers
                .FirstOrDefaultAsync(x => x.Email.ToLower() == req.Email.ToLower());

            if (user == null)
                return NotFound(new { error = "User not found." });

            user.IsActive = 1;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"User '{req.Email}' approved successfully." });
        }

        [HttpPost("reject")]
        public async Task<IActionResult> RejectUser([FromBody] UserApprovalRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { error = "Email is required." });

            var user = await _context.QapUsers
                .FirstOrDefaultAsync(x => x.Email.ToLower() == req.Email.ToLower());

            if (user == null)
                return NotFound(new { error = "User not found." });

            _context.QapUsers.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"User '{req.Email}' request rejected and removed." });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { error = "Email, current password, and new password are all required." });

            var user = await _context.QapUsers
                .FirstOrDefaultAsync(x => x.Email.ToLower() == req.Email.ToLower());

            if (user == null)
                return NotFound(new { error = "User not found." });

            bool passwordOk = false;
            try
            {
                passwordOk = BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash);
            }
            catch
            {
                passwordOk = (req.CurrentPassword == user.PasswordHash);
            }

            if (!passwordOk)
            {
                passwordOk = (req.CurrentPassword == user.PasswordHash);
            }

            if (!passwordOk)
                return BadRequest(new { error = "The current password you entered is incorrect." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
        }
    }

    public class ChangePasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserApprovalRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
