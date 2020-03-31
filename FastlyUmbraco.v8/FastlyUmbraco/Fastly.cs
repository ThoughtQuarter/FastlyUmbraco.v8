using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using System.Web;
using System.Web.Configuration;

namespace FastlyUmbraco
{
	public class Fastly
	{
		private static HttpClient Client;

		public Fastly(string fastlyKey, string fastlyServiceID)
		{
			_fastlyKey = fastlyKey;
			_fastlyServiceID = fastlyServiceID;

			Client = new HttpClient { };

			Client.DefaultRequestHeaders.Accept.Clear();
			Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			Client.DefaultRequestHeaders.Add("Fastly-Key", _fastlyKey);
		}

		private readonly string _fastlyKey;
		private readonly string _fastlyServiceID;

		private const string EntryPoint = "https://api.fastly.com";

		public Task<HttpResponseMessage> Purge(string uri)
		{
			return Purge(new Uri(uri));
		}

		public Task<HttpResponseMessage> Purge(Uri uri)
		{
			return SendAsync("PURGE", uri);
		}

		public Task<HttpResponseMessage> PurgeKey(string service, string key)
		{
			//var uri = $"{EntryPoint}/service/{WebUtility.UrlEncode(service)}/purge/{key}";
			var uri = $"{EntryPoint}/service/{WebUtility.UrlEncode(service)}/purge/{key}";
			return SendAsync("POST", new Uri(uri));
		}

		public Task<HttpResponseMessage> PurgeAll(string service)
		{
			//var uri = $"{EntryPoint}/service/{WebUtility.UrlEncode(service)}/purge_all";
			var uri = $"{EntryPoint}/service/{WebUtility.UrlEncode(service)}/purge_all";

			return SendAsync("POST", new Uri(uri));
		}

		public Task<HttpResponseMessage> PurgeAll()
		{
			//var uri = $"{EntryPoint}/service/{WebUtility.UrlEncode(_fastlyServiceID)}/purge_all";
			var uri = $"{EntryPoint}/service/{WebUtility.UrlEncode(_fastlyServiceID)}/purge_all";

			return SendAsync("POST", new Uri(uri));
		}

		private async Task<HttpResponseMessage> SendAsync(string method, Uri requestUri)
		{
			HttpResponseMessage response = null;

			int apiDelayTimeValue = 5000;
			if (int.TryParse(WebConfigurationManager.AppSettings[FastlyCacheComponent.FastlyApiDelayTimeKey], out int apiDelayTimeOut) && apiDelayTimeOut >= 0)
			{
				apiDelayTimeValue = apiDelayTimeOut;
			}

			await System.Threading.Tasks.Task.Run(async () =>
			{
				try
				{
					var request = new HttpRequestMessage(new HttpMethod(method), requestUri);
					Current.Logger.Info<Fastly>("Fastly - Purge URL Request - {request}", request.ToString().Replace("{", "(").Replace("}", ")"));

					await System.Threading.Tasks.Task.Delay(apiDelayTimeValue);
					Current.Logger.Info<Fastly>("Fastly - Purge URL Delay");

					response = await Client.SendAsync(request);
					Current.Logger.Info<Fastly>("Fastly - Purge URL Response - {response}", response.ToString().Replace("{", "(").Replace("}", ")"));
				}
				catch (Exception e)
				{
					Current.Logger.Error<Fastly>("Fastly - Exception - {e}", e);
				}
			});

			return response;
		}
	}
}