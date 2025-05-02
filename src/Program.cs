var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddXmlSerializerFormatters();
builder.Services.AddEndpointsApiExplorer();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", // Give your policy a name
        builder =>
        {
            builder.WithOrigins("http://localhost:5230") // Replace with the actual origin of your Blazor app
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var app = builder.Build();

// Enable CORS middleware
app.UseCors("AllowBlazorClient");

app.UseAuthorization();

app.MapControllers(); // This enables routing to your DocumentController

app.Run();