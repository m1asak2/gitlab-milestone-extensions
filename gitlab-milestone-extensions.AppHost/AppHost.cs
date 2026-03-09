var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.gitlab_milestone_extensions_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.gitlab_milestone_extensions_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
