using gitlab_milestone_extensions.Web;
using gitlab_milestone_extensions.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddScoped<DashboardApiClient>();
builder.Services.AddScoped<MilestoneSelectionState>();
builder.Services.AddScoped<PrivateTokenStorage>();

await builder.Build().RunAsync();
