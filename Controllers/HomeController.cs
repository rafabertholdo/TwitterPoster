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
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;

[Authorize]
public class HomeController : Controller {
        
    private readonly IHttpContextAccessor _contextAccessor;
    /*
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsFactory;
    */
    
    private HttpContext _context;
        
    public HomeController(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;        
    }    

    [HttpGet]    
    public async Task<IActionResult> Index() {
        if(Context.User.Identity != null && Context.User.Identity.IsAuthenticated)
        {
            var oauthToken = Context.User.Claims.Where(e => e.Type == "urn:twitter:access_token").FirstOrDefault();
            var oauthTokenSecret = Context.User.Claims.Where(e => e.Type == "urn:twitter:access_token_secret").FirstOrDefault();


            ////karaaaaaaaaaaaaaaaaaaaalho
            HttpClient httpClient = new HttpClient();
            var url = "https://api.twitter.com/1.1/statuses/update.json";
            
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url);

            string message = "teste de mensagem";
            var token = new OauthTwitterToken(url, message);
            token.ConsumerKey = "RIRdczCzAhZuDRhI4rvxJ67hJ";
            token.Token = oauthToken.Value;
            token.ConsumerSecret = "vrVdsGjA1V3ctTD93evjh9UFr3Zci7hsJMKykhYK4uxF5yqfMp";
            token.TokenSecret = oauthTokenSecret.Value;

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("OAuth", token.ToString());
            requestMessage.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            requestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("MyProduct", "1.0")));
            requestMessage.Content = new StringContent(string.Format("status={0}&include_entities=1&include_rts=1", message),
                        Encoding.UTF8,
                        "application/x-www-form-urlencoded");           
            
            // Send the request to the server
            HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

            // Just as an example I'm turning the response into a string here
            string responseAsString = await response.Content.ReadAsStringAsync();                        
            
            ViewBag.Message = responseAsString;
        }
        
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