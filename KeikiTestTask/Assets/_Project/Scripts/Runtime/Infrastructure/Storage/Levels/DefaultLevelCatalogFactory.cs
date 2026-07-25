using System.Collections.Generic;
using Runtime.Domain.Levels;

namespace Runtime.Infrastructure.Storage.Levels
{
    public static class DefaultLevelCatalogFactory
    {
        private const string LetterIconAddress = "level-icons/letter-a";
        private const string NumberIconAddress = "level-icons/number-1";
        private const string ShapeIconAddress = "level-icons/shape-circle";

        private static readonly string[] ColorNames =
        {
            "red",
            "orange",
            "yellow",
            "green",
            "blue",
            "indigo",
            "violet"
        };

        private static readonly string[] Colors =
        {
            "#F51717",
            "#FFA000",
            "#FFEA2B",
            "#76CA09",
            "#4B7CE8",
            "#5136AD",
            "#7531A8"
        };

        public static LevelCatalog Create()
        {
            List<LevelCategory> categories = new()
            {
                CreateCategory(
                    "letters",
                    "Trace letters",
                    "A",
                    LetterIconAddress),
                CreateCategory(
                    "numbers",
                    "Trace numbers",
                    "1",
                    NumberIconAddress),
                CreateCategory(
                    "shapes",
                    "Trace shapes",
                    "O",
                    ShapeIconAddress)
            };

            return new LevelCatalog(1, categories);
        }

        private static LevelCategory CreateCategory(
            string categoryId,
            string title,
            string symbol,
            string iconAddress)
        {
            List<LevelDefinition> levels = new(Colors.Length);

            for (int i = 0; i < Colors.Length; i++)
            {
                levels.Add(new LevelDefinition(
                    $"{categoryId}-{ColorNames[i]}",
                    symbol,
                    Colors[i],
                    iconAddress));
            }

            return new LevelCategory(categoryId, title, levels);
        }
    }
}
