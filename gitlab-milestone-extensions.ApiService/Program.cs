using gitlab_milestone_extensions.ApiService.Endpoints;
using gitlab_milestone_extensions.ApiService.Options;
using gitlab_milestone_extensions.ApiService.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GitLabExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.Configure<GitLabOptions>(builder.Configuration.GetSection("GitLab"));
builder.Services.AddHttpClient<GitLabApiClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<IGitLabDataSnapshotService, CachedGitLabDataSnapshotService>();
builder.Services.AddScoped<IDashboardDataService, GitLabDashboardDataService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDashboardEndpoints();
app.MapGitLabEndpoints();
app.MapDefaultEndpoints();

app.Run();
