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

    public abstract ShapeType Type { get; }

    // We need to provide set access to this for historical ray/circle casting
    public abstract Vector2 Position { get; internal set; }

    public abstract float Angle { get; }
    public abstract Vector2 Facing { get; }

    /// <summary>
    /// User token, for attaching arbitrary data to this shape.
    /// </summary>
    public object Token { get; set; }

    public Body Body { get; internal set; }
    public AABB AABB { get; protected set; }

    public float Area { get; protected set; }

    public float Density { get; private set; }
    public float Friction { get; private set; }
    public float Restitution { get; private set; }

    // TODO: Remove static here (for threading)
    internal static int nextId = 0;
    internal int id;

    #region Tests
    /// <summary>
    /// Returns true iff an area overlaps with our AABB.
    /// </summary>
    public bool Query(AABB area)
    {
      return this.AABB.Intersect(area);
    }

    /// <summary>
    /// Checks if a point is contained in this shape. 
    /// Begins with an AABB check.
    /// </summary>
    public bool Query(Vector2 point)
    {
      if (this.AABB.Query(point) == true)
        return this.ShapeQuery(point);
      return false;
    }

    /// <summary>
    /// Checks if a circle overlaps with this shape. 
    /// Begins with an AABB check.
    /// </summary>
    public bool Query(Vector2 point, float radius)
    {
      if (this.AABB.Query(point, radius) == true)
        return this.ShapeQuery(point, radius);
      return false;
    }

    /// <summary>
    /// Checks if a circle overlaps with this shape.
    /// Begins with an AABB check.
    /// Outputs the minimum surface distance from the shape to the origin.
    /// More expensive than a normal query.
    /// </summary>
    public bool MinDistance(
      Vector2 point, 
      float maxDistance, 
      out float minDistance)
    {
      minDistance = maxDistance;
      if (this.AABB.Query(point, maxDistance) == true)
      {
        minDistance = this.ShapeMinDistance(point);
        if (minDistance < maxDistance)
          return true;
      }
      return false;
    }

    /// <summary>
    /// Performs a raycast check on this shape. 
    /// Begins with an AABB check.
    /// </summary>
    public bool RayCast(ref RayCast ray, ref RayResult result)
    {
      Debug.Assert(ray.IsLocalSpace == false);

      // Check to see if start is contained first
      if (this.Query(ray.Origin) == true)
      {
        result.SetContained(this);
        return true;
      }

      if (this.AABB.RayCast(ref ray) == true)
        return this.ShapeRayCast(ref ray, ref result);
      return false;
    }

    /// <summary>
    /// Performs a circlecast check on this shape. 
    /// Begins with an AABB check.
    /// </summary>
    public bool CircleCast(
      ref RayCast ray, 
      float radius,
      ref RayResult result)
    {
      Debug.Assert(ray.IsLocalSpace == false);

      // Check to see if start is contained first
      if (this.Query(ray.Origin, radius) == true)
      {
        result.SetContained(this);
        return true;
      }

      if (this.AABB.CircleCast(ref ray, radius) == true)
        return this.ShapeCircleCast(ref ray, radius, ref result);
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

    public void SetWorld(Vector2 position, float radians)
    {
      this.SetWorld(position, VolatileUtil.Polar(radians));
    }

    public abstract void SetWorld(Vector2 position, Vector2 facing);

    public void ResetFromBody()
    {
      if (this.Body != null)
        this.Body.ResetShape(this);
    }

    #region Internals
    internal float ComputeMass()
    {
      return this.Area * this.Density * Config.AreaMassRatio;
    }

    internal abstract float ComputeInertia(Vector2 offset);
    #endregion

    #region Test Overrides
    internal abstract bool ShapeQuery(
      Vector2 point, 
      bool useLocalSpace = false);

    internal abstract bool ShapeQuery(
      Vector2 point, 
      float radius, 
      bool useLocalSpace = false);

    internal abstract float ShapeMinDistance(
      Vector2 point,
      bool useLocalSpace = false);

    internal abstract bool ShapeRayCast(
      ref RayCast ray,
      ref RayResult result);

    internal abstract bool ShapeCircleCast(
      ref RayCast ray,
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