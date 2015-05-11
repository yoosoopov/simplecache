using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleCache.Load
{
  using System.Diagnostics;

  using SimpleCache.Sample;

  class Program
  {
    static void Main(string[] args)
    {
      using (var cache = new CxLimitedCache<CxElement>(100000, TimeSpan.FromSeconds(4), 5, null))
      {
        var watch = Stopwatch.StartNew();
        for (var i = 0; i < 100000; i++)
        {
          cache.AddValue(new CxElement(i, string.Format("key.{0}", i)));
        }

        watch.Stop();
        Console.WriteLine("insert time {0}, {1}", watch.ElapsedMilliseconds, cache.Count);

        ////========================================================================================================

        watch = Stopwatch.StartNew();
        var stopTime = DateTime.Now.AddSeconds(5);
        var rnd = new Random();
        var accessdCount = 0;
        var findCount = 0;
        while (DateTime.Now < stopTime)
        {
          CxElement value;
          cache.TryGetValue(rnd.Next(99999), out value);
          accessdCount++;
        }

        watch.Stop();
        Console.WriteLine(
          "access time no expiration {0}, accessed {1} times, timer starts {2}, current size {3}",
          watch.ElapsedMilliseconds,
          accessdCount,
          cache.TimerStartsCount,
          cache.Count);

        ////========================================================================================================

        cache.TimerInterval = TimeSpan.FromMilliseconds(200);
        watch = Stopwatch.StartNew();
        stopTime = DateTime.Now.AddSeconds(5);
        accessdCount = 0;
        findCount = 0;
        while (DateTime.Now < stopTime)
        {
          CxElement value;
          cache.TryGetValue(rnd.Next(99999), out value);
          accessdCount++;
        }

        watch.Stop();
        Console.WriteLine(
          "access time with expiration {0}, accessed {1} times, timer starts {2}, current size {3}",
          watch.ElapsedMilliseconds,
          accessdCount,
          cache.TimerStartsCount,
          cache.Count);
      }
    }
  }
}
