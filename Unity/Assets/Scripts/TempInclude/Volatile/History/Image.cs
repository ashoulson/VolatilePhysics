using System;
using System.Collections.Generic;

using UnityEngine;

namespace Volatile
{
  /// <summary>
  /// A stored historical image of past body states, used for historical
  /// queries and raycasts (without having to do a full rollback).
  /// </summary>
  internal struct Image
  {
    internal int frame;
    internal AABB aabb;
    internal Vector2 position;
    internal Vector2 facing;

    internal Vector2 WorldToBodyPoint(Vector2 vector)
    {
      return (vector - this.position).InvRotate(this.facing);
    }

    internal Vector2 BodyToWorldPoint(Vector2 vector)
    {
      return vector.Rotate(this.facing) + this.position;
    }

    internal Vector2 WorldToBodyDirection(Vector2 vector)
    {
      return vector.InvRotate(this.facing);
    }

    internal Vector2 BodyToWorldDirection(Vector2 vector)
    {
      return vector.Rotate(this.facing);
    }

    internal Axis BodyToWorldAxis(Axis axis)
    {
      Vector2 normal = axis.Normal.Rotate(this.facing);
      float width = Vector2.Dot(normal, this.position) + axis.Width;
      return new Axis(normal, width);
    }
  }
}
