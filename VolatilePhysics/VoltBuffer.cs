/*
 *  VolatilePhysics - A 2D Physics Library for Networked Games
 *  Copyright (c) 2015-2016 - Alexander Shoulson - http://ashoulson.com
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

namespace Volatile
{
  public class VoltBuffer<T> : IEnumerable<T>
  {
    public int Count { get { return this.count; } }
    public T this[int key] { get { return this.items[key]; } }

    private T[] items;
    private int count;

    public VoltBuffer(int capacity = 256)
    {
      this.items = new T[capacity];
      this.count = 0;
    }

    /// <summary>
    /// Adds a new element to the end of the list. Returns the index of the
    /// newly-indexed object.
    /// </summary>
    internal void Add(T body)
    {
      if (this.count >= this.items.Length)
        VoltUtil.ExpandArray(ref this.items);

      this.items[this.count] = body;
      this.count++;
    }

    internal void Add(T[] bodies, int count)
    {
      if ((this.count + count) >= this.items.Length)
        VoltUtil.ExpandArray(ref this.items, (this.count + count));

      Array.Copy(bodies, 0, this.items, this.count, count);
      this.count += count;
    }

    public void Clear()
    {
      this.count = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
      for (int i = 0; i < this.count; i++)
        yield return this.items[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      for (int i = 0; i < this.count; i++)
        yield return this.items[i];
    }
  }
}
