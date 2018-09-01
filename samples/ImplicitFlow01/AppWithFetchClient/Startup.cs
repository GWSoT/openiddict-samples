using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppWithFetchClient.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Abstractions;
using AspNet.Security.OpenIdConnect.Primitives;

namespace AppWithFetchClient
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
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection"));

                options.UseOpenIddict();
            });

            services.AddDefaultIdentity<IdentityUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configure Identity to use the same JWT claims as OpenIddict instead
            // of the legacy WS-Federation claims it uses by default (ClaimTypes),
            // which saves you from doing the mapping in your authorization controller.
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddOpenIddict()
                .AddCore(options =>
                {
                    options
                        .UseEntityFrameworkCore()
                        .UseDbContext<ApplicationDbContext>();
                })
                .AddServer(options =>
                {
                    options.UseMvc();

                    options.EnableAuthorizationEndpoint("/connect/authorize");
                    options.EnableTokenEndpoint("/connect/token");

                    options.AddEphemeralSigningKey();

                    options.AllowImplicitFlow();
                })
                .AddValidation();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseFileServer(new FileServerOptions
            {
                RequestPath = "/node_modules",
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "node_modules")),
            });

            app.UseAuthentication();

            app.UseMvc();

            // Seed the database with the sample applications.
            // Note: in a real world application, this step should be part of a setup script.
            InitializeAsync(app.ApplicationServices).GetAwaiter().GetResult();
        }
        private async Task InitializeAsync(IServiceProvider services)
        {
            // Create a new service scope to ensure the database context is correctly disposed when this methods returns.
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync();

                await CreateApplicationsAsync();

                async Task CreateApplicationsAsync()
                {
                    var manager = scope.ServiceProvider.GetRequiredService<OpenIddictApplicationManager<OpenIddictApplication>>();

                    const string clientId = "my-client";

                    if (await manager.FindByClientIdAsync(clientId) == null)
                    {
                        var descriptor = new OpenIddictApplicationDescriptor
                        {
                            ClientId = clientId,
                            DisplayName = "My Client",
                            RedirectUris = { new Uri("https://localhost:5001") },
                            Permissions =
                            {
                                OpenIddictConstants.Permissions.Endpoints.Authorization,
                                OpenIddictConstants.Permissions.GrantTypes.Implicit,
                                OpenIddictConstants.Permissions.Scopes.Profile,
                            }
                        };

                        await manager.CreateAsync(descriptor);
                    }
                }
            }
        }
    }
}
