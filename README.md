# Top.TelegramFramework.Core — Документация

## Краткое описание

**Top.TelegramFramework.Core** — лёгкий фреймворк для построения Telegram‑ботов на основе концепции *сценариев* и *блоков‑обработчиков* (HandlerBlock). Проект строится вокруг:

- **Scenario / ScenarioSelector** — набор сценариев, каждый сценарий содержит набор блоков,
один из которых выделяется как стартовый, он будет обрабатывать первое сообщение пользователя.
После обработки он может передать управление другому блоку (через `Return.End("nextBlockId")`,
с возможностью передать дополнительную информацию для следующего блока через `Return.End("nextBlockId", Dictionary<string, object>? data)`).
- **HandlerBlock / Block<TState>** — базовая еденица, которая обрабатывает апдейты (сообщения, callback query) и хранит состояние (через дженерик‑параметр TState - специальный класс, поля которого сохраняються между сообщениями пользователя).
- **BotEngine** — ядро, запускается как `IHostedService`, получает апдейты (polling), загружает/сохраняет состояние через `IStateStore` и маршрутизирует апдейты в нужный блок.
- **IStateStore / EfStateStore** — абстракция и одна реализация хранения состояний пользователей (Entity Framework Core).

Документация ниже: сначала быстрый «README» для разработчика, затем подробное описание каждого компонента, и в конце — пошаговый анализ алгоритма `HandleUpdateAsync` (ядро обработки апдейтов).

---

## Создание блока

```csharp
[Block("welcome")]
public class WelcomeBlock : Block<WelcomeState>
{
    public override string BlockId => "welcome";

    public override async Task EnterAsync(BlockContext ctx, CancellationToken ct)
    {
        await ctx.ReplyAsync("Добро пожаловать! Пожалуйста, введите своё имя:");
    }

    public override async Task<HandlerBlockResult> HandleAsync(Message message, BlockContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
            return Return.Error("Имя не может быть пустым");

        State.Count++;
        await ctx.ReplyAsync($"Твоё имя: {message.Text}.\nТы бежишь в этом колесе {State.Count} круг!");

        // Сохраняем состояние и повторно вызываем EnterAsync (без End)
        return Return.Continue(reEnter: true);
    }
}

public class WelcomeState
{
    public long Count { get; set; } = 0;
}
```

## Program.cs 
```csharp
class Program
    {
        public static async Task Main(string[] args)
        {
            var token = AppData.TelegramToken;
            if (string.IsNullOrWhiteSpace(token) || token.Contains("<YOUR"))
            {
                Console.WriteLine("Please set AppData.TelegramToken before running.");
                return;
            }

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging((ctx, lb) => lb.AddConsole())
                .ConfigureServices((ctx, services) =>
                {
                    services.AddDbContext<UserStateContext>(opts => opts.UseSqlite("Data Source=userstates.db"));
                    services.AddScoped<IStateStore, EfStateStore>();

                    // Автоматическая регистрация фреймворка
                    services.AddTelegramFramework(options =>
                    {
                        options.Token = token;

                        // 1) Default сценарий: регистрируем только блоки из namespace WelcomeBlock (и под-namespace'ов)
                        options.AddScenario<WelcomeBlock>("default", predicate: null, isDefault: true, onlyNamespace: true);
                    });
                })
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<UserStateContext>();
                ctx.Database.EnsureCreated();
            }

            await host.RunAsync();
        }
    }
```
## Как работает хранение состояния
- После каждого успешного `HandleAsync` (если блок вернул `Continue`) фреймворк вызывает `CaptureState()` и сохраняет JSON состояния вместе с `ContextBag` в `IStateStore`.
- При `End` — `OnEnd()` вызывается и состояние удаляется (`stateStore.DeleteAsync`). Если `End` содержит `NextBlockId`, движок автоматически создаст и `EnterAsync` следующего блока и сохранит его состояние.

---

# Подробное описание компонентов

## HandlerBlock (Blocks/HandlerBlock.cs)
Базовый абстрактный класс для логики. Методы, которые можно переопределять:
- `string BlockId { get; }` — уникальный идентификатор блока в сценарии.
- `Task EnterAsync(BlockContext context, CancellationToken ct)` — вызывается при входе в блок.
- `Task<HandlerBlockResult> HandleAsync(Message message, BlockContext context, CancellationToken ct)` — обработка входящих текстовых сообщений/апдейтов.
- `Task<HandlerBlockResult> HandleCallbackAsync(CallbackQuery callbackQuery, BlockContext context, CancellationToken ct)` — обработка callback query.
- `void OnEnd()` — синхронный хук, вызывается при завершении блока.
- `void ApplyState(string? stateJson)` / `string? CaptureState()` — сериализация/десериализация состояния блока (если нужно хранить).

`HandlerBlock` — минимальный контракт; удобнее наследоваться от `Block<TState>`.

## Block<TState> (Blocks/Block.cs)
Удобный базовый класс, который хранит типизированное состояние `TState` и реализует ApplyState/CaptureState через System.Text.Json.

- `public TState State { get; private set; } = new TState();`
- `ApplyState` десериализует JSON в `State` (если есть), `CaptureState` сериализует `State` в JSON.

## BlockAttribute (Blocks/BlockAttribute.cs)
Простой атрибут `[Block("id")]`, используемый при регистрации сценариев: фреймворк сканирует типы и регистрирует блоки по `BlockAttribute.BlockId`.

## BlockContext (Blocks/BlockContext.cs)
Контекст выполнения блока, который инжектируется при вызове `EnterAsync/HandleAsync`:
- `ITelegramBotClient? Client` — клиент Telegram.
- `long ChatId` — идентификатор чата.
- `int? CallbackMessageId` — id сообщения (если апдейт — callback).
- `string? ScenarioId` — идентификатор сценария.
- `ILogger Logger` — логгер для слова блока.
- `Dictionary<string, object> ContextBag` — словарь для хранения вспомогательных значений между вызовами (сохраняется вместе со стейтом).

В `BlockContext` есть вспомогательные методы для отправки/редактирования сообщений и ответа на callback.

## HandlerBlockResult (Blocks/HandlerBlockResult.cs) и Utilities/Return.cs
`HandlerBlockResult` описывает результат обработки:
- `IsContinue` — продолжить в текущем блоке (стейт сохранён, блок остаётся текущим).
- `IsError` — произошла ошибка; фреймворк отправит сообщение `Ошибка: {ErrorMessage}` и не будет менять состояние.
- `IsEnd` — блок завершён; состояние удаляется; при наличии `NextBlockId` будет переход на следующий блок.

Вспомогательный класс `Return` содержит фабрики `Return.Continue/End/Error`.

## Scenario (Scenario.cs)
Сценарий — набор блоков и начальный блок. API:
- `Scenario(string id)` — создаёт сценарий.
- `RegisterBlock(string blockId, Type type)` — регистрирует тип блока.
- `RegisterBlocksFromAssembly(Assembly)` — ищет классы с `[Block("id")]` и добавляет их.
- `SetInitialBlock(Type type)` / `RegisterInitialBlockType(Type)` — задаёт начальный блок для сценария.
- `GetBlockType(string blockId)` — возвращает `Type` по `blockId`.
- `GetRegisteredBlockTypes()` — полезно для регистрации типов в DI.

## ScenarioSelector (ScenarioSelector.cs)
Позволяет выбрать сценарий для конкретного chatId. Вы регистрируете правила `Register(Scenario, Func<long,bool>)` и дефолтный сценарий `SetDefault(Scenario)`. Метод `GetScenarioForUser(long chatId)` возвращает первый подходящий сценарий или дефолтный (если не задан — бросается исключение).

## IStateStore / EfStateStore / UserStateContext (Data/*)
`IStateStore` — интерфейс с методами `GetAsync`, `SaveAsync`, `DeleteAsync`.
`EfStateStore` — реализация на EF Core, использует `UserStateContext` и `UserStateEntity`.

**UserState** (DTO) содержит `CurrentBlockId`, `StateJson` и `Dictionary<string, object> Context`.

`UserStateContext` конфигурирует `UserStateEntity` key как `(ChatId, ScenarioId)`.

---

# Подробный разбор `BotEngine.HandleUpdateAsync` (алгоритм)

Ниже — поэтапный пошаговый анализ работы метода `HandleUpdateAsync`, важные детали и предосторожности.

> Примечание: код находится в `BotEngine.cs`.

## Входные данные
Поступает `Update update` от Telegram и `ITelegramBotClient botClient`.

### 1) Определение `chatId` и `logUser`
- Если апдейт содержит `Message message` — `chatId = message.Chat.Id`, `logUser` — username/firstName/ID.
- Иначе, если апдейт содержит `CallbackQuery callback` и `callback.Message != null` — `chatId = callback.Message.Chat.Id`, `logUser` — callback.From.
- Если ни того ни другого — `return` (апдейт игнорируется).

### 2) Выбор сценария
- Используется `ScenarioSelector.GetScenarioForUser(chatId)` — если правило срабатывает, берётся соответствующий сценарий; если ничего не найдено, используется default (если default не задан — исключение).

### 3) Создание Scope и получение сервисов
- Создаётся `IServiceScope scope = _provider.CreateScope()`.
- Из scope получаем `IStateStore`, `ILoggerFactory`.

### 4) Загрузка состояния из `IStateStore` (GetAsync)
- `var stored = await stateStore.GetAsync(chatId, scenarioId, ct);`

### 5) Если состояния нет — инициализация начального блока
- Определяется `initialType = scenario.InitialBlockType` — если не задано, бросается `InvalidOperationException`.
- Создаётся экземпляр начального блока (через DI `scope.ServiceProvider.GetService(initialType)` или `ActivatorUtilities.CreateInstance`).
- Формируется `BlockContext initCtx` с клиентом, chatId, logger и пустым `ContextBag`.
- Вызывается `await initBlock.EnterAsync(initCtx, ct)`.
- Сохраняется `initBlock.CaptureState()` и `initCtx.ContextBag` в `stateStore.SaveAsync(...)` с `initBlock.BlockId` как текущим блоком.
- **После инициализации обработка апдейта завершается (return)** — поведение согласовано с старой версией фреймворка.

**Зачем так:** это позволяет корректно инициализировать вступительное сообщение / клавиатуру и не пытаться обрабатывать первоначальный апдейт как вход в блок.

### 6) Если состояние есть — восстановление блока
- `blockId = stored.CurrentBlockId`.
- Если `blockId == null` — используем `scenario.InitialBlockType`.
- Получаем `blockType = scenario.GetBlockType(blockId)` — если не найдено, бросаем исключение.
- Создаём/резолвим экземпляр блока из DI / ActivatorUtilities.
- `block.ApplyState(stored.StateJson)` — восстанавливаем состояние.
- Создаём `BlockContext ctx` и заполняем `ContextBag = stored.Context`.

### 7) Вызов обработчика
- Если `message != null` — вызываем `result = await block.HandleAsync(message, ctx, ct);`
- Иначе если `callback != null` — `result = await block.HandleCallbackAsync(callback, ctx, ct);`

> Если `result == null` — логируем предупреждение и выходим.

### 8) Обработка результата `HandlerBlockResult`

#### HandlerBlockResultState.IsError
- Логируем предупреждение и отправляем пользователю текст `Ошибка: {ErrorMessage}`.
- Обработку на этом завершаем (не меняем блок).

#### HandlerBlockResultState.IsContinue
- Сохраняем текущую сериализацию блока `var saveJson = block.CaptureState()` в `stateStore.SaveAsync(...)` вместе с `ctx.ContextBag`.
- Если `result.ReEnter == true`:
  - Используется ключ `__reenter_count` в `ContextBag` как счётчик, чтобы не допустить бесконечного re-enter цикла.
  - Если `count >= 3` — логируем и прекращаем попытки re-enter для этого апдейта.
  - Иначе: увеличиваем счётчик, вызываем `await block.EnterAsync(ctx, ct)`, затем снова сохраняем состояние `block.CaptureState()`.
  - В `finally` счётчик уменьшается на 1 (если >0).

**Замечание:** `ReEnter` позволяет, на пример, после обработки сообщения немедленно перепоместить пользователя в начальное состояние блока (или пересоздать клавиатуру) без завершения сценария.

#### HandlerBlockResultState.IsEnd
- Вызывается `block.OnEnd()` (синхронно).
- Удаляется состояние (`await stateStore.DeleteAsync(chatId, scenarioId, ct);`).
- Если `result.NextBlockId` задан:
  - Находится `nextType = scenario.GetBlockType(result.NextBlockId)` — если не найдено — бросается `InvalidOperationException`.
  - Создаётся новый блок `nextBlock` и `nextBlock.ApplyState(null)` (новый state).
  - Создаётся `BlockContext nextCtx` и вызывается `await nextBlock.EnterAsync(nextCtx, ct)`.
  - Состояние `nextBlock.CaptureState()` и `nextCtx.ContextBag` сохраняется через `stateStore.SaveAsync(...)` с `nextBlock.BlockId`.

### 9) В конце: ответ на callback query
- Если `callback != null` — `await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: ct);`.

### 10) Обработка исключений
- Внешний `try/catch` вокруг всего тела логирует ошибку через `ILoggerFactory` из scope.

---

# Рекомендации и замечания по использованию

1. **DI vs ActivatorUtilities**: фреймворк пытается получить блок из DI, и если не зарегистрирован — создаёт через `ActivatorUtilities.CreateInstance`, что позволяет использовать конструкцию с зависимостями, но для корректного использования стоит зарегистрировать блоки в DI (ServiceCollectionExtensions делает это автоматически для полученных типов).

2. **Контекст и сериализация**: `ContextBag` хранит `Dictionary<string, object>` и сериализуется/десериализуется через `System.Text.Json` в `EfStateStore`. Будьте осторожны с типами (лучше хранить простые значения и примитивы).

3. **ReEnter**: полезная механика, но лимит по умолчанию — 3. Если ваши блоки вызывают `ReEnter` многократно, риск бесконечного цикла минимизирован, но всё ещё следите за логикой.

4. **Параллельность**: движок запускает `HandleUpdateAsync` для каждого апдейта независимо (нельзя полагаться на атомарность между апдейтами одного чата). Если ожидается высокий параллелизм апдейтов для одного `chatId`, подумайте о внешней синхронизации/блокировке (например, optimistic concurrency в базе или mutex на уровне chatId).

5. **Ошибки и возвраты**: если блок возвращает `IsError`, фреймворк уведомит пользователя простым текстом. При более сложной обработке ошибок переопределите поведение в `HandleAsync/EnterAsync` и возвращайте подробные сообщения.

6. **Тестирование**: можно мокать `IStateStore` и `ITelegramBotClient` и вызывать `HandleUpdateAsync` вручную (через разрешение `BotEngine` в тестовом `IServiceProvider`).

---

