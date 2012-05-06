using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.chenlb.mmseg4j;
using MongoSearch.Structure;
using java.io;

namespace MongoSearch.Library
{
    public class ChineseSegmentor
    {
        protected Dictionary dic;

        protected Seg GetSeg()
        {
            return new ComplexSeg(dic);
        }

        /// <summary>
        /// 進行斷詞，取回斷詞後的結果
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public List<Pair<String, Int32>> SegWords(String txt)
        {
            Reader input = new StringReader(txt);
            Seg seg = GetSeg();
            MMSeg mmSeg = new MMSeg(input, seg);
            Word word = null;

            List<Pair<String, Int32>> result = new List<Pair<String, Int32>>();
            while ((word = mmSeg.next()) != null)
            {
                // 兩種 Offset 方式
                //word.getWordOffset();
                //word.getStartOffset();
                result.Add(new Pair<String, Int32>(word.getString(), word.getStartOffset()));
            }
            return result;
        }

        public void Test()
        {
            String txt = "這行文字是要被中文斷詞處理 this ia a book 的文章~~~可以從執行結果看斷詞是否成功 莊圓大師";
            List<Pair<String, Int32>> result = SegWords(txt);

            foreach (var aResult in result)
            {
                System.Console.WriteLine("{0}, {1}", aResult.First, aResult.Second);
            }
        }

        public ChineseSegmentor()
        {
            java.lang.System.setProperty("mmseg.dic.path", "./Dict");    // 指定自訂詞庫
            dic = Dictionary.getInstance();
        }
    }
}
