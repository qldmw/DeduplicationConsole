using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deduplication
{
    public class ConfigurationReader
    {
        private IConfigurationRoot _config;
        public ConfigurationReader()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            _config = builder.Build();
        }

        public T GetConfig<T>(string nodeName)
        {
            var value = _config.GetSection(nodeName).Get<T>();
            return value;
        }
    }
}
