using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace SimpleCache.Sample
{
  public class CxFund : IxCacheItem
  {
    private static readonly CxFundFactory Factory = new CxFundFactory();
    private static readonly CxLimitedCache<CxFund> Cache = new CxLimitedCache<CxFund>(100, TimeSpan.FromSeconds(60));
    private static readonly CxCachedRepository<CxFund> CachedRepo = new CxCachedRepository<CxFund>(Factory, Cache);

    public int Id { get; set; }

    public string Key { get; set; }

    public string SomeProp { get; set; }

    public static IList<CxFund> GetFunds(string[] tickers)
    {
      return CachedRepo.Get(tickers);
    }
  }
}
