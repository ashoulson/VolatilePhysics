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

using UnityEngine;

namespace CommonTools
{
  public abstract class Pool
  {
    protected abstract void DeallocateGeneric(object item);

    public static void Free(IPoolable item)
    {
      item.Pool.DeallocateGeneric(item);
    }
  }

  public abstract class Pool<T> : Pool
    where T : IPoolable
  {
    protected Stack<T> freeList;

    public Pool()
    {
      this.freeList = new Stack<T>();
    }

    public abstract T Allocate();

    public void Deallocate(T value)
    {
      Debug.Assert(value.Pool == this);
      value.Reset();
      this.freeList.Push(value);
    }

    protected override void DeallocateGeneric(object item)
    {
      this.Deallocate((T)item);
    }
  }
}
