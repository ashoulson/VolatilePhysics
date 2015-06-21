using System;
using System.Collections.Generic;

using UnityEngine;

namespace Volatile
{
  /// <summary>
  /// Data structure for attaching a Shape to a Body.
  /// </summary>
  public class Fixture
  {
    /// <summary>
    /// Computes the facing-adjusted offset between a body and shape.
    /// </summary>
    private static Vector2 ComputePositionOffset(Body body, Shape shape)
    {
      Vector2 rawOffset = shape.Position - body.Position;
      return rawOffset.InvRotate(body.Facing);
    }

    /// <summary>
    /// Computes the facing offset between two world space facing vectors.
    /// </summary>
    private static Vector2 ComputeFacingOffset(Body body, Shape shape)
    {
      return shape.Facing.InvRotate(body.Facing);
    }

    private Shape shape;
    private Vector2 positionOffset;
    private Vector2 facingOffset;

    internal Fixture(Body body, Shape shape)
    {
      this.shape = shape;
      this.positionOffset = ComputePositionOffset(body, shape);
      this.facingOffset = ComputeFacingOffset(body, shape);
    }

    internal void Apply(Vector2 bodyPosition, Vector2 bodyFacing)
    {
      Vector2 shapePosition =
        bodyPosition + this.positionOffset.Rotate(bodyFacing);
      Vector2 shapeFacing = bodyFacing.Rotate(this.facingOffset);
      this.shape.SetWorld(shapePosition, shapeFacing);
    }
  }
}