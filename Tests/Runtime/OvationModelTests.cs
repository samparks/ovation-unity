// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Ovation.Models;
using Newtonsoft.Json;
using UnityEngine.TestTools;

namespace Ovation.Tests.Runtime
{
    /// <summary>
    /// Tests that all Ovation API response models deserialize correctly from JSON.
    /// These verify that the C# models match the API contract defined in the Ovation API docs.
    /// </summary>
    public class OvationModelTests
    {
        [Test]
        public void Player_DeserializesFromJson()
        {
            var json = @"{
                ""id"": ""e2f3a4b5-c6d7-8901-ef23-456789abcdef"",
                ""anonymous"": true,
                ""external_id"": null,
                ""achievements"": [],
                ""created_at"": ""2026-03-10T12:00:00+00:00""
            }";

            var player = JsonConvert.DeserializeObject<Player>(json);
            Assert.AreEqual("e2f3a4b5-c6d7-8901-ef23-456789abcdef", player.Id);
            Assert.IsTrue(player.Anonymous);
            Assert.IsNull(player.ExternalId);
            Assert.IsNotNull(player.Achievements);
            Assert.AreEqual(0, player.Achievements.Count);
        }

        [Test]
        public void Achievement_DeserializesFromJson()
        {
            var json = @"{
                ""id"": ""f47ac10b-58cc-4372-a567-0e02b2c3d479"",
                ""slug"": ""first-blood"",
                ""display_name"": ""First Blood"",
                ""description"": ""Defeat your first enemy in combat"",
                ""repeatable"": false,
                ""archived"": false,
                ""is_hidden"": false,
                ""rarity_percentage"": 45.2,
                ""slot_assets"": {""slot-id"": ""asset-id""},
                ""created_at"": ""2026-03-10T12:00:00+00:00"",
                ""updated_at"": ""2026-03-10T12:00:00+00:00"",
                ""test_mode"": false
            }";

            var achievement = JsonConvert.DeserializeObject<Achievement>(json);
            Assert.AreEqual("first-blood", achievement.Slug);
            Assert.AreEqual("First Blood", achievement.DisplayName);
            Assert.AreEqual(45.2f, achievement.RarityPercentage, 0.01f);
            Assert.IsFalse(achievement.Repeatable);
            Assert.IsFalse(achievement.Archived);
            Assert.IsNotNull(achievement.SlotAssets);
            Assert.AreEqual("asset-id", achievement.SlotAssets["slot-id"]);
        }

        [Test]
        public void IssueAchievementResult_DeserializesFromJson()
        {
            var json = @"{
                ""slug"": ""first-blood"",
                ""display_name"": ""First Blood"",
                ""earned_at"": ""2026-03-10T12:00:00+00:00"",
                ""was_new"": true
            }";

            var result = JsonConvert.DeserializeObject<IssueAchievementResult>(json);
            Assert.AreEqual("first-blood", result.Slug);
            Assert.AreEqual("First Blood", result.DisplayName);
            Assert.IsTrue(result.WasNew);
            Assert.IsFalse(result.WasQueued); // Default
        }

        [Test]
        public void PlayerAchievement_WithAssets_DeserializesFromJson()
        {
            var json = @"{
                ""slug"": ""first-blood"",
                ""display_name"": ""First Blood"",
                ""description"": ""Defeat your first enemy"",
                ""authority_id"": ""4d38f902-de2e-487d-8dbd-f4452fc2b4a1"",
                ""authority_name"": ""Cool Game Studio"",
                ""earned_at"": ""2026-03-10T12:00:00+00:00"",
                ""assets"": [
                    {
                        ""id"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                        ""slot_id"": ""slot-uuid"",
                        ""slot_name"": ""badge"",
                        ""url"": ""https://ovation-assets-dev.s3.amazonaws.com/assets/badge.png"",
                        ""version"": 1,
                        ""display_name"": ""Gold Star Badge""
                    }
                ]
            }";

            var pa = JsonConvert.DeserializeObject<PlayerAchievement>(json);
            Assert.AreEqual("first-blood", pa.Slug);
            Assert.AreEqual("Cool Game Studio", pa.AuthorityName);
            Assert.AreEqual(1, pa.Assets.Count);
            Assert.AreEqual("badge", pa.Assets[0].SlotName);
            Assert.AreEqual(1, pa.Assets[0].Version);
        }

        [Test]
        public void Slot_DeserializesFromJson()
        {
            var json = @"{
                ""id"": ""c3d4e5f6-a7b8-9012-cdef-345678901234"",
                ""name"": ""avatar_frame"",
                ""display_name"": ""Avatar Frame"",
                ""description"": ""Decorative border"",
                ""asset_type"": ""image"",
                ""file_formats"": [""png"", ""webp""],
                ""width"": 256,
                ""height"": 256,
                ""inner_width"": 192,
                ""inner_height"": 192,
                ""max_file_size_bytes"": 131072,
                ""transparency"": ""required"",
                ""animation_allowed"": false,
                ""text_max_length"": null,
                ""text_allowed_pattern"": null,
                ""authority_guidance"": ""Create a 256x256 frame"",
                ""implementation_notes"": ""Overlay on avatar"",
                ""created_at"": ""2026-03-10T12:00:00+00:00"",
                ""updated_at"": ""2026-03-10T12:00:00+00:00""
            }";

            var slot = JsonConvert.DeserializeObject<Slot>(json);
            Assert.AreEqual("avatar_frame", slot.Name);
            Assert.AreEqual(256, slot.Width);
            Assert.AreEqual(192, slot.InnerWidth);
            Assert.AreEqual("required", slot.Transparency);
            Assert.IsFalse(slot.AnimationAllowed);
            Assert.IsNull(slot.TextMaxLength);
            Assert.AreEqual(2, slot.FileFormats.Count);
        }

        [Test]
        public void OvationError_DeserializesFromErrorResponse()
        {
            var json = @"{
                ""error"": {
                    ""code"": ""achievement_not_found"",
                    ""message"": ""Achievement not found.""
                }
            }";

            var response = JsonConvert.DeserializeObject<ErrorResponse>(json);
            Assert.AreEqual("achievement_not_found", response.Error.Code);
            Assert.AreEqual("Achievement not found.", response.Error.Message);
        }

        [Test]
        public void PaginatedResponse_DeserializesFromJson()
        {
            var json = @"{
                ""data"": [
                    {""slug"": ""a"", ""display_name"": ""A""},
                    {""slug"": ""b"", ""display_name"": ""B""}
                ],
                ""next_cursor"": ""some-uuid""
            }";

            var response = JsonConvert.DeserializeObject<PaginatedResponse<Achievement>>(json);
            Assert.AreEqual(2, response.Data.Count);
            Assert.AreEqual("a", response.Data[0].Slug);
            Assert.AreEqual("some-uuid", response.NextCursor);
        }

        [Test]
        public void PaginatedResponse_NullCursor_IndicatesLastPage()
        {
            var json = @"{
                ""data"": [{""slug"": ""a""}],
                ""next_cursor"": null
            }";

            var response = JsonConvert.DeserializeObject<PaginatedResponse<Achievement>>(json);
            Assert.IsNull(response.NextCursor);
        }

        [Test]
        public void EquippedSlotResponse_DeserializesFromJson()
        {
            var json = @"{
                ""slot"": ""avatar_frame"",
                ""player_id"": ""player-uuid"",
                ""equipped_asset"": {
                    ""id"": ""asset-uuid"",
                    ""asset_type"": ""image"",
                    ""url"": ""https://example.com/asset.png"",
                    ""version"": 1,
                    ""display_name"": ""Gold Star Badge"",
                    ""achievement_slug"": ""first-blood"",
                    ""authority_name"": ""Cool Game Studio""
                }
            }";

            var response = JsonConvert.DeserializeObject<EquippedSlotResponse>(json);
            Assert.AreEqual("avatar_frame", response.Slot);
            Assert.AreEqual("player-uuid", response.PlayerId);
            Assert.IsNotNull(response.EquippedAsset);
            Assert.AreEqual("first-blood", response.EquippedAsset.AchievementSlug);
        }

        [Test]
        public void Asset_DeserializesFromJson()
        {
            var json = @"{
                ""id"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                ""asset_type"": ""image"",
                ""slot_id"": ""slot-uuid"",
                ""slot_name"": ""avatar_frame"",
                ""display_name"": ""Gold Star Badge"",
                ""authority_attribution"": ""Art by Jane Doe"",
                ""current_version"": 1,
                ""url"": ""https://ovation-assets-dev.s3.amazonaws.com/assets/abc123/v1/badge.png"",
                ""text_content"": null,
                ""created_at"": ""2026-03-10T12:00:00+00:00"",
                ""updated_at"": ""2026-03-10T12:00:00+00:00""
            }";

            var asset = JsonConvert.DeserializeObject<Asset>(json);
            Assert.AreEqual("image", asset.AssetType);
            Assert.AreEqual("avatar_frame", asset.SlotName);
            Assert.AreEqual(1, asset.CurrentVersion);
            Assert.IsNull(asset.TextContent);
            Assert.AreEqual("Art by Jane Doe", asset.AuthorityAttribution);
        }

        [Test]
        public void Authority_DeserializesFromJson()
        {
            var json = @"{
                ""id"": ""4d38f902-de2e-487d-8dbd-f4452fc2b4a1"",
                ""name"": ""Cool Game Studio"",
                ""type"": ""game_studio"",
                ""website"": ""https://coolgame.com"",
                ""verified"": true,
                ""created_at"": ""2026-03-10T12:00:00+00:00""
            }";

            var authority = JsonConvert.DeserializeObject<Authority>(json);
            Assert.AreEqual("Cool Game Studio", authority.Name);
            Assert.AreEqual("game_studio", authority.Type);
            Assert.IsTrue(authority.Verified);
        }
    }
}
