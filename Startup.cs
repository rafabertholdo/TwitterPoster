using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TwitterPoster
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);           
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();            
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
                        
            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {               
                LoginPath = new PathString("/Home/ExternalLogin/"),                
                AutomaticAuthenticate = true,
                AutomaticChallenge = true
            });            
            
            app.UseTwitterAuthentication(new TwitterOptions
            {
                ConsumerKey = "RIRdczCzAhZuDRhI4rvxJ67hJ",
                ConsumerSecret = "vrVdsGjA1V3ctTD93evjh9UFr3Zci7hsJMKykhYK4uxF5yqfMp",                
                Events = new TwitterEvents
                {
                    OnCreatingTicket = async context => {
                        ((ClaimsIdentity)context.Principal.Identity).AddClaim(new Claim("urn:twitter:access_token", context.AccessToken));
                        ((ClaimsIdentity)context.Principal.Identity).AddClaim(new Claim("urn:twitter:access_token_secret", context.AccessTokenSecret));
                        await Task.FromResult(0);
                    }
                }
            });

            app.UseMvcWithDefaultRoute();
        }
    }
}
