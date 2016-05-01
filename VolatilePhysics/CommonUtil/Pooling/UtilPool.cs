/*
 *  Common Utilities for Working with C# and Unity
 *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace CommonUtil
{
  public interface IUtilPool<T>
  {
    T Allocate();
    void Deallocate(T obj);
    IUtilPool<T> Clone();
  }

  public static class UtilPool
  {
    public static void Free<T>(T obj)
      where T : IUtilPoolable<T>
    {
      obj.Pool.Deallocate(obj);
    }

    public static void SafeReplace<T>(ref T destination, T obj)
      where T : IUtilPoolable<T>
    {
      if (destination != null)
        UtilPool.Free(destination);
      destination = obj;
    }
  }

  public class UtilPool<T> : IUtilPool<T>
    where T : IUtilPoolable<T>, new()
  {
    private readonly Stack<T> freeList;

    public UtilPool()
    {
      this.freeList = new Stack<T>();
    }

    public T Allocate()
    {
      if (this.freeList.Count > 0)
        return this.freeList.Pop();

      T obj = new T();
      obj.Pool = this;
      obj.Reset();
      return obj;
    }

    public void Deallocate(T obj)
    {
      UtilDebug.Assert(obj.Pool == this);

      obj.Reset();
      this.freeList.Push(obj);
    }

    public IUtilPool<T> Clone()
    {
      return new UtilPool<T>();
    }
  }

  public class UtilPool<TBase, TDerived> : IUtilPool<TBase>
    where TBase : IUtilPoolable<TBase>
    where TDerived : TBase, new()
  {
    private readonly Stack<TBase> freeList;

    public UtilPool()
    {
      this.freeList = new Stack<TBase>();
    }

    public TBase Allocate()
    {
      if (this.freeList.Count > 0)
        return this.freeList.Pop();

      TBase obj = new TDerived();
      obj.Pool = this;
      obj.Reset();
      return obj;
    }

    public void Deallocate(TBase obj)
    {
      UtilDebug.Assert(obj.Pool == this);

      obj.Reset();
      this.freeList.Push(obj);
    }

    public IUtilPool<TBase> Clone()
    {
      return new UtilPool<TBase, TDerived>();
    }
  }
}
