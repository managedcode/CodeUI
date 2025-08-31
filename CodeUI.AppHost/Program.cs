using Aspire.Hosting;

namespace CodeUI.AppHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var web = builder.AddProject("codeui-web", "../CodeUI.Web/CodeUI.Web.csproj");

        builder.Build().Run();
    }
}
