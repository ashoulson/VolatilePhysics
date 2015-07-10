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
  public sealed class Polygon : Shape
  {
    #region Factory Functions
    public static Polygon FromWorldVertices(
      Vector2 origin,
      Vector2 facing, 
      Vector2[] vertices,
      float density = 1.0f,
      float friction = Config.DEFAULT_FRICTION,
      float restitution = Config.DEFAULT_RESTITUTION)
    {
      return new Polygon(
        origin,
        facing,
        Polygon.ComputeOffsetVertices(origin, facing, vertices),
        density,
        friction,
        restitution);
    }

    public static Polygon FromLocalVertices(
      Vector2 origin,
      Vector2 facing, 
      Vector2[] vertices,
      float density = 1.0f,
      float friction = Config.DEFAULT_FRICTION,
      float restitution = Config.DEFAULT_RESTITUTION)
    {
      return new Polygon(
        origin, 
        facing, 
        vertices, 
        density, 
        friction, 
        restitution);
    }
    #endregion

    #region Private Static Methods
    /// <summary>
    /// Converts world space vertices to offsets.
    /// </summary>
    /// <param name="origin">Shape origin point in world space.</param>
    /// <param name="facing">World space orientation of shape.</param>
    /// <param name="vertices">Vertex positions in world space.</param>
    /// <returns></returns>
    private static Vector2[] ComputeOffsetVertices(
      Vector2 origin,
      Vector2 facing,
      Vector2[] vertices)
    {
      Vector2[] offsets = new Vector2[vertices.Length];
      for (int i = 0; i < offsets.Length; i++)
        offsets[i] = ((vertices[i]) - origin).InvRotate(facing);
      return offsets;
    }

    /// <summary>
    /// Computes the vector between the vertex and the origin, given a 
    /// (rotation-adjusted) offset to that origin.
    /// </summary>
    private static Vector2 ComputeVertexOffset(
      Vector2 vertex, 
      Vector2 originOffset,
      Vector2 shapeFacing)
    {
      return originOffset + shapeFacing.Rotate(vertex);
    }

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

    private static float ComputeArea(Vector2[] vertices)
    {
      float sum = 0;

      for (int i = 0; i < vertices.Length; i++)
      {
        Vector2 v = vertices[i];
        Vector2 u = vertices[(i + 1) % vertices.Length];
        Vector2 w = vertices[(i + 2) % vertices.Length];
        sum += u.x * (v.y - w.y);
      }

      return sum / 2.0f;
    }

    private static float ComputeInertia(
      Vector2[] vertices, 
      Vector2 originOffset, 
      Vector2 shapeFacing)
    {
      float s1 = 0.0f;
      float s2 = 0.0f;

      // Compute the vertex offsets to the origin point
      Vector2[] vertexOffsets = new Vector2[vertices.Length];
      for (int i = 0; i < vertexOffsets.Length; i++)
        vertexOffsets[i] =
          ComputeVertexOffset(vertices[i], originOffset, shapeFacing);

      // Given the offsets, compute the inertia
      for (int i = 0; i < vertexOffsets.Length; i++)
      {
        Vector2 v = vertexOffsets[i];
        Vector2 u = vertexOffsets[(i + 1) % vertexOffsets.Length];
        float a = VolatileUtil.Cross(u, v);
        float b = v.sqrMagnitude + u.sqrMagnitude + Vector2.Dot(v, u);
        s1 += a * b;
        s2 += a;
      }

      return s1 / (6.0f * s2);
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
      Vector2[] vertices =
        new Vector2[source.Length];
      for (int i = 0; i < source.Length; i++)
        vertices[i] = source[i];
      return vertices;
    }

    private static Vector2[] CloneNormals(Axis[] source)
    {
      Vector2[] normals =
        new Vector2[source.Length];
      for (int i = 0; i < source.Length; i++)
        normals[i] = source[i].Normal;
      return normals;
    }
    #endregion

    #region Properties
    public override Shape.ShapeType Type { get { return ShapeType.Polygon; } }
    public override Vector2 Position { get { return this.origin; } }
    public override Vector2 Facing { get { return this.facing; } }
    public override float Angle { get { return this.facing.Angle(); } }

    public Vector2[] LocalVertices 
    { 
      get { return Polygon.CloneVertices(this.vertices); } 
    }

    public Vector2[] WorldVertices
    {
      get { return Polygon.CloneVertices(this.cachedWorldVertices); }
    }

    public Vector2[] LocalNormals
    {
      get { return Polygon.CloneNormals(this.axes); }
    }

    public Vector2[] WorldNormals
    {
      get { return Polygon.CloneNormals(this.cachedWorldAxes); }
    }
    #endregion

    #region Fields
    // Local space values
    private Vector2[] vertices;
    private Axis[] axes;

    // World space values
    private Vector2 origin;
    private Vector2 facing;

    // Cached world space computation results
    internal Vector2[] cachedWorldVertices;
    internal Axis[] cachedWorldAxes;
    #endregion

    #region Tests
    internal override float ShapeMinDistance(Vector2 point)
    {
      // Get the axis on the polygon closest to the circle's origin
      float dist;
      int ix = Collision.FindAxisShortestDistance(point, this, out dist);

      if (ix == -1)
        return dist;

      int length = this.cachedWorldAxes.Length;
      Vector2 a = this.cachedWorldVertices[ix];
      Vector2 b = this.cachedWorldVertices[(ix + 1) % length];
      Axis axis = this.cachedWorldAxes[ix];

      // If the point is past one of the two vertices, check it like
      // a point-circle intersection where the vertex has radius 0
      float d = VolatileUtil.Cross(axis.Normal, point);
      if (d > VolatileUtil.Cross(axis.Normal, a))
        return (point - a).magnitude;
      if (d < VolatileUtil.Cross(axis.Normal, b))
        return (point - b).magnitude;
      return Mathf.Abs(dist);
    }

    internal override bool ShapeQuery(Vector2 point)
    {
      foreach (Axis axis in this.cachedWorldAxes)
        if (Vector2.Dot(axis.Normal, point) > axis.Width)
          return false;
      return true;
    }

    internal override bool ShapeQuery(Vector2 origin, float radius)
    {
      // Get the axis on the polygon closest to the circle's origin
      float penetration;
      int ix =
        Collision.FindAxisMaxPenetration(origin, radius, this, out penetration);

      if (ix < 0)
        return false;

      int length = this.cachedWorldAxes.Length;
      Vector2 a = this.cachedWorldVertices[ix];
      Vector2 b = this.cachedWorldVertices[(ix + 1) % length];
      Axis axis = this.cachedWorldAxes[ix];

      // If the circle is past one of the two vertices, check it like
      // a circle-circle intersection where the vertex has radius 0
      float d = VolatileUtil.Cross(axis.Normal, origin);
      if (d > VolatileUtil.Cross(axis.Normal, a))
        return Collision.TestPointCircleSimple(a, origin, radius);
      if (d < VolatileUtil.Cross(axis.Normal, b))
        return Collision.TestPointCircleSimple(b, origin, radius);
      return true;
    }

    internal override bool ShapeRayCast(ref RayCast ray, ref RayResult result)
    {
      int foundIndex = -1;
      float inner = float.MaxValue;
      float outer = 0;

      for (int i = 0; i < this.cachedWorldVertices.Length; i++)
      {
        Axis curAxis = this.cachedWorldAxes[i];

        // Distance between the ray origin and the axis/edge along the normal
        // (i.e., shortest distance between ray origin and the edge).
        float proj = Vector2.Dot(curAxis.Normal, ray.Origin) - curAxis.Width;

        // Projection of the ray direction onto the axis normal (use negative
        // normal because we want to get the penetration length).
        float slope = Vector2.Dot(-curAxis.Normal, ray.Direction);

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

      if (foundIndex >= 0 && outer <= ray.Distance)
      {
        result.Set(
          this, 
          outer, 
          this.cachedWorldAxes[foundIndex].Normal);
        return true;
      }

      return false;
    }

    internal override bool ShapeCircleCast(
      ref RayCast ray, 
      float radius,
      ref RayResult result)
    {
      bool edges = this.CircleCastEdges(ref ray, radius, ref result);
      bool vertices = this.CircleCastVertices(ref ray, radius, ref result);
      return edges || vertices;
    }

    private bool CircleCastEdges(
      ref RayCast ray, 
      float radius,
      ref RayResult result)
    {
      int foundIndex = -1;
      int length = this.cachedWorldVertices.Length;

      // Pre-compute and initialize values
      float shortestDist = float.MaxValue;
      Vector2 v3 = ray.Direction.Left();

      // Check the edges -- this will be different from the raycast because
      // we care about staying within the ends of the segment this time
      for (int i = 0; i < this.cachedWorldVertices.Length; i++)
      {
        Axis curAxis = this.cachedWorldAxes[i];

        // Only consider rays pointing towards the edge
        if (Vector2.Dot(curAxis.Normal, ray.Direction) >= 0.0f)
          continue;

        // Push the edges out by the radius
        Vector2 extension = curAxis.Normal * radius;
        Vector2 a = this.cachedWorldVertices[i] + extension;
        Vector2 b = this.cachedWorldVertices[(i + 1) % length] + extension;

        // See: 
        // https://rootllama.wordpress.com/2014/06/20/ray-line-segment-intersection-test-in-2d/
        Vector2 v1 = ray.Origin - a;
        Vector2 v2 = b - a;

        float denominator = Vector2.Dot(v2, v3);
        float t1 = VolatileUtil.Cross(v2, v1) / denominator;
        float t2 = Vector2.Dot(v1, v3) / denominator;

        if (t2 >= 0.0f && t2 <= 1.0f && t1 > 0.0f && t1 < shortestDist)
        {
          shortestDist = t1;
          foundIndex = i;
        }
      }

      // Report results
      if (foundIndex >= 0 && shortestDist <= ray.Distance)
      {
        result.Set(
          this,
          shortestDist,
          this.cachedWorldAxes[foundIndex].Normal);
        return true;
      }
      return false;
    }

    private bool CircleCastVertices(
      ref RayCast ray,
      float radius,
      ref RayResult result)
    {
      float sqrRadius = radius * radius;
      bool castHit = false;

      for (int i = 0; i < this.cachedWorldVertices.Length; i++)
      {
        castHit |= 
          Circle.CircleRayCast(
            this,
            this.cachedWorldVertices[i],
            sqrRadius,
            ref ray,
            ref result);
      }

      return castHit;
    }
    #endregion

    public override void SetWorld(Vector2 position, Vector2 facing)
    {
      this.origin = position;
      this.facing = facing;
      this.ComputeWorldVertices();
      this.AABB = Polygon.ComputeBounds(this.cachedWorldVertices);
    }

    #region Internals
    /// <summary>
    /// Creates a new polygon from an origin and local-space vertices.
    /// </summary>
    /// <param name="origin">Shape origin point in world space.</param>
    /// <param name="facing">World space orientation of shape.</param>
    /// <param name="vertices">Vertex positions relative to the origin.</param>
    /// <param name="density">Shape density.</param>
    /// <param name="friction">Shape friction.</param>
    /// <param name="restitution">Shape restitution.</param>
    private Polygon(
      Vector2 origin,
      Vector2 facing,
      Vector2[] vertices,
      float density,
      float friction,
      float restitution)
      : base(density, friction, restitution)
    {
      this.origin = origin;
      this.facing = facing;
      this.vertices = vertices;

      this.axes = Polygon.ComputeAxes(this.vertices);
      this.cachedWorldVertices = new Vector2[this.vertices.Length];
      this.cachedWorldAxes = new Axis[this.vertices.Length];

      // Defined in Shape class
      this.Area = Polygon.ComputeArea(vertices);

      this.ComputeWorldVertices();
    }

    /// <summary>
    /// Computes inertia given an offset from the origin.
    /// </summary>
    internal override float ComputeInertia(Vector2 offset)
    {
      return Polygon.ComputeInertia(this.vertices, offset, this.facing);
    }

    /// <summary>
    /// Used in collision, for consistency.
    /// </summary>
    internal bool ContainsPoint(Vector2 point)
    {
      return this.ShapeQuery(point);
    }

    /// <summary>
    /// Special case that ignores axes pointing away from the normal.
    /// </summary>
    internal bool ContainsPointPartial(Vector2 point, Vector2 normal)
    {
      foreach (Axis axis in this.cachedWorldAxes)
        if (Vector2.Dot(axis.Normal, normal) >= 0.0f &&
            Vector2.Dot(axis.Normal, point) > axis.Width)
          return false;
      return true;
    }

    /// <summary>
    /// Coverts the local space axes and vertices to world space.
    /// </summary>
    private void ComputeWorldVertices()
    {
      for (int i = 0; i < this.vertices.Length; i++)
      {
        this.cachedWorldVertices[i] =
          this.origin + this.vertices[i].Rotate(this.facing);

        Vector2 normal = this.axes[i].Normal.Rotate(this.facing);
        float dot =
          Vector2.Dot(normal, this.origin) +
          this.axes[i].Width;
        this.cachedWorldAxes[i] = new Axis(normal, dot);
      }
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

      Vector2[] worldNormals = this.WorldNormals;
      for (int i = 0; i < this.cachedWorldVertices.Length; i++)
      {
        Vector2 u = this.cachedWorldVertices[i];
        Vector2 v = 
          this.cachedWorldVertices[(i + 1) % this.cachedWorldVertices.Length];
        Vector2 n = worldNormals[i];

        Vector2 delta = v - u;
        Vector2 midPoint = u + (delta * 0.5f);

        // Draw edge
        Gizmos.color = edgeColor;
        Gizmos.DrawLine(u, v);

        // Draw normal
        Gizmos.color = normalColor;
        Gizmos.DrawLine(midPoint, midPoint + (n * normalLength));

        // Draw line to origin
        Gizmos.color = originColor;
        Gizmos.DrawLine(u, this.Position);
      }

      // Draw facing
      Gizmos.color = normalColor;
      Gizmos.DrawLine(
        this.Position,
        this.Position + this.Facing * normalLength);

      // Draw origin
      Gizmos.color = originColor;
      Gizmos.DrawWireSphere(this.Position, 0.05f);

      this.AABB.GizmoDraw(aabbColor);

      Gizmos.color = current;
    }
    #endregion
  }
}
