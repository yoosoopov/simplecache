using System.Collections.Generic;

namespace SimpleCache
{
  public interface IxCacheItemFactory<T>
  {
    T Load(int id);
    IList<T> Load(int[] ids);
    T Load(string key);
    IList<T> Load(string[] keys);
  }
}