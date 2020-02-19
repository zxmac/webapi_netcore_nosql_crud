using System;
using System.Collections.Generic;
using MongoConnector;

namespace Zx.Core.Services
{
    public class CustomerModel
    {
        public class Rootobject : IMongoCollection
        {
            public string _id { get; set; }
            public string name { get; set; }
            public string email { get; set; }
            public string number { get; set; }
        }
    }

    public class CustomerModelQuery<T> : MongoBaseModelQuery, IMongoModelQuery
        where T : IMongoCollection
    {
        public string GetCollectionName() => "customers";

        public IMongoDefinition GetModelQuery(MongoModel model)
        {
            ConstructDefault<T>(model, out var constructor);

            return constructor.FindQuery();
        }

        public IMongoDefinition CreateModelQuery(Dictionary<string, object> document)
        {
            var constructor = new MongoConstructor();
            SchemaTrim<Dictionary<string, object>, T>(document, out var newDoc);
            constructor.DataInsert(newDoc);

            return constructor.InsertOneQuery();
        }

        public IMongoDefinition UpdateModelQuery(Dictionary<string, object> filter, Dictionary<string, object> document)
        {
            var constructor = new MongoConstructor();
            SchemaTrim<Dictionary<string, object>, T>(document, out var newDoc);
            constructor.Filter(filter);
            constructor.DataUpdate(newDoc);

            return constructor.UpdateOneQuery();
        }

        public IMongoDefinition DeleteModelQuery(Dictionary<string, object> filter)
        {
            var constructor = new MongoConstructor();
            constructor.Filter(filter);

            return constructor.DeleteOneQuery();
        }

        public IMongoDefinition CreateManyModelQuery(List<Dictionary<string, object>> docs)
        {
            throw new System.NotImplementedException();
        }

        public IMongoDefinition BulkUpdateModelQuery(List<Tuple<Dictionary<string, object>, Dictionary<string, object>>> docs)
        {
            throw new System.NotImplementedException();
        }
    }
}
