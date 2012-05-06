using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace MongoDb.Structure
{
    public class WordItem
    {
        public ObjectId Id { get; set; }

        public String Word { get; set; }

        public Int32 WordId { get; set; }
    }
}
