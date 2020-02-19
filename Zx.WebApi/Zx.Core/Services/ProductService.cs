using System;
using System.Collections.Generic;
using System.Linq;
using MongoConnector;
using Zx.Core.Common;

namespace Zx.Core.Services
{
    public interface IProductService
    {
        object Get(string id);
        IEnumerable<object> Get();
        void Create(string data);
        void Update(string id, string data);
        void Delete(string id);
    }
    
    public class ProductService : BaseService, IProductService
    {
        private readonly IMongoGateway _mongoGateway;

        public ProductService(string connString)
        {
            _mongoGateway = new MongoGateway(
                new ProductModelQuery<ProductModel.Rootobject>(),
                connString);
        }

        public object Get(string id)
        {
            var model = new MongoModel
            {
                Filter = new KeyValuePair<string, string>("_id", id)
            };
            var result = _mongoGateway
                .First(model);

            return result;
        }

        public IEnumerable<object> Get()
        {
            var model = new MongoModel();

            var result = _mongoGateway
                .Find(model);

            return result;
        }

        public void Create(string data)
        {
            var requiredFields = new[]
            {
                "name",
                "category"
            };

            var doc = NJson.Deserialize<Dictionary<string, object>>(data);
            if (!Validate(doc, requiredFields)) return;
            DocNewGuid(doc, out var newDoc);

            _mongoGateway.InsertOne(newDoc);
        }

        public void Update(string id, string data)
        {
            var requiredFields = new[]
            {
                "name",
                "category"
            };

            var filter = NewDoc("_id", id);
            var doc = NJson.Deserialize<Dictionary<string, object>>(data);
            if (!ValidateAny(doc, requiredFields)) return;

            _mongoGateway.UpdateOne(filter, doc);
        }

        public void Delete(string id)
        {
            var filter = new Dictionary<string, object>() { { "_id", id } };

            _mongoGateway.DeleteOne(filter);
        }
    }
}
