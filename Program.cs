using cater_ease_api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Cần để generate swagger docs
builder.Services.AddEndpointsApiExplorer();    
// Thêm SwaggerGen
builder.Services.AddSwaggerGen();                   
builder.Services.AddSingleton<MongoDbService>();

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

// Map controller routes
app.MapControllers();                                

app.Run();