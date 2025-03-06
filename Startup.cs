using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Inventory
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // Add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add cookie-based authentication
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login/Index";  // Set path to your login page
                    options.LogoutPath = "/Login/Logout"; // Optional: Set logout path
                    options.SlidingExpiration = true; // Optional: Enables sliding expiration for sessions
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Optional: Set session timeout duration
                });

            // Add MVC controllers and views
            services.AddControllersWithViews();
        }

        // Configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Redirect HTTP to HTTPS
            app.UseHttpsRedirection();

            // Serve static files from "wwwroot" (default location)
            app.UseStaticFiles();

            // Optional: Serve additional static files from a custom location
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
                RequestPath = ""
            });

            // Enable routing
            app.UseRouting();

            // Enable authentication (cookie-based)
            app.UseAuthentication();  // Add this line for cookie authentication

            // Enable authorization (if required)
            app.UseAuthorization(); 
            // You can customize this for role-based authorization, etc.

            // Configure endpoints for MVC controllers
            app.UseEndpoints(endpoints =>
            {
                // Default route for login page
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Distribution}/{action=ViewShipment}/{id?}");

                // Route for inventory-related actions
               
            });
        }
    }
}
