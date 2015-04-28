using System.Collections.Generic;

namespace SimpleCache
{
  public class CxCachedRepository<T> where T : class, IxCacheItem
  {
    private readonly IxCacheItemFactory<T> cacheItemFactory;
    private readonly CxCache<T> cache;

    public CxCachedRepository(IxCacheItemFactory<T> cacheItemFactory, CxCache<T> cache)
    {
      this.cacheItemFactory = cacheItemFactory;
      this.cache = cache;
    }

    public T Get(int id)
    {
      T value;
      if (!this.cache.TryGetValue(id, out value))
      {
        value = this.cacheItemFactory.Load(id);
        this.cache.AddValue(value);
      }

      return value;
    }

    public T Get(string key)
    {
      T value;
      if (!this.cache.TryGetValue(key, out value))
      {
        value = this.cacheItemFactory.Load(key);
        this.cache.AddValue(value);
      }

      return value;
    }

    public IList<T> Get(int[] ids)
    {
      var retVal = new List<T>(ids.Length);
      for (var i = 0; i != ids.Length; i++)
      {
        retVal[i] = this.Get(ids[i]);
      }

      return retVal;
    }

    public IList<T> Get(string[] keys)
    {
      var retVal = new List<T>(keys.Length);
      for (var i = 0; i != keys.Length; i++)
      {
        retVal[i] = this.Get(keys[i]);
      }

      return retVal;
    }
  }
}
