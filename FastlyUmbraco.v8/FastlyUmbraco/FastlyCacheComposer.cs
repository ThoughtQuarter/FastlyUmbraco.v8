using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Xml;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;
using Umbraco.Web.Routing;

namespace FastlyUmbraco
{
	public class FastlyCacheComposer : ComponentComposer<FastlyCacheComponent> { }

	public class FastlyCacheComponent : IComponent
	{
		private int MaxAge;

		private const string DoNotCacheControlPropertyName = "fastlyDoNotCache";
		private const string CacheForPropertyName = "fastlyCacheFor";

		public static Fastly FastlyAPI;

		private List<Uri> urlsToPurge = new List<Uri>();

		//Fastly Settings strings
		public const string FastlyMaxAgeKey = "Fastly:MaxAge";
		public const string FastlySurrogateControlMaxAgeKey = "Fastly:SurrogateControlMaxAge";
		public const string FastlyApplicationIdKey = "Fastly:AppID";
		public const string FastlyApiKey = "Fastly:APIKey";
		public const string FastlyStaleWhileInvalidateKey = "Fastly:StaleWhileInValidate";
		public const string FastlyStaleIfErrorKey = "Fastly:StaleIfError";
		public const string FastlyPurgeAllOnPublishKey = "Fastly:PurgeOnPublish";
		public const string FastlyDisableAzureARRAffinityKey = "Fastly:DisableAzureARRAffinity";
		public const string FastlyDomainKey = "Fastly:Domain";
		public const string FastlyApiDelayTimeKey = "Fastly:ApiDelayTime";

		private string[] settingKeys = new string[] {
			FastlyDomainKey,
			FastlyApplicationIdKey,
			FastlyApiKey,
			FastlyMaxAgeKey,
			FastlySurrogateControlMaxAgeKey,
			FastlyPurgeAllOnPublishKey,
			FastlyStaleWhileInvalidateKey,
			FastlyStaleIfErrorKey,
			FastlyDisableAzureARRAffinityKey,
			FastlyApiDelayTimeKey
		};

		public FastlyCacheComponent() { }

		// initialize: runs once when Umbraco starts
		public void Initialize()
		{
			CreateWebConfigSettings();

			//Create Fastly Instance providing APIKey and AppID
			FastlyAPI = new Fastly(
				GetAppSetting(FastlyApiKey, " "),
				GetAppSetting(FastlyApplicationIdKey, " ")
			);

			int.TryParse(GetAppSetting(FastlySurrogateControlMaxAgeKey, "86400"), out MaxAge);

			//Subscribe to events
			PublishedRequest.Prepared += ConfigurePublishedRequestCaching;

			bool.TryParse(GetAppSetting(FastlyPurgeAllOnPublishKey, "true"), out bool purgeOnPublish);
			if (purgeOnPublish)
			{
				ContentService.Publishing += SetUrlsToPurge;
				ContentService.Published += PurgeUrl;
				ContentService.Unpublishing += SetUrlsToPurge;
				ContentService.Unpublished += PurgeUrl;
			}
		}

		public void CreateWebConfigSettings()
		{
			var settings = new Dictionary<string, object>();

			settings.Add(FastlyDomainKey, GetAppSetting(FastlyDomainKey, "https://www.example.com"));
			settings.Add(FastlyApplicationIdKey, GetAppSetting(FastlyApplicationIdKey, " "));
			settings.Add(FastlyApiKey, GetAppSetting(FastlyApiKey, " "));
			settings.Add(FastlyMaxAgeKey, GetAppSetting(FastlyMaxAgeKey, "3600"));
			settings.Add(FastlySurrogateControlMaxAgeKey, GetAppSetting(FastlySurrogateControlMaxAgeKey, "86400"));
			settings.Add(FastlyPurgeAllOnPublishKey, GetAppSetting(FastlyPurgeAllOnPublishKey, "true"));
			settings.Add(FastlyStaleWhileInvalidateKey, GetAppSetting(FastlyStaleWhileInvalidateKey, "60"));
			settings.Add(FastlyStaleIfErrorKey, GetAppSetting(FastlyStaleIfErrorKey, "86400"));
			settings.Add(FastlyDisableAzureARRAffinityKey, GetAppSetting(FastlyDisableAzureARRAffinityKey, "true"));
			settings.Add(FastlyApiDelayTimeKey, GetAppSetting(FastlyApiDelayTimeKey, "5000"));

			var config = WebConfigurationManager.OpenWebConfiguration("/");

			bool shouldSave = false;
			foreach (var setting in settings)
			{
				if (config.AppSettings.Settings.AllKeys.Contains(setting.Key) == false)
				{
					config.AppSettings.Settings.Add(setting.Key, setting.Value.ToString());
					shouldSave = true;
				}
			}

			if (shouldSave)
			{
				config.Save(ConfigurationSaveMode.Minimal);
			}
		}

		public void RemoveWebConfigSettings()
		{
			var config = WebConfigurationManager.OpenWebConfiguration("/");

			bool shouldSave = false;
			foreach (var setting in settingKeys)
			{
				if (config.AppSettings.Settings.AllKeys.Contains(setting))
				{
					config.AppSettings.Settings.Remove(setting);
					shouldSave = true;
				}
			}

			if (shouldSave)
			{
				config.Save(ConfigurationSaveMode.Minimal);
			}
		}

		private string GetAppSetting(string key, string defaultValue)
		{
			var appSettings = WebConfigurationManager.AppSettings;
			return appSettings.AllKeys.Contains(key) ? appSettings[key] : defaultValue;
		}

		//Prepublishing / preunpublishing
		private void SetUrlsToPurge(IContentService sender, PublishEventArgs<IContent> e)
		{
			var helper = Umbraco.Web.Composing.Current.UmbracoHelper;
			string domain = WebConfigurationManager.AppSettings[FastlyDomainKey];
			
			//If domain isn't set skip getting urls
			if (string.IsNullOrWhiteSpace(domain))
			{
				return;
			}

			//Loop through content to publish and store the URLs for after publish finishes
			foreach (IContent content in e.PublishedEntities)
			{
				if (content.GetStatus() == ContentStatus.Published)
				{
					IPublishedContent publishedContent = helper.Content(content.Id);
					urlsToPurge.Add(new Uri(domain + publishedContent.Url()));
				}
			}
		}

		//After publish / unpublish
		protected void PurgeUrl(IContentService sender, PublishEventArgs<IContent> e)
		{
			//loop through urls and send them to Fastly purge API
			foreach (Uri url in urlsToPurge)
			{
				FastlyAPI.Purge(url);
			}
			urlsToPurge.Clear();
		}

		//Catch request to published content to edit cache control headers
		private void ConfigurePublishedRequestCaching(object sender, EventArgs eventArgs)
		{
			var req = sender as PublishedRequest;
			var res = HttpContext.Current.Response;

			if (req == null || req.HasPublishedContent == false) return;
			if (HttpContext.Current == null) return;

			//Skip setting cache control if in preview mode
			if (req.UmbracoContext.InPreviewMode == false)
			{
				var content = req.PublishedContent;
				var maxAge = MaxAge;

				//Find Do Not Cache property on document type and get its value

				if (content.HasProperty(DoNotCacheControlPropertyName))
				{
					if ((bool)content.GetProperty(DoNotCacheControlPropertyName).GetValue() == false)
					{
						//Set do Cache control headers here
						if (content.HasProperty(CacheForPropertyName) && content.HasValue(CacheForPropertyName))
						{
							maxAge = int.Parse(content.GetProperty(CacheForPropertyName).GetValue().ToString());
						}
						if (maxAge <= 0) return;

						res.AppendHeader("Surrogate-Control", "max-age=" + maxAge);
						res.Cache.SetCacheability(HttpCacheability.NoCache);

						// stale while invalidate - https://docs.fastly.com/guides/performance-tuning/serving-stale-content
						int staleWhileInvalidate;
						if (int.TryParse(WebConfigurationManager.AppSettings[FastlyStaleWhileInvalidateKey], out staleWhileInvalidate) && staleWhileInvalidate > 0)
						{
							res.Cache.AppendCacheExtension($"stale-while-revalidate={staleWhileInvalidate}");
						}

						// stale if error - https://docs.fastly.com/en/guides/serving-stale-content#manually-enabling-serve-stale
						int staleIfError;
						if (int.TryParse(WebConfigurationManager.AppSettings[FastlyStaleIfErrorKey], out staleIfError) && staleIfError > 0)
						{
							res.Cache.AppendCacheExtension($"stale-if-error={staleIfError}");
						}
					}
					else
					{
						//Set do no cache control headers here
						res.Cache.SetCacheability(HttpCacheability.Private);
						res.Cache.SetNoStore();
					}
				}
				else
				{
					//Set do no cache control headers here
					res.Cache.SetCacheability(HttpCacheability.Private);
					res.Cache.SetNoStore();
				}
			}
		}

		//Returns the settings config xml as a JSON object
		public static string GetSettingsJSON()
		{
			var config = JsonConvert.SerializeObject(WebConfigurationManager.AppSettings);

			XmlDocument FastlySettingsXML = new XmlDocument();
			XmlNode rootNode = FastlySettingsXML.CreateElement("fastlySettings");
			FastlySettingsXML.AppendChild(rootNode);

			foreach (var setting in WebConfigurationManager.AppSettings.AllKeys)
			{
				if (setting.StartsWith("Fastly"))
				{
					XmlElement elem = FastlySettingsXML.CreateElement(setting);
					elem.InnerText = WebConfigurationManager.AppSettings[setting];
					FastlySettingsXML.SelectSingleNode("/fastlySettings").AppendChild(elem);
				}
			}

			return JsonConvert.SerializeXmlNode(FastlySettingsXML.SelectSingleNode("/fastlySettings")); //FastlySettingsXML.OuterXml;
		}

		//Given a JSON settings object it saves it to the config xml file after converting
		public static bool SaveSettingsJSON(string jsonObject)
		{
			XmlDocument FastlySettingsXML = JsonConvert.DeserializeXmlNode(jsonObject, "fastlySettings");

			if (FastlySettingsXML == null)
			{
				return false;
			}
			else
			{
				bool shouldSave = false;
				var config = WebConfigurationManager.OpenWebConfiguration("/");

				foreach (XmlElement setting in FastlySettingsXML.SelectSingleNode("/fastlySettings"))
				{
					if (config.AppSettings.Settings["Fastly:" + setting.Name].Value != setting.InnerText)
					{
						config.AppSettings.Settings["Fastly:" + setting.Name].Value = setting.InnerText;
						shouldSave = true;
					}				
				}

				if (shouldSave)
				{
					config.Save(ConfigurationSaveMode.Minimal);
				}

				return true;
			}
		}

		// terminate: runs once when Umbraco stops
		public void Terminate() { }
	}
}