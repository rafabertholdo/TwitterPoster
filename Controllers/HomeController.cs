using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using TwitterPoster.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;

[Authorize]
public class HomeController : Controller {
        
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IConfigurationRoot _config;   
    
    private HttpContext _context;
        
    public HomeController(IHttpContextAccessor contextAccessor,
    IConfigurationRoot config)
    {
        _contextAccessor = contextAccessor;
        _config = config;        
    }    

    [HttpPost]
    public async Task<IActionResult> Twita(TwitterViewModel model)
    {
        if(Context.User.Identity != null && Context.User.Identity.IsAuthenticated)
        {
            var oauthToken = Context.User.Claims.Where(e => e.Type == "urn:twitter:access_token").FirstOrDefault();
            var oauthTokenSecret = Context.User.Claims.Where(e => e.Type == "urn:twitter:access_token_secret").FirstOrDefault();

            HttpClient httpClient = new HttpClient();
            var url = "https://api.twitter.com/1.1/statuses/update.json";
            
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url);

            var token = new OauthTwitterToken(url, model.Message);
            token.ConsumerKey = _config["ConsumerKey"];
            token.ConsumerSecret = _config["ConsumerSecret"];
            token.Token = oauthToken.Value;            
            token.TokenSecret = oauthTokenSecret.Value;

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("OAuth", token.ToString());
            requestMessage.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            requestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("MyProduct", "1.0")));
            requestMessage.Content = new StringContent(string.Format("status={0}&include_entities=1&include_rts=1", model.Message),
                        Encoding.UTF8,
                        "application/x-www-form-urlencoded");           
            
            // Send the request to the server
            HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

            // Just as an example I'm turning the response into a string here
            string responseAsString = await response.Content.ReadAsStringAsync();                        
            
            ViewBag.Message = responseAsString;
        }
        return View("Index");
    }

    [HttpGet]    
    public IActionResult Index() {
        return View();
    }
        
    protected internal HttpContext Context
    {
        get
        {
            var context = _context ?? _contextAccessor?.HttpContext;
            if (context == null)
            {
                throw new InvalidOperationException("HttpContext must not be null.");
            }
            return context;
        }
        set
        {
            _context = value;
        }
    }

    //
    // POST: /Account/ExternalLogin
    [HttpGet]
    [AllowAnonymous]    
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
        // Request a redirect to the external login provider.
        var redirectUrl = Url.Action("ExternalLoginCallback", "Home", new { ReturnUrl = returnUrl });
        
        var loginProviders = GetExternalAuthenticationSchemes().ToList();
        var properties = ConfigureExternalAuthenticationProperties(loginProviders.FirstOrDefault().AuthenticationScheme, redirectUrl);
        return Challenge(properties, "Twitter");
    }


    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Caralho(string returnUrl = null, string remoteError = null)
    {
        const string Issuer = "https://gov.uk";

        var claims = new List<Claim> {
            new Claim(ClaimTypes.Name, "Andrew", ClaimValueTypes.String, Issuer),
            new Claim(ClaimTypes.Surname, "Lock", ClaimValueTypes.String, Issuer),
            new Claim(ClaimTypes.Country, "UK", ClaimValueTypes.String, Issuer),
            new Claim("ChildhoodHero", "Ronnie James Dio", ClaimValueTypes.String)
        };

        var userIdentity = new ClaimsIdentity(claims, "Passport");

        var userPrincipal = new ClaimsPrincipal(userIdentity);


        await Context.Authentication.SignInAsync("Cookie", userPrincipal,
            new AuthenticationProperties
            {
                ExpiresUtc = DateTime.UtcNow.AddMinutes(20),
                IsPersistent = false,
                AllowRefresh = false
            });       

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
    {
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
            return View(nameof(Index));
        }

        var auth = new AuthenticateContext("Twitter");
        await Context.Authentication.AuthenticateAsync(auth);        
        if (auth == null)
        {
            return RedirectToAction(nameof(Index));
        }               


        await Context.Authentication.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, auth.Principal,
            new AuthenticationProperties
            {
                ExpiresUtc = DateTime.UtcNow.AddMinutes(20),
                IsPersistent = false,
                AllowRefresh = false
            });        

        return RedirectToAction(nameof(Index));        
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    
    public IEnumerable<AuthenticationDescription> GetExternalAuthenticationSchemes()
    {
        return Context.Authentication.GetAuthenticationSchemes().Where(d => !string.IsNullOrEmpty(d.DisplayName));
    }

    public AuthenticationProperties ConfigureExternalAuthenticationProperties(string provider, string redirectUrl, string userId = null)
    {
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        properties.Items["LoginProvider"] = provider;
        if (userId != null)
        {
            properties.Items["XsrfId"] = userId;
        }
        return properties;
    }    
}