// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using NUnit.Framework;
using Ovation;
using UnityEngine;

namespace Ovation.Tests.Editor
{
    public class OvationConfigTests
    {
        [Test]
        public void DefaultBaseUrl_ReturnsDevUrl()
        {
            var config = ScriptableObject.CreateInstance<OvationConfig>();
            Assert.AreEqual("https://dev.api.ovation.games", config.BaseUrl);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BaseUrlOverride_TakePrecedence()
        {
            var config = ScriptableObject.CreateInstance<OvationConfig>();
            config.SetBaseUrlOverride("https://custom.api.example.com");
            Assert.AreEqual("https://custom.api.example.com", config.BaseUrl);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BaseUrlOverride_TrailingSlashTrimmed()
        {
            var config = ScriptableObject.CreateInstance<OvationConfig>();
            config.SetBaseUrlOverride("https://custom.api.example.com/");
            Assert.AreEqual("https://custom.api.example.com", config.BaseUrl);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void SetApiKey_UpdatesValue()
        {
            var config = ScriptableObject.CreateInstance<OvationConfig>();
            config.SetApiKey("ovn_test_abc123");
            Assert.AreEqual("ovn_test_abc123", config.ApiKey);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void DefaultConfig_HasExpectedDefaults()
        {
            var config = ScriptableObject.CreateInstance<OvationConfig>();
            Assert.IsTrue(config.AutoManagePlayerId);
            Assert.IsTrue(config.EnableDebugLogging);
            Assert.AreEqual(100, config.MaxQueueSize);
            Assert.AreEqual(60f, config.QueueFlushIntervalSeconds);
            Assert.AreEqual(50, config.MaxCacheSizeMB);
            Assert.AreEqual(OvationEnvironment.Test, config.Environment);
            Object.DestroyImmediate(config);
        }
    }
}
