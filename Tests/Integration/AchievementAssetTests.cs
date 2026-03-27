using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Ovation.Models;
using Ovation.Tests.Integration.Helpers;

namespace Ovation.Tests.Integration
{
    [TestFixture]
    internal class AchievementAssetTests : IntegrationTestBase
    {
        [Test]
        public async Task ListAchievements_ReturnsAchievements()
        {
            var result = await Achievements.GetAchievementsAsync();

            Assert.IsTrue(result.Success, $"API call failed: {result.Error?.Message}");
            Assert.IsNotEmpty(result.Data, "Expected at least one achievement");

            foreach (var achievement in result.Data)
            {
                Assert.IsNotNull(achievement.Id, "Achievement Id should not be null");
                Assert.IsNotEmpty(achievement.Slug, "Achievement Slug should not be empty");
                Assert.IsNotEmpty(achievement.DisplayName, "Achievement DisplayName should not be empty");
            }
        }

        [Test]
        public async Task ListAchievements_SomeHaveSlotAssets()
        {
            var result = await Achievements.GetAchievementsAsync();
            Assert.IsTrue(result.Success, $"API call failed: {result.Error?.Message}");

            var withAssets = result.Data
                .Where(a => a.SlotAssets != null && a.SlotAssets.Count > 0)
                .ToList();

            Assert.IsNotEmpty(withAssets,
                "No achievements have bound assets — test data may be missing");

            foreach (var achievement in withAssets)
            {
                foreach (var kvp in achievement.SlotAssets)
                {
                    Assert.IsNotEmpty(kvp.Key, $"SlotAssets key (slot ID) should not be empty on achievement {achievement.Slug}");
                    Assert.IsNotEmpty(kvp.Value, $"SlotAssets value (asset ID) should not be empty on achievement {achievement.Slug}");
                }
            }
        }

        [Test]
        public async Task GetAchievementBySlug_ReturnsCorrectAchievement()
        {
            var listResult = await Achievements.GetAchievementsAsync();
            Assert.IsTrue(listResult.Success, $"List API call failed: {listResult.Error?.Message}");
            Assert.IsNotEmpty(listResult.Data, "No achievements to test with");

            var slug = listResult.Data.First().Slug;

            var result = await Achievements.GetAchievementAsync(slug);

            Assert.IsTrue(result.Success, $"API call failed: {result.Error?.Message}");
            Assert.AreEqual(slug, result.Data.Slug, "Returned achievement slug should match requested slug");
            Assert.IsNotNull(result.Data.Id, "Achievement Id should not be null");
            Assert.IsNotEmpty(result.Data.DisplayName, "Achievement DisplayName should not be empty");
        }

        [Test]
        public async Task GetAchievementBySlug_WithAssets_ReturnsSlotAssets()
        {
            var listResult = await Achievements.GetAchievementsAsync();
            Assert.IsTrue(listResult.Success, $"List API call failed: {listResult.Error?.Message}");

            var withAssets = listResult.Data
                .FirstOrDefault(a => a.SlotAssets != null && a.SlotAssets.Count > 0);
            Assert.IsNotNull(withAssets,
                "No achievements have bound assets — test data may be missing");

            var result = await Achievements.GetAchievementAsync(withAssets.Slug);

            Assert.IsTrue(result.Success, $"API call failed: {result.Error?.Message}");
            Assert.IsNotNull(result.Data.SlotAssets, "SlotAssets should not be null");
            Assert.IsNotEmpty(result.Data.SlotAssets, "SlotAssets should not be empty");
            CollectionAssert.AreEquivalent(withAssets.SlotAssets, result.Data.SlotAssets,
                "SlotAssets from single fetch should match list fetch");
        }

        [Test]
        public async Task GetAsset_ReturnsValidAssetMetadata()
        {
            var listResult = await Achievements.GetAchievementsAsync();
            Assert.IsTrue(listResult.Success, $"List API call failed: {listResult.Error?.Message}");

            var withAssets = listResult.Data
                .FirstOrDefault(a => a.SlotAssets != null && a.SlotAssets.Count > 0);
            Assert.IsNotNull(withAssets,
                "No achievements have bound assets — test data may be missing");

            var assetId = withAssets.SlotAssets.Values.First();
            var result = await Assets.GetAssetAsync(assetId);

            Assert.IsTrue(result.Success, $"API call failed: {result.Error?.Message}");
            var asset = result.Data;

            Assert.AreEqual(assetId, asset.Id, "Asset Id should match requested Id");
            Assert.IsNotNull(asset.SlotId, "Asset SlotId should not be null");
            Assert.IsNotEmpty(asset.SlotName, "Asset SlotName should not be empty");
            Assert.That(asset.AssetType, Is.AnyOf("image", "text", "audio"),
                $"Unexpected asset type: {asset.AssetType}");
            Assert.GreaterOrEqual(asset.CurrentVersion, 1, "Asset version should be >= 1");

            if (asset.AssetType == "image")
                Assert.IsNotEmpty(asset.Url, "Image asset should have a URL");
            if (asset.AssetType == "text")
                Assert.IsNotEmpty(asset.TextContent, "Text asset should have TextContent");
        }

        [Test]
        public async Task AchievementSlotAssets_PointToValidAssets()
        {
            var listResult = await Achievements.GetAchievementsAsync();
            Assert.IsTrue(listResult.Success, $"List API call failed: {listResult.Error?.Message}");

            var withAssets = listResult.Data
                .Where(a => a.SlotAssets != null && a.SlotAssets.Count > 0)
                .Take(3)
                .ToList();
            Assert.IsNotEmpty(withAssets,
                "No achievements have bound assets — test data may be missing");

            foreach (var achievement in withAssets)
            {
                foreach (var (slotId, assetId) in achievement.SlotAssets)
                {
                    var result = await Assets.GetAssetAsync(assetId);
                    Assert.IsTrue(result.Success,
                        $"Failed to fetch asset {assetId} from achievement {achievement.Slug}: {result.Error?.Message}");
                    Assert.AreEqual(slotId, result.Data.SlotId,
                        $"Asset {assetId} SlotId should match the key in achievement {achievement.Slug}'s SlotAssets");
                }
            }
        }

        [Test]
        public async Task GetAsset_ImageAsset_HasDownloadableUrl()
        {
            var listResult = await Achievements.GetAchievementsAsync();
            Assert.IsTrue(listResult.Success, $"List API call failed: {listResult.Error?.Message}");

            Asset imageAsset = null;
            foreach (var achievement in listResult.Data)
            {
                if (achievement.SlotAssets == null || achievement.SlotAssets.Count == 0)
                    continue;

                foreach (var assetId in achievement.SlotAssets.Values)
                {
                    var assetResult = await Assets.GetAssetAsync(assetId);
                    if (assetResult.Success && assetResult.Data.AssetType == "image")
                    {
                        imageAsset = assetResult.Data;
                        break;
                    }
                }

                if (imageAsset != null) break;
            }

            Assert.IsNotNull(imageAsset, "No image assets found — test data may be missing");
            Assert.IsNotEmpty(imageAsset.Url, "Image asset URL should not be empty");

            var bytes = await Client.DownloadBytesAsync(imageAsset.Url);
            Assert.IsNotNull(bytes, "Downloaded bytes should not be null");
            Assert.Greater(bytes.Length, 0, "Downloaded bytes should not be empty");
        }

        [Test]
        public async Task PlayerAchievements_IncludeAssetSummaries()
        {
            // Create a player
            var playerResult = await Players.CreatePlayerAsync();
            Assert.IsTrue(playerResult.Success, $"Create player failed: {playerResult.Error?.Message}");
            var playerId = playerResult.Data.Id;

            // Find an achievement with bound assets
            var listResult = await Achievements.GetAchievementsAsync();
            Assert.IsTrue(listResult.Success, $"List achievements failed: {listResult.Error?.Message}");

            var withAssets = listResult.Data
                .FirstOrDefault(a => a.SlotAssets != null && a.SlotAssets.Count > 0);
            Assert.IsNotNull(withAssets,
                "No achievements have bound assets — test data may be missing");

            // Issue the achievement
            var issueResult = await Players.IssueAchievementAsync(playerId, withAssets.Slug);
            Assert.IsTrue(issueResult.Success, $"Issue achievement failed: {issueResult.Error?.Message}");

            // Fetch player achievements
            var paResult = await Players.GetPlayerAchievementsAsync(playerId);
            Assert.IsTrue(paResult.Success, $"Get player achievements failed: {paResult.Error?.Message}");

            var earned = paResult.Data.FirstOrDefault(pa => pa.Slug == withAssets.Slug);
            Assert.IsNotNull(earned, $"Player should have earned achievement {withAssets.Slug}");
            Assert.IsNotNull(earned.Assets, "PlayerAchievement.Assets should not be null");
            Assert.IsNotEmpty(earned.Assets, "PlayerAchievement.Assets should not be empty");

            var expectedAssetIds = withAssets.SlotAssets.Values.ToHashSet();

            foreach (var summary in earned.Assets)
            {
                Assert.IsNotEmpty(summary.Id, "AssetSummary Id should not be empty");
                Assert.IsNotEmpty(summary.SlotId, "AssetSummary SlotId should not be empty");
                Assert.IsNotEmpty(summary.SlotName, "AssetSummary SlotName should not be empty");
                Assert.GreaterOrEqual(summary.Version, 1, "AssetSummary Version should be >= 1");
                Assert.IsNotEmpty(summary.DisplayName, "AssetSummary DisplayName should not be empty");
            }

            var earnedAssetIds = earned.Assets.Select(a => a.Id).ToHashSet();
            Assert.IsTrue(expectedAssetIds.SetEquals(earnedAssetIds),
                $"Asset IDs from player achievement should match achievement's SlotAssets. " +
                $"Expected: [{string.Join(", ", expectedAssetIds)}], " +
                $"Got: [{string.Join(", ", earnedAssetIds)}]");
        }

        [Test]
        public async Task GetAsset_NonExistentId_ReturnsError()
        {
            var result = await Assets.GetAssetAsync("00000000-0000-0000-0000-000000000000");

            Assert.IsFalse(result.Success, "Expected failure for non-existent asset");
            Assert.IsNotNull(result.Error, "Error should not be null");
            Assert.AreEqual(404, result.Error.HttpStatusCode, "Expected 404 status code");
        }

        [Test]
        public async Task GetAchievement_NonExistentSlug_ReturnsError()
        {
            var result = await Achievements.GetAchievementAsync("this-slug-does-not-exist-12345");

            Assert.IsFalse(result.Success, "Expected failure for non-existent slug");
            Assert.IsNotNull(result.Error, "Error should not be null");
            Assert.AreEqual(404, result.Error.HttpStatusCode, "Expected 404 status code");
        }
    }
}
