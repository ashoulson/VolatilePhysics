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
  /// <summary>
  /// Data structure for attaching a Shape to a Body.
  /// </summary>
  internal class Fixture
  {
    #region Factories
    /// <summary>
    /// Creates a new fixture taking a body and a shape independent from
    /// one another in world space, and computing their offsets.
    /// </summary>
    public static Fixture FromWorldSpace(Body body, Shape shape)
    {
      return new Fixture(
        shape,
        ComputePositionOffset(body, shape),
        ComputeFacingOffset(body, shape));
    }
    #endregion

    #region Static Computation Functions
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
    #endregion

    public Shape Shape { get { return this.shape; } }

    private Shape shape;
    private Vector2 positionOffset;
    private Vector2 facingOffset;

    private Fixture(Shape shape, Vector2 positionOffset, Vector2 facingOffset)
    {
      this.shape = shape;
      this.positionOffset = positionOffset;
      this.facingOffset = facingOffset;
    }

    /// <summary>
    /// Converts the body's position and facing into a world space 
    /// position and facing for the shape, and sets it on the shape.
    /// </summary>
    internal void Apply(Vector2 bodyPosition, Vector2 bodyFacing)
    {
      Vector2 shapePosition =
        bodyPosition + this.positionOffset.Rotate(bodyFacing);
      Vector2 shapeFacing = bodyFacing.Rotate(this.facingOffset);
      this.shape.SetWorld(shapePosition, shapeFacing);
    }

    internal float ComputeMass()
    {
      return this.shape.ComputeMass();
    }

    internal float ComputeInertia(Vector2 bodyFacing)
    {
      return this.shape.ComputeInertia(bodyFacing.Rotate(this.positionOffset));
    }
  }
}