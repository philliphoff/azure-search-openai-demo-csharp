var builder = DistributedApplication.CreateBuilder(args);

var backend =
    builder
        .AddProject<Projects.MinimalApi>("backend")
        .WithAzdEnvironment("AZURE_KEY_VAULT_ENDPOINT")
        .WithDaprSidecar();

builder.Build().Run();
