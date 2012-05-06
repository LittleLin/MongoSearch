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

        public const String TblSourceText = "SourceText";

        public const String TblWordList = "WordList";

        public const String TblFullText = "FullText";
    }
}
