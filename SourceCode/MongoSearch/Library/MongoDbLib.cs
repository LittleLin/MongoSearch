using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;

namespace MongoSearch.Library
{
    public class MongoDbLib
    {
        public static MongoServer GetServerConnection(String address)
        {
            var connectionString = string.Format("mongodb://{0}/?safe=true", address);
            var server = MongoServer.Create(connectionString);

            return server;
        }
    }
}
