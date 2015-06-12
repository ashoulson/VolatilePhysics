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
    public override float Area { get { return this.area; } }
    public override float Inertia { get { return this.inertia; } }
    public float Radius { get { return this.radius; } }

    private float area;
    private float inertia;

    private float radius;
    private float sqrRadius;
    private Vector2 offset;

    internal Vector2 cachedWorldCenter;

    public Circle(float radius, Vector2 offset)
      : base()
    {
      this.radius = radius;
      this.sqrRadius = radius * radius;
      this.offset = offset;

      this.area = Circle.ComputeArea(this.sqrRadius);
      this.inertia = Circle.ComputeInertia(this.sqrRadius, offset);
    }

    public override bool ContainsPoint(Vector2 point)
    {
      return (point - this.cachedWorldCenter).sqrMagnitude < this.sqrRadius;
    }

    internal override void UpdateCache()
    {
      this.CacheWorldData();

      // Note we're creating the bounding box in world space
      this.aabb = Circle.ComputeBounds(this.cachedWorldCenter, this.radius);
    }

    /// <summary>
    /// Creates a cache of the vertices and axes in world space
    /// </summary>
    protected override void CacheWorldData()
    {
      this.cachedWorldCenter =
        this.body.Position + this.offset.Rotate(this.body.Direction);
    }
  }
}