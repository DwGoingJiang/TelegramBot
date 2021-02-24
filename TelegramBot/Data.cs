using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DwFramework.Core;
using DwFramework.Core.Extensions;
using DwFramework.ORM;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace TelegramBot
{
    [Registerable(lifetime: Lifetime.Singleton, isAutoActivate: true)]
    public sealed class Data
    {
        private readonly ILogger<Data> _logger;
        private readonly ORMService _ormService;
        private List<NotAllowWord> _notAllowWords;

        public string[] NotAllowWords => _notAllowWords.Select(item => item.Value).ToArray();

        public Data(ILogger<Data> logger, ORMService ormService)
        {
            _logger = logger;
            _ormService = ormService;
            _ = GetNotAllowWordsAsync();
        }

        public async Task<List<NotAllowWord>> GetNotAllowWordsAsync()
        {
            var con = _ormService.CreateConnection("MySql");
            _notAllowWords = await con.Queryable<NotAllowWord>().ToListAsync();
            return _notAllowWords;
        }

        public async Task AddNotAllowWordsAsync(ICollection<string> words)
        {
            var con = _ormService.CreateConnection("MySql");
            words = words.GroupBy(p => p).Select(p => p.Key).ToArray();
            var exists = con.Queryable<NotAllowWord>().In("value", words).Select(item => item.Value).ToList();
            var entities = words.Where(item => !exists.Contains(item)).Select(item => new NotAllowWord() { Value = item }).ToArray();
            await con.Insertable(entities).ExecuteCommandAsync();
            _notAllowWords = await GetNotAllowWordsAsync();
        }

        public async Task DeleteNotAllowWordsAsync(ICollection<string> words)
        {
            var con = _ormService.CreateConnection("MySql");
            words = words.GroupBy(p => p).Select(p => p.Key).ToArray();
            var exists = con.Queryable<NotAllowWord>().In("value", words).Select(item => item.ID).ToList();
            await con.Deleteable<NotAllowWord>().Where(item => exists.Contains(item.ID)).ExecuteCommandAsync();
            _notAllowWords = await GetNotAllowWordsAsync();
        }

        public async Task<List<LinkData>> GetLinkDatasAsync(string word)
        {
            var con = _ormService.CreateConnection("MySql");
            return await con.Ado.SqlQueryAsync<LinkData>("SELECT * FROM link_data WHERE MATCH (key_words) AGAINST (@keyWord)",
                new SugarParameter("@keyWord", word));
        }

        public async Task AddLinkDatasAsync(string[] keywords, LinkType type, string value)
        {
            if (keywords.Length <= 0) return;
            var con = _ormService.CreateConnection("MySql");
            var keywordStr = string.Join(' ', keywords);
            var exists = await con.Queryable<LinkData>().Where(item => item.Type == type && item.Value == value).ToListAsync();
            if (exists.Count <= 0)
            {
                await con.Insertable(new LinkData()
                {
                    KeyWords = keywordStr,
                    Type = type,
                    Value = value
                }).ExecuteCommandAsync();
            }
            else
            {
                await con.Updateable<LinkData>().SetColumns(item => new LinkData()
                {
                    KeyWords = keywordStr
                }).Where(item => exists.Contains(item)).ExecuteCommandAsync();
            }
        }
    }

    public enum LinkType
    {
        Unknow = 1,
        Private = 2,
        Channel = 4,
        Group = 8
    }

    [SugarTable("not_allow_word")]
    public sealed class NotAllowWord
    {
        [SugarColumn(ColumnName = "id", IsIdentity = true, IsPrimaryKey = true)]
        public int ID { get; set; }
        [SugarColumn(ColumnName = "value")]
        public string Value { get; set; }
    }

    [SugarTable("link_data")]
    public sealed class LinkData
    {
        [SugarColumn(ColumnName = "id", IsIdentity = true, IsPrimaryKey = true)]
        public int ID { get; set; }
        [SugarColumn(ColumnName = "key_words")]
        public string KeyWords { get; set; }
        [SugarColumn(ColumnName = "type")]
        public LinkType Type { get; set; }
        [SugarColumn(ColumnName = "value")]
        public string Value { get; set; }
    }
}
