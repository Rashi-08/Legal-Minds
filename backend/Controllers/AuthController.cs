using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using LegalMinds.Backend.Database;
using LegalMinds.Backend.Models;

namespace LegalMinds.Backend.Controllers
{
    [ApiController]
    [Route("")]
    public class AuthController : ControllerBase
    {
        private readonly LegalMindsDbContext _context;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<string> _hasher;

        public AuthController(LegalMindsDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _hasher = new PasswordHasher<string>();
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] UserCreate model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (existing)
                return BadRequest(new { detail = "Email already registered" });

            if (model.Password.Length > 72)
                return BadRequest(new { detail = "Password too long (max 72 characters)" });

            var newUser = new User
            {
                Email = model.Email,
                Role = model.Role.ToLower()
            };
            newUser.PasswordHash = _hasher.HashPassword(newUser.Email, model.Password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User created successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (user == null)
                return BadRequest(new { detail = "Invalid email" });

            var verifyResult = _hasher.VerifyHashedPassword(user.Email, user.PasswordHash, model.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
                return BadRequest(new { detail = "Invalid password" });

            // Generate JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _config["Jwt:Secret"] ?? "SuperSecretKeyMustBeAtLeast32BytesLong!";
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                access_token = tokenString,
                role = user.role()
            });
        }

        [Authorize]
        [HttpGet("protected")]
        public IActionResult ProtectedRoute()
        {
            var email = User.Identity?.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

            return Ok(new
            {
                message = "You are authenticated",
                user = new
                {
                    email = email,
                    role = role
                }
            });
        }
    }

    public static class UserExtensions
    {
        public static string role(this User user)
        {
            // Frontend expects specific casing or role format
            return user.Role;
        }
    }
}
