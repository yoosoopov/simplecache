using System.Collections.Generic;

namespace SimpleCache
{
  public abstract class CxCache<TValue> where TValue : class, IxCacheItem
  {
    protected interface ICacheValue
    {
      TValue Value { get; set; }
    }

    protected readonly IDictionary<int, ICacheValue> ValueCacheById = new Dictionary<int, ICacheValue>();
    protected readonly IDictionary<string, ICacheValue> ValueCacheByKey = new Dictionary<string, ICacheValue>();
    protected object SyncRoot = new object();

    protected abstract ICacheValue CreateCacheValue(TValue value);
    protected abstract void UpdateElementAccess(ICacheValue cacheValue);
    protected abstract void CacheValueInvalidated(ICacheValue cacheValue);

    public virtual int Count
    {
      get { return this.ValueCacheById.Count; }
    }

    protected virtual ICacheValue GetCacheValueUnlocked(int id)
    {
      ICacheValue v;
      return this.ValueCacheById.TryGetValue(id, out v) ? v : null;
    }

    protected virtual ICacheValue GetCacheValueUnlocked(string key)
    {
      ICacheValue v;
      return this.ValueCacheByKey.TryGetValue(key, out v) ? v : null;
    }

    protected virtual ICacheValue AddValueUnlocked(TValue value)
    {
      ICacheValue cacheValue = this.GetCacheValueUnlocked(value.Id);
      if (cacheValue == null)
      {
        ICacheValue cacheValueByKey = this.GetCacheValueUnlocked(value.Key);
        if (cacheValueByKey != null)
        {
          throw new ExBadCacheKeyException();
        }

        cacheValue = this.CreateCacheValue(value);
        this.ValueCacheById[value.Id] = cacheValue;
        this.ValueCacheByKey[value.Key] = cacheValue;
      }
      else
      {
        cacheValue.Value = value;
      }
      this.UpdateElementAccess(cacheValue);
      return cacheValue;
    }

    protected virtual void InvalidateUnlocked(int id)
    {
      var value = this.GetCacheValueUnlocked(id);
      if (value != null)
      {
        this.ValueCacheById.Remove(value.Value.Id);
        this.ValueCacheByKey.Remove(value.Value.Key);
        this.CacheValueInvalidated(value);
      }
    }

    protected virtual void InvalidateUnlocked(string key)
    {
      var value = this.GetCacheValueUnlocked(key);
      if (value != null)
      {
        this.ValueCacheById.Remove(value.Value.Id);
        this.ValueCacheByKey.Remove(value.Value.Key);
        this.CacheValueInvalidated(value);
      }
    }

    public bool TryGetValue(int id, out TValue value)
    {
      value = default(TValue);

      lock (this.SyncRoot)
      {
        ICacheValue v = this.GetCacheValueUnlocked(id);
        if (v != null)
        {
          value = v.Value;
          this.UpdateElementAccess(v);
          return true;
        }
      }

      return false;
    }

    public bool TryGetValue(string key, out TValue value)
    {
      value = default(TValue);

      lock (this.SyncRoot)
      {
        ICacheValue v = this.GetCacheValueUnlocked(key);
        if (v != null)
        {
          value = v.Value;
          this.UpdateElementAccess(v);
          return true;
        }
      }

      return false;
    }

    public void AddValue(TValue value)
    {
      lock (this.SyncRoot)
      {
        this.AddValueUnlocked(value);
      }
    }
    
    public void Invalidate(int id)
    {
      lock (this.SyncRoot)
      {
        this.InvalidateUnlocked(id);
      }
    }

    public void Invalidate(string key)
    {
      lock (this.SyncRoot)
      {
        this.InvalidateUnlocked(key);
      }
    }

    public virtual void Flush()
    {
      lock (this.SyncRoot)
      {
        this.FlushUnlocked();
      }
    }

    protected virtual void FlushUnlocked()
    {
      this.ValueCacheById.Clear();
      this.ValueCacheByKey.Clear();
    }
  }
}

