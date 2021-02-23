using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public struct DataEntity
    {
        public string Text { get; init; }
        public string Url { get; init; }
        public string[] Keys { get; init; }
    }

    public static class Data
    {
        private static readonly Dictionary<string, DataEntity> _data = new Dictionary<string, DataEntity>();

        public static void Add(string text, string url, params string[] keys)
        {
            _data[url] = new DataEntity()
            {
                Text = text,
                Url = url,
                Keys = keys
            };
        }
    }
}
