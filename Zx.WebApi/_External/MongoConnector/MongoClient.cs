using System;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoConnector
{
    public class MongoClient
    {
        protected MongoDB.Driver.MongoClient Client;
        protected string DbName;
        protected string CollectionName;
        protected IMongoCollection<BsonDocument> Collection;

        public MongoClient(string collectionName, string connString)
        {
            try
            {
                CollectionName = collectionName;
                Client = new MongoDB.Driver.MongoClient(connString);
                DbName = MongoUrl.Create(connString).DatabaseName;
                var database = Client.GetDatabase(DbName);
                Collection = database.GetCollection<BsonDocument>(CollectionName);
            }
            catch (Exception e)
            {
                throw new Exception($"MongoConnector Error: {e.Message}");
            }
        }
    }
}
