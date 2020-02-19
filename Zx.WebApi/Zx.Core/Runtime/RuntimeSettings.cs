using System;
using System.Collections.Generic;
using System.Linq;

namespace Zx.Core.Runtime
{
    public enum Environment
    {
        Production,
        Development
    }

    public enum ConfigKeys
    {
        ConnString
    }

    public class RuntimeSettings
    {
        public readonly Environment Envrnmnt;
        private readonly List<Tuple<Environment, ConfigKeys, string>> _configs;

        public RuntimeSettings(Environment env)
        {
            _configs = new List<Tuple<Environment, ConfigKeys, string>>();
            Envrnmnt = env;

            // Production
            AddConfig(Environment.Production, ConfigKeys.ConnString, "mongodb://zxmongodbuser:PASSword123@cluster0-shard-00-00-qp67j.mongodb.net:27017,cluster0-shard-00-01-qp67j.mongodb.net:27017,cluster0-shard-00-02-qp67j.mongodb.net:27017/test?ssl=true&replicaSet=Cluster0-shard-0&authSource=admin&retryWrites=true&w=majority");
            //AddConfig(Environment.Production, ConfigKeys.SomeKey1, "");
            //AddConfig(Environment.Production, ConfigKeys.SomeKey2, "");

            // Development
            //AddConfig(Environment.Development, ConfigKeys.SomeKey1, "");
            //AddConfig(Environment.Development, ConfigKeys.SomeKey2, "");

            // Common
        }

        public string Config(ConfigKeys key)
        {
            return _configs
                .Where(x => x.Item1 == Envrnmnt && x.Item2 == key)
                .Select(x => x.Item3)
                .FirstOrDefault();
        }

        public void AddConfig(Environment env, ConfigKeys key, string val)
        {
            _configs.Add(new Tuple<Environment, ConfigKeys, string>(env, key, val));
        }

        public void AddCommonConfig(ConfigKeys key, string val)
        {
            AddConfig(Environment.Production, key, val);
            AddConfig(Environment.Development, key, val);
        }
    }
}
