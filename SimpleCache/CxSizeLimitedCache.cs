using System.Collections.Generic;

namespace SimpleCache
{
  public class CxSizeLimitedCache<TValue> : CxCache<TValue> where TValue : class, IxCacheItem
  {
    protected sealed class CacheValue : ICacheValue
    {
      public CacheValue(TValue value)
      {
        this.Value = value;
      }

      public LinkedListNode<KeyValuePair<int, CacheValue>> IndexRef { get; set; }
      public TValue Value { get; set; }
    }

    private readonly LinkedList<KeyValuePair<int, CacheValue>> indexList = new LinkedList<KeyValuePair<int, CacheValue>>();

    public int MaxSize { get; set; }

    public CxSizeLimitedCache(int maxSize)
    {
      this.MaxSize = maxSize;
    }

    protected override void UpdateElementAccess(ICacheValue cacheValue)
    {
      var value = (CacheValue)cacheValue;
      // put element at front of the index list
      // remove first if already present in list, create new otherwise
      var idxRef = value.IndexRef;
      if (idxRef != null)
      {
        this.indexList.Remove(idxRef);
      }
      else
      {
        idxRef = new LinkedListNode<KeyValuePair<int, CacheValue>>(new KeyValuePair<int, CacheValue>(cacheValue.Value.Id, value));
        value.IndexRef = idxRef;
      }
      this.indexList.AddFirst(idxRef);

      // remove all entries from end of list until max size is satisfied
      while (this.indexList.Count > this.MaxSize)
      {
        this.InvalidateUnlocked(this.indexList.Last.Value.Key);
      }
    }

    protected override ICacheValue CreateCacheValue(TValue value)
    {
      return new CacheValue(value);
    }

    protected override void CacheValueInvalidated(ICacheValue cacheValue)
    {
      this.indexList.Remove(((CacheValue)cacheValue).IndexRef);
    }

    protected override void FlushUnlocked()
    {
      base.FlushUnlocked();
      this.indexList.Clear();
    }
  }
}
