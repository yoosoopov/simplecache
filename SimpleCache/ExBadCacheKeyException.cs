using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleCache
{
  public class ExBadCacheKeyException : Exception
  {
    public ExBadCacheKeyException()
    {
    }

    public ExBadCacheKeyException(string message)
      : base(message)
    {
    }

    public ExBadCacheKeyException(string message, Exception innerException)
      : base(message, innerException)
    {
    }
  }
}
