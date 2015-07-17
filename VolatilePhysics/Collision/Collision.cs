﻿/*
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
  internal static class Collision
  {
    #region Dispatch
    private delegate Manifold Test(
      Shape sa, 
      Shape sb, 
      ObjectPool<Manifold> pool);

    private readonly static Test[,] tests = new Test[,]
      {
        { __Circle_Circle, __Circle_Polygon },
        { __Polygon_Circle, __Polygon_Polygon}
      };

    internal static Manifold Dispatch(
      Shape sa, 
      Shape sb, 
      ObjectPool<Manifold> pool)
    {
      Test test = Collision.tests[(int)sa.Type, (int)sb.Type];
      return test(sa, sb, pool);
    }

    private static Manifold __Circle_Circle(
      Shape sa, 
      Shape sb, 
      ObjectPool<Manifold> pool)
    {
      return Circle_Circle((Circle)sa, (Circle)sb, pool);
    }

    private static Manifold __Circle_Polygon(
      Shape sa, 
      Shape sb, 
      ObjectPool<Manifold> pool)
    {
      return Circle_Polygon((Circle)sa, (Polygon)sb, pool);
    }

    private static Manifold __Polygon_Circle(
      Shape sa, 
      Shape sb, 
      ObjectPool<Manifold> pool)
    {
      return Circle_Polygon((Circle)sb, (Polygon)sa, pool);
    }

    private static Manifold __Polygon_Polygon(
      Shape sa, 
      Shape sb, 
      ObjectPool<Manifold> pool)
    {
      return Polygon_Polygon((Polygon)sa, (Polygon)sb, pool);
    }
    #endregion

    #region Collision Tests
    private static Manifold Circle_Circle(
      Circle circA,
      Circle circB,
      ObjectPool<Manifold> pool)
    {
      return 
        TestCircles(
          circA, 
          circB, 
          circB.Position, 
          circB.Radius, 
          pool);
    }

    private static Manifold Circle_Polygon(
      Circle circ,
      Polygon poly,
      ObjectPool<Manifold> pool)
    {
      // Get the axis on the polygon closest to the circle's origin
      float penetration;
      int ix = 
        Collision.FindAxisMaxPenetration(
          circ.Position, 
          circ.Radius, 
          poly.worldAxes, 
          out penetration);

      if (ix < 0)
        return null;

      int length = poly.worldAxes.Length;
      Vector2 a = poly.worldVertices[ix];
      Vector2 b = poly.worldVertices[(ix + 1) % length];
      Axis axis = poly.worldAxes[ix];

      // If the circle is past one of the two vertices, check it like
      // a circle-circle intersection where the vertex has radius 0
      float d = VolatileUtil.Cross(axis.Normal, circ.Position);
      if (d > VolatileUtil.Cross(axis.Normal, a))
        return Collision.TestCircles(circ, poly, a, 0.0f, pool);
      if (d < VolatileUtil.Cross(axis.Normal, b))
        return Collision.TestCircles(circ, poly, b, 0.0f, pool);

      // Build the collision Manifold
      Manifold manifold = pool.Acquire().Assign(circ, poly);
      Vector2 pos =
        circ.Position - (circ.Radius + penetration / 2) * axis.Normal;
      manifold.AddContact(pos, -axis.Normal, penetration);
      return manifold;
    }

    private static Manifold Polygon_Polygon(
      Polygon polyA,
      Polygon polyB,
      ObjectPool<Manifold> pool)
    {
      Axis a1, a2;
      if (Collision.FindMinSepAxis(polyA, polyB, out a1) == false)
        return null;
      if (Collision.FindMinSepAxis(polyB, polyA, out a2) == false)
        return null;

      // We will use poly1's axis, so we may need to swap
      if (a2.Width > a1.Width)
      {
        VolatileUtil.Swap(ref polyA, ref polyB);
        VolatileUtil.Swap(ref a1, ref a2);
      }

      // Build the collision Manifold
      Manifold manifold = pool.Acquire().Assign(polyA, polyB);
      Collision.FindVerts(polyA, polyB, a1.Normal, a1.Width, manifold);
      return manifold;
    }
    #endregion

    #region Internals
    /// <summary>
    /// Workhorse for circle-circle collisions, compares origin distance
    /// to the sum of the two circles' radii, returns a Manifold.
    /// </summary>
    /// 
    private static Manifold TestCircles(
      Circle shapeA,
      Shape shapeB,
      Vector2 overrideBCenter, // For testing vertices in circles
      float overrideBRadius,
      ObjectPool<Manifold> pool)
    {
      Vector2 r = overrideBCenter - shapeA.Position;
      float min = shapeA.Radius + overrideBRadius;
      float distSq = r.sqrMagnitude;

      if (distSq >= min * min)
        return null;

      float dist = Mathf.Sqrt(distSq);
      float distInv = 1.0f / dist;

      Vector2 pos =
        shapeA.Position +
        (0.5f + distInv * (shapeA.Radius - min / 2.0f)) * r;

      // Build the collision Manifold
      Manifold manifold = pool.Acquire().Assign(shapeA, shapeB);
      manifold.AddContact(pos, distInv * r, dist - min);
      return manifold;
    }

    private static bool FindMinSepAxis(
      Polygon poly1,
      Polygon poly2,
      out Axis axis)
    {
      axis = new Axis(Vector2.zero, float.NegativeInfinity);
      foreach (Axis a in poly1.worldAxes)
      {
        float min = float.PositiveInfinity;
        foreach (Vector2 v in poly2.worldVertices)
          min = Mathf.Min(min, Vector2.Dot(a.Normal, v));
        min -= a.Width;

        if (min > 0)
          return false;
        if (min > axis.Width)
          axis = new Axis(a.Normal, min);
      }

      return true;
    }

    /// <summary>
    /// Add contacts for penetrating vertices. Note that this does not handle
    /// cases where an overlap was detected, but no vertices fall inside the
    /// opposing polygon (like a Star of David). For this we have a fallback.
    /// 
    /// See http://chipmunk-physics.googlecode.com/svn/trunk/src/cpCollision.c
    /// </summary>
    private static void FindVerts(
      Polygon poly1,
      Polygon poly2,
      Vector2 normal,
      float penetration,
      Manifold manifold)
    {
      bool found = false;

      foreach (Vector2 vertex in poly1.worldVertices)
      {
        if (poly2.ContainsPoint(vertex) == true)
        {
          if (manifold.AddContact(vertex, normal, penetration) == false)
            return;
          found = true;
        }
      }

      foreach (Vector2 vertex in poly2.worldVertices)
      {
        if (poly1.ContainsPoint(vertex) == true)
        {
          if (manifold.AddContact(vertex, normal, penetration) == false)
            return;
          found = true;
        }
      }

      // Fallback to check the degenerate "Star of David" case
      if (found == false)
        FindVertsFallback(poly1, poly2, normal, penetration, manifold);
    }

    /// <summary>
    /// A fallback for handling degenerate "Star of David" cases.
    /// </summary>
    private static void FindVertsFallback(
      Polygon poly1,
      Polygon poly2,
      Vector2 normal,
      float penetration,
      Manifold manifold)
    {
      foreach (Vector2 vertex in poly1.worldVertices)
      {
        if (poly2.ContainsPointPartial(vertex, normal) == true)
          if (manifold.AddContact(vertex, normal, penetration) == false)
            return;
      }

      foreach (Vector2 vertex in poly2.worldVertices)
      {
        if (poly1.ContainsPointPartial(vertex, -normal) == true)
          if (manifold.AddContact(vertex, normal, penetration) == false)
            return;
      }
    }
    #endregion

    #region Simple Tests and Helper Functions
    /// <summary>
    /// Simple check for point-circle containment.
    /// </summary>
    internal static bool TestPointCircleSimple(
      Vector2 point,
      Vector2 origin,
      float radius)
    {
      Vector2 delta = origin - point;
      return delta.sqrMagnitude <= (radius * radius);
    }

    /// <summary>
    /// Simple check for two overlapping circles.
    /// </summary>
    internal static bool TestCircleCircleSimple(
      Vector2 originA,
      Vector2 originB,
      float radiusA,
      float radiusB)
    {
      float radiusTotal = radiusA + radiusB;
      return (originA - originB).sqrMagnitude <= (radiusTotal * radiusTotal);
    }

    /// <summary>
    /// Returns the index of the nearest axis on the poly to a point.
    /// Outputs the minimum distance between the axis and the point.
    /// </summary>
    internal static int FindAxisShortestDistance(
      Vector2 point,
      Axis[] axes,
      out float minDistance)
    {
      int ix = 0;
      minDistance = float.PositiveInfinity;
      bool inside = true;

      for (int i = 0; i < axes.Length; i++)
      {
        float dot = Vector2.Dot(axes[i].Normal, point);
        float dist = axes[i].Width - dot;

        if (dist < 0.0f)
          inside = false;

        if (dist < minDistance)
        {
          minDistance = dist;
          ix = i;
        }
      }

      if (inside == true)
      {
        minDistance = 0.0f;
        ix = -1;
      }

      return ix;
    }

    /// <summary>
    /// Returns the index of the axis with the max circle penetration depth.
    /// Breaks out if a separating axis is found between the two shapes.
    /// Outputs the penetration depth of the circle in the axis (if any).
    /// </summary>
    internal static int FindAxisMaxPenetration(
      Vector2 origin,
      float radius,
      Axis[] axes,
      out float penetration)
    {
      int ix = 0;
      penetration = float.NegativeInfinity;

      for (int i = 0; i < axes.Length; i++)
      {
        float dot = Vector2.Dot(axes[i].Normal, origin);
        float dist = dot - axes[i].Width - radius;

        if (dist > 0)
          return -1;

        if (dist > penetration)
        {
          penetration = dist;
          ix = i;
        }
      }

      return ix;
    }
    #endregion
  }
}
