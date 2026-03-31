using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Ovation.Tests.Integration.Helpers;

namespace Ovation.Tests.Integration
{
    [TestFixture]
    public class SmokeTest
    {
        [Test]
        public void Config_Loads_Successfully()
        {
            var config = TestConfig.Load();
            Assert.IsNotNull(config.ApiKey, "API key must be set in test-config.json");
            Assert.IsNotNull(config.BaseUrl, "Base URL must be set in test-config.json");
            Assert.That(config.ApiKey, Does.StartWith("ovn_"), "API key should start with 'ovn_'");
        }

        [Test]
        public async Task StandardHttpClient_Can_Reach_API()
        {
            var config = TestConfig.Load();
            var client = new StandardHttpClient(config.BaseUrl);
            client.SetApiKey(config.ApiKey);

            // Slots endpoint doesn't even require auth — simplest possible test
            var result = await client.GetAsync<List<Ovation.Models.Slot>>("/slots/standard", requiresAuth: false);
            Assert.IsTrue(result.Success, $"API call failed: {result.Error?.Message}");
            Assert.IsNotEmpty(result.Data, "Expected at least one standard slot");
        }
    }
}
