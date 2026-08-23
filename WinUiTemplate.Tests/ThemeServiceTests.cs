using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Moq;
using Windows.UI;
using WinUiTemplate.Core.Services;
using WinUiTemplate.Core.Stores.Interfaces;

namespace WinUiTemplate.Tests
{
    public class ThemeServiceTests
    {
        private static readonly Color testAccentColour = new Color { A = 255, R = 18, G = 52, B = 86 };

        private readonly Mock<IUserSettings> mockUserSettings = new Mock<IUserSettings>();
        private readonly Mock<IServiceProvider> mockServiceProvider = new Mock<IServiceProvider>();

        public ThemeServiceTests()
        {
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IUserSettings)))
                .Returns(mockUserSettings.Object);

            mockUserSettings.SetupGet(x => x.Theme).Returns(ThemeOption.Light);
            mockUserSettings.SetupGet(x => x.AccentSource).Returns(AccentSourceOption.Custom);
            mockUserSettings.SetupGet(x => x.CustomAccentColour).Returns(testAccentColour);

        }

        private ThemeService CreateThemeService()
        {
            return new ThemeService(mockServiceProvider.Object);
        }

        [Fact]
        public void Constructor_ResolvesUserSettings()
        {
            CreateThemeService();

            mockServiceProvider.Verify(x => x.GetService(typeof(IUserSettings)), Times.Once);
        }

        [Theory]
        [InlineData(ThemeOption.Light, false)]
        [InlineData(ThemeOption.Dark, true)]
        public void DarkMode_ReturnsExpectedValueForExplicitTheme(ThemeOption theme, bool expected)
        {
            mockUserSettings.SetupGet(x => x.Theme).Returns(theme);
            ThemeService themeService = CreateThemeService();

            themeService.DarkMode.Should().Be(expected);
        }

        [Fact(Skip = "Requires the WinUI application runtime")]
        public void ApplyTheme_SetsAllAccentResourcesAndRaisesEvent()
        {
            ThemeService themeService = CreateThemeService();
            int eventCount = 0;
            themeService.ThemeChangeRequested += () => eventCount++;

            themeService.ApplyTheme();

            string[] resourceNames = {
                "SystemAccentColor",
                "SystemAccentColorLight1",
                "SystemAccentColorLight2",
                "SystemAccentColorLight3",
                "SystemAccentColorDark1",
                "SystemAccentColorDark2",
                "SystemAccentColorDark3",
            };

            foreach (string resourceName in resourceNames)
            {
                Application.Current.Resources[resourceName].Should().Be(testAccentColour);
            }

            eventCount.Should().Be(1);
        }

        [Fact(Skip = "Requires the WinUI application runtime")]
        public void ToggleTheme_FromLight_SetsDarkAndRaisesEvent()
        {
            ThemeService themeService = CreateThemeService();
            int eventCount = 0;
            themeService.ThemeChangeRequested += () => eventCount++;

            themeService.ToggleTheme();

            mockUserSettings.VerifySet(x => x.Theme = ThemeOption.Dark, Times.Once);
            eventCount.Should().Be(1);
        }

        [Fact(Skip = "Requires the WinUI application runtime")]
        public void ToggleTheme_FromDark_SetsLightAndRaisesEvent()
        {
            mockUserSettings.SetupGet(x => x.Theme).Returns(ThemeOption.Dark);
            ThemeService themeService = CreateThemeService();
            int eventCount = 0;
            themeService.ThemeChangeRequested += () => eventCount++;

            themeService.ToggleTheme();

            mockUserSettings.VerifySet(x => x.Theme = ThemeOption.Light, Times.Once);
            eventCount.Should().Be(1);
        }

        [Theory(Skip = "Requires the WinUI application runtime")]
        [InlineData(nameof(IUserSettings.Theme))]
        [InlineData(nameof(IUserSettings.Backdrop))]
        [InlineData(nameof(IUserSettings.AccentSource))]
        [InlineData(nameof(IUserSettings.CustomAccentColour))]
        [InlineData(nameof(IUserSettings.WindowTintSource))]
        [InlineData(nameof(IUserSettings.SolidWindowTintColour))]
        [InlineData(nameof(IUserSettings.GradientPreset))]
        [InlineData(nameof(IUserSettings.WindowTintOpacity))]
        public void SettingChanged_ForAppearanceSetting_AppliesTheme(string settingName)
        {
            ThemeService themeService = CreateThemeService();
            int eventCount = 0;
            themeService.ThemeChangeRequested += () => eventCount++;

            mockUserSettings.Raise(x => x.SettingChanged += null, settingName);

            eventCount.Should().Be(1);
        }

        [Fact]
        public void SettingChanged_ForNonAppearanceSetting_DoesNotApplyTheme()
        {
            ThemeService themeService = CreateThemeService();
            int eventCount = 0;
            themeService.ThemeChangeRequested += () => eventCount++;

            mockUserSettings.Raise(x => x.SettingChanged += null, nameof(IUserSettings.DefaultWidth));

            eventCount.Should().Be(0);
        }
    }
}
