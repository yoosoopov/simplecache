using System;
using System.Collections.Generic;
using System.Threading;

namespace SimpleCache
{
  public class CxLimitedCache<TValue> : CxCache<TValue>, IDisposable where TValue : class, IxCacheItem
  {
    protected sealed class CacheValue : ICacheValue
    {
      public CacheValue(TValue value)
      {
        this.Value = value;
        this.LastAccess = DateTime.Now;
      }

      public LinkedListNode<KeyValuePair<int, CacheValue>> IndexRef { get; set; }
      public TValue Value { get; set; }
      public DateTime LastAccess { get; set; }
      public int AccessCount { get; set; }
    }

    private const int DefaultExpiryIntervalSec = 600;
    private const int DefaultMaxAccessCpount = 50;
    private readonly LinkedList<KeyValuePair<int, CacheValue>> indexList = new LinkedList<KeyValuePair<int, CacheValue>>();
    private TimeSpan expiryInterval;
    private int maxAccessCount;
    private Timer expiryTimer;
    private int expiryIsRunning = 0;

    public TimeSpan MaxEntryAge { get; set; }

    public int MaxSize { get; set; }

    public TimeSpan ExpiryInterval
    {
      get { return this.expiryInterval; }
      set
      {
        this.expiryInterval = value;
        this.DisposeTimer();
        this.expiryTimer = new Timer(o => this.Expire(), null, value, value);
      }
    }

    public int MaxAccessCount
    {
      get
      {
        return this.maxAccessCount;
      }
      set
      {
        this.maxAccessCount = value;
        lock (this.SyncRoot)
        {
          foreach (var cacheValue in this.ValueCacheById)
          {
            if (((CacheValue)cacheValue.Value).AccessCount > this.maxAccessCount)
            {
              this.InvalidateUnlocked(cacheValue.Key);
            }
          }
        }
      }
    }

    public CxLimitedCache(int maxSize, int maxAccessCount, TimeSpan maxEntryAge, TimeSpan expiryInterval)
    {
      this.MaxSize = maxSize;
      this.maxAccessCount = maxAccessCount;
      this.MaxEntryAge = maxEntryAge;
      this.ExpiryInterval = expiryInterval;
    }

    public CxLimitedCache(int maxSize, TimeSpan maxEntryAge)
      : this(maxSize, DefaultMaxAccessCpount, maxEntryAge, TimeSpan.FromSeconds(DefaultExpiryIntervalSec))
    {
    }

    private void DisposeTimer()
    {
      if (this.expiryTimer != null)
      {
        this.expiryTimer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.Zero);
        this.expiryTimer.Dispose();
        this.expiryTimer = null;
      }
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        this.DisposeTimer();
      }
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

      value.LastAccess = DateTime.Now;
      value.AccessCount++;

      if (value.AccessCount > this.MaxAccessCount)
      {
        this.InvalidateUnlocked(value.Value.Id);
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

    private void Expire()
    {
      if (Interlocked.CompareExchange(ref this.expiryIsRunning, 1, 0) == 1)
      {
        // expiry is still running
        return;
      }
      try
      {
        lock (this.SyncRoot)
        {
          var maxAge = this.MaxEntryAge;

          foreach (var cacheValue in this.ValueCacheById)
          {
            if (((CacheValue)cacheValue.Value).LastAccess + maxAge < DateTime.Now)
            {
              this.InvalidateUnlocked(cacheValue.Key);
            }
          }
        }
      }
      finally
      {
        this.expiryIsRunning = 0;
      }
    }
  }
}
