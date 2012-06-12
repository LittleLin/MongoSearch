using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoSearch;
using MongoSearch.Structure;

namespace Demo
{
    class Program
    {
        static List<SourceDocument> CreateSrcDoc()
        {
            List<SourceDocument> srcDocs = new List<SourceDocument>();
            SourceDocument aSrcDoc = null;
            foreach (String aSrcFile in Directory.EnumerateFiles(@"D:\Projects\MongoSearch\Source"))
            {
                aSrcDoc = new SourceDocument();

                String rawContent = File.ReadAllText(aSrcFile);
                String[] paras = rawContent.Split(new String[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                aSrcDoc.FullText["title"] = paras[0];
                aSrcDoc.FullText["content"] = String.Join("\n", paras.Skip(1).Take(paras.Count() - 1));
                srcDocs.Add(aSrcDoc);
            }

            return srcDocs;
        }

        static void Main(string[] args)
        {
            // 1. Create index repository & Make Index.
            // 1.1 Create repository.
            Indexer indexer = Indexer.Instance;
            Repository repo = indexer.CreateRepository("Test");

            // 1.2 Add document by JSON format.
            repo.AddDocument("{'full_text': {'title': '一級淹水警戒區域 增至9縣市', " +
                             " 'text':'（中央社記者黃巧雯台北12日電）受鋒面和西南氣流影響，各地降豪雨。水利署表示，截至今天下午5時，共18座水庫正洩洪、溢流或調節性放水；一級淹水警戒區域也從11日發布時的4個縣市，增至9個縣市。'}, " +
                             " 'meta_data': {'created_time': '2012/06/01 13:24:33'}}");

            // 1.3 Add document by object format.
            SourceDocument srcDoc = new SourceDocument();
            srcDoc.FullText["title"] = "大雨灌進低底盤公車 乘客傻眼";
            srcDoc.FullText["text"] = "暴雨襲台，全台災情不斷，現在就連行駛中的公車也遭殃，早上7點多時，一輛從三峽開往新埔捷運站的802公車，因為底盤低，經過淹水路段時，大雨直接灌進公車裡，車上乘客全嚇傻了，就像在坐船一樣，相當誇張。";
            srcDoc.MetaData["created_time"] = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            repo.AddDocument(srcDoc);

            // 1.4 Make index.
            repo.MakeIndex();

            // 2. Search the documents            
            // 2.1 Search, returned by JSON format
            // 
            // Format: 
            //   {"matches":2,
            //    "search_time":0.041,
            //    "results":[
            //      {"rank":1,"score":2,"hit_field":"一級<<淹水>>警戒區域 增至9縣市\r\n（中央社記者黃巧雯台北12日電）受鋒面和西南氣流影響，各地降豪雨。水利署表示，截至今天下午5時，共18座水庫正洩洪、溢流或調節性放水；一級<<淹水>>警戒區域也從11日發布時的4個縣市，增至9個縣市。"},
            //      {"rank":2,"score":1,"hit_field":"大雨灌進低底盤公車 乘客傻眼\r\n暴雨襲台，全台災情不斷，現在就連行駛中的公車也遭殃，早上7點多時，一輛從三峽開往新埔捷運站的802公車，因為底盤低，經過<<淹水>>路段時，大雨直接灌進公車裡，車上乘客全嚇傻了，就像在坐船一樣，相當誇張。"}]
            //   }
            String jsonResult = repo.Search("淹水", 0, 3).ToJson();
            Console.WriteLine(jsonResult);

            // 2.2 Search, returned by object format
            SearchResult result = repo.Search("淹水", 0, 1);

            // 2.3 Print out the search result.
            Console.WriteLine("Matches: " + result.Matches);
            Console.WriteLine("Search Time: " + result.SearchTime);
            Console.WriteLine("==============================");

            foreach (var aDoc in result.Results)
            {
                Console.WriteLine("Rank: " + aDoc.Rank);
                Console.WriteLine("Score: " + aDoc.Score);
                Console.WriteLine("Content: " + aDoc.HitField);
                Console.WriteLine("==============================");
            }

            Console.ReadLine();
        }
    }
}
