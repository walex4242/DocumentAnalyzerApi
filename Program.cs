var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
          .AddXmlSerializerFormatters();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers(); // This enables routing to your DocumentController

app.Run();
