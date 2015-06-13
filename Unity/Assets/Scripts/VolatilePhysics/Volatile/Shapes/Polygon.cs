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
    #region Static Methods
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

    private static float ComputeInertia(Vector2[] vertices)
    {
      float s1 = 0.0f;
      float s2 = 0.0f;

      for (int i = 0; i < vertices.Length; i++)
      {
        Vector2 v = vertices[i];
        Vector2 u = vertices[(i + 1) % vertices.Length];
        float a = Util.Cross(u, v);
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
        top = Mathf.Min(top, vertices[i].y);
        bottom = Mathf.Max(bottom, vertices[i].y);
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

    public override Shape.ShapeType Type { get { return ShapeType.Polygon; } }

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

    private Vector2[] vertices;
    private Axis[] axes;

    internal Vector2[] cachedWorldVertices;
    internal Axis[] cachedWorldAxes;

    public override bool ContainsPoint(Vector2 point)
    {
      foreach (Axis axis in this.cachedWorldAxes)
        if (Vector2.Dot(axis.Normal, point) > axis.Width)
          return false;
      return true;
    }

    public Polygon(Vector2[] vertices, float density = 1.0f)
      : base()
    {
      this.vertices = vertices;
      this.axes = Polygon.ComputeAxes(vertices);
      this.cachedWorldVertices = new Vector2[this.vertices.Length];
      this.cachedWorldAxes = new Axis[this.vertices.Length];

      // Defined in Shape class
      this.Area = Polygon.ComputeArea(vertices);
      this.Mass = Shape.ComputeMass(this.Area, density);
      this.Inertia = Polygon.ComputeInertia(vertices);
    }

    /// <summary>
    /// Creates a cache of the vertices and axes in world space. This should
    /// be called every time the world updates or the shape/body is moved
    /// externally.
    /// </summary>
    internal override void UpdateWorldCache(
      Vector2 bodyPosition,
      Vector2 bodyFacing)
    {
      for (int i = 0; i < this.vertices.Length; i++)
      {
        this.cachedWorldVertices[i] =
          bodyPosition + this.vertices[i].Rotate(bodyFacing);

        Vector2 normal = this.axes[i].Normal.Rotate(bodyFacing);
        float dot =
          Vector2.Dot(normal, bodyPosition) +
          this.axes[i].Width;
        this.cachedWorldAxes[i] = new Axis(normal, dot);
      }

      // Note we're creating the bounding box in world space
      this.AABB = Polygon.ComputeBounds(this.cachedWorldVertices);
    }
  }
}