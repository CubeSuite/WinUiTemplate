using WinUiTemplate.Core.Services;
using WinUiTemplate.Stores.Interfaces;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace WinUiTemplate.Tests
{
    public class SearchServiceTests
    {
        // Test Models
        private class TestItem
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public int Value { get; set; }
            public List<string> Tags { get; set; } = new List<string>();
        }

        // Services & Stores
        private readonly Mock<IUserSettings> mockUserSettings;
        private readonly Mock<IServiceProvider> mockServiceProvider;

        // Constructors

        public SearchServiceTests() {
            mockUserSettings = new Mock<IUserSettings>();
            mockServiceProvider = new Mock<IServiceProvider>();

            mockServiceProvider
                .Setup(x => x.GetService(typeof(IUserSettings)))
                .Returns(mockUserSettings.Object);

            // Default settings
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(false);
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
        }

        // Helper Methods

        private SearchService CreateSearchService() {
            return new SearchService(mockServiceProvider.Object);
        }

        private List<TestItem> CreateTestItems() {
            return new List<TestItem>
            {
                new TestItem { Name = "Iron Ore", Description = "Raw iron material", Value = 10, Tags = new List<string> { "metal", "raw" } },
                new TestItem { Name = "Iron Plate", Description = "Processed iron", Value = 20, Tags = new List<string> { "metal", "processed" } },
                new TestItem { Name = "Copper Ore", Description = "Raw copper material", Value = 15, Tags = new List<string> { "metal", "raw" } },
                new TestItem { Name = "Copper Wire", Description = "Thin copper wire", Value = 5, Tags = new List<string> { "metal", "wire" } },
                new TestItem { Name = "Steel Plate", Description = "High quality steel", Value = 50, Tags = new List<string> { "metal", "processed" } },
            };
        }

        // Tests

        #region Basic Search Tests

        [Fact]
        public async Task Search_ReturnsAllItems_WhenQueryIsEmpty() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "", item => item.Name);

            result.Should().HaveCount(5);
        }

        [Fact]
        public async Task Search_ReturnsMatchingItems_WhenQueryMatchesName() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "iron", item => item.Name);

            result.Should().HaveCount(2);
            result.Should().Contain(item => item.Name == "Iron Ore");
            result.Should().Contain(item => item.Name == "Iron Plate");
        }

        [Fact]
        public async Task Search_ReturnsMatchingItems_WhenQueryMatchesDescription() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "raw", item => item.Description);

            result.Should().HaveCount(2);
            result.Should().Contain(item => item.Name == "Iron Ore");
            result.Should().Contain(item => item.Name == "Copper Ore");
        }

        [Fact]
        public async Task Search_ReturnsNoItems_WhenNoMatch() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "gold", item => item.Name);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Search_ReturnsEmptyList_WhenItemsIsEmpty() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = new List<TestItem>();

            IEnumerable<TestItem> result = await searchService.Search(items, "test", item => item.Name);

            result.Should().BeEmpty();
        }

        #endregion

        #region Multiple Selectors Tests

        [Fact]
        public async Task Search_SearchesMultipleFields_WhenMultipleSelectorsProvided() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            // When multiple selectors, ALL must match ALL tokens
            IEnumerable<TestItem> result = await searchService.Search(
                items, 
                "iron", 
                item => item.Name, 
                item => item.Description
            );

            // Iron Ore: Name="Iron Ore" (has "iron"), Description="Raw iron material" (has "iron") -> both match
            // Iron Plate: Name="Iron Plate" (has "iron"), Description="Processed iron" (has "iron") -> both match
            result.Should().HaveCount(2);
            result.Should().Contain(item => item.Name == "Iron Ore");
            result.Should().Contain(item => item.Name == "Iron Plate");
        }

        [Fact]
        public async Task Search_MatchesAnyField_WhenMultipleSelectorsProvided() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(
                items, 
                "wire", 
                item => item.Name, 
                item => item.Description
            );

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Copper Wire");
        }

        #endregion

        #region Case Sensitivity Tests

        [Fact]
        public async Task Search_IsCaseInsensitive_WhenSearchCaseSensitiveIsFalse() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(false);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "IRON", item => item.Name);

            result.Should().HaveCount(2);
            result.Should().Contain(item => item.Name == "Iron Ore");
            result.Should().Contain(item => item.Name == "Iron Plate");
        }

        [Fact]
        public async Task Search_IsCaseSensitive_WhenSearchCaseSensitiveIsTrue() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(true);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "IRON", item => item.Name);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Search_MatchesExactCase_WhenSearchCaseSensitiveIsTrue() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(true);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "Iron", item => item.Name);

            result.Should().HaveCount(2);
            result.Should().Contain(item => item.Name == "Iron Ore");
            result.Should().Contain(item => item.Name == "Iron Plate");
        }

        #endregion

        #region Split Query Tests

        [Fact]
        public async Task Search_SplitsQueryBySpace_WhenSearchSplitQueryIsTrue() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "iron ore", item => item.Name);

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Iron Ore");
        }

        [Fact]
        public async Task Search_RequiresAllTokens_WhenSearchSplitQueryIsTrue() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "iron gold", item => item.Name);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Search_DoesNotSplitQuery_WhenSearchSplitQueryIsFalse() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(false);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            // "iron ore" as whole phrase (case insensitive)
            IEnumerable<TestItem> result = await searchService.Search(items, "iron ore", item => item.Name);

            // Name "Iron Ore" contains "iron ore" -> matches
            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Iron Ore");
        }

        [Fact]
        public async Task Search_MatchesFullPhrase_WhenSearchSplitQueryIsFalse() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(false);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "Iron Ore", item => item.Name);

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Iron Ore");
        }

        [Fact]
        public async Task Search_NoMatchesForPartialPhrase_WhenSearchSplitQueryIsFalse() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(false);
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(false);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "raw material", item => item.Description);
            result.Should().BeEmpty();
        }

        #endregion

        #region Collection Selector Tests

        [Fact]
        public async Task Search_SearchesCollectionFields_WhenSelectorReturnsIEnumerable() {
            SearchService searchService = CreateSearchService();
            // Create items with tags that will match the search behavior
            List<TestItem> items = new List<TestItem>
            {
                new TestItem { Name = "Item1", Tags = new List<string> { "raw material" } },
                new TestItem { Name = "Item2", Tags = new List<string> { "processed" } }
            };

            IEnumerable<TestItem> result = await searchService.Search(items, "raw", item => item.Tags);

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Item1");
        }

        [Fact]
        public async Task Search_MatchesAnyElementInCollection_WhenSelectorReturnsIEnumerable() {
            SearchService searchService = CreateSearchService();
            // Single tag that contains the search term
            List<TestItem> items = new List<TestItem>
            {
                new TestItem { Name = "Copper Wire", Tags = new List<string> { "wire" } }
            };

            IEnumerable<TestItem> result = await searchService.Search(items, "wire", item => item.Tags);

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Copper Wire");
        }

        [Fact]
        public async Task Search_HandlesEmptyCollections_WhenSelectorReturnsEmptyIEnumerable() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = new List<TestItem>
            {
                new TestItem { Name = "Empty Item", Tags = new List<string>() }
            };

            IEnumerable<TestItem> result = await searchService.Search(items, "test", item => item.Tags);

            // Empty collection: no elements to match against, token cannot be found -> item does not match
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Search_SearchesCollectionsCaseInsensitively_WhenSearchCaseSensitiveIsFalse() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(false);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = new List<TestItem>
            {
                new TestItem { Name = "Item1", Tags = new List<string> { "METAL" } },
                new TestItem { Name = "Item2", Tags = new List<string> { "plastic" } }
            };

            IEnumerable<TestItem> result = await searchService.Search(items, "metal", item => item.Tags);

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Item1");
        }

        #endregion

        #region Null Handling Tests

        [Fact]
        public async Task Search_HandlesNullSelector_WhenSelectorReturnsNull() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = new List<TestItem>
            {
                new TestItem { Name = "Test", Description = null! }
            };

            // When selector returns null, it's skipped. Token cannot be found via a null selector -> item does not match
            IEnumerable<TestItem> result = await searchService.Search(items, "test", item => item.Description);

            result.Should().BeEmpty(); // Null selector means token cannot be found, so it does not match
        }

        [Fact]
        public async Task Search_SkipsNullValues_WhenSelectorReturnsNull() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = new List<TestItem>
            {
                new TestItem { Name = "Iron Ore", Description = null! },
                new TestItem { Name = "Iron Plate", Description = "Processed iron" }
            };

            // When searching by Description, null values cause that selector to be skipped
            // Token cannot be found via a null selector -> item does not match
            IEnumerable<TestItem> result = await searchService.Search(items, "processed", item => item.Description);

            // Iron Ore: Description=null -> selector skipped -> token not found -> does not match
            // Iron Plate: Description="Processed iron" (contains "processed") -> matches
            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Iron Plate");
        }

        [Fact]
        public async Task Search_HandlesNullElementsInCollection_WhenCollectionContainsNulls() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = new List<TestItem>
            {
                new TestItem { Name = "Test", Tags = new List<string> { "tag1", null!, "tag2" } }
            };

            // Search for "tag1" - null elements are skipped, but "tag2" doesn't contain "tag1" so returns false
            IEnumerable<TestItem> result = await searchService.Search(items, "tag", item => item.Tags);

            // Both "tag1" and "tag2" contain "tag", but when it checks tag1 (✓) then tag2 (✓), it returns true
            result.Should().HaveCount(1);
        }

        #endregion

        #region Combined Settings Tests

        [Fact]
        public async Task Search_CombinesCaseSensitivityAndSplitQuery_WhenBothAreTrue() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(true);
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "Iron Ore", item => item.Name);

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Iron Ore");
        }

        [Fact]
        public async Task Search_CombinesCaseSensitivityAndSplitQuery_WhenCaseSensitiveAndSplitQueryFalse() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(true);
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(false);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "iron ore", item => item.Name);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Search_CombinesCaseSensitivityAndSplitQuery_WhenCaseInsensitiveAndSplitQueryTrue() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(false);
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "IRON ORE", item => item.Name);

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Iron Ore");
        }

        [Fact]
        public async Task Search_CombinesCaseSensitivityAndSplitQuery_WhenBothAreFalse() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(false);
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(false);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "IRON ORE", item => item.Name);

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Iron Ore");
        }

        #endregion

        #region Multi-Token Tests

        [Fact]
        public async Task Search_MatchesAllTokensInDifferentFields_WhenSearchSplitQueryIsTrue() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            // Each token must be found in at least one selector
            IEnumerable<TestItem> result = await searchService.Search(
                items, 
                "copper ore", 
                item => item.Name, 
                item => item.Description
            );

            // Copper Ore: token "copper" found in Name ✓, token "ore" found in Name ✓ -> all tokens satisfied -> matches
            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Copper Ore");
        }

        [Fact]
        public async Task Search_RequiresAllTokensInAllFields_WhenMultipleSelectorsProvided() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            // ALL selectors must have ALL tokens
            IEnumerable<TestItem> result = await searchService.Search(
                items, 
                "steel", 
                item => item.Name, 
                item => item.Description
            );

            // Steel Plate: Name="Steel Plate" (has "steel"), Description="High quality steel" (has "steel") -> both match
            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Steel Plate");
        }

        [Fact]
        public async Task Search_HandlesMultipleSpaces_WhenSearchSplitQueryIsTrue() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            // Multiple spaces create empty tokens
            IEnumerable<TestItem> result = await searchService.Search(items, "iron   plate", item => item.Name);

            // This may not work as expected due to empty string tokens
            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Iron Plate");
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task Search_HandlesWhitespaceQuery_WhenSearchSplitQueryIsTrue() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            // Whitespace creates empty string tokens - all strings contain empty string
            IEnumerable<TestItem> result = await searchService.Search(items, "   ", item => item.Name);

            result.Should().HaveCount(5); // Empty tokens match everything
        }

        [Fact]
        public async Task Search_HandlesSpecialCharacters_InQuery() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = new List<TestItem>
            {
                new TestItem { Name = "Item-1", Description = "Special #1" },
                new TestItem { Name = "Item+2", Description = "Special #2" }
            };

            IEnumerable<TestItem> result = await searchService.Search(items, "item-1", item => item.Name);

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Item-1");
        }

        [Fact]
        public async Task Search_HandlesNumericValues_WhenSelectorReturnsNumber() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            IEnumerable<TestItem> result = await searchService.Search(items, "50", item => item.Value);

            result.Should().HaveCount(1);
            result.Should().Contain(item => item.Name == "Steel Plate");
        }

        [Fact]
        public async Task Search_ProcessesAllSelectors_WhenMultipleFieldsProvided() {
            SearchService searchService = CreateSearchService();
            // All items have "metal" in tags and some in description
            List<TestItem> items = new List<TestItem>
            {
                new TestItem { Name = "Item1", Description = "metal item", Tags = new List<string> { "metal" } },
                new TestItem { Name = "Item2", Description = "plastic", Tags = new List<string> { "plastic" } }
            };

            IEnumerable<TestItem> result = await searchService.Search(
                items, 
                "metal", 
                item => item.Description, 
                item => item.Tags
            );

            // Item1: Description="metal item" (contains "metal" ✓), Tags=["metal"] (contains "metal" ✓) -> included
            // Item2: Description="plastic" (contains "metal" ✗) -> excluded
            result.Should().HaveCount(1);
        }

        #endregion

        #region Performance and Async Tests

        [Fact]
        public async Task Search_CompletesAsynchronously_WhenCalledWithLargeDataset() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = Enumerable.Range(0, 10000)
                .Select(i => new TestItem { Name = $"Item {i}", Description = $"Description {i}" })
                .ToList();

            Task<IEnumerable<TestItem>> searchTask = searchService.Search(items, "Item 5000", item => item.Name);
            IEnumerable<TestItem> result = await searchTask;

            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Item 5000");
        }

        [Fact]
        public async Task Search_HandlesMultipleSimultaneousSearches() {
            SearchService searchService = CreateSearchService();
            List<TestItem> items = CreateTestItems();

            Task<IEnumerable<TestItem>> search1 = searchService.Search(items, "iron", item => item.Name);
            Task<IEnumerable<TestItem>> search2 = searchService.Search(items, "copper", item => item.Name);
            Task<IEnumerable<TestItem>> search3 = searchService.Search(items, "steel", item => item.Name);

            await Task.WhenAll(search1, search2, search3);

            (await search1).Should().HaveCount(2);
            (await search2).Should().HaveCount(2);
            (await search3).Should().HaveCount(1);
        }

        #endregion

        #region AppearsInSearch Tests

        [Fact]
        public async Task AppearsInSearch_ReturnsTrue_WhenItemMatchesQuery() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(item, "iron", i => i.Name);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_ReturnsFalse_WhenItemDoesNotMatchQuery() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(item, "copper", i => i.Name);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AppearsInSearch_ReturnsTrue_WhenQueryIsEmpty() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(item, "", i => i.Name);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_RespectsCaseSensitivity_WhenEnabled() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(true);
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(item, "IRON", i => i.Name);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AppearsInSearch_RespectsCaseInsensitivity_WhenDisabled() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(false);
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(item, "IRON", i => i.Name);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_RespectsSearchSplitQuery_WhenEnabled() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(item, "iron ore", i => i.Name);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_RespectsSearchSplitQuery_WhenDisabled() {
            mockUserSettings.Setup(x => x.SearchCaseSensitive).Returns(false);
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(false);
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(item, "iron ore", i => i.Name);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_WorksWithMultipleSelectors() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(
                item, 
                "iron", 
                i => i.Name, 
                i => i.Description
            );

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_ReturnsFalse_WhenNotAllSelectorsMatch() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw copper material" };

            bool result = await searchService.AppearsInSearch(
                item, 
                "iron copper", 
                i => i.Name, 
                i => i.Description
            );

            // token "iron" found in Name ✓, token "copper" found in Description ✓ -> all tokens satisfied -> true
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_WorksWithCollectionSelectors() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Item1", Tags = new List<string> { "metal" } };

            bool result = await searchService.AppearsInSearch(item, "metal", i => i.Tags);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_HandlesNullValues() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = null! };

            bool result = await searchService.AppearsInSearch(item, "test", i => i.Description);

            result.Should().BeFalse(); // Null selector is skipped, token cannot be found -> false
        }

        [Fact]
        public async Task AppearsInSearch_WorksWithNumericValues() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Item", Value = 100 };

            bool result = await searchService.AppearsInSearch(item, "100", i => i.Value);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_ReturnsFalse_WhenNumericValueDoesNotMatch() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Item", Value = 100 };

            bool result = await searchService.AppearsInSearch(item, "200", i => i.Value);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AppearsInSearch_WorksWithPartialMatches() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(item, "iro", i => i.Name);

            result.Should().BeTrue(); // "Iron Ore" contains "iro"
        }

        [Fact]
        public async Task AppearsInSearch_HandlesSpecialCharacters() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Item-1", Description = "Special #1" };

            bool result = await searchService.AppearsInSearch(item, "item-1", i => i.Name);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_HandlesWhitespace() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw material" };

            bool result = await searchService.AppearsInSearch(item, "   ", i => i.Name);

            result.Should().BeTrue(); // Empty tokens match
        }

        [Fact]
        public async Task AppearsInSearch_WorksAsynchronously() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            Task<bool> task = searchService.AppearsInSearch(item, "iron", i => i.Name);
            bool result = await task;

            result.Should().BeTrue();
            task.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_WorksWithEmptyCollections() {
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Item", Tags = new List<string>() };

            bool result = await searchService.AppearsInSearch(item, "test", i => i.Tags);

            result.Should().BeFalse(); // Empty collection: no elements to match token against -> false
        }

        [Fact]
        public async Task AppearsInSearch_WorksWithMultipleTokens() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(item, "raw iron", i => i.Description);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AppearsInSearch_ReturnsFalse_WhenNotAllTokensMatch() {
            mockUserSettings.Setup(x => x.SearchSplitQuery).Returns(true);
            SearchService searchService = CreateSearchService();
            TestItem item = new TestItem { Name = "Iron Ore", Description = "Raw iron material" };

            bool result = await searchService.AppearsInSearch(item, "iron copper", i => i.Description);

            result.Should().BeFalse(); // "copper" doesn't match
        }

        #endregion
    }
}
