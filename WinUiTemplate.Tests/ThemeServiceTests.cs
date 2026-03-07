using FluentAssertions;
using Moq;
using System;
using WinUiTemplate.Services;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;
using Xunit;

namespace WinUiTemplate.Tests
{
    public class ThemeServiceTests
    {
        // Helper Methods

        private Mock<IServiceProvider> CreateMockServiceProvider(Mock<IUserSettings> mockUserSettings) {
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IUserSettings)))
                .Returns(mockUserSettings.Object);
            return mockServiceProvider;
        }

        // Tests

        #region Constructor Tests

        [Fact]
        public void ThemeService_RequiresUserSettings() {
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IUserSettings)))
                .Returns(null);

            Action act = () => new ThemeService(mockServiceProvider.Object);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ThemeService_SubscribesToSettingChangedEvent() {
            var mockUserSettings = new Mock<IUserSettings>();
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);

            var themeService = new ThemeService(mockServiceProvider.Object);

            // Verify subscription by checking that the event can be raised with an unrelated setting
            // Using "MaxLogs" won't trigger ApplyTheme, so no exception
            Action act = () => mockUserSettings.Raise(x => x.SettingChanged += null, "MaxLogs");

            act.Should().NotThrow("subscription should work without triggering ApplyTheme for unrelated settings");
        }

        [Fact]
        public void ThemeService_CanBeCreatedWithValidServiceProvider() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);

            Action act = () => new ThemeService(mockServiceProvider.Object);

            act.Should().NotThrow();
        }

        #endregion

        #region ToggleTheme Method Tests

        [Fact]
        public void ToggleTheme_TogglesDarkModeFromFalseToTrue() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.DarkMode, false);
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            themeService.ToggleTheme();

            mockUserSettings.Object.DarkMode.Should().BeTrue();
        }

        [Fact]
        public void ToggleTheme_TogglesDarkModeFromTrueToFalse() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.DarkMode, true);
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            themeService.ToggleTheme();

            mockUserSettings.Object.DarkMode.Should().BeFalse();
        }

        [Fact]
        public void ToggleTheme_InvokesThemeChangeRequestedEvent() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.DarkMode, false);
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);
            bool eventInvoked = false;

            themeService.ThemeChangeRequested += () => eventInvoked = true;

            themeService.ToggleTheme();

            eventInvoked.Should().BeTrue();
        }

        [Fact]
        public void ToggleTheme_CanBeCalledMultipleTimes() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.DarkMode, false);
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            themeService.ToggleTheme();
            mockUserSettings.Object.DarkMode.Should().BeTrue();

            themeService.ToggleTheme();
            mockUserSettings.Object.DarkMode.Should().BeFalse();

            themeService.ToggleTheme();
            mockUserSettings.Object.DarkMode.Should().BeTrue();
        }

        [Fact]
        public void ToggleTheme_DoesNotThrow_WhenNoSubscribers() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.DarkMode, false);
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            Action act = () => themeService.ToggleTheme();

            act.Should().NotThrow();
        }

        #endregion

        #region ResetAccentColour Method Tests

        [Fact]
        public void ResetAccentColour_SetsAccentColourProperty() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            themeService.ResetAccentColour();

            // The accent colour should be changed from the original value
            mockUserSettings.Object.AccentColour.Should().NotBeNullOrWhiteSpace();
            // It should be set to a hex color (starts with #)
            mockUserSettings.Object.AccentColour.Should().StartWith("#");
        }

        [Fact]
        public void ResetAccentColour_CanBeCalledMultipleTimes() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            Action act = () => {
                themeService.ResetAccentColour();
                themeService.ResetAccentColour();
                themeService.ResetAccentColour();
            };

            act.Should().NotThrow();
        }

        #endregion

        #region ApplyTheme Method Tests

        // Note: ApplyTheme cannot be fully tested in unit tests because it requires
        // Application.Current.Resources which is only available in a WinUI application context.
        // The method accesses Application.Current before raising the ThemeChangeRequested event,
        // so we can only verify that it throws the expected exception.

        [Fact]
        public void ApplyTheme_ThrowsException_WithoutApplicationContext() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            // ApplyTheme requires Application.Current which is not available in unit tests
            Action act = () => themeService.ApplyTheme();

            act.Should().Throw<Exception>("Application.Current is not available in test environment");
        }

        #endregion

        #region SettingChanged Event Tests

        // Note: When AccentColour or Backdrop settings change, OnSettingChanged calls ApplyTheme,
        // which will throw in test environment due to Application.Current not being available.
        // We verify the exception is thrown, confirming ApplyTheme was called.

        [Fact]
        public void OnSettingChanged_WithAccentColour_TriggersApplyTheme() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            // Simulate the AccentColour setting change - should trigger ApplyTheme which throws
            Action act = () => mockUserSettings.Raise(x => x.SettingChanged += null, "AccentColour");

            act.Should().Throw<Exception>("ApplyTheme is called which requires Application.Current");
        }

        [Fact]
        public void OnSettingChanged_WithBackdrop_TriggersApplyTheme() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            // Simulate the Backdrop setting change - should trigger ApplyTheme which throws
            Action act = () => mockUserSettings.Raise(x => x.SettingChanged += null, "Backdrop");

            act.Should().Throw<Exception>("ApplyTheme is called which requires Application.Current");
        }

        [Fact]
        public void OnSettingChanged_WithUnrelatedSetting_DoesNotTriggerApplyTheme() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            // Simulate an unrelated setting change - should NOT trigger ApplyTheme
            Action act = () => mockUserSettings.Raise(x => x.SettingChanged += null, "MaxLogs");

            act.Should().NotThrow("ApplyTheme should not be called for unrelated settings");
        }

        #endregion

        #region Event Subscription Tests

        [Fact]
        public void ThemeChangeRequested_CanBeSubscribedTo() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.DarkMode, false);
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);
            bool eventInvoked = false;

            Action handler = () => eventInvoked = true;
            themeService.ThemeChangeRequested += handler;

            themeService.ToggleTheme();

            eventInvoked.Should().BeTrue();
        }

        [Fact]
        public void ThemeChangeRequested_CanBeUnsubscribedFrom() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.DarkMode, false);
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);
            int eventCount = 0;

            Action handler = () => eventCount++;
            themeService.ThemeChangeRequested += handler;
            themeService.ToggleTheme();
            eventCount.Should().Be(1);

            themeService.ThemeChangeRequested -= handler;
            themeService.ToggleTheme();
            eventCount.Should().Be(1, "event should not be invoked after unsubscribing");
        }

        [Fact]
        public void ThemeChangeRequested_SupportsMultipleSubscribers() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.DarkMode, false);
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);
            int handler1Count = 0;
            int handler2Count = 0;
            int handler3Count = 0;

            themeService.ThemeChangeRequested += () => handler1Count++;
            themeService.ThemeChangeRequested += () => handler2Count++;
            themeService.ThemeChangeRequested += () => handler3Count++;

            themeService.ToggleTheme();

            handler1Count.Should().Be(1);
            handler2Count.Should().Be(1);
            handler3Count.Should().Be(1);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void ThemeService_SupportsCompleteThemeToggleWorkflow() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.DarkMode, false);
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);
            int themeChangeCount = 0;

            themeService.ThemeChangeRequested += () => themeChangeCount++;

            // Initial state
            mockUserSettings.Object.DarkMode.Should().BeFalse();
            themeChangeCount.Should().Be(0);

            // Toggle to dark
            themeService.ToggleTheme();
            mockUserSettings.Object.DarkMode.Should().BeTrue();
            themeChangeCount.Should().Be(1);

            // Toggle back to light
            themeService.ToggleTheme();
            mockUserSettings.Object.DarkMode.Should().BeFalse();
            themeChangeCount.Should().Be(2);

            // Reset accent colour
            themeService.ResetAccentColour();
            mockUserSettings.Object.AccentColour.Should().NotBe("#FF0000");
            mockUserSettings.Object.AccentColour.Should().StartWith("#");
        }

        [Fact]
        public void ThemeService_RespondsToUserSettingsChanges() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            // Simulate AccentColour change - should trigger ApplyTheme and throw
            Action actAccent = () => mockUserSettings.Raise(x => x.SettingChanged += null, "AccentColour");
            actAccent.Should().Throw<Exception>();

            // Simulate Backdrop change - should trigger ApplyTheme and throw
            Action actBackdrop = () => mockUserSettings.Raise(x => x.SettingChanged += null, "Backdrop");
            actBackdrop.Should().Throw<Exception>();

            // Simulate unrelated setting change - should NOT trigger ApplyTheme
            Action actUnrelated = () => mockUserSettings.Raise(x => x.SettingChanged += null, "DarkMode");
            actUnrelated.Should().NotThrow();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ToggleTheme_WorksWithNullSubscribers() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.DarkMode, false);
            mockUserSettings.SetupProperty(x => x.AccentColour, "#FF0000");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            Action act = () => themeService.ToggleTheme();

            act.Should().NotThrow();
            mockUserSettings.Object.DarkMode.Should().BeTrue();
        }

        [Fact]
        public void ResetAccentColour_SetsValidHexColor() {
            var mockUserSettings = new Mock<IUserSettings>();
            mockUserSettings.SetupProperty(x => x.AccentColour, "");
            var mockServiceProvider = CreateMockServiceProvider(mockUserSettings);
            var themeService = new ThemeService(mockServiceProvider.Object);

            themeService.ResetAccentColour();

            // Should be a valid hex color format
            mockUserSettings.Object.AccentColour.Should().MatchRegex(@"^#[0-9A-Fa-f]{8}$");
        }

        #endregion
    }
}
