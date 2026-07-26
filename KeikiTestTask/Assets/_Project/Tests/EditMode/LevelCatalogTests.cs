using NUnit.Framework;
using Runtime.Domain.Levels;
using Runtime.Domain.Levels.Extensions;
using Runtime.Services.Levels;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class LevelCatalogTests
    {
        [Test]
        public void BundledJsonContainsThreeCategoriesAndTwentyOneLevels()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Configs/levels.json");
            Assert.That(json, Is.Not.Null);

            LevelCatalog catalog = JsonUtility.FromJson<LevelCatalog>(json.text);
            Assert.DoesNotThrow(catalog.Validate);
            Assert.That(catalog.Categories, Has.Count.EqualTo(3));

            int levelCount = 0;

            foreach (LevelCategory category in catalog.Categories)
                levelCount += category.Levels.Count;

            Assert.That(levelCount, Is.EqualTo(21));
        }

        [Test]
        public void LastLevelWrapsToFirstLevelOfSameCategory()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Configs/levels.json");
            LevelCatalog catalog = JsonUtility.FromJson<LevelCatalog>(json.text);
            LevelCategory category = catalog.Categories[0];
            LevelContext current = new(
                category,
                category.Levels[^1],
                category.Levels.Count - 1);
            LevelSequenceService service = new();

            LevelContext next = service.GetNext(catalog, current);

            Assert.That(next.Category.Id, Is.EqualTo(category.Id));
            Assert.That(next.Index, Is.Zero);
            Assert.That(next.Level.Id, Is.EqualTo(category.Levels[0].Id));
        }
    }
}
