using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TwitterPoster
{

    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment env){
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();    
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);           
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<IConfigurationRoot>(Configuration);         
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
                 
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles();
                  
            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {               
                LoginPath = new PathString("/Home/ExternalLogin/"),                
                AutomaticAuthenticate = true,
                AutomaticChallenge = true
            });            
            
            app.UseTwitterAuthentication(new TwitterOptions
            {
                ConsumerKey = Configuration["ConsumerKey"],
                ConsumerSecret = Configuration["ConsumerSecret"],                
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
