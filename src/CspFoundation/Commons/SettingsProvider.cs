using System.Runtime.Caching;

namespace CspFoundation.Commons
{
    public class SettingsProvider
    {
        #region Variable・Const
        private static MemoryCache Cache = new MemoryCache(nameof(SettingsProvider));
        private static TimeSpan Expiry = TimeSpan.FromSeconds(60);
        private static string storage_connect_string;
        private static string key_vault_url;
        private static string is_enterprise;
        #endregion

        #region Constructor
        public SettingsProvider() : base() { }
        #endregion

        #region Method
        public static string GetString(string key)
        {
            return Environment.GetEnvironmentVariable(key);
        }

        public static string KEY_VAULT_URL
        {
            get
            {
                key_vault_url = GetOrAddCacheMemory("KEY_VAULT_URL");
                return key_vault_url;
            }
        }

        public static string STORAGE_CONNECT_STRING
        {
            get
            {
                storage_connect_string = GetOrAddCacheMemory("STORAGE_CONNECT_STRING");
                return storage_connect_string;
            }
        }

        public static string IS_ENTERPRISE
        {
            get
            {
                is_enterprise = GetOrAddCacheMemory("IS_ENTERPRISE");
                return is_enterprise;
            }
        }

        private static string GetOrAddCacheMemory(string env_name)
        {
            string retValue = string.Empty;

            if (Cache.Contains(env_name))
            {
                var cacheItem = Cache.Get(env_name);
                if (cacheItem != null)
                {
                    return cacheItem.ToString();
                }
            }
            else
            {
                retValue = Environment.GetEnvironmentVariable(env_name);
            }

            if (retValue != null)
                Cache.Add(env_name, retValue, new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTime.Now + Expiry
                });

            return retValue;
        }
        #endregion
    }
}
