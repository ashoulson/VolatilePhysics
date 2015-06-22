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
  /// <summary>
  /// Intermediate class that sits between the historical quad tree and the
  /// body itself and records a history of that body's positions for
  /// in-the-past raycasts and geometric checks. Note that this state doesn't
  /// (and really can't) hold enough information to fully roll back to previous
  /// world states.
  /// </summary>
  // TODO: Make me hold an array instead of using a whole bunch of standalone 
  // instances
  internal class ShapeHandle
  {
    private struct Record
    {
      internal int time;
      internal Vector2 position;
      internal Vector2 facing;
      internal ShapeHandle next;

      internal void Set(
        int time, 
        Vector2 position, 
        Vector2 facing,
        ShapeHandle next)
      {
        this.time = time;
        this.position = position;
        this.facing = facing;
        this.next = next;
      }
    }

    #region Linked List Fields
    internal ShapeHandle next = null;
    internal ShapeHandle prev = null;
    internal int cellKey;
    #endregion

    internal AABB CurrentAABB { get { return this.shape.AABB; } }

    private int historyLength;
    private Record[] records;
    private Shape shape;

    internal ShapeHandle(Shape shape, int historyLength)
    {
      this.historyLength = historyLength;
      this.shape = shape;
      this.records = new Record[this.historyLength];
      for (int i = 0; i < this.records.Length; i++)
        this.records[i].time = Config.INVALID_TIME;
    }

    internal void RecordState(int time, int slot)
    {
      this.records[slot].Set(
        time, 
        this.shape.Position, 
        this.shape.Facing,
        this.next);
    }

    internal void Rollback(int time, int slot)
    {
      Record record = this.records[slot];
      this.shape.SetWorld(record.position, record.facing);
    }

    internal void ResetShape()
    {
      this.shape.ResetFromBody();
    }

    internal ShapeHandle Next(int time)
    {
      int slot = QuadtreeBuffer.SlotForTime(time, this.historyLength);
      Debug.Assert(this.records[slot].time == time);
      return this.records[slot].next;
    }

    #region Debug
    internal void GizmoDraw(int time)
    {
      this.Rollback(
        time,
        QuadtreeBuffer.SlotForTime(time, this.historyLength));
      DebugDraw.Draw(this.shape);
      this.ResetShape();
    }
    #endregion
  }
}
