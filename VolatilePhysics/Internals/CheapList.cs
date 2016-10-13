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

using System.Collections;
using System.Collections.Generic;

namespace Volatile
{
  /// <summary>
  /// A very loose partial encapsulation of a list array. Supports fast item
  /// at end, and fast arbitrary element removal. Does not guarantee order.
  /// </summary>
  internal class CheapList<T> : IEnumerable<T>
    where T : class, IIndexedValue
  {
    public int Count { get { return this.count; } }
    public T this[int key] { get { return this.values[key]; } }

    private T[] values;
    private int count;

    public CheapList(int capacity = 10)
    {
      this.values = new T[capacity];
      this.count = 0;
    }

    /// <summary>
    /// Adds a new element to the end of the list. Returns the index of the
    /// newly-indexed object.
    /// </summary>
    public void Add(T value)
    {
      if (this.count >= this.values.Length)
        VoltUtil.ExpandArray(ref this.values);

      this.values[this.count] = value;
      value.Index = this.count;
      this.count++;
    }

    /// <summary>
    /// Removes the element by swapping it for the last element in the list.
    /// </summary>
    public void Remove(T value)
    {
      int index = value.Index;
      VoltDebug.Assert(index >= 0);
      VoltDebug.Assert(index < this.count);

      int lastIndex = this.count - 1;
      if (index < lastIndex)
      {
        T lastValue = this.values[lastIndex];

        this.values[lastIndex].Index = -1;
        this.values[lastIndex] = null;

        this.values[index] = lastValue;
        lastValue.Index = index;
      }

      this.count--;
    }

    public void Clear()
    {
      this.count = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
      for (int i = 0; i < this.count; i++)
        yield return this.values[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      for (int i = 0; i < this.count; i++)
        yield return this.values[i];
    }
  }
}
