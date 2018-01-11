using System;
using System.Collections.Generic;
using System.Text;

namespace Volatile
{
  /// <summary>
  /// Anything attached to a UserData variable on a VoltObject must use
  /// this interface. This is to allow for find-reference searching.
  /// </summary>
  public interface IVoltData { }
}

namespace Volatile.Internal
{
  public abstract class VoltObject
  {
    /// <summary>
    /// For attaching arbitrary data to this object.
    /// </summary>
    public IVoltData UserData { get; set; }

    public TData GetData<TData>()
      where TData : class, IVoltData
    {
      return (this.UserData as TData);
    }

    public bool TryGetData<TData>(out TData data)
      where TData : class, IVoltData
    {
      data = (this.UserData as TData);
      return (data != null);
    }
  }
}
