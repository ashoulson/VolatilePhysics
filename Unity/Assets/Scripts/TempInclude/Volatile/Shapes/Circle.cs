﻿/*
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

using UnityEngine;

namespace Volatile
{
  public sealed class Circle : Shape
  {
    #region Factory Functions
    public static Circle FromWorldPosition(
      Vector2 origin, 
      float radius, 
      float density = 1.0f,
      float friction = Config.DEFAULT_FRICTION,
      float restitution = Config.DEFAULT_RESTITUTION)
    {
      return new Circle(origin, radius, density, friction, restitution);
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
    internal override bool ShapeQuery(Vector2 bodySpacePoint)
    {
      return 
        Collision.TestPointCircleSimple(
          this.bodySpacePosition,
          bodySpacePoint, 
          this.radius);
    }

    internal override bool ShapeQuery(
      Vector2 bodySpacePoint, 
      float radius)
    {
      return 
        Collision.TestCircleCircleSimple(
          this.bodySpacePosition,
          bodySpacePoint, 
          this.radius, 
          radius);
    }

    internal override bool ShapeRayCast(
      ref RayCast bodySpaceRay, 
      ref RayResult result)
    {
      return Collision.CircleRayCast(
        this,
        this.bodySpacePosition,
        this.sqrRadius,
        ref bodySpaceRay, 
        ref result);
    }

    internal override bool ShapeCircleCast(
      ref RayCast bodySpaceRay, 
      float radius,
      ref RayResult result)
    {
      float totalRadius = this.radius + radius;
      return Collision.CircleRayCast(
        this,
        this.bodySpacePosition,
        totalRadius * totalRadius,
        ref bodySpaceRay,
        ref result);
    }
    #endregion

    /// <summary>
    /// Creates a cache of the origin in world space. This should be called
    /// every time the world updates or the shape/body is moved externally.
    /// </summary>
    internal override void UpdateWorld()
    {
      this.origin = 
        this.Body.TransformPointBodyToWorld(this.bodySpacePosition);
      this.AABB = new AABB(this.origin, this.radius);
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
    }

    internal override void ComputeMetrics()
    {
      this.bodySpacePosition = 
        this.Body.TransformPointWorldToBody(this.Position);
      this.bodySpaceFacing = 
        this.Body.TransformDirectionWorldToBody(this.Facing);
      this.bodySpaceAABB = new AABB(this.bodySpacePosition, this.Radius);

      this.Area = this.sqrRadius * Mathf.PI;
      this.Mass = this.Area * this.Density * Config.AreaMassRatio;
      this.Inertia = 
        this.sqrRadius / 2.0f + this.bodySpacePosition.sqrMagnitude;
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
