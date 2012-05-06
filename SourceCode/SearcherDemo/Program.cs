using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoSearch;

namespace SearcherDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Searcher searcher = new Searcher();
            Dictionary<Int32, Int32> hittedDocs = searcher.Search("艾比希");

            // 顯示搜尋結果
            foreach (var aDoc in hittedDocs)
            {
                Console.WriteLine(searcher.GetDocumentById(aDoc.Key));
                Console.WriteLine("==============================");
            }

            Console.WriteLine("搜尋結束...");
            Console.ReadLine();
        }
    }
}
