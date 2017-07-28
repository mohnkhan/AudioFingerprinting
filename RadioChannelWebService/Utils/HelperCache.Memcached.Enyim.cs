﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using CDR.Logging;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;

namespace CDRWebservice
{
    static class HelperCache
    {
        public static MemcachedClient defaultCache = null;


        #region CachePolicy
        public static CacheItemPolicy ExpirePolicySliding1Hour = new CacheItemPolicy { Priority = CacheItemPriority.Default, SlidingExpiration = TimeSpan.FromHours(1) };
        public static CacheItemPolicy ExpirePolicySliding10Minutes = new CacheItemPolicy { Priority = CacheItemPriority.Default, SlidingExpiration = TimeSpan.FromMinutes(10) };

        /// <summary>
        /// After 1 hour from now the data WILL BE REMOVED!
        /// </summary>
        public static CacheItemPolicy ExpirePolicyAbsolute1Hour
        {
            get
            {
                return new CacheItemPolicy { Priority = CacheItemPriority.Default, AbsoluteExpiration = DateTimeOffset.Now.AddHours(1) };
            }
        }

        /// <summary>
        /// After 24 hours from now the data WILL BE REMOVED!
        /// </summary>
        public static CacheItemPolicy ExpirePolicyAbsolute24Hours
        {
            get
            {
                return new CacheItemPolicy { Priority = CacheItemPriority.Default, AbsoluteExpiration = DateTimeOffset.Now.AddHours(24) };
            }
        }

        /// <summary>
        /// After 5 minutesfrom now the data WILL BE REMOVED!
        /// </summary>
        public static CacheItemPolicy ExpirePolicyAbsolute5Minutes
        {
            get
            {
                return new CacheItemPolicy { Priority = CacheItemPriority.Default, AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5) };
            }
        }
        #endregion

        /// <summary>
        /// Initiate the default cache and the search cache with specific settings 
        /// </summary>
        public static void Init()
        {
            // try to hit all lines in the config classes
            MemcachedClientConfiguration configuration = new MemcachedClientConfiguration();

            configuration.Servers.Add(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 11211));

            configuration.NodeLocator = typeof(DefaultNodeLocator);
            configuration.KeyTransformer = new SHA1KeyTransformer();
            configuration.Transcoder = new DefaultTranscoder();

            configuration.SocketPool.MinPoolSize = 10;
            configuration.SocketPool.MaxPoolSize = 20;
            configuration.SocketPool.ConnectionTimeout = new TimeSpan(0, 0, 10);
            configuration.SocketPool.DeadTimeout = new TimeSpan(0, 0, 15);

            defaultCache = new MemcachedClient(configuration);
            //Enyim.Caching.LogManager.AssignFactory(new Enyim.Caching.DiagnosticsLogFactory("Enyim.log"));
        }

        /// <summary>
        /// Release memory for the default and search cache. Var will be null after this call.
        /// (You can caal init again!)
        /// </summary>
        public static void Done()
        {
            defaultCache.Dispose();
            defaultCache = null;
        }


        public static object Get(string key)
        {
            if (defaultCache != null)
            {
                key = key.Replace(' ', '_');

                return defaultCache.Get(key);
            }

            return null;
        }

        public static bool Add(string key, object value, CacheItemPolicy policy)
        {
            if (defaultCache != null)
            {
                key = key.Replace(' ', '_');

                // if (policy.AbsoluteExpiration != null
                if (policy.SlidingExpiration.Ticks == 0)
                {
                    if (defaultCache.Store(Enyim.Caching.Memcached.StoreMode.Set, key, value, policy.AbsoluteExpiration.DateTime))
                    {
                        return true;
                    }
                }
                else
                {
                    if (defaultCache.Store(Enyim.Caching.Memcached.StoreMode.Set, key, value, policy.SlidingExpiration))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static object Remove(string key)
        {
            if (defaultCache != null)
            {
                key = key.Replace(' ', '_');

                defaultCache.Remove(key);
            }

            return null;
        }

        public static System.Xml.Linq.XElement GetXElement(string key)
        {
            if (defaultCache != null)
            {
                key = key.Replace(' ', '_');

                object obj = defaultCache.Get(key);
                if (obj != null)
                {
                    return System.Xml.Linq.XElement.Parse(obj.ToString());
                }
            }

            return null;
        }

        public static bool Add(string key, System.Xml.Linq.XElement value, CacheItemPolicy policy)
        {
            if (defaultCache != null)
            {
                key = key.Replace(' ', '_');

                // if (policy.AbsoluteExpiration != null
                if (policy.SlidingExpiration.Ticks == 0)
                {
                    if (defaultCache.Store(Enyim.Caching.Memcached.StoreMode.Set, key, value.ToString(), policy.AbsoluteExpiration.DateTime))
                    {
                        return true;
                    }
                }
                else
                {
                    if (defaultCache.Store(Enyim.Caching.Memcached.StoreMode.Set, key, value.ToString(), policy.SlidingExpiration))
                    {
                        return true;
                    }
                }
            }
            CDRLogger.Logger.LogInfo("Cache.Add failed: " + key);

            return false;
        }



        public static void Invalidate_Cache()
        {
            if (defaultCache != null)
            {
                defaultCache.FlushAll();
            }
        }
    }

    public enum CacheType
    {
        SmallCache = 0,
        SearchCache = 1
    }
}