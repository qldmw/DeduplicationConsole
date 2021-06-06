using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Deduplication
{
    public class ConfigurationReader
    {
        private JObject _object;
        public ConfigurationReader()
        {
            string currentPaht = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            _object = JObject.Parse(File.ReadAllText(Path.Combine(currentPaht, "appsettings.json")));
        }

        public T GetConfig<T>(string nodeName)
        {
            var node = _object[nodeName];
            var value = node.Value<T>();
            return value;
        }

        public string GetTargetFolderPath()
        {
            var node = _object["TargetFolderPath"];
            return node.ToString().ToUtf8();
        }

        public IList<string> GetStringList(string nodeName)
        {
            var node = _object[nodeName];
            IList<string> paths = node
                .Children()
                .Select(s => s.ToString().ToUtf8())
                .ToList();
            return paths;
            
        }

        public Dictionary<string, HashSet<string>> GetMapping()
        {
            var node = _object["ExtensionFolderMapping"];
            Dictionary<string, HashSet<string>> mapping = node
                .Children()
                .ToDictionary(s => s["FolderName"].Value<string>(), s => s["Extensions"].Children().Select(t => t.ToString().ToUtf8()).ToHashSet());
            return mapping;
        }
    }

    public static class StringExtension
    {
        public static string ToUtf8(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            byte[] bytes = Encoding.Default.GetBytes(source);
            string utf8String = Encoding.UTF8.GetString(bytes);
            return utf8String;
        }
    }
}
