/*
 *  VolatilePhysics - A 2D Physics Library for Networked Games
 *  Copyright (c) 2015 - Alexander Shoulson - http://ashoulson.com
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
using System.Collections.Generic;

using UnityEngine;

namespace Volatile
{
  internal abstract class ObjectPool<T>
    where T : class, IPoolable<T>
  {
    private T first;
    private T last;

    protected abstract T Create();

    public ObjectPool()
    {
      this.first = null;
      this.last = null;
    }

    public void Release(T value)
    {
      if (this.first == null)
      {
        this.first = value;
        this.last = value;
      }
      else
      {
        this.last.Next = value;
        this.last = value;
      }

      value.IsValid = false;
    }

    public T Acquire()
    {
      if (this.first == null)
        return this.Create();

      T toReturn = first;
      this.first = toReturn.Next;
      toReturn.Next = null;

      if (this.first == null)
        this.last = null;

      return toReturn;
    }
  }
}