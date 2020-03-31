using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPoco.fastJSON;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace FastlyUmbraco
{
	[PluginController("FastlyUmbraco")]
	public class FastlyAPIController : UmbracoAuthorizedApiController
	{
		[System.Web.Http.AcceptVerbs("POST")]
		[HttpPost]
		public async Task<HttpResponseMessage> GetFastlyStats()
		{
			JObject requestData = JObject.Parse(await Request.Content.ReadAsStringAsync());

			var Client = new HttpClient();
			Client.DefaultRequestHeaders.Accept.Clear();
			Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			Client.DefaultRequestHeaders.Add("Fastly-Key", WebConfigurationManager.AppSettings[FastlyCacheComponent.FastlyApiKey]);

			int fromUnixTimestamp = (int)DateTime.UtcNow.AddDays(-7).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			int toUnixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

			var request = new HttpRequestMessage
			{
				Method = new HttpMethod("GET"),
				RequestUri = new Uri("https://api.fastly.com/stats/service/" + WebConfigurationManager.AppSettings[FastlyCacheComponent.FastlyApplicationIdKey] + "/field/" + requestData["fieldName"] + "?from=" + fromUnixTimestamp + "&by=" + requestData["byFormat"]),
			};

			HttpResponseMessage response = await Client.SendAsync(request);

			return response;
		}

		[System.Web.Http.AcceptVerbs("GET")]
		[HttpGet]
		public HttpResponseMessage GetFastlySettings()
		{
			var result = FastlyCacheComponent.GetSettingsJSON();
			return new HttpResponseMessage() { Content = new StringContent(string.Concat(result), Encoding.UTF8, "application/json") };
		}

		[System.Web.Http.AcceptVerbs("POST")]
		[HttpPost]
		public async Task<HttpResponseMessage> SaveFastlySettings()
		{
			string rawData = await Request.Content.ReadAsStringAsync();

			HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);

			if (FastlyCacheComponent.SaveSettingsJSON(rawData))
			{
				response.Content = new StringContent("success");
			}
			else
			{
				response.Content = new StringContent("error");
				response.StatusCode = HttpStatusCode.InternalServerError;
			}

			return response;
		}

		[System.Web.Http.AcceptVerbs("POST")]
		[HttpPost]
		public async Task<HttpResponseMessage> PurgeURLAsync()
		{
			string rawUrl = await Request.Content.ReadAsStringAsync();

			HttpResponseMessage response = null;
			if (String.IsNullOrWhiteSpace(rawUrl) == false)
			{
				response = await FastlyCacheComponent.FastlyAPI.Purge(Uri.EscapeUriString(rawUrl));
				//response = this.Request.CreateResponse(HttpStatusCode.OK);
			}
			else
			{
				response = this.Request.CreateResponse(HttpStatusCode.BadRequest);
			}

			return response;
		}

		[System.Web.Http.AcceptVerbs("POST")]
		[HttpPost]
		public async Task<HttpResponseMessage> PurgeURLByIDAsync()
		{
			string contentId = await Request.Content.ReadAsStringAsync();

			HttpResponseMessage response = null;
			if (string.IsNullOrWhiteSpace(contentId) == false)
			{
				string domain = WebConfigurationManager.AppSettings[FastlyCacheComponent.FastlyDomainKey];

				if (string.IsNullOrWhiteSpace(domain))
				{
					response = this.Request.CreateResponse(HttpStatusCode.InternalServerError);
				} else
				{
					string url = domain + Umbraco.Content(contentId).Url();
					response = await FastlyCacheComponent.FastlyAPI.Purge(url);
				}				
			}
			else
			{
				response = this.Request.CreateResponse(HttpStatusCode.BadRequest);
			}

			return response;
		}

		[System.Web.Http.AcceptVerbs("POST")]
		[HttpPost]
		public async Task<HttpResponseMessage> PurgeAll()
		{
			HttpResponseMessage response = await FastlyCacheComponent.FastlyAPI.PurgeAll();

			return response;
		}
	}
}