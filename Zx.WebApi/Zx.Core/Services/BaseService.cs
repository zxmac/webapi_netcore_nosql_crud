using System;
using System.Collections.Generic;
using System.Linq;
using Zx.Core.Common;

namespace Zx.Core.Services
{
    public class BaseService
    {
        protected string NewGuid() => System.Guid.NewGuid().ToString().ToUpper();

        protected void DocNewGuid(Dictionary<string, object> doc, out Dictionary<string, object> output)
        {
            //_id on top
            var newDoc = NewDoc("_id", NewGuid());
            output = newDoc.Union(doc).ToDictionary(x => x.Key, x => x.Value);
        }

        protected Dictionary<string, object> NewDoc(string key, object value)
        {
            return new Dictionary<string, object> { { key, value} };
        }

        protected void ModelDocument<T>(Dictionary<string, object> doc, out Dictionary<string, object> output)
        {
            var modelFields = GetFields<T>();

            output = doc
                .Where(x => modelFields.Any(xx => xx == x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        protected void FormatToDocJson(Dictionary<string, object> doc, string[] objKeys, string[] objListKeys, out Dictionary<string, object> output)
        {
            var jsonDoc = doc
                .Select(x =>
                {
                    if (objKeys != null && objKeys.Any(xx => xx == x.Key))
                    {
                        if (x.Value == null)
                        {
                            var val = NJson.Serialize(new object());
                            return new KeyValuePair<string, object>(x.Key, val);
                        }

                        var json = NJson.Serialize(x.Value);
                        return new KeyValuePair<string, object>(x.Key, json);
                    }

                    if (objListKeys != null && objListKeys.Any(xx => xx == x.Key))
                    {
                        if (x.Value == null)
                        {
                            var val = new List<string>();
                            return new KeyValuePair<string, object>(x.Key, val);
                        }

                        var objList = NJson.Deserialize<List<object>>(x.Value);
                        var jsonList = objList
                            .Select(NJson.Serialize)
                            .ToList();

                        return new KeyValuePair<string, object>(x.Key, jsonList);
                    }

                    return x;
                })
                .ToDictionary(x => x.Key, x => x.Value);

            output = jsonDoc;
        }

        protected void ConvertToDocument<T>(T model, out Dictionary<string, object> output)
        {
            output = NJson.Deserialize<Dictionary<string, object>>(model);
        }

        protected void ConvertToModel<T>(Dictionary<string, object> doc, out T output)
        {
            var modelFields = GetFields<T>();

            var modelDoc = doc
                .Where(x => modelFields.Any(xx => xx == x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            output = NJson.Deserialize<T>(modelDoc);
        }

        protected bool Validate(Dictionary<string, object> doc, string[] requiredFields)
        {
            var validated = doc
                .Where(x => requiredFields.Any(xx => xx == x.Key))
                .Where(x => x.Value is string val && !string.IsNullOrEmpty(val))
                .ToList();

            return validated.Count == requiredFields.Length;
        }

        protected bool ValidateAny(Dictionary<string, object> doc, string[] requiredFields)
        {
            var validated = doc
                .Any(x => requiredFields.Any(xx => xx == x.Key));

            return validated;
        }

        protected void DocumentTrim(object data, string[] trimFields, out Dictionary<string, object> output)
        {
            var doc = NJson.Deserialize<Dictionary<string, object>>(data);
            var newDoc = doc
                .Where(x => trimFields.Any(xx => xx == x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            output = newDoc;
        }

        protected void DocumentTrim(Dictionary<string, object> doc, string[] trimFields, out Dictionary<string, object> output)
        {
            var newDoc = doc
                .Where(x => trimFields.Any(xx => xx == x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            output = newDoc;
        }

        protected string[] GetFields<T>()
        {
            return typeof(T).GetProperties()
                .Select(x => x.Name)
                .ToArray();
        }

        protected string[] GetFields<T>(string[] excludeFields)
        {
            return typeof(T).GetProperties()
                .Where(x => excludeFields != null
                    && excludeFields.All(xx => xx != x.Name))
                .Select(x => x.Name)
                .ToArray();
        }

        protected string[] GetStringFields<T>()
        {
            return typeof(T).GetProperties()
                .Where(x => x.PropertyType == typeof(string))
                .Select(x => x.Name)
                .ToArray();
        }

        protected string[] GetStringFields<T>(string[] excludeFields)
        {
            return typeof(T).GetProperties()
                .Where(x => x.PropertyType == typeof(string))
                .Where(x => excludeFields != null
                    && excludeFields.All(xx => xx != x.Name))
                .Select(x => x.Name)
                .ToArray();
        }

        //protected void AppendProps(Dictionary<string, object> doc, IMongoCollection clss, out Dictionary<string, object> output)
        //{
        //    var baseDocs = NJson.Deserialize<Dictionary<string, object>>(clss);
        //    var toAppendDocs = baseDocs
        //        .Where(x => doc.Keys.All(xx => xx != x.Key))
        //        .ToDictionary(x => x.Key, x => x.Value);

        //    output = doc
        //        .Union(toAppendDocs)
        //        .ToDictionary(x => x.Key, x => x.Value);
        //}

        protected void FillDocument(Dictionary<string, object> data, string[] fields, out Dictionary<string, object> output)
        {
            output = data
                .Select(x =>
                {
                    if (fields.Any(xx => xx == x.Key) && x.Value == null)
                        return new KeyValuePair<string, object>(x.Key, "");

                    return x;
                })
                .ToDictionary(x => x.Key, x => x.Value);
        }

        protected object GetValue(Dictionary<string, object> doc, string key)
        {
            return doc.FirstOrDefault(x => x.Key == key).Value;
        }

        public List<Dictionary<string, object>> MergeDocuments(
            List<Tuple<Dictionary<string, object>, Dictionary<string, object>>> dataList)
        {
            var modifiedDocs = dataList
                .Select(doc =>
                {
                    var imported = doc.Item1;
                    var entity = doc.Item2;

                    var modifiedDoc = imported
                        .Select(imp =>
                        {
                            if (imp.Value == null)
                                return new KeyValuePair<string, object>(null, null);

                            var elem = entity.FirstOrDefault(x => x.Key == imp.Key);
                            if (string.IsNullOrEmpty(elem.Key)) return imp;

                            if (imp.Value != elem.Value)
                                return new KeyValuePair<string, object>(imp.Key, imp.Value);

                            return new KeyValuePair<string, object>(null, null);
                        })
                        .Where(x => !string.IsNullOrEmpty(x.Key))
                        .ToDictionary(x => x.Key, x => x.Value);

                    if (modifiedDoc.Any())
                        modifiedDoc["_id"] = entity["_id"];

                    return modifiedDoc;
                })
                .Where(x => x.Any())
                .ToList();

            return modifiedDocs;
        }

        public List<Tuple<Dictionary<string, object>, Dictionary<string, object>>> MatchDocumentsByFields(
            List<Dictionary<string, object>> dataList,
            List<Dictionary<string, object>> entities,
            string[] matchedFields)
        {
            var matchedDocs = dataList
                .Select(data =>
                {
                    var matchedEntities = entities
                        .Where(entity => {
                            var resultFields = matchedFields
                                .Where(field =>
                                {
                                    var com1 = data.FirstOrDefault(x => x.Key == field);
                                    var com2 = entity.FirstOrDefault(x => x.Key == field);

                                    return com1.Value == com2.Value;
                                })
                                .ToArray();

                            return matchedFields.Length == resultFields.Length;
                        })
                        .ToList();

                    if (matchedEntities.Count != 1) return null;

                    return new Tuple<Dictionary<string, object>,
                        Dictionary<string, object>>(data, matchedEntities.First());
                })
                .Where(x => x != null)
                .ToList();

            return matchedDocs;
        }

        public List<Tuple<Dictionary<string, object>, Dictionary<string, object>>> MatchDocumentsByDualFields(
            List<Dictionary<string, object>> dataList,
            List<Dictionary<string, object>> entities,
            List<Tuple<string, string>> matchedFields)
        {
            var matchedDocs = dataList
                .Select(data =>
                {
                    var matchedEntities = entities
                        .Where(entity =>
                        {
                            var resultFields = matchedFields
                                .Where(mf =>
                                {
                                    var com1 = data.FirstOrDefault(x => x.Key == mf.Item1);
                                    var com2 = entity.FirstOrDefault(x => x.Key == mf.Item2);

                                    if (string.IsNullOrEmpty(com2.Key)) return false;
                                    return com1.Value == com2.Value;

                                })
                                .ToList();

                            return matchedFields.Count == resultFields.Count;
                        })
                        .ToList();

                    if (matchedEntities.Count != 1) return null;

                    return new Tuple<Dictionary<string, object>,
                        Dictionary<string, object>>(data, matchedEntities.First());
                })
                .Where(x => x != null)
                .ToList();

            return matchedDocs;
        }

        //public void AddCreated(By by, ref Dictionary<string, object> doc)
        //{
        //    var created = NJson.Serialize(
        //        new Created
        //        {
        //            by = by,
        //            on = DateTime.UtcNow
        //        });
        //    doc["created"] = created;
        //}

        //public void AddModified(By by, ref Dictionary<string, object> doc)
        //{
        //    var modified = NJson.Serialize(
        //        new Modified
        //        {
        //            by = by,
        //            on = DateTime.UtcNow
        //        });
        //    doc["modified"] = modified;
        //}

        //public void PushModified(By by, ref Dictionary<string, object> doc)
        //{
        //    if (doc.DocValue("modified") == null) return;

        //    var modified = NJson.Serialize(
        //        new Modified
        //        {
        //            by = by,
        //            on = DateTime.UtcNow
        //        });
        //    doc["modified"] = modified;
        //}

        //public void OverrideToNull(string key, ref Dictionary<string, object> doc)
        //{
        //    doc[key] = doc.DocValue(key) != null
        //        && !string.IsNullOrEmpty(doc.DocValue(key).ToString())
        //        ? doc.DocValue(key)
        //        : null;
        //}
    }
}

