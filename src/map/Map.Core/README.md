# Map.Core: техническое устройство

`Map.Core` превращает доменные `Location` в детерминированный снимок карты, проверяет и сериализует его, а затем при необходимости накладывает результат на `Plot`. Это описание реализации `v1`, а не справочник API.

Краткий сценарий находится в [../README.md](../README.md), а граница с Unity - в [../DETAILED_GUIDE.md](../DETAILED_GUIDE.md).

## Граница сборки

Проект собирается для `net8.0` и `netstandard2.1` и использует `BaseClasses` и стабильный MessagePack `3.1.12`. Core использует `IElement`, `Plot`, `Relation`, `ParamBag` и `ParamKey<T>`, но не использует `LocationGenerator`, `UnityEngine`, сеть, файловую систему или `Assets/Scripts`.

Это дает два свойства:

- генерацию, валидацию и MessagePack v1 можно запускать вне Unity;
- `netstandard2.1` DLL можно вложить в UPM-пакет без ссылки Unity на исходный проект.

При изменении публичной модели или MessagePack нужно пересобрать и заменить DLL в `../Packages/com.ang-gpo.map.unity/Runtime/Plugins`; иначе Unity продолжит компилироваться против старого контракта. Для MessagePack `3.1.12` это не только `MessagePack.dll`: копируется полный matching runtime closure из `obj/project.assets.json` для `.NETStandard,Version=v2.1` — `MessagePack.Annotations.dll`, `Microsoft.NET.StringTools.dll`, `System.Collections.Immutable.dll`, `Microsoft.Bcl.AsyncInterfaces.dll`, `System.Buffers.dll`, `System.IO.Pipelines.dll`, `System.Memory.dll`, `System.Numerics.Vectors.dll`, `System.Runtime.CompilerServices.Unsafe.dll`, `System.Text.Encodings.Web.dll`, `System.Text.Json.dll` и `System.Threading.Tasks.Extensions.dll` — вместе с `Map.Core.dll`, `BaseClasses.dll` и `LocationGenerator.dll`. Используйте runtime assets, а не ref assets, и сохраняйте соответствующие Unity `.meta` файлы.

## Два представления карты

```text
Plot (изменяемая доменная модель)
  Elements: реальные IElement
  Relations: реальные Relation
              ^
              | ApplyTo
              |
MapSnapshot (самодостаточные данные)
  MapId, размеры, seed, версия
  биомы, cells, decorations, placements, roads
```

`MapSnapshot` хранит GUID локаций, но не ссылки на `IElement`. Поэтому он переносим между процессами и сохраняем как MessagePack v1 `byte[]`. `ApplyTo` находит элементы в `Plot` по GUID, записывает им параметры и материализует дороги как `Relation`.

Snapshot не получает ссылок на Plot и не меняется при применении. Один и тот же объект можно безопасно сериализовать, рендерить и применять повторно в другом `Plot` с совместимыми ID.

## Модель и инварианты

| Тип | Данные | Локальная гарантия |
| --- | --- | --- |
| `MapDimensions` | `Width`, `Height` | Оба значения положительны. |
| `GridPosition` | `X`, `Y` | Любые `int`; границы проверяет snapshot. |
| `BiomeId` | `Value` | Строка не пуста и не состоит из whitespace. |
| `LocationPlacement` | ID, позиция, биом | ID локации не пуст. |
| `Road` | ID, endpoint ID, `GridPath` | ID не пусты; путь в полном конструкторе непуст. |

Допустимая клетка определяется единственной формулой:

```csharp
position.X >= 0 && position.X < Width &&
position.Y >= 0 && position.Y < Height
```

Таким образом, карта `4 x 3` содержит `X = 0..3` и `Y = 0..2`.

`MapSnapshot` копирует все входные коллекции в новые `List<T>` и оборачивает их в `Array.AsReadOnly`. Внешний код не может изменить snapshot через исходный список. Конструктор проверяет только базовые условия; межобъектные правила проверяет `MapSnapshotValidator`.

### Полная проверка

Валидатор работает за `O(N + R + P)`, где N - число placement-ов, R - число дорог, P - суммарное число точек путей. Он использует hash set для ID биомов, локаций, клеток и дорог, поэтому большинство поисков амортизированно `O(1)`.

Он отклоняет:

1. Версию, отличную от поддерживаемой `"v1"`.
2. Пустой или повторяющийся биом.
3. Placement вне карты, с повторным ID, в уже занятой клетке или со ссылкой на неизвестный биом.
4. Дорогу с повторным ID, endpoint-ом вне набора placements и decorations, пустым путём или точкой вне карты.

В `v1` не проверяется непрерывность `GridPath` и совпадение его крайних точек с endpoint-ами. Контракт гарантирует наличие и границы геометрии, но не навязывает конкретный поиск пути. Если это станет правилом, его следует добавить в validator вместе с изменением версии контракта.

## Генерация: от Plot к placement-ам

`MapGenerator.Generate(plot, options)` не меняет `Plot`. Он возвращает новый `MapGenerationResult` с полным snapshot: terrain cells, decorations, story-location placements и маршрутами дорог. Placement учитывает активные явные `BaseRelationKeys.Passage` между story-локациями как предпочтение соседства; disconnected components сохраняют детерминированное placement без такого предпочтения.

### Стабильный вход

Из `plot.Elements` выбираются только `ElemType.Location` и сортируются по GUID:

```csharp
var locations = plot.Elements
    .Where(e => e.Type == ElemType.Location)
    .OrderBy(e => e.Id)
    .ToList();
```

`Plot.Elements` - `HashSet<IElement>`, у которого нет бизнес-порядка. Сортировка устраняет зависимость от порядка вставки и обхода hash set. Генератор отдельно требует непустые и уникальные ID: внешняя реализация `IElement` может иметь правила равенства, отличные от ID.

Затем проверяется `locations.Count <= Width * Height` в checked-контексте. Переполнение произведения не превращается тихо в неверное число клеток.

### Выбор координат

Все клетки сначала материализуются в row-major порядке:

```text
(0,0), (1,0), ... (Width-1,0), (0,1), ...
```

После этого выполняется Fisher-Yates shuffle с состоянием `uint`:

```text
state = state * 1664525 + 1013904223
j = state mod (i + 1)
swap(cells[i], cells[j])
```

Начальное состояние выводится из seed с фиксированным смещением `0x9E3779B9`. Переполнение `uint` намеренное и делает последовательность одинаковой на всех поддерживаемых платформах. Первые N клеток выдаются N отсортированным локациям.

Следствие: размещение не использует системный RNG, не зависит от порядка `Plot` и не может дать двум placement-ам одну клетку. Это воспроизводимый, а не криптографический генератор.

Память этого этапа составляет `O(Width * Height)`: список клеток хранится целиком. Замена на алгоритм без полной материализации потенциально изменит распределение и должна рассматриваться как изменение алгоритма.

### Шум и выбор биома

Для каждой выбранной клетки вызывается:

```csharp
var value = noise.Sample(x * 0.37d + 0.17d, y * 0.37d + 0.29d);
var biomeIndex = Math.Min(biomes.Count - 1,
    (int)Math.Floor(value * biomes.Count));
```

`ImprovedPerlinNoise` строит перестановку `0..255` тем же LCG и дублирует её в массив из 512 элементов. Дублирование упрощает доступ к соседним индексам на краях периода 256.

В `Sample` целая часть координат выбирает клетку шума, дробная часть - точку внутри неё. Полином `Fade(t) = 6t^5 - 15t^4 + 10t^3` сглаживает переходы; четыре градиента смешиваются двумя линейными интерполяциями. Значение из диапазона около `[-1, 1]` переводится в `[0, 1]` и явно ограничивается.

Коэффициент `0.37` задает пространственную частоту, а смещения `0.17`/`0.29` не дают первым строкам и колонкам постоянно попадать ровно на границу шума. Эти числа - часть поведения `v1`.

## Версия как часть данных

`MapGenerationOptions` отвергает неизвестную версию на входе генератора, а `MapSnapshotValidator` повторяет проверку на границах MessagePack, применения и рендера. Это не дублирование ради страховки:

- генератор не может создать snapshot, который Core далее сам отвергнет;
- внешний или старый payload не может незаметно попасть в новый pipeline.

Изменение shuffle, формулы шума, правил выбора биома или трактовки дороги не должно молча менять `v1`. Если результаты станут другими, вводится новая версия, иначе прежний seed начнет обозначать другую карту.

## MessagePack v1: изоляция модели от формата

`MapSnapshotSerializer` преобразует snapshot во внутренние конкретные DTO с простыми GUID, строками, числами и массивами. Он не сериализует record-ы напрямую. `Serialize` возвращает `byte[]`, а `Deserialize` принимает `byte[]`. Внешний envelope использует числовой ключ 0 для версии формата `1` и ключ 1 для DTO snapshot; все остальные DTO используют фиксированные числовые ключи. Поток при чтении таков:

```text
MessagePack v1 bytes -> envelope -> SnapshotDto -> value objects -> MapSnapshot -> Validate -> результат
```

При записи validation выполняется до построения байтов. При чтении пустой, поврежденный, содержащий хвостовые байты или несовместимый payload отклоняется с `MapValidationException`; `null` у полей или коллекций и доменно недопустимые данные после создания value objects отклоняются тем же actionable исключением.

Core не открывает файлы и не выбирает место хранения. Файловая система, база, миграции и политика ключей принадлежат хосту; serializer отвечает только за бинарный контракт snapshot. Поддерживается только MessagePack v1.

## Demo: полный v1-снимок на диске

`Map.Core.Demo` — минимальный .NET-хост без Unity. Он генерирует полный v1 snapshot (cells, decorations, placements и roads), проверяет его, записывает MessagePack, читает эти же байты, повторно проверяет восстановленный snapshot и сравнивает все данные, включая точки путей дорог.

```bash
dotnet run --project src/map/Map.Core.Demo/Map.Core.Demo.csproj -- /tmp/ang-gpo-map-v1.msgpack
```

Единственный необязательный аргумент — путь результата `.msgpack`; без него demo использует временный файл `map-core-demo-v1.msgpack`. В summary выводятся путь, размер бинарного payload и количество cells, decorations, placements и roads. Для сохранения и загрузки используйте только MessagePack v1.

## Применение: как меняется Plot

`MapGenerationResult.ApplyTo(plot, mode)` сначала проводит подготовительную фазу, а только потом меняет объекты.

### Подготовительная фаза

До первой записи выполняются:

1. Проверка `mode` и всего snapshot.
2. Построение `Dictionary<Guid, IElement>` из `plot.Elements`; повторные ID запрещаются.
3. Проверка каждого placement: элемент существует, имеет тип `Location`, а в `RejectConflicts` у него нет ключа namespace этой карты.
4. Построение ожидаемых relations дорог и поиск конфликтующих ключей.

Это дает практически атомарное поведение для ошибок входа: отсутствующая локация, неверный тип, конфликт или плохая дорога обнаруживаются до изменения параметров. Метод не реализует общий rollback для низкоуровневых исключений `ParamBag`/`HashSet`, но после подготовки он записывает уже проверенные значения в типизированные ключи.

### Параметры

`MapParams.For(mapId)` создает namespace `Map/<map-id в формате D>` и пять ключей:

```text
Position          ParamKey<GridPosition>
Biome             ParamKey<BiomeId>
MapId             ParamKey<Guid>
Seed              ParamKey<int>
AlgorithmVersion  ParamKey<string>
```

`ParamBag` внутренне хранит `IParamKey -> object?`, но `Add` и `Set` сверяют значение с типом ключа. Локальный `SetOrAdd` обновляет существующий ключ или добавляет новый, не позволяя записать координаты строкой либо seed объектом неверного типа.

### Режимы применения

`RejectConflicts` - режим по умолчанию. Любой параметр того же map namespace на целевой локации запрещает применение. Это предотвращает наложение нового snapshot поверх старого с тем же `MapId`.

`ReplaceMap` сначала собирает все relation текущего map namespace, затем удаляет этот namespace у **всех** элементов Plot, а не только у placement-ов нового снимка. После этого он удаляет старые relation и создает новые. Поэтому локации и дороги, исчезнувшие из нового snapshot, не сохраняют устаревшие следы. Namespace других карт не затрагиваются.

## Дороги: хранение и проекция

`Road.GridPath` - источник истины для геометрии. Путь сериализуется точка за точкой, а Unity рисует именно эти точки. Core не восстанавливает маршрут по endpoint-ам: это гарантирует точный round-trip и отделяет сохраненную карту от будущих изменений pathfinding. При генерации road pair возникает только из активной явной `BaseRelationKeys.Passage` между двумя размещёнными story-локациями; containment, `Entrance`, proximity и legacy namespaced keys игнорируются.

`RoadGenerationPolicy.PreferredMaximumTerrainHeight` — мягкий порог: terrain выше него остаётся доступным, но получает дополнительную стоимость. Поэтому дорога может пересечь высокий terrain, если это лучший доступный путь.

Для интеграции с `Plot.Relations` создается каноническая relation только когда оба endpoint-а дороги — story locations в `Plot`:

```text
source: endpoint с меньшим GUID
target: endpoint с большим GUID
key:    Map/<map-id>.Road/<road-id>
value:  road-id (Guid)
```

Нормализация source/target нужна, потому что `Relation` направлена. A-B и B-A должны проецироваться одинаково. `RoadProjectionValidator` требует ровно одну relation, совпадающую по source, target, ключу и значению.

Дороги к decoration-ам остаются визуальными данными snapshot: Unity рисует их по `GridPath`, но они не проецируются в `Plot.Relations`. Это сравнение значения существенно: equality `Relation` в `BaseClasses` учитывает source, target и ключ, но не `Value`. Наличие relation в `HashSet` само по себе не доказывает, что она относится к нужному `RoadId`.

## Подготовка отдельных локаций

`MapLocationService.PlaceExistingLocation` проверяет, что объект - `Location`, присутствует в Plot ровно один раз и имеет placement в snapshot. Затем он записывает контекст карты: позицию, биом, MapId, seed и версию.

`PrepareLocation` и `PrepareLocationAsync` делают то же перед передачей объекта внешнему `IGenerator`. После вызова обязательна `ReferenceEquals(prepared, generated)`: генератор должен вернуть тот же объект, а не создать замену. Иначе разорвется связь между ID placement-а и существующим экземпляром `Plot`. Эти методы не добавляют элемент в Plot.

## Масштабирование и изменения

- Генерация: `O(W * H + L log L)` и `O(W * H)` памяти.
- Валидация: `O(N + R + P)`.
- Применение: `O(E + N + R)` плюс удаление namespace в `ReplaceMap`.
- MessagePack: линейно по всем данным, особенно по количеству точек `GridPath`.

Хранение полного пути дороги увеличивает MessagePack payload пропорционально числу точек, зато полностью убирает восстановление маршрута при загрузке. `Map.Core.Benchmarks` измеряет этот компромисс отдельно от runtime-кода.

При изменении Core сначала определите, затрагивает ли оно данные snapshot. Для несовместимого поведения добавляйте версию алгоритма и тесты, а не меняйте `v1` молча. Затем проверьте оба target framework и обновите Unity DLL:

```bash
dotnet test src/map/Map.Core.Tests/Map.Core.Tests.csproj --framework net8.0
dotnet restore src/map/Map.Core/Map.Core.csproj -p:TargetFramework=netstandard2.1 -m:1
dotnet build src/base/BaseClasses/BaseClasses.csproj --framework netstandard2.1 -p:LangVersion=latest
dotnet build src/LocationGenerator/LocationGenerator.csproj --framework netstandard2.1 -p:LangVersion=latest
dotnet build src/map/Map.Core/Map.Core.csproj --framework netstandard2.1 -p:LangVersion=latest
```

## Полный workflow и Unity smoke test

Release-путь всегда использует один complete snapshot: Core генерирует его из `Plot`, host сохраняет байты в `.msgpack`, новый host загружает те же байты и Unity рендерит сохранённые cells, features и `GridPath` дорог. При необходимости после генерации можно отдельно вызвать `MapGenerationResult.ApplyTo`; это опциональная story-only проекция в `Plot`, а не часть сохранения или рендера.

Перед проверкой в Unity обновите `Map.Core.dll`, `BaseClasses.dll` и `LocationGenerator.dll` в `../Packages/com.ang-gpo.map.unity/Runtime/Plugins` из сборок `netstandard2.1`; при изменении MessagePack обновите также весь checked-in runtime closure, перечисленный выше. Затем в Unity откройте `Assets/Scenes/MapGeneratorDemo.unity`, вызовите `Generate And Save Sample`, создайте/используйте fresh host с тем же `SnapshotPath` и вызовите `Load And Render`. На сцене должны быть одна текстурная surface карты, decoration и location instances/labels, а каждая дорога должна повторять сохранённый `GridPath`.
