using cater_ease_api.Data;
using cater_ease_api.Dtos.Auth;
using cater_ease_api.Models;
using cater_ease_api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMongoCollection<AuthModel> _auth;
        private readonly CloudinaryService _cloudinary;
        private readonly JwtService _jwtService;
        
        public AuthController(MongoDbService mongoDbService, CloudinaryService cloudinary, JwtService jwtService)
        {
            _auth = mongoDbService.Database?.GetCollection<AuthModel>("auth");
            _cloudinary = cloudinary;
            _jwtService = jwtService;
        }

        // [POST] api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existing = await _auth.Find(a => a.Email == dto.Email && !a.Deleted).FirstOrDefaultAsync();
            if (existing != null) return Conflict("Email already exists");
            
            var user = new AuthModel
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone,
                Address = dto.Address,
                Status = "active",
                Role = "customer",
                Deleted = false
            };

            await _auth.InsertOneAsync(user);
            return Ok(user);
        }

        // [POST] api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _auth.Find(a => a.Email == dto.Email && !a.Deleted).FirstOrDefaultAsync();
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                return Unauthorized("Invalid credentials");

            if (user.Status.ToLower() != "active")
                return Unauthorized("Account is inactive or blocked");

            var token = _jwtService.GenerateToken(user.Id!, user.Role);
            return Ok(new
            {
                token,
                user = new {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role
                }
            });
        }

        // [GET] api/auth/:id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _auth.Find(a => a.Id == id && !a.Deleted).FirstOrDefaultAsync();
            return user == null ? NotFound() : Ok(user);
        }
        
        // [PATCH] api/auth/:id
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateProfile(string id, [FromForm] UpdateProfileDto dto)
        {
            var user = await _auth.Find(u => u.Id == id && !u.Deleted).FirstOrDefaultAsync();
            if (user == null) return NotFound("User not found.");

            var updateDefs = new List<UpdateDefinition<AuthModel>>();

            if (!string.IsNullOrEmpty(dto.Name))
                updateDefs.Add(Builders<AuthModel>.Update.Set(u => u.Name, dto.Name));

            if (!string.IsNullOrEmpty(dto.Phone))
                updateDefs.Add(Builders<AuthModel>.Update.Set(u => u.Phone, dto.Phone));

            if (!string.IsNullOrEmpty(dto.Address))
                updateDefs.Add(Builders<AuthModel>.Update.Set(u => u.Address, dto.Address));

            if (!string.IsNullOrEmpty(dto.Password))
                updateDefs.Add(Builders<AuthModel>.Update.Set(u => u.Password, BCrypt.Net.BCrypt.HashPassword(dto.Password)));

            if (dto.Avatar != null)
            {
                if (!string.IsNullOrEmpty(user.Avatar))
                {
                    await _cloudinary.DeleteAsync(user.Avatar);
                }

                var avatarUrl = await _cloudinary.UploadAsync(dto.Avatar);
                updateDefs.Add(Builders<AuthModel>.Update.Set(u => u.Avatar, avatarUrl));
            }

            if (!updateDefs.Any())
                return BadRequest("No valid fields to update.");

            var update = Builders<AuthModel>.Update.Combine(updateDefs);
            var result = await _auth.UpdateOneAsync(u => u.Id == id, update);

            return result.ModifiedCount == 0 ? NotFound() : Ok("Profile updated successfully.");
        }

        // [DELETE] api/auth/:id
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(string id)
        {
            var result = await _auth.UpdateOneAsync(
                u => u.Id == id && !u.Deleted,
                Builders<AuthModel>.Update.Set(u => u.Deleted, true)
            );

            return result.ModifiedCount == 0 ? NotFound("User not found or already deleted") : Ok("User deleted (soft)");
        }
    }
}
