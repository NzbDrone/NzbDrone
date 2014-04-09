using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NzbDrone.Common;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration.Events;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;


namespace NzbDrone.Core.Configuration
{
    public interface IConfigFileProvider : IHandleAsync<ApplicationStartedEvent>,
                                           IExecute<ResetApiKeyCommand>
    {
        Dictionary<string, object> GetConfigDictionary();
        void SaveConfigDictionary(Dictionary<string, object> configValues);

        int Port { get; }
        int SslPort { get; }
        bool EnableSsl { get; }
        bool LaunchBrowser { get; }
        bool AuthenticationEnabled { get; }
        string Username { get; }
        string Password { get; }
        string LogLevel { get; }
        string Branch { get; }
        bool AutoUpdate { get; }
        string ApiKey { get; }
        bool Torrent { get; }
        string SslCertHash { get; }
        string UrlBase { get; }
    }

    public class ConfigFileProvider : IConfigFileProvider
    {
        public const string CONFIG_ELEMENT_NAME = "Config";

        private readonly IEventAggregator _eventAggregator;
        private readonly ICached<string> _cache;

        private readonly string _configFile;

        public ConfigFileProvider(IAppFolderInfo appFolderInfo, ICacheManager cacheManager, IEventAggregator eventAggregator)
        {
            _cache = cacheManager.GetCache<string>(GetType());
            _eventAggregator = eventAggregator;
            _configFile = appFolderInfo.GetConfigPath();
        }

        public Dictionary<string, object> GetConfigDictionary()
        {
            var dict = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            var type = GetType();
            var properties = type.GetProperties();

            foreach (var propertyInfo in properties)
            {
                var value = propertyInfo.GetValue(this, null);

                dict.Add(propertyInfo.Name, value);
            }

            return dict;
        }

        public void SaveConfigDictionary(Dictionary<string, object> configValues)
        {
            _cache.Clear();

            var allWithDefaults = GetConfigDictionary();

            foreach (var configValue in configValues)
            {
                if (configValue.Key.Equals("ApiKey", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                object currentValue;
                allWithDefaults.TryGetValue(configValue.Key, out currentValue);
                if (currentValue == null) continue;

                var equal = configValue.Value.ToString().Equals(currentValue.ToString());

                if (!equal)
                {
                    SetValue(configValue.Key.FirstCharToUpper(), configValue.Value.ToString());
                }
            }

            _eventAggregator.PublishEvent(new ConfigFileSavedEvent());
        }

        public int Port
        {
            get { return GetValueInt("Port", 8989); }
        }

        public int SslPort
        {
            get { return GetValueInt("SslPort", 9898); }
        }

        public bool EnableSsl
        {
            get { return GetValueBoolean("EnableSsl", false); }
        }

        public bool LaunchBrowser
        {
            get { return GetValueBoolean("LaunchBrowser", true); }
        }

        public string ApiKey
        {
            get
            {
                return GetValue("ApiKey", GenerateApiKey());
            }
        }

        public bool Torrent
        {
            get { return GetValueBoolean("Torrent", false, persist: false); }
        }

        public bool AuthenticationEnabled
        {
            get { return GetValueBoolean("AuthenticationEnabled", false); }
        }

        public string Branch
        {
            get { return GetValue("Branch", "master").ToLowerInvariant(); }
        }

        public bool AutoUpdate
        {
            get { return GetValueBoolean("AutoUpdate", false, persist: false); }
        }

        public string Username
        {
            get { return GetValue("Username", ""); }
        }

        public string Password
        {
            get { return GetValue("Password", ""); }
        }

        public string LogLevel
        {
            get { return GetValue("LogLevel", "Info"); }
        }

        public string SslCertHash
        {
            get { return GetValue("SslCertHash", ""); }
        }

        public string UrlBase
        {
            get
            {
                var urlBase = GetValue("UrlBase", "");

                if (String.IsNullOrEmpty(urlBase))
                {
                    return urlBase;
                }

                return "/" + urlBase.Trim('/').ToLower();
            }
        }

        public int GetValueInt(string key, int defaultValue)
        {
            return Convert.ToInt32(GetValue(key, defaultValue));
        }

        public bool GetValueBoolean(string key, bool defaultValue, bool persist = true)
        {
            return Convert.ToBoolean(GetValue(key, defaultValue, persist));
        }

        public T GetValueEnum<T>(string key, T defaultValue)
        {
            return (T)Enum.Parse(typeof(T), GetValue(key, defaultValue), true);
        }

        public string GetValue(string key, object defaultValue, bool persist = true)
        {
            return _cache.Get(key, () =>
                {
                    EnsureDefaultConfigFile();

                    var xDoc = LoadConfigFile();
                    var config = xDoc.Descendants(CONFIG_ELEMENT_NAME).Single();

                    var parentContainer = config;

                    var valueHolder = parentContainer.Descendants(key).ToList();

                    if (valueHolder.Count() == 1)
                        return valueHolder.First().Value.Trim();

                    //Save the value
                    if (persist)
                    {
                        SetValue(key, defaultValue);
                    }

                    //return the default value
                    return defaultValue.ToString();
                });
        }

        public void SetValue(string key, object value)
        {
            EnsureDefaultConfigFile();

            var valueString = value.ToString().Trim();
            var xDoc = LoadConfigFile();
            var config = xDoc.Descendants(CONFIG_ELEMENT_NAME).Single();

            var parentContainer = config;

            var keyHolder = parentContainer.Descendants(key);

            if (keyHolder.Count() != 1)
            {
                parentContainer.Add(new XElement(key, valueString));
            }

            else
            {
                parentContainer.Descendants(key).Single().Value = valueString;
            }

            _cache.Set(key, valueString);

            xDoc.Save(_configFile);
        }

        public void SetValue(string key, Enum value)
        {
            SetValue(key, value.ToString().ToLower());
        }

        private void EnsureDefaultConfigFile()
        {
            if (!File.Exists(_configFile))
            {
                var xDoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
                xDoc.Add(new XElement(CONFIG_ELEMENT_NAME));
                xDoc.Save(_configFile);

                SaveConfigDictionary(GetConfigDictionary());
            }
        }

        private void DeleteOldValues()
        {
            EnsureDefaultConfigFile();

            var xDoc = LoadConfigFile();
            var config = xDoc.Descendants(CONFIG_ELEMENT_NAME).Single();

            var type = GetType();
            var properties = type.GetProperties();

            foreach (var configValue in config.Descendants().ToList())
            {
                var name = configValue.Name.LocalName;

                if (!properties.Any(p => p.Name == name))
                {
                    config.Descendants(name).Remove();
                }
            }

            xDoc.Save(_configFile);
        }

        private XDocument LoadConfigFile()
        {
            try
            {
                return XDocument.Load(_configFile);
            }

            catch (XmlException ex)
            {
                throw new InvalidConfigFileException(_configFile + " is invalid, please see the http://wiki.nzbdrone.com for steps to resolve this issue.", ex);
            }
        }

        private string GenerateApiKey()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        public void HandleAsync(ApplicationStartedEvent message)
        {
            DeleteOldValues();
        }

        public void Execute(ResetApiKeyCommand message)
        {
            SetValue("ApiKey", GenerateApiKey());
        }
    }
}
