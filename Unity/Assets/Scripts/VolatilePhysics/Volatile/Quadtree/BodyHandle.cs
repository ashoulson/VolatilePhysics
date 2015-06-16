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
  public class BodyHandle
  {
    public int Time { get { return this.time; } }
    public Vector2 Position { get { return this.position; } }
    public float Angle { get { return this.angle; } }

    private int time;
    private Vector2 position;
    private float angle;

    public BodyHandle(
      int time,
      Vector2 position,
      float angle)
    {
      this.time = time;
      this.position = position;
      this.angle = angle;
    }

    public void Assign(
      int time,
      Vector2 position,
      float angle)
    {
      this.time = time;
      this.position = position;
      this.angle = angle;
    }

    private Body body;

    // TODO: Properties
    public BodyHandle Next = null;
    public BodyHandle Prev = null;
    public AABB AABB;
    public int CellKey;
  }
}
