using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva
{
  public static class Disposer
  {
    public static void SafeDispose<T>(ref T obj)
        where T : class, IDisposable
    {
      if (obj != null)
      {
        obj.Dispose();
        obj = null;
      }
    }
  }
}
