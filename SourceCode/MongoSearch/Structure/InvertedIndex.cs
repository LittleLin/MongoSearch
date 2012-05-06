using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace MongoSearch.Structure
{
    public class InvertedIndex
    {
        public ObjectId Id { get; set; }

        public String Word { get; set; }
        public Int32 WordId { get; set; }

        public List<IndexElement> Indexes { get; set; }

        public InvertedIndex()
        {
            Indexes = new List<IndexElement>();
        }
    }

    public class IndexElement
    {
        public Int32 DocId { get; set; }

        public Int32 ParaId { get; set; }

        public Int32 Offset { get; set; }
    }
}
