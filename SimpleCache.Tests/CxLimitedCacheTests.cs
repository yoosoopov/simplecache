using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleCache.Sample;

namespace SimpleCache.Tests
{
  [TestClass]
  public class CxLimitedCacheTests
  {
    [TestMethod]
    public void CacheSizeLimitedTest()
    {
      using (var cache = new CxLimitedCache<CxFund>(1, TimeSpan.FromHours(1)))
      {
        var fund1 = new CxFund { Id = 1, Key = "test1" };
        var fund2 = new CxFund { Id = 2, Key = "test2" };
        CxFund temp;

        cache.AddValue(fund1);
        Assert.AreEqual(cache.Count, 1);
        cache.AddValue(fund2);
        Assert.AreEqual(cache.Count, 1);
      
        Assert.IsTrue(cache.TryGetValue(fund2.Id, out temp));
        Assert.AreEqual(fund2, temp);
        Assert.IsTrue(cache.TryGetValue(fund2.Key, out temp));
        Assert.AreEqual(fund2, temp);

        Assert.IsFalse(cache.TryGetValue(fund1.Id, out temp));
        Assert.IsNull(temp);
        Assert.IsFalse(cache.TryGetValue(fund1.Key, out temp));
        Assert.IsNull(temp);
      }
    }

    [TestMethod]
    public void CacheExpiresByTimeTest()
    {
      using (var cache = new CxLimitedCache<CxFund>(100, 100, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)))
      {
        var fund = new CxFund { Id = 1, Key = "test1" };
        CxFund temp;

        cache.AddValue(fund);
        Assert.AreEqual(cache.Count, 1);
        Assert.IsTrue(cache.TryGetValue(fund.Id, out temp));
        Assert.AreEqual(fund, temp);

        Thread.Sleep(TimeSpan.FromSeconds(4));
        Assert.AreEqual(cache.Count, 0);
      }
    }

    [TestMethod]
    public void CacheExpiresByTimeNoTimerTest()
    {
      using (var cache = new CxLimitedCache<CxFund>(100, 100, TimeSpan.FromSeconds(2), TimeSpan.FromHours(1)))
      {
        var fund = new CxFund { Id = 1, Key = "test1" };
        CxFund temp;

        cache.AddValue(fund);
        Assert.AreEqual(cache.Count, 1);
        Assert.IsTrue(cache.TryGetValue(fund.Id, out temp));
        Assert.AreEqual(fund, temp);

        Thread.Sleep(TimeSpan.FromSeconds(4));

        // expires on access
        Assert.AreEqual(cache.Count, 1);
        Assert.IsFalse(cache.TryGetValue(fund.Id, out temp));
      }
    }

    [TestMethod]
    public void CacheExpiresByAccessCountTest()
    {
      using (var cache = new CxLimitedCache<CxFund>(100, 1, TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(60)))
      {
        var fund = new CxFund { Id = 1, Key = "test1" };
        CxFund temp;

        cache.AddValue(fund);
        Assert.AreEqual(cache.Count, 1);
        
        Assert.IsTrue(cache.TryGetValue(fund.Id, out temp));
        Assert.AreEqual(fund, temp);

        Assert.AreEqual(cache.Count, 0);
        Assert.IsFalse(cache.TryGetValue(fund.Id, out temp));
        Assert.IsNull(temp);
      }
    }

    [TestMethod]
    [ExpectedException(typeof(ExBadCacheKeyException))]
    public void CacheThrowsExceptionOnBadKeysTest()
    {
      using (var cache = new CxLimitedCache<CxFund>(100, TimeSpan.FromMinutes(5)))
      {
        var fund1 = new CxFund { Id = 1, Key = "test" };
        var fund2 = new CxFund { Id = 2, Key = "test" };

        cache.AddValue(fund1);
        cache.AddValue(fund2);
      }
    }
  }
}
