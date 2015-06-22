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

namespace Volatile.History
{
  internal class QuadtreeBuffer
  {
    internal static int SlotForTime(int time, int historyLength)
    {
      return time % historyLength;
    }

    private List<ShapeHandle> shapes;
    private Quadtree[] history;
    private MutableQuadtree current;
    private int historyLength;

    internal QuadtreeBuffer(
      int startingTime, 
      int historyLength,
      int initialCapacity, 
      int maxDepth, 
      int maxBodiesPerCell,
      float extent)
    {
      this.historyLength = historyLength;
      this.current = 
        new MutableQuadtree(
          startingTime, 
          initialCapacity, 
          maxDepth, 
          maxBodiesPerCell, 
          extent);
      this.history = new Quadtree[historyLength];
      for (int i = 0; i < history.Length; i++)
        this.history[i] = new Quadtree();
      this.shapes = new List<ShapeHandle>();
    }

    internal Quadtree GetTree(int time)
    {
      int slot = QuadtreeBuffer.SlotForTime(time, this.historyLength);
      Debug.Log("Getting tree at " + time);
      return this.history[slot];
    }

    internal void AddShape(Shape shape)
    {
      ShapeHandle entry = new ShapeHandle(shape, this.historyLength);
      this.shapes.Add(entry);
      this.current.Insert(entry);
    }

    internal void Update(int time)
    {
      this.current.Time = time;
      this.UpdateShapes();
      this.Store(time);
    }

    private void Store(int time)
    {
      int slot = QuadtreeBuffer.SlotForTime(time, this.historyLength);
      foreach (ShapeHandle entry in this.shapes)
        entry.RecordState(time, slot);
      this.history[slot].ReceiveBlit(this.current);
    }

    private void UpdateShapes()
    {
      foreach (ShapeHandle entry in this.shapes)
        this.current.Update(entry);
    }
  }
}