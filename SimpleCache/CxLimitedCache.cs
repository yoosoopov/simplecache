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
        this.CreatedAt = DateTime.Now;
      }

      public LinkedListNode<KeyValuePair<int, CacheValue>> IndexRef { get; set; }
      public LinkedListNode<KeyValuePair<int, CacheValue>> IndexTimeRef { get; set; }
      public TValue Value { get; set; }
      public DateTime CreatedAt { get; set; }
      public int AccessCount { get; set; }
    }

    private const int DefaultExpiryIntervalSec = 600;
    private const int DefaultMaxAccessCount = 50;
    private readonly LinkedList<KeyValuePair<int, CacheValue>> indexList = new LinkedList<KeyValuePair<int, CacheValue>>();
    private readonly LinkedList<KeyValuePair<int, CacheValue>> indexTimeList = new LinkedList<KeyValuePair<int, CacheValue>>();
    private TimeSpan? timerInterval;
    private Timer expiryTimer;
    private int expiryIsRunning = 0;
    private int timerStartsCount = 0;

    public TimeSpan? MaxEntryAge { get; set; }

    public int MaxSize { get; set; }

    public TimeSpan? TimerInterval
    {
      get { return this.timerInterval; }
      set
      {
        this.timerInterval = value;
        this.DisposeTimer();
        if (value != null)
        {
          this.expiryTimer = new Timer(o => this.Expire(), null, value.Value, value.Value);
        }
      }
    }

    public int MaxAccessCount { get; set; }

    public int TimerStartsCount { get { return this.timerStartsCount; } }

    public CxLimitedCache(int maxSize, TimeSpan? maxEntryAge, int maxAccessCount, TimeSpan? timerInterval)
    {
      this.MaxSize = maxSize;
      this.MaxAccessCount = maxAccessCount;
      this.MaxEntryAge = maxEntryAge;
      this.TimerInterval = timerInterval;
    }

    public CxLimitedCache(int maxSize, TimeSpan maxEntryAge)
      : this(maxSize, maxEntryAge, DefaultMaxAccessCount, TimeSpan.FromSeconds(DefaultExpiryIntervalSec))
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

    protected virtual ICacheValue CheckValueExpiration(ICacheValue value)
    {
      var maxAge = this.MaxEntryAge;
      if (maxAge != null && value != null && (((CacheValue)value).CreatedAt + maxAge.Value) < DateTime.Now)
      {
        // remove elemnt on access if cache value expired
        this.InvalidateUnlocked(value);
        value = null;
      }

      return value;
    }

    protected override ICacheValue GetCacheValueUnlocked(int id)
    {
      var value = base.GetCacheValueUnlocked(id);
      return this.CheckValueExpiration(value);
    }

    protected override ICacheValue GetCacheValueUnlocked(string key)
    {
      var value = base.GetCacheValueUnlocked(key);
      return this.CheckValueExpiration(value);
    }

    protected override void UpdateElementAccess(ICacheValue cacheValue)
    {
      var value = (CacheValue)cacheValue;

      if (this.MaxEntryAge != null)
      {
        var idxTimeRef = value.IndexTimeRef;

        if (idxTimeRef == null)
        {
          idxTimeRef = new LinkedListNode<KeyValuePair<int, CacheValue>>(new KeyValuePair<int, CacheValue>(cacheValue.Value.Id, value));
          value.IndexTimeRef = idxTimeRef;
          this.indexTimeList.AddFirst(idxTimeRef);
        }
      }

      if (this.MaxSize > 0)
      {
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

        if (this.MaxEntryAge != null && this.indexTimeList.Count > 0)
        {
          var maxAge = this.MaxEntryAge;
          LinkedListNode<KeyValuePair<int, CacheValue>> node;
          while ((node = this.indexTimeList.Last) != null && node.Value.Value.CreatedAt + maxAge < DateTime.Now)
          {
            this.InvalidateUnlocked(this.indexList.Last.Value.Value);
          }
        }

        // remove all entries from end of list until max size is satisfied
        while (this.indexList.Count > this.MaxSize)
        {
          this.InvalidateUnlocked(this.indexList.Last.Value.Value);
        }
      }

      if (this.MaxAccessCount > 0)
      {
        value.AccessCount++;

        // remove element if max access count limit exceeded
        if (value.AccessCount > this.MaxAccessCount)
        {
          this.InvalidateUnlocked(value);
        }
      }
    }

    protected override ICacheValue CreateCacheValue(TValue value)
    {
      return new CacheValue(value);
    }

    protected override void CacheValueInvalidated(ICacheValue cacheValue)
    {
      var value = (CacheValue)cacheValue;

      if (value.IndexRef != null)
      {
        this.indexList.Remove(value.IndexRef);
      }

      if (value.IndexTimeRef != null)
      {
        this.indexTimeList.Remove(value.IndexTimeRef);
      }
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

          if (maxAge != null)
          {
            IList<KeyValuePair<int, ICacheValue>> list = new List<KeyValuePair<int, ICacheValue>>();
            foreach (var cacheValue in this.ValueCacheById)
            {
              list.Add(cacheValue);
            }

            foreach (var cacheValue in list)
            {
              if (((CacheValue)cacheValue.Value).CreatedAt + maxAge.Value < DateTime.Now)
              {
                this.InvalidateUnlocked(cacheValue.Value);
              }
            }
          }

          this.timerStartsCount++;
        }
      }
      finally
      {
        this.expiryIsRunning = 0;
      }
    }
  }
}
