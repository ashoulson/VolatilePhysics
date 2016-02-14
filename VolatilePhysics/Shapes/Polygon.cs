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
  public sealed class Polygon : Shape
  {
    #region Factory Functions
    public static Polygon FromWorldVertices(
      Vector2[] vertices,
      float density = Config.DEFAULT_DENSITY,
      float friction = Config.DEFAULT_FRICTION,
      float restitution = Config.DEFAULT_RESTITUTION)
    {
      return new Polygon(vertices, density, friction, restitution);
    }
    #endregion

    #region Static Helpers
    private static Axis[] ComputeAxes(Vector2[] vertices)
    {
      Axis[] axes = new Axis[vertices.Length];

      for (int i = 0; i < vertices.Length; i++)
      {
        Vector2 u = vertices[i];
        Vector2 v = vertices[(i + 1) % vertices.Length];
        Vector2 normal = (v - u).Left().normalized;
        axes[i] = new Axis(normal, Vector2.Dot(normal, u));
      }

      return axes;
    }

    private static AABB ComputeBounds(Vector2[] vertices)
    {
      float top = vertices[0].y;
      float bottom = vertices[0].y;
      float left = vertices[0].x;
      float right = vertices[0].x;

      for (int i = 1; i < vertices.Length; i++)
      {
        top = Mathf.Max(top, vertices[i].y);
        bottom = Mathf.Min(bottom, vertices[i].y);
        left = Mathf.Min(left, vertices[i].x);
        right = Mathf.Max(right, vertices[i].x);
      }

      return new AABB(top, bottom, left, right);
    }

    private static Vector2[] CloneVertices(Vector2[] source)
    {
      Vector2[] vertices = new Vector2[source.Length];
      for (int i = 0; i < source.Length; i++)
        vertices[i] = source[i];
      return vertices;
    }
    #endregion

    #region Properties
    public override Shape.ShapeType Type { get { return ShapeType.Polygon; } }

    public Vector2[] Vertices 
    {
      get { return Polygon.CloneVertices(this.worldSpaceVertices); }
    }
    #endregion

    #region Fields
    internal Vector2[] worldSpaceVertices;
    internal Axis[] worldSpaceAxes;

    // Precomputed body-space values (these should never change unless we
    // want to support moving shapes relative to their body root later on)
    private Vector2[] bodySpaceVertices;
    private Axis[] bodySpaceAxes;
    #endregion

    private Polygon(
      Vector2[] worldSpaceVertices,
      float density,
      float friction,
      float restitution)
      : base(density, friction, restitution)
    {
      this.worldSpaceVertices = Polygon.CloneVertices(worldSpaceVertices);
      this.worldSpaceAxes = Polygon.ComputeAxes(worldSpaceVertices);
      this.AABB = Polygon.ComputeBounds(worldSpaceVertices);

      // Will be initialized later once we're attached to a body
      this.bodySpaceVertices = null;
      this.bodySpaceAxes = null;
      this.bodySpaceAABB = new AABB();
    }

    #region Functionalty Overrides
    protected override void ComputeMetrics()
    {
      // Compute body-space geometry data (only need to do this once)
      this.bodySpaceVertices = new Vector2[this.worldSpaceVertices.Length];
      for (int i = 0; i < this.worldSpaceVertices.Length; i++)
        this.bodySpaceVertices[i] =
          this.Body.WorldToBodyPointCurrent(this.worldSpaceVertices[i]);
      this.bodySpaceAxes = Polygon.ComputeAxes(this.bodySpaceVertices);
      this.bodySpaceAABB = Polygon.ComputeBounds(this.bodySpaceVertices);

      this.Area = this.ComputeArea();
      this.Mass = this.Area * this.Density * Config.AreaMassRatio;
      this.Inertia = this.ComputeInertia();
    }

    protected override void ApplyBodyPosition()
    {
      for (int i = 0; i < this.bodySpaceVertices.Length; i++)
      {
        this.worldSpaceVertices[i] =
          this.Body.BodyToWorldPointCurrent(this.bodySpaceVertices[i]);
        this.worldSpaceAxes[i] =
          this.Body.BodyToWorldAxisCurrent(this.bodySpaceAxes[i]);
      }

      this.AABB = Polygon.ComputeBounds(this.worldSpaceVertices);
    }
    #endregion

    #region Test Overrides
    protected override bool ShapeQueryPoint(
      Vector2 bodySpacePoint)
    {
      foreach (Axis axis in this.bodySpaceAxes)
        if (Vector2.Dot(axis.Normal, bodySpacePoint) > axis.Width)
          return false;
      return true;
    }

    protected override bool ShapeQueryCircle(
      Vector2 bodySpaceOrigin,
      float radius)
    {
      // Get the axis on the polygon closest to the circle's origin
      float penetration;
      int foundIndex =
        Collision.FindAxisMaxPenetration(
          bodySpaceOrigin,
          radius,
          this.bodySpaceAxes,
          out penetration);

      if (foundIndex < 0)
        return false;

      int numVertices = this.bodySpaceVertices.Length;
      Vector2 a = this.bodySpaceVertices[foundIndex];
      Vector2 b = this.bodySpaceVertices[(foundIndex + 1) % numVertices];
      Axis axis = this.bodySpaceAxes[foundIndex];

      // If the circle is past one of the two vertices, check it like
      // a circle-circle intersection where the vertex has radius 0
      float d = VolatileUtil.Cross(axis.Normal, bodySpaceOrigin);
      if (d > VolatileUtil.Cross(axis.Normal, a))
        return Collision.TestPointCircleSimple(a, bodySpaceOrigin, radius);
      if (d < VolatileUtil.Cross(axis.Normal, b))
        return Collision.TestPointCircleSimple(b, bodySpaceOrigin, radius);
      return true;
    }

    protected override bool ShapeRayCast(
      ref RayCast bodySpaceRay,
      ref RayResult result)
    {
      int foundIndex = -1;
      float inner = float.MaxValue;
      float outer = 0;
      bool couldBeContained = true;

      for (int i = 0; i < this.bodySpaceVertices.Length; i++)
      {
        Axis curAxis = this.bodySpaceAxes[i];

        // Distance between the ray origin and the axis/edge along the 
        // normal (i.e., shortest distance between ray origin and the edge)
        float proj = 
          Vector2.Dot(curAxis.Normal, bodySpaceRay.origin) - curAxis.Width;

        // See if the point is outside of any of the axes
        if (proj > 0.0f)
          couldBeContained = false;

        // Projection of the ray direction onto the axis normal (use 
        // negative normal because we want to get the penetration length)
        float slope = Vector2.Dot(-curAxis.Normal, bodySpaceRay.direction);

        if (slope == 0.0f)
          continue;
        float dist = proj / slope;

        // The ray is pointing opposite the edge normal (towards the edge)
        if (slope > 0.0f)
        {
          if (dist > inner)
          {
            return false;
          }
          if (dist > outer)
          {
            outer = dist;
            foundIndex = i;
          }
        }
        // The ray is pointing along the edge normal (away from the edge)
        else
        {
          if (dist < outer)
          {
            return false;
          }
          if (dist < inner)
          {
            inner = dist;
          }
        }
      }

      if (couldBeContained == true)
      {
        result.SetContained(this);
        return true;
      }
      else if (foundIndex >= 0 && outer <= bodySpaceRay.distance)
      {
        result.Set(
          this,
          outer,
          this.bodySpaceAxes[foundIndex].Normal);
        return true;
      }

      return false;
    }

    protected override bool ShapeCircleCast(
      ref RayCast bodySpaceRay,
      float radius,
      ref RayResult result)
    {
      bool checkVertices =
        this.CircleCastVertices(
          ref bodySpaceRay,
          radius,
          ref result);

      bool checkEdges =
        this.CircleCastEdges(
          ref bodySpaceRay,
          radius,
          ref result);

      // We need to check both to get the closest hit distance
      return checkVertices || checkEdges;
    }
    #endregion

    #region Collision Helpers
    /// <summary>
    /// A world-space point query, used as a shortcut in collision tests.
    /// </summary>
    internal bool ContainsPoint(
      Vector2 worldSpacePoint)
    {
      for (int i = 0; i < this.worldSpaceAxes.Length; i++)
      {
        Axis axis = this.worldSpaceAxes[i];
        if (Vector2.Dot(axis.Normal, worldSpacePoint) > axis.Width)
          return false;
      }

      return true;
    }

    /// <summary>
    /// Special case that ignores axes pointing away from the normal.
    /// </summary>
    internal bool ContainsPointPartial(
      Vector2 worldSpacePoint,
      Vector2 worldSpaceNormal)
    {
      foreach (Axis axis in this.worldSpaceAxes)
        if (Vector2.Dot(axis.Normal, worldSpaceNormal) >= 0.0f &&
            Vector2.Dot(axis.Normal, worldSpacePoint) > axis.Width)
          return false;
      return true;
    }
    #endregion

    #region Internals
    private float ComputeArea()
    {
      float sum = 0;

      for (int i = 0; i < this.bodySpaceVertices.Length; i++)
      {
        int numVertices = this.bodySpaceVertices.Length;
        Vector2 v = this.bodySpaceVertices[i];
        Vector2 u = this.bodySpaceVertices[(i + 1) % numVertices];
        Vector2 w = this.bodySpaceVertices[(i + 2) % numVertices];

        sum += u.x * (v.y - w.y);
      }

      return sum / 2.0f;
    }

    private float ComputeInertia()
    {
      float s1 = 0.0f;
      float s2 = 0.0f;

      for (int i = 0; i < this.bodySpaceVertices.Length; i++)
      {
        int numVertices = this.bodySpaceVertices.Length;
        Vector2 v = this.bodySpaceVertices[i];
        Vector2 u = this.bodySpaceVertices[(i + 1) % numVertices];

        float a = VolatileUtil.Cross(u, v);
        float b = v.sqrMagnitude + u.sqrMagnitude + Vector2.Dot(v, u);
        s1 += a * b;
        s2 += a;
      }

      return s1 / (6.0f * s2);
    }

    private bool CircleCastEdges(
      ref RayCast bodySpaceRay,
      float radius,
      ref RayResult result)
    {
      int foundIndex = -1;
      int length = this.bodySpaceVertices.Length;
      bool couldBeContained = true;

      // Pre-compute and initialize values
      float shortestDist = float.MaxValue;
      Vector2 v3 = bodySpaceRay.direction.Left();

      // Check the edges -- this will be different from the raycast because
      // we care about staying within the ends of the edge line segment
      for (int i = 0; i < length; i++)
      {
        Axis curAxis = this.bodySpaceAxes[i];

        // Push the edges out by the radius
        Vector2 extension = curAxis.Normal * radius;
        Vector2 a = this.bodySpaceVertices[i] + extension;
        Vector2 b = this.bodySpaceVertices[(i + 1) % length] + extension;

        // Update the check for containment
        if (couldBeContained == true)
        {
          float proj = 
            Vector2.Dot(curAxis.Normal, bodySpaceRay.origin) - curAxis.Width;

          // The point lies outside of the outer layer
          if (proj > radius)
          {
            couldBeContained = false;
          }
          // The point lies between the outer and inner layer
          else if (proj > 0.0f)
          {
            // See if the point is within the center Vornoi region of the edge
            float d = VolatileUtil.Cross(curAxis.Normal, bodySpaceRay.origin);
            if (d > VolatileUtil.Cross(curAxis.Normal, a))
              couldBeContained = false;
            if (d < VolatileUtil.Cross(curAxis.Normal, b))
              couldBeContained = false;
          }
        }

        // For the cast, only consider rays pointing towards the edge
        if (Vector2.Dot(curAxis.Normal, bodySpaceRay.direction) >= 0.0f)
          continue;

        // See: 
        // https://rootllama.wordpress.com/2014/06/20/ray-line-segment-intersection-test-in-2d/
        Vector2 v1 = bodySpaceRay.origin - a;
        Vector2 v2 = b - a;

        float denominator = Vector2.Dot(v2, v3);
        float t1 = VolatileUtil.Cross(v2, v1) / denominator;
        float t2 = Vector2.Dot(v1, v3) / denominator;

        if ((t2 >= 0.0f) && (t2 <= 1.0f) && (t1 > 0.0f) && (t1 < shortestDist))
        {
          // See if the point is outside of any of the axes
          shortestDist = t1;
          foundIndex = i;
        }
      }

      // Report results
      if (couldBeContained == true)
      {
        result.SetContained(this);
        return true;
      }
      else if (foundIndex >= 0 && shortestDist <= bodySpaceRay.distance)
      {
        result.Set(
          this,
          shortestDist,
          this.bodySpaceAxes[foundIndex].Normal);
        return true;
      }
      return false;
    }

    private bool CircleCastVertices(
      ref RayCast bodySpaceRay,
      float radius,
      ref RayResult result)
    {
      float sqrRadius = radius * radius;
      bool castHit = false;

      for (int i = 0; i < this.bodySpaceVertices.Length; i++)
      {
        castHit |=
          Collision.CircleRayCast(
            this,
            this.bodySpaceVertices[i],
            sqrRadius,
            ref bodySpaceRay,
            ref result);
        if (result.IsContained == true)
          return true;
      }

      return castHit;
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

      for (int i = 0; i < this.worldSpaceVertices.Length; i++)
      {
        Vector2 u = this.worldSpaceVertices[i];
        Vector2 v =
          this.worldSpaceVertices[(i + 1) % this.worldSpaceVertices.Length];
        Vector2 n = worldSpaceAxes[i].Normal;

        Vector2 delta = v - u;
        Vector2 midPoint = u + (delta * 0.5f);

        // Draw edge
        Gizmos.color = edgeColor;
        Gizmos.DrawLine(u, v);

        // Draw normal
        Gizmos.color = normalColor;
        Gizmos.DrawLine(midPoint, midPoint + (n * normalLength));
      }

      this.AABB.GizmoDraw(aabbColor);

      Gizmos.color = current;
    }
#endif
    #endregion
  }
}
