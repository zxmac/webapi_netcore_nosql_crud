using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoConnector
{
    public interface IMongoCollection
    {
        string _id { get; set; }
    }

    public interface IMongoDefinition
    {
        FilterDefinition<BsonDocument> Filter { get; set; }
        ProjectionDefinition<BsonDocument> Projection { get; set; }
        BsonDocument Insert { get; set; }
        List<BsonDocument> InsertMany { get; set; }
        UpdateDefinition<BsonDocument> Update { get; set; }
        List<UpdateOneModel<BsonDocument>> BulkUpdateList{ get; set; }
        int Skip { get; set; }
        int Limit { get; set; }
    }

    public interface IMongoModelQuery
    {
        string GetCollectionName();
        IMongoDefinition GetModelQuery(MongoModel model);
        IMongoDefinition CreateModelQuery(Dictionary<string, object> doc);
        IMongoDefinition CreateManyModelQuery(List<Dictionary<string, object>> docs);
        IMongoDefinition UpdateModelQuery(Dictionary<string, object> filter, Dictionary<string, object> docs);
        IMongoDefinition BulkUpdateModelQuery(List<Tuple<Dictionary<string, object>, Dictionary<string, object>>> docs);
        IMongoDefinition DeleteModelQuery(Dictionary<string, object> doc);
    }

    public class MongoModel
    {
        public KeyValuePair<string, string> Filter { get; set; }
        public Dictionary<string, object> Filters { get; set; }
        public Dictionary<string, object[]> FilterIn { get; set; }
        public Tuple<QueryOperators, List<MongoQuery>> FilterQueries { get; set; }
        public MongoQuery FilterQuery { get; set; }
        public string[] Project { get; set; }
        public string Query { get; set; }
    }

    public class Created
    {
        public Created() { }
        public Created(By _by)
        {
            by = _by;
            on = DateTime.UtcNow;
        }

        public object on { get; set; }
        public By by { get; set; }
    }

    public class Modified
    {
        public Modified() { }
        public Modified(By _by)
        {
            by = _by;
        }

        public object on { get; set; }
        public By by { get; set; }
    }

    public class By
    {
        public string _id { get; set; }
        public string name { get; set; }
    }

    public class MongoBaseModelQuery
    {
        protected void ConstructDefault<T>(MongoModel model, out MongoConstructor output)
            where T : IMongoCollection
        {
            var constructor = new MongoConstructor();

            if (!string.IsNullOrEmpty(model.Query))
                constructor.Query(model.Query);
            else if (!string.IsNullOrEmpty(model.Filter.Key))
                constructor.AddFilter(model.Filter.Key, model.Filter.Value);
            else if (model.Filters != null)
                constructor.Filter(model.Filters);
            else if (model.FilterIn != null)
                constructor.FilterIn(model.FilterIn);
            else if (model.FilterQuery != null)
                constructor.FilterQuery(model.FilterQuery);
            else if (model.FilterQueries != null)
                constructor.FilterQueries(model.FilterQueries);

            if (model.Project != null)
                constructor.AddProject(model.Project);
            else
                constructor.Project<T>();

            output = constructor;
        }

        protected void ConstructInsertJson(string key, Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            var data = document.FirstOrDefault(x => x.Key == key);

            if (data.Value == null) return;
            constructor.AddDataInsertJson(key, data.Value);
        }

        protected void ConstructInsertJsonList(string key, Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            var data = document.FirstOrDefault(x => x.Key == key);

            if (data.Value == null) return;
            constructor.AddDataInsertJsonList(key, data.Value);
        }

        protected void ConstructInsertArray(string key, Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            var data = document.FirstOrDefault(x => x.Key == key);

            if (data.Value == null) return;
            constructor.AddDataInsertArray(key, data.Value);
        }

        protected void ConstructUpdateJson(string key, Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            var data = document.FirstOrDefault(x => x.Key == key);

            if (data.Value == null) return;
            constructor.AddDataUpdateJson(key, data.Value);
        }

        protected void ConstructUpdateJsonList(string key, Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            var data = document.FirstOrDefault(x => x.Key == key);

            if (data.Value == null) return;
            constructor.AddDataUpdateJsonList(key, data.Value);
        }

        protected void ConstructUpdateArray(string key, Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            var data = document.FirstOrDefault(x => x.Key == key);

            if (data.Value == null) return;
            constructor.AddDataUpdateArray(key, data.Value);
        }

        protected void ConstructUpdateInc(string key, Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            var data = document.FirstOrDefault(x => x.Key == key);

            if (data.Value == null) return;
            constructor.AddDataUpdateInc(key, data.Value);
        }

        protected void ConstructUpdatePush(string key, Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            var data = document.FirstOrDefault(x => x.Key == key);

            if (data.Value == null) return;
            constructor.AddDataUpdatePush(key, data.Value);
        }

        protected void ConstructUpdatePushJson(string key, Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            var data = document.FirstOrDefault(x => x.Key == key);

            if (data.Value == null) return;
            constructor.AddDataUpdatePush(key, data.Value);
        }

        protected void ConstructAllowNull(string key, string value, ref MongoConstructor constructor)
        {
            var enumVal = CovertToEnum(value, Enums.NONE);
            if (enumVal == Enums.ALLOWNULL)
                constructor.AddDataUpdate(key, null);
        }

        private static bool IsNullDoc(Dictionary<string, object> document, string key)
        {
            return document.FirstOrDefault(x => x.Key == key).Value == null;
        }

        protected void ConstructCreated(Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            if (!IsNullDoc(document, "created"))
            {
                var objCreated = document["created"];
                var created = BsonSerializer.Deserialize<Created>(objCreated.ToString());

                constructor.AddDataInsert("created", DateTime.UtcNow, created.by._id, created.by.name);
            }

            if (!IsNullDoc(document, "modified"))
            {
                var objModified = document["modified"];
                var modified = BsonSerializer.Deserialize<Modified>(objModified.ToString());

                constructor.AddDataInsert("modified", DateTime.UtcNow, modified.by._id, modified.by.name);
            }
        }

        protected void ConstructModified(Dictionary<string, object> document, ref MongoConstructor constructor)
        {
            if (IsNullDoc(document, "modified")) return;

            var obj = document["modified"];

            var modified = BsonSerializer.Deserialize<Modified>(obj.ToString());
            var by = modified?.by ?? new By();

            if (string.IsNullOrEmpty(by._id) || string.IsNullOrEmpty(by.name))
                throw new InvalidOperationException();

            constructor.AddDataUpdate("modified.on", DateTime.UtcNow);
            constructor.AddDataUpdate("modified.by._id", by._id);
            constructor.AddDataUpdate("modified.by.name", by.name);
        }

        protected string[] TrimProps<T>(params string[] exclude)
        {
            return typeof(T).GetProperties()
                .Where(x => exclude.All(xx => xx != x.Name))
                .Select(x => x.Name)
                .ToArray();
        }

        protected void SchemaTrim<T, T2>(T data, out T output)
            where T : Dictionary<string, object>
            where T2 : IMongoCollection
        {
            var fields = typeof(T2).GetProperties()
                .Select(x => x.Name)
                .ToArray();

            SchemaTrim(data, fields, out T output1);

            output = output1;
        }

        protected void SchemaTrim<T>(T data, string[] trimFields, out T output)
            where T : Dictionary<string, object>
        {
            var schema = data
                .Where(x => trimFields == null
                    || trimFields.Any(xx => xx == x.Key))
                .Where(x => x.Key != "created" || x.Key != "modified")
                .ToDictionary(x => x.Key, x => x.Value);

            output = (T)schema;
        }

        protected void SchemaExclude<T>(T data, string[] excludeFields, out T output)
            where T : Dictionary<string, object>
        {
            var schema = data
                .Where(x => excludeFields.All(xx => xx != x.Key))
                .Where(x => x.Key != "created" && x.Key != "modified")
                .ToDictionary(x => x.Key, x => x.Value);

            output = (T)schema;
        }

        protected TEnum CovertToEnum<TEnum>(string strEnumValue, TEnum defaultValue)
        {
            if (!Enum.IsDefined(typeof(TEnum), strEnumValue))
                return defaultValue;

            return (TEnum)Enum.Parse(typeof(TEnum), strEnumValue);
        }

        protected object GetValue(Dictionary<string, object> doc, string key)
        {
            return doc.FirstOrDefault(x => x.Key == key).Value;
        }
    }

    public class MongoDefinition : IMongoDefinition
    {
        public FilterDefinition<BsonDocument> Filter { get; set; }
        public ProjectionDefinition<BsonDocument> Projection { get; set; }
        public BsonDocument Insert { get; set; }
        public List<BsonDocument> InsertMany { get; set; }
        public UpdateDefinition<BsonDocument> Update { get; set; }
        public List<UpdateOneModel<BsonDocument>> BulkUpdateList { get; set; }
        public int Skip { get; set; }
        public int Limit { get; set; }
    }
}
