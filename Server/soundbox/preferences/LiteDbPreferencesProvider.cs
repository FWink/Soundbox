using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class LiteDbPreferencesProvider<T> : IPreferencesProvider<T>
    {
        private const string COLLECTION_PREFERENCES_NAME = "preferences_keyvalue";

        protected readonly LiteDbPreferencesDatabaseProvider DatabaseProvider;

        public LiteDbPreferencesProvider(LiteDbPreferencesDatabaseProvider databaseProvider)
        {
            this.DatabaseProvider = databaseProvider;
        }

        protected ILiteDatabase GetDatabase()
        {
            return DatabaseProvider.GetDatabase();
        }

        protected ILiteCollection<KeyValue<T>> GetCollection()
        {
            return GetDatabase().GetCollection<KeyValue<T>>(COLLECTION_PREFERENCES_NAME);
        }

        public Task<bool> Contains(string key)
        {
            return Task.FromResult(
                GetCollection().FindOne(kv => kv.Key == key) != null
            );
        }

        public Task Delete(string key)
        {
            GetCollection().DeleteMany(kv => kv.Key == key);
            GetDatabase().Checkpoint();

            return Task.FromResult(true);
        }

        public Task<T> Get(string key)
        {
            var result = GetCollection().FindOne(kv => kv.Key == key);
            if (result == null)
                return Task.FromResult(default(T));

            return Task.FromResult(result.Value);
        }

        public Task Set(string key, T value)
        {
            //try updating first
            int updated = GetCollection().UpdateMany(
                kv => new KeyValue<T>
                {
                    Value = value
                },
                kv => kv.Key == key
            );

            if(updated <= 0)
            {
                //insert instead
                GetCollection().Insert(new KeyValue<T>
                {
                    Key = key,
                    Value = value
                });
            }
            GetDatabase().Checkpoint();

            return Task.FromResult(true);
        }

        protected class KeyValue<T>
        {
            public string Key
            {
                get;
                set;
            }

            public T Value
            {
                get;
                set;
            }
        }
    }
}
