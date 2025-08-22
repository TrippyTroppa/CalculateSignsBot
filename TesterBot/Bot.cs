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

namespace TesterBot
{
    internal class Bot: BackgroundService
    {
        private ITelegramBotClient _botClient;

        public Bot (ITelegramBotClient botClient)
        {
            _botClient = botClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _botClient.StartReceiving(HandleUpdate,
                HandleError,

                new ReceiverOptions() { AllowedUpdates = { } }, 
                cancellationToken: stoppingToken);

            Console.WriteLine("Бот запущен");
        }

        async Task HandleUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
                
             await _botClient.SendMessage(update.Message.Chat.Id, $"Длина сообщения: {update.Message.Text.Length} знаков",
                 cancellationToken: cancellationToken);
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
