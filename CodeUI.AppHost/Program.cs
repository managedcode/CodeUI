using Aspire.Hosting;
using System.IO;

namespace CodeUI.AppHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        
        var web = builder.AddProject<Projects.CodeUI_Web>("codeui-web");

        builder.Build().Run();
    }
}
