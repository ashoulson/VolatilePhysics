/*
 *  VolatilePhysics - A 2D Physics Library for Networked Games
 *  Copyright (c) 2015-2016 - Alexander Shoulson - http://ashoulson.com
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

#if VOLATILE_UNITY
using UnityEngine;
#else
using VolatileEngine;
#endif

namespace Volatile
{
  public sealed class Circle : Shape
  {
    #region Factory Functions
    public static Circle FromWorldPosition(
      Vector2 origin, 
      float radius,
      float density = Config.DEFAULT_DENSITY,
      float friction = Config.DEFAULT_FRICTION,
      float restitution = Config.DEFAULT_RESTITUTION)
    {
      return new Circle(origin, radius, density, friction, restitution);
    }
    #endregion

    #region Properties
    public override Shape.ShapeType Type { get { return ShapeType.Circle; } }

    public Vector2 Origin { get { return this.worldSpaceOrigin; } }
    public float Radius { get { return this.radius; } }
    #endregion

    #region Fields
    internal Vector2 worldSpaceOrigin;
    internal float radius;
    internal float sqrRadius;

    // Precomputed body-space values (these should never change unless we
    // want to support moving shapes relative to their body root later on)
    private Vector2 bodySpaceOrigin;
    #endregion

    private Circle(
      Vector2 worldSpaceOrigin,
      float radius,
      float density,
      float friction,
      float restitution)
      : base(density, friction, restitution)
    {
      this.radius = radius;
      this.sqrRadius = radius * radius;

      this.worldSpaceOrigin = worldSpaceOrigin;
      this.AABB = new AABB(worldSpaceOrigin, radius);
    }

    #region Functionality Overrides
    protected override void ComputeMetrics()
    {
      this.bodySpaceOrigin =
        this.Body.WorldToBodyPointCurrent(this.worldSpaceOrigin);
      this.bodySpaceAABB = new AABB(this.bodySpaceOrigin, this.radius);

      this.Area = this.sqrRadius * Mathf.PI;
      this.Mass = this.Area * this.Density * Config.AreaMassRatio;
      this.Inertia =
        this.sqrRadius / 2.0f + this.bodySpaceOrigin.sqrMagnitude;
    }

    protected override void ApplyBodyPosition()
    {
      this.worldSpaceOrigin =
        this.Body.BodyToWorldPointCurrent(this.bodySpaceOrigin);
      this.AABB = new AABB(this.worldSpaceOrigin, this.radius);
    }
    #endregion

    #region Test Overrides
    protected override bool ShapeQueryPoint(
      Vector2 bodySpacePoint)
    {
      return 
        Collision.TestPointCircleSimple(
          this.bodySpaceOrigin,
          bodySpacePoint, 
          this.radius);
    }

    protected override bool ShapeQueryCircle(
      Vector2 bodySpaceOrigin, 
      float radius)
    {
      return 
        Collision.TestCircleCircleSimple(
          this.bodySpaceOrigin,
          bodySpaceOrigin, 
          this.radius, 
          radius);
    }

    protected override bool ShapeRayCast(
      ref RayCast bodySpaceRay, 
      ref RayResult result)
    {
      return Collision.CircleRayCast(
        this,
        this.bodySpaceOrigin,
        this.sqrRadius,
        ref bodySpaceRay, 
        ref result);
    }

    protected override bool ShapeCircleCast(
      ref RayCast bodySpaceRay, 
      float radius,
      ref RayResult result)
    {
      float totalRadius = this.radius + radius;
      return Collision.CircleRayCast(
        this,
        this.bodySpaceOrigin,
        totalRadius * totalRadius,
        ref bodySpaceRay,
        ref result);
    }
    #endregion

    #region Debug
#if VOLATILE_UNITY
    public override void GizmoDraw(
      Color edgeColor, 
      Color normalColor, 
      Color originColor, 
      Color aabbColor, 
      float normalLength)
    {
      Color current = Gizmos.color;

      Gizmos.color = edgeColor;
      Gizmos.DrawWireSphere(this.worldSpaceOrigin, this.radius);

      this.AABB.GizmoDraw(aabbColor);

      Gizmos.color = current;
    }
#endif
    #endregion
  }
}
