using CodeUI.Web.Components;
using CodeUI.Core.Data;
using CodeUI.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeUI.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Configure SQLite database
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=codeui.db";
            options.UseSqlite(connectionString);
        });

        // Configure Identity with settings from configuration
        builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
        {
            var authConfig = builder.Configuration.GetSection("Authentication");
            options.SignIn.RequireConfirmedAccount = authConfig.GetValue<bool>("RequireConfirmedAccount", false);
            
            var passwordConfig = authConfig.GetSection("Password");
            options.Password.RequireDigit = passwordConfig.GetValue<bool>("RequireDigit", false);
            options.Password.RequiredLength = passwordConfig.GetValue<int>("RequiredLength", 6);
            options.Password.RequireNonAlphanumeric = passwordConfig.GetValue<bool>("RequireNonAlphanumeric", false);
            options.Password.RequireUppercase = passwordConfig.GetValue<bool>("RequireUppercase", false);
            options.Password.RequireLowercase = passwordConfig.GetValue<bool>("RequireLowercase", false);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>();

        // Register CLI execution services
        builder.Services.AddScoped<ICliExecutor, CliExecutor>();

        var app = builder.Build();

        // Ensure SQLite database is created (skip for testing environment)
        if (!app.Environment.IsEnvironment("Testing"))
        {
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Ensure directory exists for SQLite database
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=codeui.db";
                if (connectionString.Contains("Data Source=") && (connectionString.Contains("/") || connectionString.Contains("\\")))
                {
                    var dbPath = connectionString.Replace("Data Source=", "").Split(';')[0];
                    var directory = Path.GetDirectoryName(dbPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                
                context.Database.EnsureCreated();
            }
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapRazorPages();

        app.Run();
    }
}
