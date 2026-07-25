using System.IO;
using System.Threading;
using Core.Extensions;
using Core.Levels;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Runtime.Infrastructure.Levels
{
    public sealed class JsonLevelRepository : ILevelRepository
    {
        private const string FileName = "levels.json";

        private readonly string _filePath = Path.Combine(Application.persistentDataPath, FileName);

        public async UniTask<LevelCatalog> LoadAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string json = await UniTask.RunOnThreadPool(EnsureFileAndRead, cancellationToken: ct);

            ct.ThrowIfCancellationRequested();

            LevelCatalog catalog = JsonUtility.FromJson<LevelCatalog>(json);
            catalog.Validate();
            
            return catalog;
        }

        private string EnsureFileAndRead()
        {
            string directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            if (!File.Exists(_filePath))
            {
                LevelCatalog defaultCatalog = DefaultLevelCatalogFactory.Create();
                string defaultJson = JsonUtility.ToJson(defaultCatalog, true);

                File.WriteAllText(_filePath, defaultJson);
            }

            return File.ReadAllText(_filePath);
        }
    }
}
