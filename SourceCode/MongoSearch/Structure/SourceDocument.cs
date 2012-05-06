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
    public class SourceDocument
    {
        public ObjectId Id { get; set; }

        public Int32 DocId { get; set; }
        public Int32 ParaId { get; set; }
        public String Para { get; set; }

        //public List<String> Paras { get; set; }
    }
}
