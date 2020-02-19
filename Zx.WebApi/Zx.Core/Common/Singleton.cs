using System;
using System.Collections.Generic;
using System.Text;

namespace Zx.Core.Common
{
    //public abstract class Singleton<T> where T : class, new()
    //{
    //    private static T _instance;

    //    public static T GetInstance()
    //    {
    //        if (_instance == null)
    //            _instance = new T();
    //        return _instance;
    //    }
    //}

    public sealed class Singleton
    {
        private static readonly Lazy<Singleton>
            lazy =
            new Lazy<Singleton>
                (() => new Singleton());

        public static Singleton Instance { get { return lazy.Value; } }

        private Singleton()
        {
        }
    }
}
