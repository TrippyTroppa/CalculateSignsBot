using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TesterBot
{
    internal class Bot : BackgroundService
    {
        private ITelegramBotClient _botClient;
        private Dictionary<long, string> _userStates;

        public Bot(ITelegramBotClient botClient)
        {
            _botClient = botClient;
            _userStates = new Dictionary<long, string>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _botClient.StartReceiving(HandleUpdate, HandleError,
                new ReceiverOptions() { AllowedUpdates = { } },
                cancellationToken: stoppingToken);

            Console.WriteLine("Бот запущен");
        }

        async Task HandleUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message!;
                var chatId = message.Chat.Id;
                var userId = message.From!.Id;

                if (message.Type == MessageType.Text)
                {
                    var text = message.Text;

                    if (text == "/start")
                    {
                        await ShowMainMenu(chatId, "🎯 Добро пожаловать в главное меню! Выберите действие:", cancellationToken);
                        return;
                    }

                    if (text == "/menu")
                    {
                        await ShowMainMenu(chatId, "🎯 Главное меню", cancellationToken);
                        return;
                    }

                    if (_userStates.ContainsKey(userId))
                    {
                        var state = _userStates[userId];

                        if (state == "count_chars")
                        {
                            await HandleCountChars(chatId, text, cancellationToken);
                        }
                        else if (state == "sum_numbers")
                        {
                            await HandleSumNumbers(chatId, text, cancellationToken);
                        }
                    }
                    else
                    {
                        await ShowMainMenu(chatId,
                            "🎯 Главное меню\n\nВыберите действие из меню ниже или используйте команды:\n/start - Главное меню\n/menu - Показать меню",
                            cancellationToken);
                    }
                }
                else
                {
                    await _botClient.SendMessage(chatId,
                        "❌ Данный тип сообщений не поддерживается. Пожалуйста, отправьте текст или используйте /menu для вызова меню.",
                        replyMarkup: GetMainMenuKeyboard(),
                        cancellationToken: cancellationToken);
                }
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                var callbackQuery = update.CallbackQuery!;
                var userId = callbackQuery.From.Id;
                var chatId = callbackQuery.Message!.Chat.Id;
                var data = callbackQuery.Data;

                if (data == "count_chars")
                {
                    _userStates[userId] = "count_chars";
                    await _botClient.SendMessage(chatId,
                        "📊 <b>Режим подсчёта символов</b>\n\nОтправьте мне текст, и я посчитаю количество символов в нём.\n\nИспользуйте /menu для возврата в главное меню.",
                        parseMode: ParseMode.Html,
                        replyMarkup: GetBackToMenuKeyboard(),
                        cancellationToken: cancellationToken);
                }
                else if (data == "sum_numbers")
                {
                    _userStates[userId] = "sum_numbers";
                    await _botClient.SendMessage(chatId,
                        "🧮 <b>Режим подсчёта суммы чисел</b>\n\nОтправьте мне числа через пробел, и я вычислю их сумму.\n\n<b>Пример:</b> 2 3 15\n\nИспользуйте /menu для возврата в главное меню.",
                        parseMode: ParseMode.Html,
                        replyMarkup: GetBackToMenuKeyboard(),
                        cancellationToken: cancellationToken);
                }
                else if (data == "main_menu")
                {
                    _userStates.Remove(userId);
                    await ShowMainMenu(chatId, "🎯 Главное меню", cancellationToken);
                }
                else if (data == "help")
                {
                    await ShowHelp(chatId, cancellationToken);
                }

                
            }
        }

        private async Task ShowMainMenu(long chatId, string message, CancellationToken cancellationToken)
        {
            await _botClient.SendMessage(chatId,
                message,
                replyMarkup: GetMainMenuKeyboard(),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        private async Task ShowHelp(long chatId, CancellationToken cancellationToken)
        {
            var helpText = @"📖 <b>Помощь по боту</b>

<b>Доступные команды:</b>
/start - Главное меню
/menu - Показать меню

<b>Функции бота:</b>
• 📊 <b>Подсчёт символов</b> - считает количество символов в вашем тексте
• 🧮 <b>Сумма чисел</b> - вычисляет сумму чисел, разделённых пробелами

<b>Как использовать:</b>
1. Выберите функцию из меню
2. Отправьте боту соответствующие данные
3. Получите результат!

Для возврата в меню используйте кнопку ниже или команду /menu";

            await _botClient.SendMessage(chatId, helpText,
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToMenuKeyboard(),
                cancellationToken: cancellationToken);
        }

        private InlineKeyboardMarkup GetMainMenuKeyboard()
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📊 Подсчёт символов", "count_chars"),
                    InlineKeyboardButton.WithCallbackData("🧮 Сумма чисел", "sum_numbers")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📖 Помощь", "help")
                }
            });

            return inlineKeyboard;
        }

        private InlineKeyboardMarkup GetBackToMenuKeyboard()
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("↩️ Главное меню", "main_menu") }
            });

            return inlineKeyboard;
        }

        private async Task HandleCountChars(long chatId, string text, CancellationToken cancellationToken)
        {
            if (text.StartsWith("/"))
            {
                return;
            }

            var charCount = text.Length;
            var response = $"📊 <b>Результат подсчёта символов</b>\n\nВ вашем сообщении <b>{charCount}</b> символов";

            await _botClient.SendMessage(chatId, response,
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToMenuKeyboard(),
                cancellationToken: cancellationToken);
        }

        private async Task HandleSumNumbers(long chatId, string text, CancellationToken cancellationToken)
        {
            if (text.StartsWith("/"))
            {
                return;
            }

            try
            {
                var numbers = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(num => double.Parse(num))
                                 .ToArray();

                var sum = numbers.Sum();
                var numbersList = string.Join(" + ", numbers);
                var response = $"🧮 <b>Результат вычисления суммы</b>\n\n{numbersList} = <b>{sum}</b>";

                await _botClient.SendMessage(chatId, response,
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMenuKeyboard(),
                    cancellationToken: cancellationToken);
            }
            catch (FormatException)
            {
                await _botClient.SendMessage(chatId,
                    "❌ <b>Ошибка:</b> Пожалуйста, введите корректные числа через пробел.\n\n<b>Пример:</b> 2 3 15\n\nИспользуйте /menu для возврата в главное меню.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMenuKeyboard(),
                    cancellationToken: cancellationToken);
            }
            catch (OverflowException)
            {
                await _botClient.SendMessage(chatId,
                    "❌ <b>Ошибка:</b> Одно из чисел слишком большое.\n\nПожалуйста, введите числа поменьше.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMenuKeyboard(),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendMessage(chatId,
                    $"❌ <b>Произошла ошибка:</b> {ex.Message}\n\nИспользуйте /menu для возврата в главное меню.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMenuKeyboard(),
                    cancellationToken: cancellationToken);
            }
        }

        Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            Console.WriteLine("Ожидаем 10 секунд перед повторным подключением.");
            Thread.Sleep(10000);

            return Task.CompletedTask;
        }
    }
}