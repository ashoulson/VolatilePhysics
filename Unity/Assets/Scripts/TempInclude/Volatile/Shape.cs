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

using UnityEngine;

namespace Volatile
{
  public abstract class Shape
  {
    #region Static Methods
    internal static void OrderShapes(ref Shape sa, ref Shape sb)
    {
      if (sa.Type > sb.Type)
      {
        Shape temp = sa;
        sa = sb;
        sb = temp;
      }
    }
    #endregion

    public enum ShapeType
    {
      Circle,
      Polygon,
    }

    /// <summary>
    /// For attaching arbitrary data to this shape.
    /// </summary>
    public object UserData { get; set; }

    public abstract ShapeType Type { get; }

    public Body Body { get; private set; }

    internal float Density { get; private set; }
    internal float Friction { get; private set; }
    internal float Restitution { get; private set; }

    /// <summary>
    /// The world-space bounding AABB for this shape.
    /// </summary>
    public AABB AABB { get; protected set; }

    /// <summary>
    /// Total area of the shape.
    /// </summary>
    public float Area { get; protected set; }

    /// <summary>
    /// Total mass of the shape (area * density).
    /// </summary>
    public float Mass { get; protected set; }

    /// <summary>
    /// Total inertia of the shape relative to the body's origin.
    /// </summary>
    public float Inertia { get; protected set; }

    // Body-space bounding AABB for pre-checks during queries/casts
    internal AABB bodySpaceAABB;

    // TODO: Remove static here (for threading)
    internal static int nextId = 0;
    internal int id;

    #region Events
    internal void OnBodyAssigned(Body body)
    {
      this.Body = body;
      this.ComputeMetrics();
    }

    internal void OnBodyPositionUpdated()
    {
      this.ApplyBodyPosition();
    }
    #endregion

    #region Tests
    /// <summary>
    /// Checks if a point is contained in this shape. 
    /// Begins with an AABB check.
    /// </summary>
    internal bool Query(Vector2 bodySpacePoint)
    {
      // Queries and casts on shapes are always done in body space
      if (this.bodySpaceAABB.Query(bodySpacePoint) == true)
        return this.ShapeQuery(bodySpacePoint);
      return false;
    }

    /// <summary>
    /// Checks if a circle overlaps with this shape. 
    /// Begins with an AABB check.
    /// </summary>
    internal bool Query(Vector2 bodySpacePoint, float radius)
    {
      // Queries and casts on shapes are always done in body space
      if (this.bodySpaceAABB.Query(bodySpacePoint, radius) == true)
        return this.ShapeQuery(bodySpacePoint, radius);
      return false;
    }

    /// <summary>
    /// Performs a raycast check on this shape. 
    /// Begins with an AABB check.
    /// </summary>
    internal bool RayCast(
      ref RayCast bodySpaceRay, 
      ref RayResult result)
    {
      // Queries and casts on shapes are always done in body space
      if (this.bodySpaceAABB.RayCast(ref bodySpaceRay) == true)
        return this.ShapeRayCast(ref bodySpaceRay, ref result);
      return false;
    }

    /// <summary>
    /// Performs a circlecast check on this shape. 
    /// Begins with an AABB check.
    /// </summary>
    internal bool CircleCast(
      ref RayCast bodySpaceRay, 
      float radius, 
      ref RayResult result)
    {
      // Queries and casts on shapes are always done in body space
      if (this.bodySpaceAABB.CircleCast(ref bodySpaceRay, radius) == true)
        return this.ShapeCircleCast(ref bodySpaceRay, radius, ref result);
      return false;
    }
    #endregion

    internal Shape(float density, float friction, float restitution)
    {
      this.id = nextId++;
      this.Density = density;
      this.Friction = friction;
      this.Restitution = restitution;
    }

    #region Functionality Overrides
    protected abstract void ComputeMetrics();
    protected abstract void ApplyBodyPosition();
    #endregion

    #region Test Overrides
    protected abstract bool ShapeQuery(
      Vector2 bodySpacePoint);

    protected abstract bool ShapeQuery(
      Vector2 bodySpacePoint,
      float radius);

    protected abstract bool ShapeRayCast(
      ref RayCast bodySpaceRay,
      ref RayResult result);

    protected abstract bool ShapeCircleCast(
      ref RayCast bodySpaceRay,
      float radius,
      ref RayResult result);
    #endregion

    #region Debug
    public abstract void GizmoDraw(
      Color edgeColor,
      Color normalColor,
      Color originColor,
      Color aabbColor,
      float normalLength);
    #endregion
  }
}