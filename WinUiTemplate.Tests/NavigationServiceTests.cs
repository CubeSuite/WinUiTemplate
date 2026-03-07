using CommunityToolkit.Mvvm.ComponentModel;
using FluentAssertions;
using WinUiTemplate.Services;
using Xunit;

namespace WinUiTemplate.Tests
{
    public class NavigationServiceTests
    {
        // Helper Classes

        private class TestViewModel : ObservableObject
        {
            public string Name { get; set; } = "";
        }

        // Tests

        #region Navigate Method Tests

        [Fact]
        public void Navigate_InvokesNavigationRequestedEvent_WhenAllowNavigationIsTrue() {
            var navigationService = new NavigationService();
            var viewModel = new TestViewModel { Name = "TestPage" };
            ObservableObject? capturedViewModel = null;

            navigationService.NavigationRequested += (vm) => capturedViewModel = vm;

            navigationService.Navigate(viewModel);

            capturedViewModel.Should().NotBeNull();
            capturedViewModel.Should().BeSameAs(viewModel);
        }

        [Fact]
        public void Navigate_DoesNotInvokeNavigationRequestedEvent_WhenAllowNavigationIsFalse() {
            var navigationService = new NavigationService();
            var viewModel = new TestViewModel { Name = "TestPage" };
            bool eventInvoked = false;

            navigationService.AllowNavigation = false;
            navigationService.NavigationRequested += (vm) => eventInvoked = true;

            navigationService.Navigate(viewModel);

            eventInvoked.Should().BeFalse();
        }

        [Fact]
        public void Navigate_PassesCorrectViewModel_ToEvent() {
            var navigationService = new NavigationService();
            var viewModel1 = new TestViewModel { Name = "Page1" };
            var viewModel2 = new TestViewModel { Name = "Page2" };
            ObservableObject? lastNavigatedViewModel = null;

            navigationService.NavigationRequested += (vm) => lastNavigatedViewModel = vm;

            navigationService.Navigate(viewModel1);
            (lastNavigatedViewModel as TestViewModel)?.Name.Should().Be("Page1");

            navigationService.Navigate(viewModel2);
            (lastNavigatedViewModel as TestViewModel)?.Name.Should().Be("Page2");
        }

        [Fact]
        public void Navigate_CanBeCalledMultipleTimes() {
            var navigationService = new NavigationService();
            var viewModel1 = new TestViewModel { Name = "Page1" };
            var viewModel2 = new TestViewModel { Name = "Page2" };
            var viewModel3 = new TestViewModel { Name = "Page3" };
            int navigationCount = 0;

            navigationService.NavigationRequested += (vm) => navigationCount++;

            navigationService.Navigate(viewModel1);
            navigationService.Navigate(viewModel2);
            navigationService.Navigate(viewModel3);

            navigationCount.Should().Be(3);
        }

        [Fact]
        public void Navigate_DoesNotThrow_WhenNoSubscribers() {
            var navigationService = new NavigationService();
            var viewModel = new TestViewModel { Name = "TestPage" };

            Action act = () => navigationService.Navigate(viewModel);

            act.Should().NotThrow();
        }

        [Fact]
        public void Navigate_InvokesMultipleSubscribers() {
            var navigationService = new NavigationService();
            var viewModel = new TestViewModel { Name = "TestPage" };
            int subscriber1Count = 0;
            int subscriber2Count = 0;
            int subscriber3Count = 0;

            navigationService.NavigationRequested += (vm) => subscriber1Count++;
            navigationService.NavigationRequested += (vm) => subscriber2Count++;
            navigationService.NavigationRequested += (vm) => subscriber3Count++;

            navigationService.Navigate(viewModel);

            subscriber1Count.Should().Be(1);
            subscriber2Count.Should().Be(1);
            subscriber3Count.Should().Be(1);
        }

        #endregion

        #region AllowNavigation Property Tests

        [Fact]
        public void AllowNavigation_DefaultsToTrue() {
            var navigationService = new NavigationService();

            navigationService.AllowNavigation.Should().BeTrue();
        }

        [Fact]
        public void AllowNavigation_CanBeSetToFalse() {
            var navigationService = new NavigationService();

            navigationService.AllowNavigation = false;

            navigationService.AllowNavigation.Should().BeFalse();
        }

        [Fact]
        public void AllowNavigation_CanBeSetToTrue() {
            var navigationService = new NavigationService();
            navigationService.AllowNavigation = false;

            navigationService.AllowNavigation = true;

            navigationService.AllowNavigation.Should().BeTrue();
        }

        [Fact]
        public void AllowNavigation_CanBeToggledMultipleTimes() {
            var navigationService = new NavigationService();

            navigationService.AllowNavigation = false;
            navigationService.AllowNavigation.Should().BeFalse();

            navigationService.AllowNavigation = true;
            navigationService.AllowNavigation.Should().BeTrue();

            navigationService.AllowNavigation = false;
            navigationService.AllowNavigation.Should().BeFalse();
        }

        [Fact]
        public void AllowNavigation_WhenSetToFalse_BlocksNavigation() {
            var navigationService = new NavigationService();
            var viewModel = new TestViewModel { Name = "TestPage" };
            int navigationCount = 0;

            navigationService.NavigationRequested += (vm) => navigationCount++;
            navigationService.Navigate(viewModel);

            navigationService.AllowNavigation = false;
            navigationService.Navigate(viewModel);
            navigationService.Navigate(viewModel);

            navigationCount.Should().Be(1, "only the first navigation should have been processed");
        }

        [Fact]
        public void AllowNavigation_WhenSetBackToTrue_AllowsNavigationAgain() {
            var navigationService = new NavigationService();
            var viewModel = new TestViewModel { Name = "TestPage" };
            int navigationCount = 0;

            navigationService.NavigationRequested += (vm) => navigationCount++;

            navigationService.Navigate(viewModel);
            navigationCount.Should().Be(1);

            navigationService.AllowNavigation = false;
            navigationService.Navigate(viewModel);
            navigationCount.Should().Be(1);

            navigationService.AllowNavigation = true;
            navigationService.Navigate(viewModel);
            navigationCount.Should().Be(2);
        }

        #endregion

        #region Event Subscription Tests

        [Fact]
        public void NavigationRequested_CanBeSubscribedTo() {
            var navigationService = new NavigationService();
            bool eventSubscribed = false;

            Action<ObservableObject> handler = (vm) => eventSubscribed = true;
            navigationService.NavigationRequested += handler;

            navigationService.Navigate(new TestViewModel());

            eventSubscribed.Should().BeTrue();
        }

        [Fact]
        public void NavigationRequested_CanBeUnsubscribedFrom() {
            var navigationService = new NavigationService();
            int eventCount = 0;

            Action<ObservableObject> handler = (vm) => eventCount++;
            navigationService.NavigationRequested += handler;
            navigationService.Navigate(new TestViewModel());
            eventCount.Should().Be(1);

            navigationService.NavigationRequested -= handler;
            navigationService.Navigate(new TestViewModel());
            eventCount.Should().Be(1, "event should not be invoked after unsubscribing");
        }

        [Fact]
        public void NavigationRequested_SupportsMultipleSubscribersIndependently() {
            var navigationService = new NavigationService();
            int handler1Count = 0;
            int handler2Count = 0;

            Action<ObservableObject> handler1 = (vm) => handler1Count++;
            Action<ObservableObject> handler2 = (vm) => handler2Count++;

            navigationService.NavigationRequested += handler1;
            navigationService.NavigationRequested += handler2;

            navigationService.Navigate(new TestViewModel());
            handler1Count.Should().Be(1);
            handler2Count.Should().Be(1);

            navigationService.NavigationRequested -= handler1;
            navigationService.Navigate(new TestViewModel());
            handler1Count.Should().Be(1);
            handler2Count.Should().Be(2);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void NavigationService_SupportsCompleteWorkflow() {
            var navigationService = new NavigationService();
            var page1 = new TestViewModel { Name = "Page1" };
            var page2 = new TestViewModel { Name = "Page2" };
            var navigationHistory = new System.Collections.Generic.List<string>();

            navigationService.NavigationRequested += (vm) => {
                if (vm is TestViewModel testVm) {
                    navigationHistory.Add(testVm.Name);
                }
            };

            // Navigate to page 1
            navigationService.Navigate(page1);
            navigationHistory.Should().ContainSingle().Which.Should().Be("Page1");

            // Navigate to page 2
            navigationService.Navigate(page2);
            navigationHistory.Should().HaveCount(2);
            navigationHistory[1].Should().Be("Page2");

            // Disable navigation
            navigationService.AllowNavigation = false;
            navigationService.Navigate(page1);
            navigationHistory.Should().HaveCount(2, "navigation should be blocked");

            // Re-enable navigation
            navigationService.AllowNavigation = true;
            navigationService.Navigate(page1);
            navigationHistory.Should().HaveCount(3);
            navigationHistory[2].Should().Be("Page1");
        }

        [Fact]
        public void NavigationService_CanBeCreatedWithoutDependencies() {
            Action act = () => new NavigationService();

            act.Should().NotThrow();
        }

        #endregion
    }
}
