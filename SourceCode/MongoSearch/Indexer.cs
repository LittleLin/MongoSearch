using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MongoDB.Driver;
using MongoSearch.Structure;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MongoSearch.Library;

namespace MongoSearch
{
    public class Indexer
    {
        /// <summary>
        /// MongoDB Server Address
        /// </summary>
        public String MongoDbServer { get; set; }

        /// <summary>
        /// MongoDB Db Name
        /// </summary>
        public String DbName { get; set; }

        /// <summary>
        /// 儲存所有索引庫資訊
        /// </summary>
        public Dictionary<String, Repository> Repositories { get; set; }

        /// <summary>
        /// private instance
        /// </summary>
        private static Indexer instance;

        private Indexer()
        {
            this.MongoDbServer = Constants.DefaultServerAddress;
            this.DbName = Constants.DefaultDbName;
            this.Repositories = new Dictionary<string, Repository>();

            // 取得 repository 資訊列表
            var server = MongoDbLib.GetServerConnection(MongoDbServer);            
            var database = server.GetDatabase(DbName);
            var tblRepos = database.GetCollection<RepositoryInfo>(Constants.TblRepository);
            var repos = from r in tblRepos.AsQueryable<RepositoryInfo>()
                        select r;
            foreach (var aRepo in repos)
            {
                this.Repositories.Add(aRepo.Name, new Repository(this, aRepo.Name));
            }
        }

        public static Indexer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Indexer();
                }
                return instance;
            }
        }

        /// <summary>
        /// 新增索引庫
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Repository CreateRepository(String name)
        {
            try
            {
                if (this.Repositories.ContainsKey(name))
                    return Repositories[name];
                else
                {
                    try
                    {
                        // 新增 repository 資訊
                        Repository repo = new Repository(this, name);
                        this.Repositories.Add(name, repo);

                        // 將 repository 資訊寫入 MongoDB 中
                        var server = MongoDbLib.GetServerConnection(MongoDbServer);
                        var database = server.GetDatabase(DbName);
                        var tblRepos = database.GetCollection<RepositoryInfo>(Constants.TblRepository);
                        tblRepos.Insert(new RepositoryInfo()
                        {
                            Name = name
                        });

                        return repo;
                    }
                    catch (Exception e)
                    {
                        // roll back
                        if (this.Repositories.ContainsKey(name))
                            this.Repositories.Remove(name);
                        throw e;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 取得索引庫
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Repository GetRepository(String name)
        {
            try
            {
                if (this.Repositories.ContainsKey(name))
                    return Repositories[name];
                else
                    return null;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 刪除索引庫
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool DeleteRepository(String name)
        {
            try
            {
                var server = MongoDbLib.GetServerConnection(MongoDbServer);
                var database = server.GetDatabase(DbName);
                database.DropCollection(Constants.TblFullText);
                database.DropCollection(Constants.TblSourceText);
                database.DropCollection(Constants.TblWordList);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
