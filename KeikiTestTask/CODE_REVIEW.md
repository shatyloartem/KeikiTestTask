# Code review: `feature/level-editor-tool`

Переглянуто фінальний стан гілки відносно `develop`, без оцінювання проміжних реалізацій. Підтверджених блокерів компіляції немає.

## High

| Де | Проблема | Чому варто змінити | Запропонована зміна |
|---|---|---|---|
| `Scripts/Editor/Tracing/TraceRouteEditorWindow.cs:279`, `Domain/Tracing/TraceStrokeDefinition.cs:14` | Зміни knots/strokes через вбудований inspector не запускають bake; baked-поля також доступні для ручного редагування. | Runtime може використати застарілу або пошкоджену геометрію, хоча asset виглядатиме відредагованим. | Сховати baked-дані з inspector, зробити окремий serialized editor і автоматично bake/validate після зміни вихідної геометрії. |
| `UI/Game/Tracing/TraceSurfaceView.cs:56`, `Services/Tracing/Input/TraceInputController.cs:111` | `ScreenToNormalized` повертає `NaN` при помилці та через `Mathf.InverseLerp` затискає позицію поза surface до `[0..1]`; результат не перевіряється. | Drag поза полем може продовжити trace вздовж краю, а невдала конвертація здатна передати `NaN` у progress tracker. | Замінити контракт на `TryScreenToNormalized`, рахувати unclamped координати, відкидати non-finite та позиції поза surface. |
| `Domain/Tracing/TraceGeometryAsset.cs:86` | Валідація не перевіряє всі tuning-параметри, finite coordinates, knots, монотонність cumulative lengths і відповідність останньої довжини `TotalLength`. | Некоректний asset може пройти validation, а потім зависнути, мовчки змінити поведінку через runtime clamp або зламати path sampling. | Додати повну перевірку інваріантів і окремі точні повідомлення для кожного поля/stroke. |

## Medium

| Де | Проблема | Чому варто змінити | Запропонована зміна |
|---|---|---|---|
| `Services/Tracing/Hints/TraceHintController.cs:70,114` | `Stop()` лише змінює прапорець, але цикл `while (true)` його не читає. | Контракт `Stop` самостійно не зупиняє session, helper loop або запущене повторення audio; завершення залежить від зовнішнього token. | Додати внутрішній linked CTS і завершувати/await усі hint-задачі в `Stop`. |
| `Data/Tracing/Trace_A.asset:17-22`, `Domain/Tracing/TraceGeometryAsset.cs:93` | Для `letter-a` видимий trail (`0.24`) ширший за повну допустиму input-зону (`0.141 + 2 × 0.025 = 0.191`); relationship не валідовується. | Дитина може вести палець усередині намальованого stroke, але trace не зарахується. | Визначати hit radius від trail width + tolerance або валідовувати, що corridor покриває видиму ширину. |
| `UI/Game/Tracing/Graphics/TraceTrailGraphic.cs:19`, `TraceSurfaceView.cs:124`, `Input/StrokeProgressTracker.cs:58` | Mascot і tracker стартують з inset, але trail створюється з progress `0` і не синхронізується до першого `Advanced`. | Під час першого руху слід стрибком домальовує весь початковий inset. | Ініціалізувати active trail значенням `firstPointInset` разом із позицією mascot. |
| `Infrastructure/AssetManagement/AddressableGameAssetProvider.cs:18` | Cache не дедуплікує одночасні завантаження одного key; обидва виклики можуть дійти до `Dictionary.Add`. | При паралельному використанні один виклик впаде, а handle буде зайво завантажений. | Кешувати in-flight task/handle під lock або застосувати per-key async gate. |
| `Domain/Levels/LevelCatalog.cs:24`, `Extensions/LevelCatalogValidator.cs:10` | Поле `Version` десеріалізується, але ніколи не перевіряється. | Несумісний формат JSON може бути прийнятий частково й дати неочевидні помилки пізніше. | Ввести supported schema version і відхиляти/мігрувати інші версії при завантаженні. |
| `Editor/Tracing/TraceRouteEditorWindow.cs` | Один клас на 765 рядків поєднує inspector, handles, mesh generation, shader preview, mutation і validation UI. | Код важко безпечно розширювати; зміна preview може зачепити редагування даних. | Розділити window state, scene renderer, mesh builder і geometry editing service. |
| `Editor/Tracing/TraceRouteEditorWindow.cs:305`, `TraceGeometryBaker.cs:9` | Preview показує trail, але не показує фактичний input corridor/tolerance; bake використовує фіксовану дискретизацію. | Editor не дає оцінити головний gameplay-параметр, а складні криві можуть мати нерівномірну похибку. | Додати overlay effective corridor та adaptive subdivision за довжиною/curvature. |

## Low

| Де | Проблема | Чому варто змінити | Запропонована зміна |
|---|---|---|---|
| `Services/Tracing/Flow/GameFlowController.cs:64` | `_runCts` не dispose/reset у `finally`; повторний `RunAsync` перезапише старий source. | Непотрібно утримуються ресурси, а lifecycle повторного запуску нечіткий. | Dispose і зануляти CTS у `finally`, узгодивши це зі `Stop/Dispose`. |
| `Infrastructure/Storage/Levels/DefaultLevelCatalogFactory.cs` | Factory дублює JSON-конфіг, ніде не використовується і може дрейфувати окремо від нього. | Є два джерела правди для адрес, таймінгів і структури рівнів. | Видалити factory або явно використовувати як fallback/test fixture. |
| `Domain/Tracing/TraceGeometryAsset.cs:28,55`, `Input/StrokeProgressTracker.cs:64`, `TraceSampleResult.cs:22` | Є невикористані `ReferenceSize`, `Configure`, `NormalizedProgress` і `Tangent`. | Зайвий API приховує справжній контракт і ускладнює підтримку. | Видалити dead API/data або додати реального споживача. |
| Уся гілка, зокрема `GameInstaller.cs`, `LevelCatalog.cs`, `DefaultLevelCatalogFactory.cs` | Немає `.editorconfig`; у коді є trailing whitespace та змішані CRLF/LF. | Форматування неоднорідне й створює шум у diff. | Додати `.editorconfig`, увімкнути trim/final newline/єдиний EOL та відформатувати project code. |

## Рекомендований порядок

1. Захистити editor → bake → validation pipeline.
2. Виправити перетворення input coordinates та effective corridor.
3. Закрити async lifecycle.
4. Після функціональних змін виконати структурний і форматувальний cleanup.
