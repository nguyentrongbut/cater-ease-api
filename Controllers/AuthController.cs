using cater_ease_api.Data;
using cater_ease_api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace cater_ease_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMongoCollection<AuthModel> _auth;
        
        public AuthController(MongoDbService mongoDbService)
        {
            _auth = mongoDbService.Database?.GetCollection<AuthModel>("auth");
        }

        // [POST] api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(AuthModel user)
        {
            var existing = await _auth.Find(a => a.Email == user.Email).FirstOrDefaultAsync();
            if (existing != null) return Conflict("Email already exists");
            
            user.Status = "active";
            user.Role = "customer";

            await _auth.InsertOneAsync(user);
            return Ok(user);
        }

        // [POST] api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel dto)
        {
            var user = await _auth.Find(a => a.Email == dto.Email && a.Password == dto.Password).FirstOrDefaultAsync();
            
            if (user == null)
                return Unauthorized("Invalid credentials");

            if (user.Status.ToLower() != "active")
                return Unauthorized("Account is inactive or blocked");

            return Ok(user);
        }

        // [GET] api/auth/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _auth.Find(a => a.Id == id).FirstOrDefaultAsync();
            return user == null ? NotFound() : Ok(user);
        }
    }
}