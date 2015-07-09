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
    public static Circle FromWorldPosition(
      Vector2 origin, 
      float radius, 
      float density = 1.0f,
      float friction = Config.DEFAULT_FRICTION,
      float restitution = Config.DEFAULT_RESTITUTION)
    {
      return new Circle(origin, radius, density, friction, restitution);
    }

    #region Static Methods
    /// <summary>
    /// Workhorse for circle ray checks, also used for Polygon circlecasts.
    /// </summary>
    internal static bool CircleRayCast(
      Shape shape,
      Vector2 origin, 
      float sqrRadius, 
      ref RayCast ray, 
      ref RayResult result)
    {
      Vector2 toOrigin = origin - ray.Origin;

      float slope = Vector2.Dot(toOrigin, ray.Direction); 
      if (slope < 0) 
        return false;

      float sqrSlope = slope * slope;
      float d = sqrRadius + sqrSlope - Vector2.Dot(toOrigin, toOrigin);
      if (d < 0) 
        return false;

      float dist = slope - Mathf.Sqrt(d); 
      if (dist < 0) 
        return false;

      Vector2 normal = (dist * ray.Direction - toOrigin).normalized;
      result.Set(shape, dist, normal);
      return true;
    }
    
    private static float ComputeArea(float sqrRadius)
    {
      return sqrRadius * Mathf.PI;
    }

    private static float ComputeInertia(Vector2 originOffset, float sqrRadius)
    {
      return sqrRadius / 2.0f + originOffset.sqrMagnitude;
    }

    private static AABB ComputeBounds(Vector2 center, float radius)
    {
      return new AABB(center, new Vector2(radius, radius));
    }
    #endregion

    public override Shape.ShapeType Type { get { return ShapeType.Circle; } }
    public override Vector2 Position { get { return this.origin; } }
    public override Vector2 Facing { get { return new Vector2(1.0f, 0.0f); } }
    public override float Angle { get { return 0.0f; } }

    public float Radius { get { return this.radius; } }

    private float radius;
    private float sqrRadius;
    private Vector2 origin;

    #region Tests
    internal override bool ShapeQuery(Vector2 point)
    {
      return 
        Collision.TestCirclesSimple(
          this.Position, 
          point, 
          this.radius);
    }

    internal override bool ShapeQuery(Vector2 point, float radius)
    {
      return 
        Collision.TestCirclesSimple(
          this.Position, 
          point, 
          this.radius, 
          radius);
    }

    internal override bool ShapeRayCast(ref RayCast ray, ref RayResult result)
    {
      return CircleRayCast(
        this, 
        this.origin, 
        this.sqrRadius, 
        ref ray, 
        ref result);
    }

    internal override bool ShapeCircleCast(
      ref RayCast ray, 
      ref RayResult result, 
      float radius)
    {
      float totalRadius = this.radius + radius;
      return CircleRayCast(
        this,
        this.origin,
        totalRadius * totalRadius,
        ref ray,
        ref result);
    }
    #endregion

    /// <summary>
    /// Creates a cache of the origin in world space. This should be called
    /// every time the world updates or the shape/body is moved externally.
    /// </summary>
    public override void SetWorld(Vector2 position, Vector2 facing)
    {
      this.origin = position;
      this.AABB = Circle.ComputeBounds(this.origin, this.radius);
    }

    #region Internals
    private Circle(
      Vector2 origin,
      float radius,
      float density,
      float friction,
      float restitution)
      : base(density, friction, restitution)
    {
      this.origin = origin;
      this.radius = radius;
      this.sqrRadius = radius * radius;

      // Defined in Shape class
      this.Area = Circle.ComputeArea(this.sqrRadius);
    }

    internal override float ComputeInertia(Vector2 offset)
    {
      return Circle.ComputeInertia(offset, this.sqrRadius);
    }
    #endregion

    #region Debug
    public override void GizmoDraw(
      Color edgeColor, 
      Color normalColor, 
      Color originColor, 
      Color aabbColor, 
      float normalLength)
    {
      Color current = Gizmos.color;

      Gizmos.color = edgeColor;
      Gizmos.DrawWireSphere(this.Position, this.Radius);

      this.AABB.GizmoDraw(aabbColor);

      Gizmos.color = current;
    }
    #endregion
  }
}
