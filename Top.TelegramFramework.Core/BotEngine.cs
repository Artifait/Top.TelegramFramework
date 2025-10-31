// File: Top.TelegramFramework.Core/BotEngine.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Top.TelegramFramework.Core.Blocks;
using Top.TelegramFramework.Core.Data;

namespace Top.TelegramFramework.Core
{
    // BotEngine запускается как IHostedService, обрабатывает апдейты, использует DI для блоков и IStateStore
    public class BotEngine : IHostedService, IDisposable
    {
        private readonly ILogger<BotEngine> _logger;
        private readonly IServiceProvider _provider;
        private readonly ScenarioSelector _scenarioSelector;
        private readonly ITelegramBotClient _client;
        private CancellationTokenSource? _cts;

        public BotEngine(ILogger<BotEngine> logger, IServiceProvider provider, ScenarioSelector scenarioSelector, ITelegramBotClient client)
        {
            _logger = logger;
            _provider = provider;
            _scenarioSelector = scenarioSelector;
            _client = client;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var receiverOptions = new ReceiverOptions();
            _client.StartReceiving(
                updateHandler: new DefaultUpdateHandler(
                    (botClient, update, token) => HandleUpdateAsync(botClient, update, token),
                    (botClient, exception, token) =>
                    {
                        _logger.LogError(exception, "Telegram polling error");
                        return Task.CompletedTask;
                    }
                ),
                receiverOptions: receiverOptions,
                cancellationToken: _cts.Token
            );

            _logger.LogInformation("BotEngine started");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            _logger.LogInformation("BotEngine stopping");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts?.Dispose();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update == null) return;

            try
            {
                long chatId;
                string scenarioId;
                string logUser;
                Message? message = update.Message;
                var callback = update.CallbackQuery;

                if (message != null)
                {
                    chatId = message.Chat.Id;
                    logUser = message.From?.Username ?? message.From?.FirstName ?? chatId.ToString();
                }
                else if (callback?.Message != null)
                {
                    chatId = callback.Message.Chat.Id;
                    logUser = callback.From?.Username ?? callback.From?.FirstName ?? chatId.ToString();
                }
                else return;

                var scenario = _scenarioSelector.GetScenarioForUser(chatId) ?? throw new InvalidOperationException("Default scenario not set");
                scenarioId = scenario.ScenarioId;

                using var scope = _provider.CreateScope();
                var stateStore = scope.ServiceProvider.GetRequiredService<IStateStore>();
                var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("BotEngine.Update");

                var stored = await stateStore.GetAsync(chatId, scenarioId, ct);

                // Если нет сохранённого состояния — это первый заход, вызываем EnterAsync стартового блока и сохраняем состояние
                if (stored == null)
                {
                    var initialType = scenario.InitialBlockType ?? throw new Exception("Initial block type not set for scenario " + scenarioId);

                    var initBlock = (HandlerBlock?)scope.ServiceProvider.GetService(initialType)
                                    ?? (HandlerBlock?)ActivatorUtilities.CreateInstance(scope.ServiceProvider, initialType);

                    if (initBlock == null)
                    {
                        logger.LogWarning("Failed to resolve initial block of type {BlockType}", initialType);
                        return;
                    }

                    initBlock.ApplyState(null);

                    var initCtx = new BlockContext
                    {
                        Client = botClient,
                        ChatId = chatId,
                        CallbackMessageId = callback?.Message?.MessageId,
                        ScenarioId = scenarioId,
                        Logger = loggerFactory.CreateLogger(initBlock.GetType())
                    };

                    await initBlock.EnterAsync(initCtx, ct);

                    var initStateJson = initBlock.CaptureState();
                    await stateStore.SaveAsync(chatId, scenarioId, initBlock.BlockId, initStateJson, initCtx.ContextBag, ct);

                    // не обрабатываем входящее сообщение/коллбек дальше — поведение как в старой версии
                    return;
                }

                // Иначе — продолжаем обработку существующего состояния
                var blockId = stored.CurrentBlockId;
                var blockType = blockId == null ? scenario.InitialBlockType : scenario.GetBlockType(blockId) ?? throw new Exception($"Block type for id '{blockId}' not found");

                var block = (HandlerBlock?)scope.ServiceProvider.GetService(blockType)
                            ?? (HandlerBlock?)ActivatorUtilities.CreateInstance(scope.ServiceProvider, blockType);

                if (block == null)
                {
                    logger.LogWarning("Failed to resolve block of type {BlockType}", blockType);
                    return;
                }

                var stateJson = stored.StateJson;
                block.ApplyState(stateJson);

                var ctx = new BlockContext
                {
                    Client = botClient,
                    ChatId = chatId,
                    CallbackMessageId = callback?.Message?.MessageId,
                    ScenarioId = scenarioId,
                    Logger = loggerFactory.CreateLogger(block.GetType())
                };

                HandlerBlockResult result;
                if (message != null)
                {
                    result = await block.HandleAsync(message, ctx, ct);
                }
                else if (callback != null)
                {
                    result = await block.HandleCallbackAsync(callback, ctx, ct);
                }
                else
                {
                    return;
                }

                if (result == null)
                {
                    logger.LogWarning("Block {BlockId} returned null result", block.BlockId);
                    return;
                }

                switch (result.ResultState)
                {
                    case HandlerBlockResultState.IsError:
                        logger.LogWarning("[{User}] Block {BlockId} error: {Error}", logUser, block.BlockId, result.ErrorMessage);
                        await botClient.SendMessage(chatId, $"Ошибка: {result.ErrorMessage}", cancellationToken: ct);
                        break;

                    case HandlerBlockResultState.IsContinue:
                        {
                            var saveJson = block.CaptureState();
                            await stateStore.SaveAsync(chatId, scenarioId, block.BlockId, saveJson, ctx.ContextBag, ct);

                            if (result.ReEnter)
                            {
                                const string key = "__reenter_count";
                                int count = 0;
                                if (ctx.ContextBag.TryGetValue(key, out var o) && o is int oi) count = oi;

                                // лимит re-enter, чтобы избежать бесконечного цикла
                                if (count >= 3)
                                {
                                    logger.LogWarning("ReEnter limit reached for chat {ChatId} block {BlockId}", chatId, block.BlockId);
                                    break;
                                }

                                ctx.ContextBag[key] = count + 1;

                                try
                                {
                                    await block.EnterAsync(ctx, ct);
                                    var reSaveJson = block.CaptureState();
                                    await stateStore.SaveAsync(chatId, scenarioId, block.BlockId, reSaveJson, ctx.ContextBag, ct);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Error during ReEnter EnterAsync for block {BlockId}", block.BlockId);
                                }
                                finally
                                {
                                    // уменьшаем счетчик — сохраняем поведение, чтобы следующий проход начинался заново
                                    if (ctx.ContextBag.TryGetValue(key, out var o2) && o2 is int v2 && v2 > 0)
                                        ctx.ContextBag[key] = v2 - 1;
                                }
                            }
                        }
                        break;

                    case HandlerBlockResultState.IsEnd:
                        block.OnEnd();
                        await stateStore.DeleteAsync(chatId, scenarioId, ct);
                        if (!string.IsNullOrEmpty(result.NextBlockId))
                        {
                            var nextType = scenario.GetBlockType(result.NextBlockId) ?? throw new Exception($"Next block type '{result.NextBlockId}' not registered");
                            var nextBlock = (HandlerBlock?)scope.ServiceProvider.GetService(nextType)
                                          ?? (HandlerBlock?)ActivatorUtilities.CreateInstance(scope.ServiceProvider, nextType);

                            if (nextBlock != null)
                            {
                                nextBlock.ApplyState(null);
                                var nextCtx = new BlockContext
                                {
                                    Client = botClient,
                                    ChatId = chatId,
                                    ScenarioId = scenarioId,
                                    Logger = loggerFactory.CreateLogger(nextBlock.GetType())
                                };
                                await nextBlock.EnterAsync(nextCtx, ct);
                                var nextState = nextBlock.CaptureState();
                                await stateStore.SaveAsync(chatId, scenarioId, result.NextBlockId, nextState, result.Data ?? new System.Collections.Generic.Dictionary<string, object>(), ct);
                            }
                        }
                        break;
                }

                if (callback != null)
                {
                    await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                var lf = _provider.GetService<ILoggerFactory>();
                lf?.CreateLogger<BotEngine>()?.LogError(ex, "Unhandled exception while handling update");
            }
        }
    }
}
