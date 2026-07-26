using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using DataLayer;
using Inventory.Infrastructure;
using Inventory.Security;

namespace Inventory
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("CompanyDB");
            if (!string.IsNullOrWhiteSpace(connectionString))
                DbConfig.Initialize(connectionString);

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login/Login";
                    options.LogoutPath = "/Login/Logout";
                    options.AccessDeniedPath = "/Login/Login";
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                });

            services.AddControllersWithViews(options =>
            {
                options.Filters.Add<ModuleAuthorizationFilter>();
                options.Filters.Add<RequiredDetailWarningFilter>();
            });

            services.AddScoped<Inventory.Services.IApprovalWorkflowService, Inventory.Services.ApprovalWorkflowService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Correlation ID must wrap everything so logs and responses share one ID.
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
                RequestPath = ""
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Login}/{action=Login}/{id?}");
            });
        }
    }
}
