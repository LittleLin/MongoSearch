using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoSearch.Structure;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoSearch.Library;
using MongoDb.Structure;

namespace MongoSearch
{
    public class Searcher
    {
        /// <summary>
        /// MongoDB Server Address
        /// </summary>
        public String ServerAddress { get; set; }

        /// <summary>
        /// MongoDB Index Db Name
        /// </summary>
        public String DbName { get; set; }

        public Searcher()
        {
            this.ServerAddress = Constants.DefaultServerAddress;
            this.DbName = Constants.DefaultDbName;
        }

        private bool CheckDocumentIsHitted(List<Pair<String, Int32>> keywordTokens, int tokenSeq, List<List<IndexElement>> checkedIndex, IndexElement previosIndex)
        {
            try
            {
                var currentTokenIndex = checkedIndex[tokenSeq].Where(t => t.DocId == previosIndex.DocId && t.ParaId == previosIndex.ParaId).ToList();
                foreach (var currentIndex in currentTokenIndex)
                {
                    // 如果索引已越界 (不是同一文件、同一段落)，則不再往下檢查
                    if (currentIndex.DocId > previosIndex.DocId)
                    {
                        break;
                    }
                    else if (currentIndex.DocId == previosIndex.DocId && currentIndex.ParaId > previosIndex.ParaId)
                    {
                        break;
                    }
                    else if (currentIndex.DocId == previosIndex.DocId && currentIndex.ParaId < previosIndex.ParaId)
                    {
                        continue;
                    }
                    else if (currentIndex.DocId < previosIndex.DocId)
                    {
                        continue;
                    }

                    int offsetDiff = currentIndex.Offset - previosIndex.Offset;
                    if (offsetDiff <= 0)
                        continue;
                    else
                    {
                        int targetDfif = keywordTokens[tokenSeq].Second - keywordTokens[tokenSeq - 1].Second;

                        if (offsetDiff == targetDfif)
                        {
                            if (keywordTokens.Count == (tokenSeq + 1))
                                return true;
                            else
                                return CheckDocumentIsHitted(keywordTokens, tokenSeq + 1, checkedIndex, currentIndex);
                        }
                        else
                            continue;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public String GetDocumentById(int docId)
        {
            try
            {
                var server = MongoDbLib.GetServerConnection(ServerAddress);
                var database = server.GetDatabase(DbName);
                var tblSourceText = database.GetCollection<WordItem>(Constants.TblSourceText);

                var sourceTexts = from s in tblSourceText.AsQueryable<SourceDocument>()
                                  where s.DocId == docId
                                  orderby s.ParaId
                                  select s.Para;

                return String.Join("\r\n", sourceTexts);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public Dictionary<Int32, Int32> Search(String keyword)
        {
            var server = MongoDbLib.GetServerConnection(ServerAddress);
            var database = server.GetDatabase(DbName);
            var tblWordList = database.GetCollection<WordItem>(Constants.TblWordList);
            var tblFullText = database.GetCollection<InvertedIndex>(Constants.TblFullText);

            // 針對搜尋關鍵字斷詞
            ChineseSegmentor segmentor = new ChineseSegmentor();
            List<Pair<String, Int32>> keywordTokens = segmentor.SegWords(keyword);

            // 自索引中取出對應的 word list
            var buf = (from t in keywordTokens select t.First).ToList();
            var query = from w in tblWordList.AsQueryable<WordItem>()
                        where w.Word.In(buf)
                        select new { w.WordId };
            List<Int32> wordIdList = new List<Int32>();
            foreach (var aWord in query)
            {
                wordIdList.Add(aWord.WordId);
            }

            // 自全文索引中，取出對應的記錄
            var indexes = from i in tblFullText.AsQueryable<InvertedIndex>()
                          where i.WordId.In(wordIdList)
                          select i;

            if (indexes.Count() != wordIdList.Count)
            {
                return null;
            }

            // 將每個 keyword token 對應回相對應的 index
            List<List<IndexElement>> checkedIndex = new List<List<IndexElement>>();
            foreach (var aToken in keywordTokens)
            {
                checkedIndex.Add(indexes.Where(t => t.Word == aToken.First).First().Indexes);
            }

            // 檢查各文件是否為符合的文件
            var firstTokenIndex = checkedIndex[0];
            Dictionary<Int32, Int32> hittedDocs = new Dictionary<Int32, Int32>();
            foreach (var currentIndex in firstTokenIndex)
            {
                if (keywordTokens.Count == 1 || CheckDocumentIsHitted(keywordTokens, 1, checkedIndex, currentIndex))
                {
                    if (hittedDocs.ContainsKey(currentIndex.DocId))
                        hittedDocs[currentIndex.DocId]++;
                    else
                        hittedDocs[currentIndex.DocId] = 1;
                }
            }

            // 將文件照點閱率排序
            var sortedDict = (from entry in hittedDocs orderby entry.Value descending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);

            return sortedDict;
        }
    }
}
