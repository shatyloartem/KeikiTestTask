using System.Collections.Generic;
using Runtime.Domain.Levels;

namespace Runtime.Infrastructure.Storage.Levels
{
    public static class DefaultLevelCatalogFactory
    {
        private const string LetterShapeAddress = "level-icons/letter-a";
        private const string NumberShapeAddress = "level-icons/number-1";
        private const string CircleShapeAddress = "level-icons/shape-circle";
        private const string LetterGeometryAddress = "trace/routes/letter-a";
        private const string NumberGeometryAddress = "trace/routes/number-1";
        private const string ShapeGeometryAddress = "trace/routes/shape-circle";

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
            "#E81615",
            "#FFA500",
            "#FAEA36",
            "#79C315",
            "#497DE7",
            "#4B359D",
            "#70359D"
        };

        public static LevelCatalog Create()
        {
            GameplayDefinition gameplay = new(
                "trace/shared/mascot",
                "trace/shared/star",
                "trace/shared/helper-finger",
                new List<string>
                {
                    "audio/praise/awesome",
                    "audio/praise/excellent",
                    "audio/praise/thats-good"
                },
                1f,
                7f,
                14f,
                2.5f);

            List<LevelCategory> categories = new()
            {
                CreateCategory(
                    "letters",
                    "Trace letters",
                    "audio/instruction/letter",
                    "A",
                    LetterShapeAddress,
                    LetterGeometryAddress),
                CreateCategory(
                    "numbers",
                    "Trace numbers",
                    "audio/instruction/number",
                    "1",
                    NumberShapeAddress,
                    NumberGeometryAddress),
                CreateCategory(
                    "shapes",
                    "Trace shapes",
                    "audio/instruction/number",
                    "O",
                    CircleShapeAddress,
                    ShapeGeometryAddress)
            };

            return new LevelCatalog(2, gameplay, categories);
        }

        private static LevelCategory CreateCategory(
            string categoryId,
            string title,
            string instructionAudioAddress,
            string symbol,
            string spriteAddress,
            string geometryAddress)
        {
            List<LevelDefinition> levels = new(Colors.Length);

            for (int i = 0; i < Colors.Length; i++)
            {
                levels.Add(new LevelDefinition(
                    $"{categoryId}-{ColorNames[i]}",
                    symbol,
                    Colors[i],
                    spriteAddress,
                    geometryAddress));
            }

            return new LevelCategory(
                categoryId,
                title,
                instructionAudioAddress,
                levels);
        }
    }
}
