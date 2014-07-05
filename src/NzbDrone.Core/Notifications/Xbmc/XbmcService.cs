﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Notifications.Xbmc.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.Xbmc
{
    public interface IXbmcService
    {
        void Notify(XbmcSettings settings, string title, string message);
        void Update(XbmcSettings settings, Series series);
        void Clean(XbmcSettings settings);
        XbmcVersion GetJsonVersion(XbmcSettings settings);
        ValidationFailure Test(XbmcSettings settings);
    }

    public class XbmcService : IXbmcService
    {
        private readonly IHttpProvider _httpProvider;
        private readonly IEnumerable<IApiProvider> _apiProviders;
        private readonly Logger _logger;

        public XbmcService(IHttpProvider httpProvider, IEnumerable<IApiProvider> apiProviders, Logger logger)
        {
            _httpProvider = httpProvider;
            _apiProviders = apiProviders;
            _logger = logger;
        }

        public void Notify(XbmcSettings settings, string title, string message)
        {
            var provider = GetApiProvider(settings);
            provider.Notify(settings, title, message);
        }

        public void Update(XbmcSettings settings, Series series)
        {
            var provider = GetApiProvider(settings);
            provider.Update(settings, series);
        }

        public void Clean(XbmcSettings settings)
        {
            var provider = GetApiProvider(settings);
            provider.Clean(settings);
        }

        public XbmcVersion GetJsonVersion(XbmcSettings settings)
        {
            try
            {
                var postJson = new JObject();
                postJson.Add(new JProperty("jsonrpc", "2.0"));
                postJson.Add(new JProperty("method", "JSONRPC.Version"));
                postJson.Add(new JProperty("id", 1));

                var response = _httpProvider.PostCommand(settings.Address, settings.Username, settings.Password, postJson.ToString());

                _logger.Debug("Getting version from response: " + response);
                var result = Json.Deserialize<XbmcJsonResult<JObject>>(response);

                var versionObject = result.Result.Property("version");

                if (versionObject.Value.Type == JTokenType.Integer)
                    return new XbmcVersion((int)versionObject.Value);

                if (versionObject.Value.Type == JTokenType.Object)
                    return Json.Deserialize<XbmcVersion>(versionObject.Value.ToString());

                throw new InvalidCastException("Unknown Version structure!: " + versionObject);
            }

            catch (Exception ex)
            {
                _logger.DebugException(ex.Message, ex);
            }

            return new XbmcVersion();
        }

        private IApiProvider GetApiProvider(XbmcSettings settings)
        {
            var version = GetJsonVersion(settings);
            var apiProvider = _apiProviders.SingleOrDefault(a => a.CanHandle(version));

            if (apiProvider == null)
            {
                var message = String.Format("Invalid API Version: {0} for {1}", version, settings.Address);
                throw new InvalidXbmcVersionException(message);
            }

            return apiProvider;
        }

        public ValidationFailure Test(XbmcSettings settings)
        {
            try
            {
                _logger.Debug("Determining version of XBMC Host: {0}", settings.Address);
                var version = GetJsonVersion(settings);
                _logger.Debug("Version is: {0}", version);

                if (version == new XbmcVersion(0))
                {
                    throw new InvalidXbmcVersionException("Verion received from XBMC is invalid, please correct your settings.");
                }

                Notify(settings, "Test Notification", "Success! XBMC has been successfully configured!");
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Unable to send test message: " + ex.Message, ex);
                return new ValidationFailure("Host", "Unable to send test message");
            }

            return null;
        }
    }
}