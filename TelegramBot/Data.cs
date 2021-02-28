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
    [SugarTable("index_data")]
    public sealed class IndexData
    {
        [SugarColumn(ColumnName = "id", IsIdentity = true, IsPrimaryKey = true)]
        public int ID { get; set; }
        [SugarColumn(ColumnName = "key_words")]
        public string KeyWords { get; set; }
        [SugarColumn(ColumnName = "title")]
        public string Title { get; set; }
        [SugarColumn(ColumnName = "url")]
        public string Url { get; set; }
        [SugarColumn(ColumnName = "creater_id")]
        public int CreaterId { get; set; }
    }

    [SugarTable("banned_word")]
    public sealed class BannedWord
    {
        [SugarColumn(ColumnName = "id", IsIdentity = true, IsPrimaryKey = true)]
        public int ID { get; set; }
        [SugarColumn(ColumnName = "value")]
        public string Value { get; set; }
    }

    [Registerable(lifetime: Lifetime.Singleton, isAutoActivate: true)]
    public sealed class Data
    {
        private readonly ILogger<Data> _logger;
        private readonly ORMService _ormService;
        private List<BannedWord> _bannedWords;
        private const int PAGE_DATE_COUNT = 10;

        public string[] BannedWords => _bannedWords.Select(item => item.Value).ToArray();

        public Data(ILogger<Data> logger, ORMService ormService)
        {
            _logger = logger;
            _ormService = ormService;
            _ = GetBannedWordsAsync();
        }

        public async Task AddOrUpdateIndexDatasAsync(IndexData indexData)
        {
            if (string.IsNullOrEmpty(indexData.KeyWords) || string.IsNullOrEmpty(indexData.Url)) return;
            var con = _ormService.CreateConnection("MySql");
            var exists = await con.Queryable<IndexData>().Where(item => item.Url == indexData.Url).ToListAsync();
            if (exists.Count <= 0)
                await con.Insertable(indexData).ExecuteCommandAsync();
            else
                await con.Updateable<IndexData>().SetColumns(item => new IndexData()
                {
                    KeyWords = indexData.KeyWords
                }).Where(item => exists.Contains(item)).ExecuteCommandAsync();
        }

        public async Task<List<IndexData>> GetIndexDatasAsync(string word, int page = 0)
        {
            var con = _ormService.CreateConnection("MySql");
            return await con.Ado.SqlQueryAsync<IndexData>(
                "SELECT * FROM index_data WHERE MATCH (key_words,title) AGAINST (@keyWord) LIMIT @skip,@pageDataCount",
                new SugarParameter("@keyWord", word),
                new SugarParameter("@skip", page * PAGE_DATE_COUNT),
                new SugarParameter("@pageDataCount", PAGE_DATE_COUNT));
        }

        public async Task AddBannedWordsAsync(ICollection<string> words)
        {
            var con = _ormService.CreateConnection("MySql");
            words = words.GroupBy(p => p).Select(p => p.Key).ToArray();
            var exists = con.Queryable<BannedWord>().In("value", words).Select(item => item.Value).ToList();
            var entities = words.Where(item => !exists.Contains(item)).Select(item => new BannedWord() { Value = item }).ToArray();
            await con.Insertable(entities).ExecuteCommandAsync();
            _bannedWords = await GetBannedWordsAsync();
        }

        public async Task<List<BannedWord>> GetBannedWordsAsync()
        {
            var con = _ormService.CreateConnection("MySql");
            _bannedWords = await con.Queryable<BannedWord>().ToListAsync();
            return _bannedWords;
        }

        public async Task DeleteBannedWordsAsync(ICollection<string> words)
        {
            var con = _ormService.CreateConnection("MySql");
            words = words.GroupBy(p => p).Select(p => p.Key).ToArray();
            var exists = con.Queryable<BannedWord>().In("value", words).Select(item => item.ID).ToList();
            await con.Deleteable<BannedWord>().Where(item => exists.Contains(item.ID)).ExecuteCommandAsync();
            _bannedWords = await GetBannedWordsAsync();
        }
    }
}
