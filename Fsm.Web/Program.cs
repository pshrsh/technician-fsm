using FSM.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FsmService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // This allows "clientName" and "ClientName" to both work
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

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