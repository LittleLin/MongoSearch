using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoSearch.Library;
using MongoSearch.Structure;
using MongoDB.Driver.Linq;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace MongoSearch
{
    public class Repository
    {
        private String _name = "";
        private Indexer _indexer = null;
        private Searcher _searcher = null;
        private List<SourceDocument> _bufferedSrcDocs = null;

        /// <summary>
        /// 索引庫名稱
        /// </summary>
        public String Name { get { return _name; } }

        /// <summary>
        /// 所屬 Indexer
        /// </summary>
        public Indexer Indexer { get { return _indexer; } }

        /// <summary>
        /// 是否即時建索引
        /// </summary>
        public bool RealTimeIndexing { get; set; }

        public Repository(Indexer indexer, String name)
        {
            this._indexer = indexer;
            this._name = name;

            // 目前預設不做即時建索引
            this.RealTimeIndexing = false;

            // 初始化
            _bufferedSrcDocs = new List<SourceDocument>();
            _searcher = Searcher.Instance;
        }

        /// <summary>
        /// 批次加入文件、建索引
        /// </summary>
        /// <param name="srcDocs"></param>
        /// <returns></returns>
        public Boolean AddDocuments(IEnumerable<SourceDocument> srcDocs)
        {
            this._bufferedSrcDocs.AddRange(srcDocs);
            return true;
        }

        /// <summary>
        /// 批次加入文件、建索引 (JSON)
        /// </summary>
        /// <param name="srcDocs"></param>
        /// <returns></returns>
        public Boolean AddDocuments(String jsonDocs)
        {
            try
            {
                return AddDocuments(JsonConvert.DeserializeObject<List<SourceDocument>>(jsonDocs));
            }
            catch
            {
                return false;
            }
            
        }

        /// <summary>
        /// 加入單一文件，建索引
        /// </summary>
        /// <param name="srcDoc"></param>
        /// <returns></returns>
        public Boolean AddDocument(SourceDocument srcDoc)
        {
            this._bufferedSrcDocs.Add(srcDoc);
            return true;
        }

        /// <summary>
        /// 加入單一文件，建索引 (JSON)
        /// </summary>
        /// <param name="jsonDoc"></param>
        /// <returns></returns>
        public Boolean AddDocument(String jsonDoc)
        {
            try
            {
                return AddDocument(JsonConvert.DeserializeObject<SourceDocument>(jsonDoc));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析原始文件，存人 MongoDB 中
        /// </summary>
        /// <returns></returns>
        private bool ParseSourceDoc()
        {
            try
            {
                int docId = 0;

                // 取得 MongoDB Connection
                var server = MongoDbLib.GetServerConnection(_indexer.MongoDbServer);
                var database = server.GetDatabase(_indexer.DbName);
                var collection = database.GetCollection<SourceText>(Constants.TblSourceText);

                // 處理每份來源文件
                StringBuilder sb = new StringBuilder();
                foreach (var aDoc in _bufferedSrcDocs)
                {
                    // clear 
                    sb.Clear();
                    
                    // 取出 Full Text 欄位
                    foreach (var aField in aDoc.FullText)
                    {
                        sb.Append(aField.Value + "\n");
                    }

                    // 將 Full Text 解析成多個 Para
                    String rawContent = sb.ToString();
                    String[] paras = rawContent.Split(new String[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    // 將 Para 資訊存入 MongoDb 中                    
                    for (int i = 0; i < paras.Length; i++)
                    {
                        var doc = new SourceText();
                        doc.DocId = docId;
                        doc.ParaId = i;
                        doc.Para = paras[i];
                        collection.Insert(doc);
                    }

                    docId++;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 建索引主體
        /// </summary>
        /// <returns></returns>
        public bool MakeIndex()
        {
            try
            {
                // 1. 解文
                ParseSourceDoc();                

                // 2. 建索引
                ChineseSegmentor segmentor = new ChineseSegmentor();
                var server = MongoDbLib.GetServerConnection(_indexer.MongoDbServer);
                var database = server.GetDatabase(_indexer.DbName);
                var tblSourceText = database.GetCollection<SourceText>(Constants.TblSourceText);

                // 斷詞，處理每個 Token
                var sourcees = from s in tblSourceText.AsQueryable<SourceText>()
                               orderby s.DocId, s.ParaId
                               select s;
                Dictionary<String, InvertedIndex> fullIndexes = new Dictionary<String, InvertedIndex>();
                InvertedIndex aIndex = null;
                foreach (var aSourceText in sourcees)
                {
                    List<Pair<String, Int32>> result = segmentor.SegWords(aSourceText.Para);
                    foreach (var aToken in result)
                    {
                        if (fullIndexes.ContainsKey(aToken.First))
                        {
                            aIndex = fullIndexes[aToken.First];
                        }
                        else
                        {
                            aIndex = new InvertedIndex();
                            aIndex.Word = aToken.First;
                        }

                        aIndex.Indexes.Add(new IndexElement()
                        {
                            DocId = aSourceText.DocId,
                            ParaId = aSourceText.ParaId,
                            Offset = aToken.Second
                        });

                        fullIndexes[aToken.First] = aIndex;
                    }
                }

                // 在 Storage 存入 Word List
                var wordListCollection = database.GetCollection(Constants.TblWordList);
                List<BsonDocument> batch = new List<BsonDocument>();
                List<String> wordList = fullIndexes.Keys.ToList();
                for (int wordId = 0; wordId < fullIndexes.Count; wordId++)
                {
                    aIndex = fullIndexes[wordList[wordId]];
                    aIndex.WordId = wordId;

                    batch.Add(new BsonDocument()
                        {
                            { "Word", wordList[wordId] },
                            { "WordId", wordId}
                        });
                }

                wordListCollection.InsertBatch(batch);

                // 儲存全文索引
                var tblFullText = database.GetCollection(Constants.TblFullText);
                List<InvertedIndex> fullText = new List<InvertedIndex>();
                tblFullText.InsertBatch<InvertedIndex>(fullIndexes.Values.ToList());

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// 搜尋主體
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="fetchSize"></param>
        /// <returns></returns>
        public SearchResult Search(String keyword, int fetchSize)
        {
            return this.Search(keyword, 0, fetchSize);
        }

        /// <summary>
        /// 搜尋主體
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="startPos"></param>
        /// <param name="fetchSize"></param>
        /// <returns></returns>
        public SearchResult Search(String keyword, int startPos, int fetchSize)
        {
            return this._searcher.Search(this, keyword, startPos, fetchSize);
        }
    }
}
