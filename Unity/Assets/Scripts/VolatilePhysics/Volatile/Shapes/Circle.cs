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
  public sealed class Circle : Shape
  {
    #region Static Methods
    private static float ComputeArea(float sqrRadius)
    {
      return sqrRadius * Mathf.PI;
    }

    private static float ComputeInertia(float sqrRadius, Vector2 offset)
    {
      return sqrRadius / 2.0f + offset.sqrMagnitude;
    }

    private static AABB ComputeBounds(Vector2 center, float radius)
    {
      return new AABB(center, new Vector2(radius, radius));
    }
    #endregion

    public override Shape.ShapeType Type { get { return ShapeType.Circle; } }
    public override Vector2 Position { get { return this.worldOrigin; } }
    public override Vector2 Facing { get { return new Vector2(1.0f, 0.0f); } }

    public Vector2 WorldCenter { get { return this.worldOrigin; } }
    public Vector2 LocalCenter { get { return Vector2.zero; } } //return this.offset; } }
    public float Radius { get { return this.radius; } }

    private float radius;
    private float sqrRadius;

    internal Vector2 worldOrigin;

    public override bool ContainsPoint(Vector2 point)
    {
      return (point - this.worldOrigin).sqrMagnitude < this.sqrRadius;
    }

    public Circle(Vector2 origin, float radius, float density = 1.0f)
      : base()
    {
      this.radius = radius;
      this.sqrRadius = radius * radius;
      this.worldOrigin = origin;

      // Defined in Shape class
      this.Area = Circle.ComputeArea(this.sqrRadius);
      this.Mass = Shape.ComputeMass(this.Area, density);

      // TODO: This requires an offset from the body origin to work properly
      // Move to the fixture maybe?
      //this.Inertia = Circle.ComputeInertia(this.sqrRadius, offset);
    }

    /// <summary>
    /// Creates a cache of the origin in world space. This should be called
    /// every time the world updates or the shape/body is moved externally.
    /// </summary>
    internal override void SetWorld(Vector2 position, Vector2 facing)
    {
      this.worldOrigin = position;
      this.AABB = Circle.ComputeBounds(this.worldOrigin, this.radius);
    }
  }
}