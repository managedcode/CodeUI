using Aspire.Hosting;
using System.IO;

namespace CodeUI.AppHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Get the solution directory to build an absolute path
        var currentDir = Directory.GetCurrentDirectory();
        string projectPath;
        
        // Navigate up to find the solution root
        var solutionDir = currentDir;
        while (solutionDir != null && !File.Exists(Path.Combine(solutionDir, "CodeUI.sln")))
        {
            solutionDir = Directory.GetParent(solutionDir)?.FullName;
        }
        
        if (solutionDir != null)
        {
            projectPath = Path.Combine(solutionDir, "CodeUI.Web", "CodeUI.Web.csproj");
        }
        else
        {
            // Fallback to relative path
            projectPath = "../CodeUI.Web/CodeUI.Web.csproj";
        }

        var web = builder.AddProject("codeui-web", projectPath);

        builder.Build().Run();
    }
}
