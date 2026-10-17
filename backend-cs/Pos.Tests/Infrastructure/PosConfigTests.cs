using System;
using PosCs.Infrastructure.Config;
using Xunit;

namespace PosCs.Tests.Infrastructure
{
    public class PosConfigTests : IDisposable
    {
        private const string Key = "POS_TEST_UNIT_FLAG";

        public PosConfigTests()
        {
            Environment.SetEnvironmentVariable(Key, null);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(Key, null);
            Environment.SetEnvironmentVariable("POS_TEST_FROM_CONFIG", null);
            Environment.SetEnvironmentVariable("PRINTER_NAME", null);
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("yes", true)]
        [InlineData("on", true)]
        [InlineData("0", false)]
        [InlineData("false", false)]
        [InlineData("FALSE", false)]
        [InlineData("no", false)]
        [InlineData("off", false)]
        public void GetBool_ParsesKnownValues(string raw, bool expected)
        {
            Environment.SetEnvironmentVariable(Key, raw);

            Assert.Equal(expected, PosConfig.GetBool(Key, !expected));
        }

        [Fact]
        public void GetBool_ReturnsFallback_WhenUnset()
        {
            Assert.True(PosConfig.GetBool(Key, true));
            Assert.False(PosConfig.GetBool(Key, false));
        }

        [Fact]
        public void GetBool_ReturnsFallback_ForUnrecognizedValue()
        {
            Environment.SetEnvironmentVariable(Key, "maybe");

            Assert.True(PosConfig.GetBool(Key, true));
            Assert.False(PosConfig.GetBool(Key, false));
        }

        [Fact]
        public void Get_ReturnsTrimmedValue()
        {
            Environment.SetEnvironmentVariable(Key, "  spaced  ");

            Assert.Equal("spaced", PosConfig.Get(Key));
        }

        [Fact]
        public void Get_ReturnsNull_WhenUnset()
        {
            Assert.Null(PosConfig.Get(Key));
            Assert.Equal("fallback", PosConfig.Get(Key, "fallback"));
        }

        [Fact]
        public void Get_EnvironmentValueWinsOverAppSettings()
        {
            Environment.SetEnvironmentVariable("POS_TEST_FROM_CONFIG", "from-env");

            Assert.Equal("from-env", PosConfig.Get("POS_TEST_FROM_CONFIG"));
        }

        [Fact]
        public void Get_FallsBackToAppSettings_WhenEnvironmentUnset()
        {
            Environment.SetEnvironmentVariable("POS_TEST_FROM_CONFIG", null);

            Assert.Equal("app-config-value", PosConfig.Get("POS_TEST_FROM_CONFIG"));
        }

        [Fact]
        public void SyncToEnvironment_CopiesSupportedAppSettings()
        {
            Environment.SetEnvironmentVariable("POS_TEST_FROM_CONFIG", null);

            PosConfig.SyncToEnvironment();

            Assert.Equal("app-config-value", Environment.GetEnvironmentVariable("POS_TEST_FROM_CONFIG"));
        }

        [Fact]
        public void SyncToEnvironment_DoesNotOverrideExistingProcessVariable()
        {
            Environment.SetEnvironmentVariable("POS_TEST_FROM_CONFIG", "keep-me");

            PosConfig.SyncToEnvironment();

            Assert.Equal("keep-me", Environment.GetEnvironmentVariable("POS_TEST_FROM_CONFIG"));
        }

        [Fact]
        public void SyncToEnvironment_IgnoresUnsupportedKeys()
        {
            Environment.SetEnvironmentVariable("UNRELATED_SETTING", null);

            PosConfig.SyncToEnvironment();

            Assert.Null(Environment.GetEnvironmentVariable("UNRELATED_SETTING"));
        }
    }
}
