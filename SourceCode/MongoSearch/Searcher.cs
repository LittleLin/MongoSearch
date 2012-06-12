using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoSearch.Structure;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoSearch.Library;
using MongoDb.Structure;
using System.Diagnostics;

namespace MongoSearch
{
    public class Searcher
    {
        private ChineseSegmentor _segmentor = null;

        /// <summary>
        /// MongoDB Server Address
        /// </summary>
        public String MongoDbServer { get; set; }

        /// <summary>
        /// MongoDB Db Name
        /// </summary>
        public String DbName { get; set; }

        /// <summary>
        /// private instance
        /// </summary>
        private static Searcher instance;

        private Searcher()
        {
            this.MongoDbServer = Constants.DefaultServerAddress;
            this.DbName = Constants.DefaultDbName;

            _segmentor = new ChineseSegmentor();
        }

        public static Searcher Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Searcher();
                }
                return instance;
            }
        }

        /// <summary>
        /// 確認文件是否被搜尋到
        /// </summary>
        /// <param name="keywordTokens"></param>
        /// <param name="tokenSeq"></param>
        /// <param name="checkedIndex"></param>
        /// <param name="previosIndex"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 以 Doc Id 取出文件
        /// </summary>
        /// <param name="docId"></param>
        /// <returns></returns>
        private String GetDocumentById(int docId)
        {
            try
            {
                var server = MongoDbLib.GetServerConnection(MongoDbServer);
                var database = server.GetDatabase(DbName);
                var tblSourceText = database.GetCollection<WordItem>(Constants.TblSourceText);

                var sourceTexts = from s in tblSourceText.AsQueryable<SourceText>()
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

        /// <summary>
        /// 搜尋主體
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="fetchSize"></param>
        /// <returns></returns>
        public SearchResult Search(Repository repo, String keyword, int fetchSize)
        {
            try
            {
                return Search(repo, keyword, 0, fetchSize);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public SearchResult Search(Repository repo, String keyword, int startPos, int fetchSize)
        {
            try
            {
                SearchResult result = new SearchResult();

                // 計時器
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();

                // MongoDb 初始化
                var server = MongoDbLib.GetServerConnection(MongoDbServer);
                var database = server.GetDatabase(DbName);
                var tblWordList = database.GetCollection<WordItem>(Constants.TblWordList);
                var tblFullText = database.GetCollection<InvertedIndex>(Constants.TblFullText);

                // 針對搜尋關鍵字斷詞                
                List<Pair<String, Int32>> keywordTokens = _segmentor.SegWords(keyword);

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

                // word id 為 0 筆，表示搜尋結果為 0
                if (wordIdList.Count == 0)
                {
                    sw.Stop();
                    result.SearchTime = sw.ElapsedMilliseconds / 1000.0;
                    return result;
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

                // 文件照分數排序，取出指定區間的 doc id 列表
                var sortedDocIds = (from entry in hittedDocs orderby entry.Value descending select entry.Key).Skip(startPos).Take(fetchSize).ToList();

                // 結果儲存
                result.Matches = hittedDocs.Count;
                sw.Stop();
                result.SearchTime = sw.ElapsedMilliseconds / 1000.0;

                for (int i = 0; i < fetchSize && i < sortedDocIds.Count; i++)
                {
                    String rawText = this.GetDocumentById(sortedDocIds[i]);
                    result.Results.Add(new ResultItem()
                    {
                        Rank = startPos + 1 + i,
                        Score = hittedDocs[sortedDocIds[i]],
                        HitField = rawText.Replace(keyword, "<<" + keyword + ">>")
                    });
                }
                return result;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
