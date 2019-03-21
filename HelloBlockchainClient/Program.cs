using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Text;

namespace HelloBlockchainClient
{
	class Program
	{
		public static readonly string AUTHORITY = "https://login.microsoftonline.com/[Azure_AD_tenant]";
		public static readonly string WORKBENCH_API_URL = "https://***.azurewebsites.net/"; // URL of the ABW API
		public static readonly string RESOURCE = "da9a0af8-b32d-*****";			// GUID of Azure Blockchain Workbench resource
		public static readonly string CLIENT_APP_Id = "dd6c924c-e752-4fa-****";	// GUID of your Service Principal
		public static readonly string CLIENT_SECRET = "1fDanv8uUro8FSWPyw****"; // Secret Key

		static async Task Main(string[] args)
		{
			AuthenticationContext authenticationContext = new AuthenticationContext(AUTHORITY);
			ClientCredential clientCredential = new ClientCredential(CLIENT_APP_Id, CLIENT_SECRET);

			try
			{
				// Getting the token, it is recommended to call AcquireTokenAsync before every Workbench API call
				// The library takes care of refreshing the token when it expires
				var result = await authenticationContext.AcquireTokenAsync(RESOURCE, clientCredential).ConfigureAwait(false);

				// Using token to call Workbench's API
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

				// Get Applications
				await ListApplications(client);

				// Get Workflows for an application
				await ListWorkflows(client);

				// Start a new workflow
				 await StartWorkflow(client);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		private static async Task ListApplications(HttpClient client)
		{
			var response = await client.GetAsync($"{WORKBENCH_API_URL}/api/v2/applications");
			var applications = await response.Content.ReadAsStringAsync();

			Console.WriteLine(applications);
		}

		private static async Task ListWorkflows(HttpClient client)
		{
			var response = await client.GetAsync($"{WORKBENCH_API_URL}/api/v2/applications/1/workflows");
			var applications = await response.Content.ReadAsStringAsync();

			Console.WriteLine(applications);
		}

		private static async Task StartWorkflow(HttpClient client)
		{
			int workflowId = 1;
			int contractCodeId = 1;
			int connectionId = 1;
			string json = @"{
				'workflowFunctionID': 1,
				'workflowActionParameters': [
					{
						'name': 'message',
						'value': 'Hello from the Client',
						'workflowFunctionParameterId': 1
					}
				]
			}";
			var response = await client.PostAsync($"{WORKBENCH_API_URL}/api/v2/contracts?workflowId={workflowId}&contractCodeId={contractCodeId}&connectionId={connectionId}",
				new StringContent(json, Encoding.UTF8, "application/json"));

			Console.WriteLine(response);
		}
	}
}

