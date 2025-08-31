var builder = DistributedApplication.CreateBuilder(args);

var web = builder.AddProject<Projects.CodeUI_Web>("codeui-web");

builder.Build().Run();
