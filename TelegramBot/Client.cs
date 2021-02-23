using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
    public static class Client
    {
        private static TelegramBotClient _client;

        public static void Init(string token)
        {
            _client = new TelegramBotClient(token);
            _client.OnMessage += OnMessage;
            _client.OnCallbackQuery += OnCallbackQuery;
            var me = _client.GetMeAsync().Result;
            Console.WriteLine($"ID:{me.Id} Name:{me.Username}");
            Start();
        }

        public static void Start()
        {
            _client.StartReceiving();
        }

        public static void Stop()
        {
            _client.StopReceiving();
        }

        public static void OnMessage(object sender, MessageEventArgs e)
        {
            var chatId = e.Message.Chat.Id;
            var message = e.Message.Text;
            Console.WriteLine($"From:{chatId} Message:{message}");
            switch (message)
            {
                case "/Help":
                    OnMessageHelp(e.Message);
                    break;
                case "/Add":
                    OnMessageHelp(e.Message);
                    break;
            };
        }

        public static void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            Console.WriteLine(e.CallbackQuery.Data);
            _client.DeleteMessageAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId);
            _client.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "TEST");
        }

        private static void OnMessageHelp(Message message)
        {
            _client.SendTextMessageAsync(message.Chat.Id,
                "*尝试使用以下命令:*\n" +
                "/Help 获取帮助\n" +
                "/Add 添加链接",
                parseMode: ParseMode.Markdown,
                disableNotification: true,
                replyToMessageId: message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(
                    new InlineKeyboardButton[][]
                    {
                        new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("个人","https://t.me/@dwgoing"),
                            InlineKeyboardButton.WithUrl("链接","https://www.baidu.com"),
                            InlineKeyboardButton.WithUrl("频道","https://t.me/joinchat/TwyRY8jYiCO7S-3x"),
                            InlineKeyboardButton.WithUrl("组群","https://t.me/joinchat/IrgTkeoOSCEvUOj1")
                        },
                        new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithCallbackData("x")
                        }
                    }));
        }
    }
}
