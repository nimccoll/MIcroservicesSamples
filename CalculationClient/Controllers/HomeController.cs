using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CalculationClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CalculationClient.Services;
using CalcModels;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Routing;

namespace CalculationClient.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration _configuration = null;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IConfiguration configuration, ILogger<HomeController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize]
        [HttpGet]
        public IActionResult CallAPI()
        {
            ViewBag.Calc1Input1 = string.Empty;
            ViewBag.Calc1Input2 = string.Empty;
            ViewBag.Calc1Result = string.Empty;
            ViewBag.Calc2Input1 = string.Empty;
            ViewBag.Calc2Input2 = string.Empty;
            ViewBag.Calc2Result = string.Empty;
            return View();
        }

        [Authorize]
        [HttpPost]
        [ActionName("CallAPI")]
        public async Task<ActionResult> CallAPIPost()
        {
            ViewBag.Calc1Input1 = string.Empty;
            ViewBag.Calc1Input2 = string.Empty;
            ViewBag.Calc1Result = string.Empty;
            ViewBag.Calc2Input1 = string.Empty;
            ViewBag.Calc2Input2 = string.Empty;
            ViewBag.Calc2Result = string.Empty;

            if (!string.IsNullOrEmpty(Request.Form["btnFirstCalculation"]))
            {
                string signedInUserID = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                string authority = $"{_configuration.GetValue<string>("AzureADB2C:Instance")}/tfp/{_configuration.GetValue<string>("AzureADB2C:Domain")}/{_configuration.GetValue<string>("AzureADB2C:SignUpSignInPolicyId")}";

                // Build the redirect Uri from the current HttpContext
                // Ex. https://localhost:44340/signin-oidc
                string redirectUri = UriHelper.BuildAbsolute(this.Request.Scheme, this.Request.Host, this.Request.PathBase, this.Request.Path);

                // Create an instance of the ConfidentialClientApplication to retrieve the access token
                // via the authorization code using the authority, redirectUri, and the
                // client ID and client secret of the web application (from configuration)
                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder.Create(_configuration.GetValue<string>("AzureADB2C:ClientId"))
                .WithB2CAuthority(authority)
                .WithRedirectUri(redirectUri)
                .WithClientSecret(_configuration.GetValue<string>("AzureADB2C:ClientSecret"))
                .Build();

                // Construct a session-backed token cache based on the signed in User ID and the current HttpContext and attach it to the UserTokenCache of the ConfidentialClientApplication
                ITokenCache userTokenCache = new MSALSessionCache(signedInUserID, this.HttpContext, cca.UserTokenCache).GetMsalCacheInstance();

                // Retrieve the list of signed in accounts from the cache
                List<IAccount> accounts = (List<IAccount>)await cca.GetAccountsAsync();

                if (accounts.Count > 0)
                {
                    // Retrieve the access token for the API from the cache
                    AuthenticationResult authenticationResult = await cca.AcquireTokenSilent(_configuration.GetValue<string>("AzureADB2C:ApiScopes").Split(' '), accounts[0]).ExecuteAsync();

                    InputModel model = new InputModel();
                    if (Request.Form["calc1Input1"].ToString().IndexOf(".") > 0
                        || Request.Form["calc1Input2"].ToString().IndexOf(".") > 0)
                    {
                        model.DecimalInput = new decimal[] { Convert.ToDecimal(Request.Form["calc1Input1"]), Convert.ToDecimal(Request.Form["calc1Input2"]) };
                        model.StringInput = new string[] { "Add", "Decimal" };
                    }
                    else
                    {
                        model.IntegerInput = new int[] { Convert.ToInt32(Request.Form["calc1Input1"]), Convert.ToInt32(Request.Form["calc1Input2"]) };
                        model.StringInput = new string[] { "Add", "Integer" };
                    }
                    HttpClient httpClient = new HttpClient();
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _configuration.GetValue<string>("AzureADB2C:ApiUrl")))
                    {
                        // Set the authorization header
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);
                        string content = JsonConvert.SerializeObject(model);
                        request.Content = new StringContent(content, Encoding.UTF8, "application/json");

                        // Send the request to Graph API endpoint
                        using (HttpResponseMessage response = await httpClient.SendAsync(request))
                        {
                            string error = await response.Content.ReadAsStringAsync();

                            // Check the result for error
                            if (!response.IsSuccessStatusCode)
                            {
                                // Throw server busy error message
                                if (response.StatusCode == (HttpStatusCode)429)
                                {
                                    // TBD: Add you error handling here
                                }

                                throw new Exception(error);
                            }

                            // Return the response body, usually in JSON format
                            OutputModel outputModel = JsonConvert.DeserializeObject<OutputModel>(await response.Content.ReadAsStringAsync());
                            if (model.StringInput[1] == "Integer")
                            {
                                ViewBag.Calc1Input1 = model.IntegerInput[0];
                                ViewBag.Calc1Input2 = model.IntegerInput[1];
                                ViewBag.Calc1Result = outputModel.IntegerOutput[0];
                            }
                            else
                            {
                                ViewBag.Calc1Input1 = model.DecimalInput[0];
                                ViewBag.Calc1Input2 = model.DecimalInput[1];
                                ViewBag.Calc1Result = outputModel.DecimalOutput[0];
                            }
                        }
                    }
                }
            }

            return View();
        }
    }
}
