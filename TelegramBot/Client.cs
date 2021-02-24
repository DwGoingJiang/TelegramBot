using System;
using System.Linq;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using DwFramework.Core;
using DwFramework.Core.Plugins;
using Microsoft.Extensions.Logging;

namespace TelegramBot
{
    public class Client
    {
        private readonly ILogger<Client> _logger;
        private readonly Data _data;
        private readonly TelegramBotClient _client;
        private User _botInfo;

        public Client(string token)
        {
            _logger = ServiceHost.Provider.GetLogger<Client>();
            _data = ServiceHost.Provider.GetService<Data>();
            _client = new TelegramBotClient(token);
            _client.OnCallbackQuery += OnCallbackQuery;
            _client.OnInlineQuery += OnInlineQuery;
            _client.OnInlineResultChosen += OnInlineResultChosen;
            _client.OnMessage += OnMessage;
            _client.OnMessageEdited += OnMessageEdited;
            _client.OnReceiveError += OnReceiveError;
            _client.OnReceiveGeneralError += OnReceiveGeneralError;
            _client.OnUpdate += OnUpdate;
            UpdateBotInfoAsync();
            Start();
        }

        public async void UpdateBotInfoAsync()
        {
            _botInfo = await _client.GetMeAsync();
            Console.WriteLine($"ID:{_botInfo.Id} Name:{_botInfo.Username}");
        }

        public void Start()
        {
            _client.StartReceiving();
        }

        public void Stop()
        {
            _client.StopReceiving();
        }

        private void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            Console.WriteLine("OnCallbackQuery");
        }

        private void OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
            Console.WriteLine("OnInlineQuery");
        }

        private void OnInlineResultChosen(object sender, ChosenInlineResultEventArgs e)
        {
            Console.WriteLine("OnInlineResultChosen");
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            switch (e.Message.Type)
            {
                case MessageType.ChatMembersAdded: // 用户加入
                    Console.WriteLine("用户加入");
                    break;
                case MessageType.ChatMemberLeft: // 用户离开
                    Console.WriteLine("用户离开");
                    break;
                case MessageType.Text: // 文本消息
                    Console.WriteLine($"From:{e.Message.Chat.Id} Message:{e.Message.Text}");
                    // 违禁词
                    if (_data.NotAllowWords.Contains(e.Message.Text))
                    {
                        _client.DeleteMessageAsync(e.Message.Chat.Id, e.Message.MessageId);
                    }
                    switch (e.Message.Text.Replace($"@{_botInfo.Username}", ""))
                    {
                        case "/start":
                        case "/help":
                            OnMessageHelp(e.Message);
                            break;
                        case "/add_not_allow_words":
                            OnMessageAdd(e.Message);
                            break;
                    };
                    break;
                default:
                    Console.WriteLine(e.Message.Type);
                    break;
            }
        }

        private void OnMessageHelp(Message message)
        {
            _client.SendTextMessageAsync(message.Chat.Id,
                "*尝试使用以下命令:*\n" +
                "/help 获取帮助\n",
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

        private void OnMessageAdd(Message message)
        {
            if (message.Chat.Type != ChatType.Private)
            {
                _client.SendTextMessageAsync(message.Chat.Id,
                    "*请在私信中继续执行操作*",
                    parseMode: ParseMode.Markdown,
                    replyToMessageId: message.MessageId,
                    replyMarkup: new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithUrl($"@{_botInfo.Username}", $"https://t.me/{_botInfo.Username}"))
                    );
                _client.SendTextMessageAsync(message.From.Id,
                    "请在私信中操作",
                    parseMode: ParseMode.Markdown);
            }
        }

        private void OnMessageEdited(object sender, MessageEventArgs e)
        {
            Console.WriteLine("OnMessageEdited");
        }

        private void OnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            Console.WriteLine("OnReceiveError");
        }

        private void OnReceiveGeneralError(object sender, ReceiveGeneralErrorEventArgs e)
        {
            Console.WriteLine("OnReceiveGeneralError");
        }

        private void OnUpdate(object sender, UpdateEventArgs e)
        {
            Console.WriteLine("OnUpdate");
        }
    }
}
