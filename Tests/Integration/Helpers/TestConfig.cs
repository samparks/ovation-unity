using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Ovation.Tests.Integration.Helpers
{
    internal class TestConfig
    {
        [JsonProperty("api_key")]
        public string ApiKey { get; set; }

        [JsonProperty("base_url")]
        public string BaseUrl { get; set; }

        internal static TestConfig Load()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "test-config.json");
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "test-config.json not found. Copy test-config.example.json to test-config.json and add your API key.");

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<TestConfig>(json);
        }
    }
}
