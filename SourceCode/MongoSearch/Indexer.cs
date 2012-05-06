using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MongoDB.Driver;
using MongoSearch.Structure;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MongoSearch.Library;

namespace MongoSearch
{
    public class Indexer
    {
        /// <summary>
        /// MongoDB Server Address
        /// </summary>
        public String ServerAddress { get; set; }

        /// <summary>
        /// MongoDB Index Db Name
        /// </summary>
        public String DbName { get; set; }

        private String srcDir;
        public Indexer(String srcDir)
        {
            this.srcDir = srcDir;
            this.ServerAddress = Constants.DefaultServerAddress;
            this.DbName = Constants.DefaultDbName;
        }

        /// <summary>
        /// 取得原始文件，並將文件中的原始全文資訊(Para) 存入 Storage 中
        /// </summary>
        /// <returns></returns>
        public bool Fetch()
        {
            try
            {
                int docId = 0;
                foreach (String aSrcFile in Directory.EnumerateFiles(srcDir))
                {
                    String rawContent = File.ReadAllText(aSrcFile);
                    String[] paras = rawContent.Split(new String[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    var server = MongoDbLib.GetServerConnection(ServerAddress);
                    var database = server.GetDatabase(DbName);
                    var collection = database.GetCollection<SourceDocument>(Constants.TblSourceText);

                    for (int i = 0; i < paras.Length; i++)
                    {
                        var doc = new SourceDocument();
                        doc.DocId = docId;
                        doc.ParaId = i;
                        doc.Para = paras[i];
                        collection.Insert(doc);
                    }

                    docId++;
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool MakeIndex()
        {
            try
            {
                // 1. 解文
                Fetch();

                // 2. 建索引
                ChineseSegmentor segmentor = new ChineseSegmentor();
                var server = MongoDbLib.GetServerConnection(ServerAddress);
                var database = server.GetDatabase(DbName);
                var tblSourceText = database.GetCollection<SourceDocument>(Constants.TblSourceText);

                // 斷詞，處理每個 Token
                var sourcees = from s in tblSourceText.AsQueryable<SourceDocument>()
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
    }
}
