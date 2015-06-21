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
  public class ShapeHandle
  {
    private struct Record
    {
      internal int time;
      internal Vector2 position;
      internal Vector2 facing;

      internal void Set(int time, Vector2 position, Vector2 facing)
      {
        this.time = time;
        this.position = position;
        this.facing = facing;
      }
    }

    #region Linked List Fields
    internal ShapeHandle next = null;
    internal ShapeHandle prev = null;
    internal int cellKey;
    #endregion

    internal AABB CurrentAABB { get { return this.shape.AABB; } }

    private Record[] records;
    private Shape shape;

    public ShapeHandle(Shape shape)
    {
      this.shape = shape;
      this.records = new Record[0];
    }

    public void StoreRecord(int time)
    {
      this.records[0].Set(time, this.shape.Position, this.shape.Facing);
    }

    public void Rollback(int time)
    {
      Record record = this.records[0];
      this.shape.SetWorld(record.position, record.facing);
    }

    public void ResetShape()
    {
      this.shape.ResetFromBody();
    }
  }
}
