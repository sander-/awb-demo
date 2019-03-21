using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HelloBlockchainWebApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace HelloBlockchainWebApp.Controllers
{
	[Authorize]
	public class HomeController : Controller
	{
		public async Task<IActionResult> Index(string q)
		{
			AuthenticationResult result = null;

			try
			{
				// Because we signed-in already in the WebApp, the userObjectId is know
				string userObjectID = (User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;

				// Using ADAL.Net, get a bearer token to access the Blockchain Workbench API
				AuthenticationContext authContext = new AuthenticationContext(AzureAdOptions.Settings.Authority, 
					new NaiveSessionCache(userObjectID, HttpContext.Session));
				ClientCredential credential = new ClientCredential(AzureAdOptions.Settings.ClientId, AzureAdOptions.Settings.ClientSecret);
				result = await authContext.AcquireTokenSilentAsync(AzureAdOptions.Settings.WorkbenchResourceId, credential, 
					new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
				
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

				if (q == "applications")
				{
					var response = await client.GetAsync($"{AzureAdOptions.Settings.WorkbenchBaseAddress}/api/v2/applications");
					ViewBag.Response = await response.Content.ReadAsStringAsync();
				}

				if (q == "workflows")
				{
					var response = await client.GetAsync($"{AzureAdOptions.Settings.WorkbenchBaseAddress}/api/v2/applications/1/workflows");
					ViewBag.Response = await response.Content.ReadAsStringAsync();
				}

				if (q == "newworkflow")
				{
					var message = "Hello at " + DateTime.Now.ToLongTimeString();
					await StartNewWorkFlow(client, message);
				}

				if (q == "respond")
				{
					var message = "Hello back at " + DateTime.Now.ToLongTimeString();
					await RespondToWorkflow(client, message);
				}

				return View();
			}
			catch (Exception)
			{
				if (HttpContext.Request.Query["reauth"] == "True")
				{
					//
					// Send an OpenID Connect sign-in request to get a new set of tokens.
					// If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
					// The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
					//
					return new ChallengeResult(OpenIdConnectDefaults.AuthenticationScheme);
				}
				ViewBag.ErrorMessage = "AuthorizationRequired";
			}
			return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

		private async Task StartNewWorkFlow(HttpClient client, string message)
		{
			int workflowId = 1;
			int contractCodeId = 1;
			int connectionId = 1;
			string json = $@"{{
				'workflowFunctionID': 1,
				'workflowActionParameters': [
					{{
						'name': 'message',
						'value': '{message}',
						'workflowFunctionParameterId': 1

					}}
				]
			}}";

			try
			{
				var response = await client.PostAsync($"{AzureAdOptions.Settings.WorkbenchBaseAddress}/api/v2/contracts?workflowId={workflowId}&contractCodeId={contractCodeId}&connectionId={connectionId}",
					new StringContent(json, Encoding.UTF8, "application/json"));
				ViewBag.Response = await response.Content.ReadAsStringAsync();
			}
			catch (Exception)
			{

			}
		}

		private async Task RespondToWorkflow(HttpClient client, string message)
		{
			int contractId = 8;
			
			string json = $@"{{
				'workflowFunctionID': 3,
				'workflowActionParameters': [
					{{
						'name': 'responseMessage',
						'value': '{message}',
						'workflowFunctionParameterId': 2

					}}
				]
			}}";

			try
			{
				var response = await client.PostAsync($"{AzureAdOptions.Settings.WorkbenchBaseAddress}/api/v2/contracts/{contractId}/actions",
					new StringContent(json, Encoding.UTF8, "application/json"));
				ViewBag.Response = await response.Content.ReadAsStringAsync();
			}
			catch (Exception)
			{

			}
		}

		[AllowAnonymous]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
