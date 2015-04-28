using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleCache.Sample
{
  public class CxFundFactory : IxCacheItemFactory<CxFund>
  {
    public CxFund Load(int id)
    {
      throw new NotImplementedException();
    }

    public IList<CxFund> Load(int[] ids)
    {
      throw new NotImplementedException();
    }

    public CxFund Load(string key)
    {
      throw new NotImplementedException();
    }

    public IList<CxFund> Load(string[] keys)
    {
      throw new NotImplementedException();
    }
  }
}
