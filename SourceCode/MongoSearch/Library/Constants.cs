using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoSearch.Library
{
    public class Constants
    {
        /// <summary>
        /// 預設 MongoDB Server 位址
        /// </summary>
        public const String DefaultServerAddress = "localhost";

        /// <summary>
        /// 預設 MongoDB Db Name
        /// </summary>
        public const String DefaultDbName = "Search";

        /// <summary>
        /// 索引庫資料存放處
        /// </summary>
        public const String TblRepository = "_Repositories";

        /// <summary>
        /// 原始文件
        /// </summary>
        public const String TblSourceText = "SourceText";

        /// <summary>
        /// Word List
        /// </summary>
        public const String TblWordList = "WordList";

        /// <summary>
        /// 全文索引庫
        /// </summary>
        public const String TblFullText = "FullText";
    }
}
