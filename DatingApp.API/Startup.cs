using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DatingApp.API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace DatingApp.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // Depending on the environment we are running, either ConfigureDevelopmentService or ConfigureProductionService 
        // will be called automatically by the system
        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
            services.AddDbContext<DataContext>(x => {
                x.UseLazyLoadingProxies();
                x.UseSqlite(Configuration.GetConnectionString("DefaultConnection"));
            });
            ConfigureServices(services);
        }

        public void ConfigureProductionServices(IServiceCollection services)
        {
            services.AddDbContext<DataContext>(x => {
                x.UseLazyLoadingProxies();
                x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });
            ConfigureServices(services);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureIdentityBuilder(ref services);

            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                // by default every user will have to authenticate to every single method
                // we can remove the [Authenticate] attribute from the controllers
                options.Filters.Add(new AuthorizeFilter(policy)); 
            })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });
                
            services.AddCors();
            services.Configure<CloudinarySettings>(Configuration.GetSection("CloudinarySettings"));
            services.AddAutoMapper(typeof(Startup));
            
            //services.AddSingleton(); It creates a single instance of the repository => issues for concurrent requests
            //services.AddTransient(); It creates a new instance on every request of the repository => great for lightweight services
            //services.AddScoped(); It creates an instance per each http request but it uses the same instance in other calls within same web request
            services.AddScoped<IDatingRepository, DatingRepository>();
            services.AddScoped<LogUserActivity>();
            /* Class not used with IdentityUser
            services.AddScoped<IAuthRepository, AuthRepository>();*/
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder => {
                    builder.Run(async context => {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        if (error != null)
                        {
                            context.Response.AddApplicationError(error.Error.Message);
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // app.UseHttpsRedirection();
            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            app.UseAuthentication();
            app.UseDefaultFiles(); // will make the API app to search for file Index.html
            app.UseStaticFiles();  // it automatically look into the folder named wwwroot
            app.UseMvc(routes => 
            {
                // if the API app doesn't find a route then it will use this FallbackController,
                // call Index() method where is sent in wwwroot folder in the Index.Html file
                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new {controller = "Fallback", action = "Index"}
                );
            });
        }

        private void ConfigureIdentityBuilder(ref IServiceCollection services)
        {
            // We chose IdentityCore so we can configure it; Identity uses by default cookies and we want to use JWT token
            IdentityBuilder builder = services.AddIdentityCore<User>();
            builder = new IdentityBuilder(builder.UserType, typeof(Role), builder.Services);
            builder.AddEntityFrameworkStores<DataContext>(); 
            builder.AddRoleValidator<RoleValidator<Role>>();
            builder.AddRoleManager<RoleManager<Role>>();
            builder.AddSignInManager<SignInManager<User>>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                            .GetBytes(Configuration.GetSection("AppSettings:Token").Value)),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                options.AddPolicy("ModeratePhoto", policy => policy.RequireRole("Admin", "Moderator"));
                options.AddPolicy("VipOnly", policy => policy.RequireRole("VIP"));

            });
        }
    }
}
