using System.Security.Claims;
using System.Text;
using cater_ease_api.Data;
using cater_ease_api.Dtos.Service;
using cater_ease_api.Filters;
using cater_ease_api.Models;
using cater_ease_api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Cần để generate swagger docs
builder.Services.AddEndpointsApiExplorer();    
// Thêm SwaggerGen
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Cater Ease API", Version = "v1" });

    // Định nghĩa scheme JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "Nhập token dạng: Bearer {token}"
    });

    // Đăng ký filter để chỉ áp dụng cho các route có [Authorize]
    c.OperationFilter<AuthorizeCheckOperationFilter>();
});               

// connect momo api
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddScoped<IMomoService, MomoService>();

builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<CloudinaryService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<EmailService>();



//jwt
var jwtKey = Environment.GetEnvironmentVariable("JWT__KEY");
var keyBytes = Encoding.UTF8.GetBytes(jwtKey ?? throw new Exception("JWT__KEY is missing"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            
            RoleClaimType = ClaimTypes.Role,    
            NameClaimType = "userId",            
        };
    });

builder.Services.AddAuthorization(); // Cho phép dùng [Authorize]



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Dùng Swagger middleware
    app.UseSwagger();
    // Dùng Swagger UI
    app.UseSwaggerUI();                              
}

app.UseHttpsRedirection();

// Xác thực JWT
app.UseAuthentication(); 

// Phân quyền theo [Authorize]
app.UseAuthorization();   
// Map controller routes
app.MapControllers();                                

app.Run();