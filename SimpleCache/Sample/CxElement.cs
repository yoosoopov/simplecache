using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleCache.Sample
{
  public class CxElement : IxCacheItem
  {
    public CxElement()
    {
    }

    public CxElement(int id, string key)
    {
      this.Id = id;
      this.Key = key;
    }

    public int Id { get; set; }

    public string Key { get; set; }

    public string Something { get; set; }
  }
}
