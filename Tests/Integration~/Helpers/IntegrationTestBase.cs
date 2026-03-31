using NUnit.Framework;
using Ovation.Api;

namespace Ovation.Tests.Integration.Helpers
{
    internal class IntegrationTestBase
    {
        protected StandardHttpClient Client { get; private set; }
        protected AchievementService Achievements { get; private set; }
        protected AssetService Assets { get; private set; }
        protected PlayerService Players { get; private set; }
        protected SlotService Slots { get; private set; }

        [OneTimeSetUp]
        public void BaseSetUp()
        {
            var config = TestConfig.Load();
            Client = new StandardHttpClient(config.BaseUrl);
            Client.SetApiKey(config.ApiKey);

            Achievements = new AchievementService(Client);
            Assets = new AssetService(Client);
            Players = new PlayerService(Client);
            Slots = new SlotService(Client);
        }
    }
}
