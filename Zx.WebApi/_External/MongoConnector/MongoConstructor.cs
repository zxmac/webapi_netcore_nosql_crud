using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using System;

namespace MongoConnector
{
    public enum Enums
    {
        // Data value
        ALLOWNULL,

        // Mongo query
        QUERY_OR_IN,
        QUERY_OR,
        QUERY_WHERE,

        // Default
        NONE
    }

    public enum QueryOperators
    {
        And,
        Or,
        OrIn,
        OrInRegex,
        OrCommon,
        OrCombine,
        Where
    }

    public class MongoQuery
    {
        public QueryOperators Operator { get; set; }
        public Dictionary<string, object> Filter { get; set; }
        public List<Dictionary<string, object>> Filters { get; set; }
        public List<KeyValuePair<string, object>> CommonFilters { get; set; }
        //public Tuple<List<Dictionary<string, object>>, List<Dictionary<string, object>>> CombineFilters { get; set; }
        public List<Tuple<List<Dictionary<string, object>>, QueryOperators>> CombineFilters { get; set; }
    }

    public class MongoConstructor
    {
        private string _query;
        private Dictionary<string, object> _filters;
        private Dictionary<string, object> _params;
        private Dictionary<string, object[]> _filterIn;

        private ProjectionDefinition<BsonDocument> _projection;
        private List<string> _projections;

        private BsonDocument _dataInsert;
        private readonly List<BsonDocument> _dataInsertMany;

        private readonly UpdateDefinitionBuilder<BsonDocument> _updateBuilder;
        private List<UpdateDefinition<BsonDocument>> _dataUpdate;
        private readonly List<UpdateOneModel<BsonDocument>> _bulkUpdateList;

        private int _skip;
        private int _limit;

        public MongoConstructor()
        {
            _filters = new Dictionary<string, object>();
            _params = new Dictionary<string, object>();
            _filterIn = new Dictionary<string, object[]>();
            _projections = new List<string>();
            _dataInsert = null;
            _dataInsertMany = new List<BsonDocument>();
            _dataUpdate = new List<UpdateDefinition<BsonDocument>>();
            _bulkUpdateList = new List<UpdateOneModel<BsonDocument>>();
            _updateBuilder = Builders<BsonDocument>.Update;
            _skip = 0;
            _limit = 1000;
        }

        #region Query builder
        public IMongoDefinition FindQuery()
        {
            // Filters
            FilterDefinition<BsonDocument> filter = new BsonDocument();
            if(!string.IsNullOrEmpty(_query))
            {
                filter = _query;
            }
            else if (_filters.Any())
            {
                filter = new BsonDocument(_filters);
            }
            else if (_filterIn.Any())
            {
                filter = Builders<BsonDocument>.Filter.
                    In(_filterIn.First().Key, _filterIn.First().Value);
            }

            // Projection
            var projection = Builders<BsonDocument>.Projection.Combine();
            if (_projections.Any())
            {
                projection = Builders<BsonDocument>.Projection
                    .Combine(_projections.Select(x => Builders<BsonDocument>
                        .Projection.Include(x)).ToList());
            }
            else if (_projection != null)
            {
                projection = _projection;
            }

            return new MongoDefinition
            {
                Filter = filter,
                Projection = projection,
                Skip = _skip,
                Limit = _limit
            };
        }

        public IMongoDefinition InsertOneQuery()
        {
            var entity = _dataInsert == null
                    ? new BsonDocument(_params) : _dataInsert;

            return new MongoDefinition
            {
                Insert = entity
            };
        }

        public IMongoDefinition InsertManyQuery()
        {
            return new MongoDefinition
            {
                InsertMany = _dataInsertMany
            };
        }

        public IMongoDefinition UpdateOneQuery()
        {
            var filter = new BsonDocument(_filters);
            var entity = _updateBuilder.Combine(_dataUpdate);

            return new MongoDefinition
            {
                Filter = filter,
                Update = entity
            };
        }

        public IMongoDefinition BulkUpdateQuery()
        {
            return new MongoDefinition
            {
                BulkUpdateList = _bulkUpdateList
            };
        }

        public IMongoDefinition DeleteOneQuery()
        {
            var filter = new BsonDocument(_filters);

            return new MongoDefinition
            {
                Filter = filter
            };
        }

        public MongoConstructor Query(string query)
        {
            _query = query;
            return this;
        }
        #endregion

        public MongoConstructor AddParam(string key, object val)
        {
            _params.Add(key, val);
            return this;
        }

        public MongoConstructor AddParam(string key, DateTime val)
        {
            var date = BsonDateTime.Create(val);
            _params.Add(key, date);
            return this;
        }

        public MongoConstructor Param(Dictionary<string, object> data)
        {
            _params = data;
            return this;
        }

        public MongoConstructor AddFilter(string key, object val)
        {
            _filters.Add(key, val);
            return this;
        }

        public MongoConstructor Filter(Dictionary<string, object> filter)
        {
            _filters = filter;
            return this;
        }

        public MongoConstructor FilterIn(string key, string[] vals)
        {
            _filterIn.Add(key, vals);
            return this;
        }

        public MongoConstructor FilterIn(Dictionary<string, object[]> filterIn)
        {
            _filterIn = filterIn;
            return this;
        }

        public MongoConstructor AddProject(string prop)
        {
            _projections.Add(prop);
            return this;
        }

        public MongoConstructor AddProject(string[] projections)
        {
            _projections = projections.ToList();
            return this;
        }

        public MongoConstructor Project<T>()
        {
            _projection = Builders<BsonDocument>.Projection
                .Combine(typeof(T).GetProperties()
                    .Select(x => Builders<BsonDocument>
                        .Projection.Include(x.Name)).ToList());
            return this;
        }

        public MongoConstructor Skip(int skip)
        {
            _skip = skip;
            return this;
        }

        public MongoConstructor Limit(int limit)
        {
            _limit = limit;
            return this;
        }

        #region Insert methods
        public MongoConstructor DataInsert(Dictionary<string, object> data)
        {
            _dataInsert = new BsonDocument(data);
            return this;
        }

        public MongoConstructor AddDataInsert(string key, object val)
        {
            _dataInsert
                .AddRange(new Dictionary<string, object>
                {
                    { key, val }
                });
            return this;
        }

        public MongoConstructor AddDataInsert(string key, object date, string id, string name)
        {
            var bsonVal = new
            {
                on = BsonDateTime.Create(date),
                by = new
                {
                    _id = id,
                    name
                }
            };
            var doc = new Dictionary<string, object>
            {
                { key, bsonVal }
            }
            .ToBsonDocument();

            _dataInsert
                .AddRange(doc);

            return this;
        }

        public MongoConstructor AddDataInsertJson(string key, object val)
        {
            var doc = new Dictionary<string, object>
            {
                { key, BsonSerializer.Deserialize<BsonDocument>((string)val) }
            };

            _dataInsert.AddRange(doc);
            return this;
        }

        public MongoConstructor AddDataInsertJsonList(string key, object val)
        {
            var jsonList = (List<string>)val;
            var bsonVal = jsonList
                .Select(x => BsonSerializer.Deserialize<BsonDocument>(x))
                .ToList();
            var doc = new Dictionary<string, object>
            {
                { key, bsonVal }
            };

            _dataInsert.AddRange(doc);
            return this;
        }

        public MongoConstructor AddDataInsertArray(string key, object val)
        {
            var arr = CastToArray(val);
            var bsonVal = new BsonArray(arr);
            var doc = new Dictionary<string, object>
            {
                { key, bsonVal }
            };

            _dataInsert.AddRange(doc);
            return this;
        }

        public MongoConstructor AddToInsertList()
        {
            _dataInsertMany.Add(_dataInsert);
            _dataInsert = null;
            return this;
        }

        public MongoConstructor AddDocInsert(Dictionary<string, object> data)
        {
            _dataInsertMany.Add(data.ToBsonDocument());
            return this;
        }
        #endregion

        #region Update methods
        public MongoConstructor DataUpdate(Dictionary<string, object> data)
        {
            data.ToList().ForEach(x =>
            {
                _dataUpdate.Add(_updateBuilder.Set(x.Key, x.Value));
            });

            return this;
        }

        public MongoConstructor AddDataUpdate(string key, object val)
        {
            _dataUpdate.Add(_updateBuilder.Set(key, val));
            return this;
        }

        public MongoConstructor AddDataUpdate(string key, DateTime val)
        {
            var bsonDate = BsonDateTime.Create(val);
            _dataUpdate.Add(_updateBuilder.Set(key, bsonDate));
            return this;
        }

        public MongoConstructor AddDataUpdateInc(string key, object val)
        {
            _dataUpdate.Add(_updateBuilder.Inc(key, val));
            return this;
        }

        public MongoConstructor AddDataUpdatePush(string key, object val)
        {
            var jsonList = (List<string>)val;
            var bsonVal = jsonList
                .Select(x => BsonSerializer.Deserialize<BsonDocument>(x))
                .ToList();
            
            _dataUpdate.Add(_updateBuilder.PushEach(key, bsonVal));
            return this;
        }

        public MongoConstructor AddDataUpdateJson(string key, object val)
        {
            var bsonVal = BsonSerializer.Deserialize<BsonDocument>((string)val);
            _dataUpdate.Add(_updateBuilder.Set(key, bsonVal));
            return this;
        }

        public MongoConstructor AddDataUpdateJsonList(string key, object val)
        {
            var jsonList = (List<string>)val;
            var bsonVal = jsonList
                .Select(x => BsonSerializer.Deserialize<BsonDocument>(x))
                .ToList();
            
            _dataUpdate.Add(_updateBuilder.Set(key, bsonVal));
            return this;
        }

        public MongoConstructor AddDataUpdateArray(string key, object val)
        {
            var arr = CastToArray(val);
            var bsonVal = new BsonArray(arr);

            _dataUpdate.Add(_updateBuilder.Set(key, bsonVal));
            return this;
        }
        #endregion

        #region Bulk update methods
        public MongoConstructor AddToBulkUpdateList()
        {
            var filter = new BsonDocument(_filters);
            var entity = _updateBuilder.Combine(_dataUpdate);
            var updateOne = new UpdateOneModel<BsonDocument>(filter, entity);
            _bulkUpdateList.Add(updateOne);

            _filters = new Dictionary<string, object>();
            _dataUpdate = new List<UpdateDefinition<BsonDocument>>();
            return this;
        }
        #endregion

        public Dictionary<string, object> GetDocument()
        {
            return _params;
        }

        public string ToJson(object data)
        {
            return data.ToJson();
        }

        public MongoConstructor DataJson(string json)
        {
            _dataInsert = BsonSerializer.Deserialize<BsonDocument>(json);
            return this;
        }

        public MongoConstructor ToBsonDocument(object data)
        {
            _dataInsert = data.ToBsonDocument();
            return this;
        }

        #region Mongo query
        public MongoConstructor FilterQueries(Tuple<QueryOperators, List<MongoQuery>> mongoQueries)
        {
            var allQuery = new System.Text.StringBuilder();
            var queryOperator = mongoQueries.Item1;

            var queryOperatorStr = queryOperator.ToString().ToLower();
            var queryDataList = mongoQueries.Item2;

            allQuery.Append("{$" + queryOperatorStr + ":[");

            var queryList = queryDataList
                .Select(queryData =>
                {
                    var query = FilterBaseQuery(queryData);
                    return query;
                })
                .ToList();

            var queries = string.Join(',', queryList);
            allQuery.Append(queries);

            allQuery.Append("]}");

            _query = allQuery.ToString();
            return this;
        }

        public MongoConstructor FilterQuery(MongoQuery mongoQuery)
        {
            _query = FilterBaseQuery(mongoQuery);
            return this;
        }

        public string FilterBaseQuery(MongoQuery mongoQuery)
        {
            switch (mongoQuery.Operator)
            {
                case QueryOperators.And:
                    {
                        var query = mongoQuery.Filter.ToJson();
                        return query;
                    }
                case QueryOperators.Or:
                    {
                        var query = FilterOrQuery(mongoQuery.Filters);
                        return query;
                    }
                case QueryOperators.OrCommon:
                    {
                        var query = FilterOrCommonQuery(mongoQuery.CommonFilters);
                        return query;
                    }
                case QueryOperators.OrIn:
                    {
                        var query = FilterOrInQuery(mongoQuery.Filters);
                        return query;
                    }
                case QueryOperators.OrCombine:
                    {
                        //var query = FilterOrCombineQuery(
                        //    mongoQuery.CombineFilters.Item1,
                        //    mongoQuery.CombineFilters.Item2);

                        var query = FilterOrCombineQuery(mongoQuery.CombineFilters);

                        return query;
                    }
                default: return null;
            }
        }

        public string FilterOrCombineQuery(List<Tuple<List<Dictionary<string, object>>, QueryOperators>> CombineFilters)
        {
            var allFilterQuery = new List<string>();

            foreach (var filter in CombineFilters)
            {
                List<string> filterQuery = null;
                switch (filter.Item2)
                {
                    case QueryOperators.OrIn:
                        {
                            filterQuery = FilterOrInListQuery(filter.Item1);
                        }
                        break;
                    case QueryOperators.OrInRegex:
                        {
                            filterQuery = FilterOrInRegexListQuery(filter.Item1);
                        }
                        break;
                    case QueryOperators.Or:
                        {
                            filterQuery = FilterOrListQuery(filter.Item1);
                        }
                        break;
                    case QueryOperators.Where:
                        {
                            //var query = FilterWhereListQuery(filter.Item1);
                            //allFilterQuery.Add(query);
                            throw new InvalidOperationException();
                        }
                }

                if (filterQuery != null)
                    allFilterQuery.AddRange(filterQuery);
            }

            return "{$or:[" + string.Join(",", allFilterQuery) + "]}";
        }

        //public string FilterOrCombineQuery(List<Dictionary<string, object>> orInfilters, List<Dictionary<string, object>> orFilters)
        //{
        //    var filterQuery = FilterOrInListQuery(orInfilters);

        //    var filterQuery2 = FilterOrListQuery(orFilters);

        //    filterQuery.AddRange(filterQuery2);

        //    if (!filterQuery.Any()) return null;

        //    return "{$or:[" + string.Join(",", filterQuery) + "]}";
        //}

        public string FilterOrInQuery(List<Dictionary<string, object>> filters)
        {
            var filterQuery = FilterOrInListQuery(filters);

            if (!filterQuery.Any()) return null;

            return "{$or:[" + string.Join(",", filterQuery) + "]}";
        }

        public string FilterOrQuery(List<Dictionary<string, object>> filters)
        {
            var filterQuery = FilterOrListQuery(filters);

            if (!filterQuery.Any()) return null;

            return "{$or:[" + string.Join(",", filterQuery) + "]}";
        }

        public string FilterOrCommonQuery(List<KeyValuePair<string, object>> filters)
        {
            var filterQuery = FilterOrCommonListQuery(filters);

            if (!filterQuery.Any()) return null;

            return "{$or:[" + string.Join(",", filterQuery) + "]}";
        }

        public List<string> FilterOrListQuery(List<Dictionary<string, object>> filters)
        {
            var filterQueries = filters
                .Select(filter =>
                {
                    var filterQuery = filter
                        .Select(x =>
                        {
                            if (x.Value is bool)
                                return x.Key + ":" + FormatBool(x.Value);
                            if (x.Value is string)
                                return x.Key + ":'" + x.Value + "'";

                            return x.Key + ":" + x.Value;
                        })
                        .ToList();

                    return "{" + string.Join(",", filterQuery) + "}";
                })
                .ToList();

            return filterQueries;
        }

        public List<string> FilterOrInListQuery(List<Dictionary<string, object>> filters)
        {
            var filterQuery = filters
                .SelectMany(x => x.ToList())
                .GroupBy(x => x.Key)
                .Select(grp =>
                {
                    var values = grp.Select(x => x.Value).ToList();

                    return "{" + grp.Key + ":" + "{" + "$in:" + values.ToJson() + "}}";
                })
                .ToList();

            return filterQuery;
        }

        public List<string> FilterOrInRegexListQuery(List<Dictionary<string, object>> filters)
        {
            var filterQuery = filters
                .SelectMany(x => x.ToList())
                .GroupBy(x => x.Key)
                .Select(grp =>
                {
                    var values = grp.Select(x => x.Value).ToList();

                    return "{" + grp.Key + ":" + "{" + "$in: [" + string.Join(",", values) + "]}}";
                })
                .ToList();

            return filterQuery;
        }

        public List<string> FilterOrCommonListQuery(List<KeyValuePair<string, object>> filters)
        {
            var filterQuery = filters
                .Select(x =>
                {
                    if (x.Value is bool)
                        return "{" + x.Key + ":" + FormatBool(x.Value) + "}";

                    return "{" + x.Key + ":" + x.Value + "}";
                })
                .ToList();

            return filterQuery;
        }

        public string FilterWhereListQuery(List<Dictionary<string, object>> filters)
        {
            var filterQuery = filters
                .Where(x => x.Keys.Count == 1
                    && !string.IsNullOrEmpty(x.First().Key)
                    && x.First().Value is string s
                    && !string.IsNullOrEmpty(s))
                .Select(x =>
                {
                    var doc = x.First();

                    return $"this.{doc.Key}.replace(/[ -]/g,'') == '{doc.Value}'";
                })
                .ToArray();

            var key = filters.First().First().Key;

            var query = "{$where: " + '"'
                + $"this.{key} != null && "
                + string.Join(" || ", filterQuery) + '"' + "}";

            return query;
        }

        public string FormatBool(object v)
        {
            return v.ToString().ToLower();
        }
        #endregion

        public object[] CastToArray(object obj)
        {
            var result = ((System.Collections.IEnumerable)obj)
                .Cast<object>()
                .ToArray();

            return result;
        }
    }
}
