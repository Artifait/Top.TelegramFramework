
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Top.TelegramFramework.Core.Blocks;
using Top.TelegramFramework.Core.Data;
using Top.TelegramFramework.Core.Utilities;

namespace Top.TelegramFramework.Core
{
    public enum LoggedMessageType
    {
        Information,
        Errore,
        Warning
    }
    public delegate Task LogString(string message, LoggedMessageType messageType = LoggedMessageType.Information);

    public class BotEngine
    {
        private ScenarioSelector _scenarioSelector;
        private UserStateRepository _stateRepository;
        private readonly UserStateContext _userStateContext;
        private ITelegramBotClient _bot;
        public ITelegramBotClient Bot => _bot; 
        public LogString? Logger { get; set; } = ConsoleLogger.LogLine;

        public BotEngine(ScenarioSelector scenarioSelector)
        {
            _scenarioSelector = scenarioSelector;

            // Настройка EF Core с SQLite
            var options = new DbContextOptionsBuilder<UserStateContext>()
                .UseSqlite("Data Source=userstates.db")
                .Options;

            _userStateContext = new UserStateContext(options);
            _userStateContext.Database.EnsureCreated();

            _stateRepository = new UserStateRepository(_userStateContext);
        }

        public async Task StartBot(string token)
        {
            using var cts = new CancellationTokenSource();
            _bot = new TelegramBotClient(token);
            var me = await _bot.GetMe();

            _bot.StartReceiving(
                updateHandler: new DefaultUpdateHandler(
                    async (bot, update, token) 
                        => await HandleUpdateAsync(update, token),
                    async (bot, exception, token) 
                        => Logger?.Invoke("Ошибка: " + exception.Message, LoggedMessageType.Errore)
                ),
                cancellationToken: cts.Token);

            
            Console.WriteLine($"@{me.Username} запущен. Для завершения нажмите любую клавишу.");
            Console.ReadKey();
            cts.Cancel();
        }

        public async Task HandleUpdateAsync(Update update, CancellationToken ct)
        {
            if(update.Message == null && update.CallbackQuery == null) 
                return;

            long chatId = update.Message != null ? update.Message.Chat.Id : update.CallbackQuery!.Message!.Chat.Id;
            string username = update.Message != null ? update.Message!.From!.Username! : update.CallbackQuery!.Message!.From!.Username!;
            string receivedText = update.Message != null ? update.Message!.Text! : $"callbackData: {update.CallbackQuery!.Data!}";
            Logger?.Invoke($"[From: {username}, Received: {receivedText}]");

            // Выбор сценария для пользователя
            var scenario = _scenarioSelector.GetScenarioForUser(chatId);
            var scenarioId = scenario.ScenarioId;

            // Получаем сохранённое состояние (если есть)
            var userState = await _stateRepository.GetUserStateAsync(chatId, scenarioId);
            BlockExecutionContext context = new BlockExecutionContext { ChatId = chatId };
            HandlerBlock currentBlock;

            if (userState == null)
            {
                // Если состояние отсутствует, выбираем стартовый блок
                currentBlock = scenario.InitialBlock;
                currentBlock.Logger = Logger;
                await currentBlock.EnterAsync(context, _bot, ct);
                await _stateRepository.SaveOrUpdateUserStateAsync(chatId, scenarioId, new UserState
                {
                    CurrentCompositeBlockId = currentBlock.BlockId,
                    CompositeBlockState = currentBlock.CaptureState(),
                    Context = context.State
                });
                return;
            }
            else
            {
                // Восстанавливаем блок по его Id и применяем сохранённое внутреннее состояние
                currentBlock = scenario.GetBlock(userState.CurrentCompositeBlockId);
                currentBlock.Logger = Logger;
                currentBlock.ApplyState(userState.CompositeBlockState);
                context.State = userState.Context;
            }

            HandlerBlockResult? result = null;

            if (update.Message != null) {
                result = await currentBlock.HandleAsync(update.Message, context, _bot, ct);
            }
            else if(update.CallbackQuery != null) {
                result = await currentBlock.HandleCallbackAsync(update.CallbackQuery!, context, _bot, ct);
            }

            if(result == null)
            {
                Logger?.Invoke($"Нету результата от блока: {currentBlock.BlockId}", LoggedMessageType.Warning);
                return;
            }

            UserState newState;
            switch (result.resultState)
            {
                case HandlerBlockResult.ResultState.IsError:
                    Logger?.Invoke($"[From: {username}, On Block: {currentBlock.BlockId}] -> {result.ErrorMessage}" , LoggedMessageType.Errore);
                    await _bot.SendMessage(chatId, $"Ошибка: {result.ErrorMessage}", cancellationToken: ct);
                    break;

                case HandlerBlockResult.ResultState.IsContinue:
                    newState = new UserState
                    {
                        CurrentCompositeBlockId = currentBlock.BlockId,
                        CompositeBlockState = currentBlock.CaptureState(),
                        Context = context.State
                    };
                    await _stateRepository.SaveOrUpdateUserStateAsync(chatId, scenarioId, newState);
                    break;

                case HandlerBlockResult.ResultState.IsEnd:
                    currentBlock.OnEnd();
                    await _stateRepository.DeleteUserStateAsync(chatId, scenarioId);
                    if (result.NextBlockId != null)
                    {
                        newState = new UserState
                        {
                            CurrentCompositeBlockId = result.NextBlockId,
                            Context = result.Data
                        };
                        currentBlock = scenario.GetBlock(result.NextBlockId);
                        await currentBlock.EnterAsync(context, _bot, ct);
                        await _stateRepository.SaveOrUpdateUserStateAsync(chatId, scenarioId, newState);
                    }
                    break;
            }

            if (update.CallbackQuery != null)
            {
                // Не забудьте ответить на CallbackQuery, чтобы убрать "часики" у пользователя:
                await _bot.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);
            }
        }
    }
}

