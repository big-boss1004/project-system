﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public sealed class MiscellaneousRuleTests : XamlRuleTestBase
    {
        [Theory]
        [MemberData(nameof(GetAllDisplayedRules))]
        public void NonVisiblePropertiesShouldntBeLocalized(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            foreach (var property in GetProperties(rule))
            {
                if (!IsVisible(property) && Name(property) is not ("SplashScreen" or "MinimumSplashScreenDisplayTime"))
                {
                    AssertAttributeNotPresent(property, "DisplayName");
                    AssertAttributeNotPresent(property, "Description");
                    AssertAttributeNotPresent(property, "Category");
                }
            }

            static void AssertAttributeNotPresent(XElement element, string attributeName)
            {
                Assert.True(
                    element.Attribute(attributeName) is null,
                    userMessage: $"{attributeName} should not be present:\n{element}");
            }
        }

        [Theory]
        [MemberData(nameof(GetAllDisplayedRules))]
        public void VisiblePropertiesMustHaveDisplayName(string ruleName, string fullPath)
        {
            // The "DisplayName" property is localised, while "Name" is not.
            // Visible properties without a "DisplayName" will appear in English in all locales.

            XElement rule = LoadXamlRule(fullPath);

            foreach (var property in GetProperties(rule))
            {
                if (IsVisible(property))
                {
                    string? displayName = property.Attribute("DisplayName")?.Value;

                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        throw new Xunit.Sdk.XunitException($"""
                            Rule {ruleName} has visible property {property.Attribute("Name")} with no DisplayName value.
                            """);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void PropertyDescriptionMustEndWithFullStop(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath, out XmlNamespaceManager namespaceManager);

            foreach (var property in GetProperties(rule))
            {
                // <Rule>
                //   <StringProperty>
                //     <StringProperty.ValueEditors>
                //       <ValueEditor EditorType="LinkAction">

                var linkActionEditors = property.XPathSelectElements(@"./msb:StringProperty.ValueEditors/msb:ValueEditor[@EditorType=""LinkAction""]", namespaceManager);

                if (linkActionEditors.Any())
                {
                    // LinkAction items use the description in hyperlink or button text.
                    // Neither of these needs to end with a full stop.
                    continue;
                }

                string? description = property.Attribute("Description")?.Value;

                if (description?.EndsWith(".") == false)
                {
                    throw new Xunit.Sdk.XunitException($"""
                        Rule {ruleName} has visible property {property.Attribute("Name")} with description not ending in a period.
                        """);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void RuleMustHaveAName(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            string? name = rule.Attribute("Name")?.Value;

            Assert.NotNull(name);
            Assert.NotEqual("", name);
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void TargetResultsDataSourcesMustSpecifyTheTarget(string ruleName, string fullPath)
        {
            var root = LoadXamlRule(fullPath, out var namespaceManager);

            var dataSource = root.XPathSelectElement(@"/msb:Rule/msb:Rule.DataSource/msb:DataSource", namespaceManager);

            var sourceType = dataSource?.Attribute("SourceType");
            var msBuildTarget = dataSource?.Attribute("MSBuildTarget");

            if (sourceType is not null)
            {
                if (sourceType.Value == "TargetResults")
                {
                    // A target must be specified
                    Assert.NotNull(msBuildTarget);
                }
                else
                {
                    // Target must not be specified on other source types
                    Assert.Null(msBuildTarget);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void ItemDataSourcesMustSpecifyTheItemType(string ruleName, string fullPath)
        {
            var root = LoadXamlRule(fullPath, out var namespaceManager);

            var dataSource = root.XPathSelectElement(@"/msb:Rule/msb:Rule.DataSource/msb:DataSource", namespaceManager);

            var sourceType = dataSource?.Attribute("SourceType")?.Value;
            var itemType = dataSource?.Attribute("ItemType");

            if (sourceType == "Item")
            {
                // An item type must be specified
                Assert.NotNull(itemType);
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void PropertiesDataSourcesMustMatchItemDataSources(string ruleName, string fullPath)
        {
            var root = LoadXamlRule(fullPath, out var namespaceManager);

            // Get the top-level data source element
            var dataSource = root.XPathSelectElement(@"/msb:Rule/msb:Rule.DataSource/msb:DataSource", namespaceManager);

            // If the top level defines an ItemType, all properties must specify a matching ItemType.
            var ruleItemType = dataSource?.Attribute("ItemType")?.Value;

            if (ruleItemType is not null)
            {
                foreach (var property in GetProperties(root))
                {
                    var element = GetDataSource(property);

                    var propertyItemType = element?.Attribute("ItemType")?.Value;
                    if (propertyItemType is not null)
                    {
                        Assert.True(
                            StringComparer.Ordinal.Equals(ruleItemType, propertyItemType),
                            $"""Property data source has item type '{propertyItemType}' but the rule data source has item type '{ruleItemType} which does not match'.""");
                    }
                }
            }
        }

        public static IEnumerable<object[]> GetMiscellaneousRules()
        {
            return Project(GetRules(""));
        }

        public static IEnumerable<object[]> GetAllDisplayedRules()
        {
            return GetMiscellaneousRules()
                .Concat(ItemRuleTests.GetBrowseObjectItemRules())
                .Concat(DependencyRuleTests.GetDependenciesRules())
                .Concat(ProjectPropertiesLocalizationRuleTests.GetPropertyPagesRules());
        }

        public static IEnumerable<object[]> GetAllRules()
        {
            return GetMiscellaneousRules()
                .Concat(ItemRuleTests.GetItemRules())
                .Concat(DependencyRuleTests.GetDependenciesRules())
                .Concat(ProjectPropertiesLocalizationRuleTests.GetPropertyPagesRules());
        }

        private static bool IsVisible(XElement property)
        {
            // Properties are visible by default
            string visibleValue = property.Attribute("Visible")?.Value ?? bool.TrueString;

            Assert.True(bool.TryParse(visibleValue, out bool isVisible));

            return isVisible;
        }

        private static string Name(XElement rule)
        {
            return rule.Attribute("Name")?.Value ?? throw new Xunit.Sdk.XunitException($"Rule must have a name.\n{rule}");
        }
    }
}
