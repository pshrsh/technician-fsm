using FSM.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
// This tells the app to look for [ApiController] classes (like yours)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // This allows "clientName" and "ClientName" to both work
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
// 2. Add Swagger/OpenAPI support
// This generates the documentation page you are trying to see
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// FIX: Enable serving HTML/CSS/JS files
app.UseStaticFiles(); 

app.UseAuthorization();
app.MapControllers();

// FIX: If the user visits the root URL, show the dashboard
app.MapFallbackToFile("index.html"); 

app.Run();