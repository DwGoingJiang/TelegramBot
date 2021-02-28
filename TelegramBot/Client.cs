using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using DwFramework.Core;
using DwFramework.Core.Plugins;

namespace TelegramBot
{
    public sealed class Client
    {
        private enum HandlerType
        {
            Unknow = 0,
            AddIndex = 1
        }

        private sealed class WaitableHandler
        {
            public DateTime ExpireTime { get; set; }
            public int Uid { get; init; }
            public HandlerType Type { get; init; }
            public int Step { get; set; }
            public dynamic Data { get; init; }
        }

        private readonly ILogger<Client> _logger;
        private readonly Data _data;
        private readonly TelegramBotClient _client;
        private User _botInfo;
        private readonly Dictionary<int, WaitableHandler> _waittingHandlers = new Dictionary<int, WaitableHandler>();
        private const int WAITABLE_HANDLER_EXPIRE_MS = 60000;
        private const string URL_PREFIX = @"https://t.me/";

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

        private void OnMessage(object sender, MessageEventArgs e)
        {
            var charId = e.Message.Chat.Id;
            var messageId = e.Message.MessageId;
            var fromId = e.Message.From.Id;
            switch (e.Message.Type)
            {
                case MessageType.Text: // 文本消息
                    var text = e.Message.Text;
                    if (_data.BannedWords.Contains(text)) // 违禁词
                    {
                        _client.DeleteMessageAsync(charId, messageId);
                    }
                    switch (text.Replace($"@{_botInfo.Username}", ""))
                    {
                        case "/start":
                        case "/help":
                            _ = OnMessageHelpAsync(e.Message);
                            break;
                        case "/add_index":
                            _ = OnMessageAddIndexAsync(e.Message);
                            break;
                        case "/cancel":
                            _ = OnMessageCancelAsync(e.Message);
                            break;
                        default:
                            // 查询索引
                            if (text.StartsWith('?') || text.StartsWith('？'))
                            {
                                _ = OnMessageSearchIndexAsync(e.Message);
                                return;
                            }
                            // 可等待操作
                            if (!_waittingHandlers.ContainsKey(fromId)) return;
                            var handler = _waittingHandlers[fromId];
                            if (DateTime.Now >= handler.ExpireTime)
                                _waittingHandlers.Remove(fromId);
                            else
                            {
                                switch (handler.Type)
                                {
                                    case HandlerType.AddIndex:
                                        _ = AddIndexHandlerAsync(handler, e.Message);
                                        break;
                                }
                            }
                            break;
                    };
                    break;
                case MessageType.ChatMembersAdded: // 用户加入
                    Console.WriteLine("用户加入");
                    break;
                case MessageType.ChatMemberLeft: // 用户离开
                    Console.WriteLine("用户离开");
                    break;
                default:
                    Console.WriteLine(e.Message.Type);
                    break;
            }
        }

        private async Task OnMessageHelpAsync(Message message)
        {
            await _client.SendTextMessageAsync(message.Chat.Id,
                "*尝试使用以下命令:*\n" +
                "/help 获取帮助\n" +
                "/add_index 添加索引\n" +
                "/cancel 取消操作",
                parseMode: ParseMode.Markdown,
                replyToMessageId: message.MessageId,
                disableWebPagePreview: true
                );
        }

        private async Task OnMessageAddIndexAsync(Message message)
        {
            var charId = message.Chat.Id;
            var messageId = message.MessageId;
            var fromId = message.From.Id;
            if (message.Chat.Type != ChatType.Private)
            {
                await _client.SendTextMessageAsync(
                    charId,
                    "*请在私信中继续执行操作*",
                    parseMode: ParseMode.Markdown,
                    replyToMessageId: messageId,
                    disableWebPagePreview: true,
                    replyMarkup: new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithUrl($"@{_botInfo.Username}", $"{URL_PREFIX}{_botInfo.Username}")
                        )
                    );
            }
            await _client.SendTextMessageAsync(
                fromId,
                "*请发送要索引的链接*",
                parseMode: ParseMode.Markdown,
                disableWebPagePreview: true
                );
            _waittingHandlers[message.From.Id] = new WaitableHandler()
            {
                ExpireTime = DateTime.Now.AddMilliseconds(WAITABLE_HANDLER_EXPIRE_MS),
                Uid = message.From.Id,
                Type = HandlerType.AddIndex,
                Step = 0,
                Data = new IndexData()
            };
        }

        private async Task AddIndexHandlerAsync(WaitableHandler handler, Message message)
        {
            var charId = message.Chat.Id;
            var messageId = message.MessageId;
            var fromId = message.From.Id;
            var value = message.Text;
            var data = (IndexData)handler.Data;
            switch (handler.Step)
            {
                case 0:
                    if (!value.StartsWith(URL_PREFIX))
                    {
                        await _client.SendTextMessageAsync(
                            charId,
                            "*请发送符合规范的链接*",
                            parseMode: ParseMode.Markdown,
                            replyToMessageId: messageId,
                            disableWebPagePreview: true
                            );
                    }
                    else
                    {
                        await _client.SendTextMessageAsync(
                            charId,
                            "*请发送索引标题*\n" +
                            "1.最多不超过10个字符",
                            parseMode: ParseMode.Markdown,
                            replyToMessageId: messageId,
                            disableWebPagePreview: true
                            );
                        data.CreaterId = fromId;
                        data.Url = value.Replace(URL_PREFIX, "");
                        handler.ExpireTime = DateTime.Now.AddMilliseconds(WAITABLE_HANDLER_EXPIRE_MS);
                        handler.Step++;
                    }
                    break;
                case 1:
                    await _client.SendTextMessageAsync(
                        charId,
                        "*请发送索引关键词*\n" +
                        "1.格式：关键词1 关键词2 ...\n" +
                        "2.最多不超过5个关键词\n" +
                        "3.每个关键词至少2个字符",
                        parseMode: ParseMode.Markdown,
                        replyToMessageId: messageId,
                        disableWebPagePreview: true
                        );
                    data.Title = value;
                    handler.ExpireTime = DateTime.Now.AddMilliseconds(WAITABLE_HANDLER_EXPIRE_MS);
                    handler.Step++;
                    break;
                case 2:
                    var words = value.Split(' ');
                    if (words.Length <= 0)
                    {
                        await _client.SendTextMessageAsync(
                            charId,
                            "*请添加至少1个关键词用*",
                            parseMode: ParseMode.Markdown,
                            replyToMessageId: messageId,
                            disableWebPagePreview: true
                            );
                    }
                    else
                    {
                        foreach (var item in words)
                        {
                            if (item.Length < 2)
                            {
                                await _client.SendTextMessageAsync(
                                    charId,
                                    "*每个关键词至少2个字符*",
                                    parseMode: ParseMode.Markdown,
                                    replyToMessageId: messageId,
                                    disableWebPagePreview: true
                                    );
                                return;
                            }
                        }
                        data.KeyWords = string.Join(' ', words, 0, words.Length > 5 ? 5 : words.Length);
                        _waittingHandlers.Remove(fromId);
                        _ = _data.AddOrUpdateIndexDatasAsync(data);
                        await _client.SendTextMessageAsync(
                            charId,
                            "*索引添加成功*",
                            parseMode: ParseMode.Markdown,
                            replyToMessageId: messageId,
                            disableWebPagePreview: true
                            );
                    }
                    break;
            }
        }

        private async Task OnMessageSearchIndexAsync(Message message, bool isUpdate = false)
        {
            var charId = message.Chat.Id;
            var messageId = message.MessageId;
            var fromId = message.From.Id;
            var value = message.Text.Substring(1);
            var arr = value.Split(' ');
            var page = 0;
            if (arr.Length >= 3 && arr[^2] == "page" && int.TryParse(arr[^1], out page))
            {
                page = page < 0 ? 0 : page;
                value = value.Substring(0, value.Length - arr[^2].Length - arr[^1].Length - 2);
            }
            var result = await _data.GetIndexDatasAsync(value, page);
            if (result.Count <= 0)
            {
                if (isUpdate)
                {
                    await _client.EditMessageTextAsync(
                        charId,
                        messageId,
                        "*未找到匹配的结果*",
                        parseMode: ParseMode.Markdown,
                        disableWebPagePreview: true
                        );
                }
                else
                {
                    await _client.SendTextMessageAsync(
                        charId,
                        "*未找到匹配的结果*",
                        parseMode: ParseMode.Markdown,
                        replyToMessageId: messageId,
                        disableWebPagePreview: true
                        );
                }
            }
            else
            {
                var builder = new StringBuilder("*找到以下结果*\n");
                var index = 1;
                builder.Append(string.Join('\n', result.Select(item => $"{index++}.[{item.Title}]({URL_PREFIX}{item.Url})")));
                if (isUpdate)
                {
                    await _client.EditMessageTextAsync(
                        charId,
                        messageId,
                        builder.ToString(),
                        parseMode: ParseMode.Markdown,
                        disableWebPagePreview: true,
                        replyMarkup: new InlineKeyboardMarkup(
                            new InlineKeyboardButton[][]{
                                new InlineKeyboardButton[]
                                {
                                    InlineKeyboardButton.WithCallbackData("上一页",$"?{value} page {page - 1}"),
                                    InlineKeyboardButton.WithCallbackData("下一页",$"?{value} page {page + 1}")
                                }
                            })
                        );
                }
                else
                {
                    await _client.SendTextMessageAsync(
                        charId,
                        builder.ToString(),
                        parseMode: ParseMode.Markdown,
                        replyToMessageId: messageId,
                        disableWebPagePreview: true,
                        replyMarkup: new InlineKeyboardMarkup(
                            new InlineKeyboardButton[][]{
                                new InlineKeyboardButton[]
                                {
                                    InlineKeyboardButton.WithCallbackData("上一页",$"?{value} page {page - 1}"),
                                    InlineKeyboardButton.WithCallbackData("下一页",$"?{value} page {page + 1}")
                                }
                            })
                        );
                }
            }
        }

        private async Task OnMessageCancelAsync(Message message)
        {
            var charId = message.Chat.Id;
            var messageId = message.MessageId;
            var fromId = message.From.Id;
            if (_waittingHandlers.ContainsKey(fromId))
                _waittingHandlers.Remove(fromId);
            await _client.SendTextMessageAsync(
                charId,
                "*操作已取消*",
                parseMode: ParseMode.Markdown,
                replyToMessageId: messageId,
                disableWebPagePreview: true
                );
        }

        private void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            var charId = e.CallbackQuery.Message.Chat.Id;
            var messageId = e.CallbackQuery.Message.MessageId;
            var fromId = e.CallbackQuery.Message.From.Id;
            var text = e.CallbackQuery.Data;
            e.CallbackQuery.Message.Text = text;
            if (text.StartsWith('?'))
                _ = OnMessageSearchIndexAsync(e.CallbackQuery.Message, true);
        }

        private void OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
        }

        private void OnInlineResultChosen(object sender, ChosenInlineResultEventArgs e)
        {
        }

        private void OnMessageEdited(object sender, MessageEventArgs e)
        {
        }

        private void OnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
        }

        private void OnReceiveGeneralError(object sender, ReceiveGeneralErrorEventArgs e)
        {
        }

        private void OnUpdate(object sender, UpdateEventArgs e)
        {
        }
    }
}
