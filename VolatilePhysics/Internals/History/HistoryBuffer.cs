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
using System.Collections.Generic;

namespace Volatile
{
  internal class HistoryBuffer
    : IVoltPoolable<HistoryBuffer>
  {
    #region Interface
    IVoltPool<HistoryBuffer> IVoltPoolable<HistoryBuffer>.Pool { get; set; }
    void IVoltPoolable<HistoryBuffer>.Reset() { this.Reset(); }
    #endregion

    private HistoryRecord[] data;
    private int capacity;
    int count;
    int start;

    public int Capacity { get { return this.capacity; } }

    public HistoryBuffer()
    {
      this.data = null;
      this.capacity = 0;
      this.count = 0;
      this.start = 0;
    }

    public void Initialize(int capacity)
    {
      if ((this.data == null) || (this.data.Length < capacity))
        this.data = new HistoryRecord[capacity];
      this.capacity = capacity;
      this.count = 0;
      this.start = 0;
    }

    private void Reset()
    {
      this.count = 0;
      this.start = 0;
    }

    /// <summary>
    /// Stores a value as latest.
    /// </summary>
    public void Store(HistoryRecord value)
    {
      if (this.count < this.capacity)
      {
        this.data[this.count++] = value;
        this.IncrementStart();
      }
      else
      {
        this.data[this.start] = value;
        this.IncrementStart();
      }
    }

    /// <summary>
    /// Tries to get a value with a given number of frames behind the last 
    /// value stored. If the value can't be found, this function will find
    /// the closest and return false, indicating a clamp.
    /// </summary>
    public bool TryGet(int numBehind, out HistoryRecord value)
    {
      if (numBehind < 0)
        throw new ArgumentOutOfRangeException("numBehind");

      if (this.count < this.capacity)
      {
        if (numBehind >= this.count)
        {
          value = this.data[0];
          return false;
        }

        value = this.data[this.count - numBehind - 1];
        return true;
      }
      else
      {
        bool found = true;
        if (numBehind >= this.capacity)
        {
          numBehind = this.capacity - 1;
          found = false;
        }

        int index =
          ((this.start - numBehind - 1) + this.capacity)
          % this.capacity;
        value = this.data[index];
        return found;
      }
    }

    /// <summary>
    /// Returns all values, but not in order.
    /// </summary>
    public IEnumerable<HistoryRecord> GetValues()
    {
      for (int i = 0; i < this.count; i++)
        yield return this.data[i];
    }

    private void IncrementStart()
    {
      this.start = (this.start + 1) % this.capacity;
    }
  }
}
