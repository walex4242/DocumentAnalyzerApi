using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5223/") });

builder.Services.AddOptions<HttpClientHandler>()
    .Configure(handler => handler.MaxRequestContentBufferSize = 10 * 1024 * 1024);


await builder.Build().RunAsync();
