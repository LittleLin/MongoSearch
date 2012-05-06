using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NDesk.Options;
using MongoSearch;

namespace IndexerDemo
{
    class Program
    {
        static void ShowHelp(OptionSet p)
        {
            System.Console.WriteLine("Usage: greet [OPTIONS]+ message");
            System.Console.WriteLine("Greet a list of individuals with an optional message.");
            System.Console.WriteLine("If no message is specified, a generic greeting is used.");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            p.WriteOptionDescriptions(System.Console.Out);
        }

        static void Main(string[] args)
        {
            bool showHelp = false;
            String srcDir = null;

            var p = new OptionSet() {
                    { "s|src=", "原始資料目錄", s => srcDir = s },
                    { "h|help", "顯示程式用法說明", v => showHelp = v != null},
                };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                System.Console.Write("Indexer: ");
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine("Try `Indexer --help' for more information.");
                return;
            }

            // 顯示指令說明
            if (showHelp)
            {
                ShowHelp(p);
                return;
            }

            // 原始資料目錄未指定
            if (String.IsNullOrWhiteSpace(srcDir))
            {
                System.Console.Error.WriteLine("請以 -s 參數指定原始資料目錄！");
                return;
            }

            // 原始資料目錄不存在
            if (!System.IO.Directory.Exists(srcDir))
            {
                System.Console.Error.WriteLine("原始資料目錄不存在！");
                return;
            }

            Indexer indexer = new Indexer(srcDir);
            indexer.MakeIndex();

            Console.WriteLine("建索引已結束...");
            System.Console.ReadLine();
        }
    }
}
