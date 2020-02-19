using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoConnector
{
    public interface IMongoGateway
    {
        Dictionary<string, object> First(MongoModel model);
        List<Dictionary<string, object>> Find(MongoModel model);
        void InsertOne(Dictionary<string, object> document);
        void InsertMany(List<Dictionary<string, object>> documents);
        void UpdateOne(Dictionary<string, object> filter, Dictionary<string, object> document);
        void BulkUpdate(List<Tuple<Dictionary<string, object>, Dictionary<string, object>>> docs);
        void DeleteOne(Dictionary<string, object> filter);
    }

    public class MongoGateway : MongoClient, IMongoGateway
    {
        private readonly IMongoModelQuery _mongoModelQuery;

        public MongoGateway(IMongoModelQuery mongoModelQuery, string connString)
            : base(mongoModelQuery.GetCollectionName(), connString)
        {
            _mongoModelQuery = mongoModelQuery;
        }

        public Dictionary<string, object> First(MongoModel model)
        {
            Dictionary<string, object> result;

            try
            {
                var modelQuery = _mongoModelQuery.GetModelQuery(model);

                var query = Collection
                        .Find(modelQuery.Filter)
                        .Project(modelQuery.Projection)
                        .FirstOrDefault();

                if (query == null) return null;

                var json = query.ToString();
                result = BsonSerializer
                    .Deserialize<Dictionary<string, object>>(json);
            }
            catch (Exception e)
            {
                throw new Exception($"MongoGateway.First {CollectionName} error. {e.Message}");
            }

            return result;
        }

        public List<Dictionary<string, object>> Find(MongoModel model)
        {
            List<Dictionary<string, object>> result;

            try
            {
                var modelQuery = _mongoModelQuery.GetModelQuery(model);

                var query = Collection
                        .Find(modelQuery.Filter)
                        .Project(modelQuery.Projection)
                        .ToList();

                var json = query.ToJson();
                result = BsonSerializer
                    .Deserialize<List<Dictionary<string, object>>>(json);
            }
            catch (Exception e)
            {
                throw new Exception($"MongoGateway.Find {CollectionName} error. {e.Message}");
            }

            return result;
        }

        public void InsertOne(Dictionary<string, object> document)
        {
            try
            {
                var modelQuery = _mongoModelQuery.CreateModelQuery(document);
                Collection.InsertOne(modelQuery.Insert);
            }
            catch (Exception e)
            {
                throw new Exception($"MongoGateway.InsertOne {CollectionName} error. {e.Message}");
            }
        }

        public void InsertMany(List<Dictionary<string, object>> documents)
        {
            try
            {
                var modelQuery = _mongoModelQuery.CreateManyModelQuery(documents);
                Collection.InsertMany(modelQuery.InsertMany);
            }
            catch (Exception e)
            {
                throw new Exception($"MongoGateway.InsertMany {CollectionName} error. {e.Message}");
            }
        }

        public void UpdateOne(Dictionary<string, object> filter, Dictionary<string, object> document)
        {
            try
            {
                var modelQuery = _mongoModelQuery.UpdateModelQuery(filter, document);
                Collection.UpdateOne(modelQuery.Filter, modelQuery.Update);
            }
            catch (Exception e)
            {
                throw new Exception($"MongoGateway.UpdateOne {CollectionName} error. {e.Message}");
            }
        }

        public void BulkUpdate(List<Tuple<Dictionary<string, object>, Dictionary<string, object>>> docs)
        {
            try
            {
                var modelQuery = _mongoModelQuery.BulkUpdateModelQuery(docs);
                Collection.BulkWrite(modelQuery.BulkUpdateList);
            }
            catch (Exception e)
            {
                throw new Exception($"MongoGateway.BulkUpdate {CollectionName} error. {e.Message}");
            }
        }

        public void DeleteOne(Dictionary<string, object> filter)
        {
            try
            {
                var modelQuery = _mongoModelQuery.DeleteModelQuery(filter);
                Collection.DeleteOne(modelQuery.Filter);
            }
            catch (Exception e)
            {
                throw new Exception($"MongoGateway.DeleteOne {CollectionName} error. {e.Message}");
            }
        }
    }
}
