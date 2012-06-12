using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace MongoSearch.Structure
{
    public class RepositoryInfo
    {
        public ObjectId Id { get; set; }

        /// <summary>
        /// 索引庫名稱
        /// </summary>
        public String Name { get; set; }
    }
}
