using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace MongoSearch.Structure
{
    /// <summary>
    /// 儲存原始文件的資訊
    /// </summary>
    public class SourceText
    {
        public ObjectId Id { get; set; }

        /// <summary>
        /// Doc Id
        /// </summary>
        public Int32 DocId { get; set; }

        /// <summary>
        /// Para Id
        /// </summary>
        public Int32 ParaId { get; set; }

        /// <summary>
        /// Para 內文
        /// </summary>
        public String Para { get; set; }
    }
}
