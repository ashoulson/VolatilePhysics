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

namespace Volatile.History
{
  internal class BodyLogger
  {
    private struct BodyRecord
    {
      public int Frame { get { return this.frame; } }
      public AABB AABB { get { return this.aabb; } }

      public bool IsValid { get { return this.frame >= 0; } }

      private int frame;
      private AABB aabb;

      public void Set(int frame, Body body)
      {
        this.frame = frame;
        this.aabb = body.AABB;
      }

      public void Invalidate()
      {
        this.frame = -1;
      }

      internal bool QueryAABB(AABB aabb)
      {
        return this.aabb.Intersect(aabb);
      }

      internal bool QueryAABB(Vector2 point)
      {
        return this.aabb.Query(point);
      }

      internal bool QueryAABB(Vector2 point, float radius)
      {
        return this.aabb.Query(point, radius);
      }

      public bool RayCastAABB(ref RayCast ray)
      {
        return this.aabb.RayCast(ref ray);
      }

      public bool CircleCastAABB(ref RayCast ray, float radius)
      {
        return this.aabb.CircleCast(ref ray, radius);
      }
    }

    private Body body;
    private BodyRecord[] records;
    private ShapeLogger[] shapes;

    public BodyLogger(Body body, int capacity)
    {
      this.body = body;

      this.records = new BodyRecord[capacity];
      for (int i = 0; i < capacity; i++)
        this.records[i].Invalidate();

      IList<Shape> bodyShapes = body.Shapes;
      this.shapes = new ShapeLogger[bodyShapes.Count];
      for (int i = 0; i < bodyShapes.Count; i++)
        this.shapes[i] = new ShapeLogger(bodyShapes[i], capacity);
    }

    public void Store(int frame)
    {
      this.records[this.FrameToIndex(frame)].Set(frame, this.body);
      for (int i = 0; i < this.shapes.Length; i++)
        this.shapes[i].Store(frame);
    }

    #region Tests

    #region Queries
    /// <summary>
    /// Checks if a body's AABB overlaps with a given AABB.
    /// </summary>
    internal bool Query(
      int frame,
      AABB area)
    {
      BodyRecord record = this.records[this.FrameToIndex(frame)];

      if (record.Frame == frame)
        return record.QueryAABB(area);

      // If the record is invalid, fall back to a current-time query
      return this.body.Query(area);
    }

    /// <summary>
    /// Checks if a point is contained within this body
    /// Begins with AABB checks.
    /// </summary>
    internal bool Query(
      int frame,
      Vector2 point,
      Func<Shape, bool> filter = null)
    {
      BodyRecord record = this.records[this.FrameToIndex(frame)];

      if (record.Frame == frame)
      {
        if (record.QueryAABB(point) == true)
        {
          for (int i = 0; i < this.shapes.Length; i++)
          {
            ShapeLogger shape = this.shapes[i];
            if (filter == null || filter(shape.Shape) == true)
            {
              if (shape.Query(frame, point) == true)
              {
                return true;
              }
            }
          }
        }
        return false;
      }

      // If the record is invalid, fall back to a current-time query
      return this.body.Query(point, filter);
    }

    /// <summary>
    /// Checks if a circle overlaps with this body. 
    /// Begins with AABB checks.
    /// </summary>
    internal bool Query(
      int frame,
      Vector2 point,
      float radius,
      Func<Shape, bool> filter = null)
    {
      BodyRecord record = this.records[this.FrameToIndex(frame)];

      if (record.Frame == frame)
      {
        if (record.QueryAABB(point, radius) == true)
        {
          for (int i = 0; i < this.shapes.Length; i++)
          {
            ShapeLogger shape = this.shapes[i];
            if (filter == null || filter(shape.Shape) == true)
            {
              if (shape.Query(frame, point, radius) == true)
              {
                return true;
              }
            }
          }
        }
        return false;
      }

      // If the record is invalid, fall back to a current-time query
      return this.body.Query(point, radius, filter);
    }

    /// <summary>
    /// Returns the minimum distance between this body and the point.
    /// </summary>
    internal bool MinDistance(
      int frame,
      Vector2 point,
      float maxDistance,
      out float minDistance,
      Func<Shape, bool> filter = null)
    {
      BodyRecord record = this.records[this.FrameToIndex(frame)];

      if (record.Frame == frame)
      {
        minDistance = float.PositiveInfinity;
        bool result = false;

        if (record.QueryAABB(point, maxDistance) == true)
        {
          for (int i = 0; i < this.shapes.Length; i++)
          {
            ShapeLogger shape = this.shapes[i];
            if (filter == null || filter(shape.Shape) == true)
            {
              float distance;
              result |=
                shape.MinDistance(frame, point, maxDistance, out distance);
              if (distance < minDistance)
                minDistance = distance;
            }
          }
        }

        return result;
      }

      // If the record is invalid, fall back to a current-time query
      return
        this.body.MinDistance(point, maxDistance, out minDistance, filter);
    }
    #endregion

    #region Line/Sweep Tests
    internal bool RayCast(
      int frame,
      ref RayCast ray,
      ref RayResult result,
      Func<Shape, bool> filter = null)
    {
      BodyRecord record = this.records[this.FrameToIndex(frame)];

      if (record.Frame == frame)
      {
        bool hit = false;
        if (record.RayCastAABB(ref ray) == true)
        {
          foreach (ShapeLogger shape in this.shapes)
          {
            if (filter == null || filter(shape.Shape) == true)
            {
              if (shape.RayCast(frame, ref ray, ref result) == true)
              {
                if (result.IsContained == true)
                  return true;
                hit = true;
              }
            }
          }
        }
        return hit;
      }

      // If the record is invalid, fall back to a current-time ray cast
      return this.body.RayCast(ref ray, ref result);
    }

    internal bool CircleCast(
      int frame,
      ref RayCast ray,
      float radius,
      ref RayResult result,
      Func<Shape, bool> filter = null)
    {
      BodyRecord record = this.records[this.FrameToIndex(frame)];

      if (record.Frame == frame)
      {
        bool hit = false;
        if (record.CircleCastAABB(ref ray, radius) == true)
        {
          foreach (ShapeLogger shape in this.shapes)
          {
            if (filter == null || filter(shape.Shape) == true)
            {
              if (shape.CircleCast(frame, ref ray, radius, ref result) == true)
              {
                if (result.IsContained == true)
                  return true;
                hit = true;
              }
            }
          }
        }
        return hit;
      }
        
      // If the record is invalid, fall back to a current-time ray cast
      return this.body.RayCast(ref ray, ref result);
    }
    #endregion

    #endregion

    #region Internals
    private int FrameToIndex(int frame)
    {
      return frame % this.records.Length;
    }
    #endregion

    #region Debug
    public void GizmoDraw(
      Color aabbColorBody,
      Color aabbColorShape)
    {
      for (int i = 0; i < this.records.Length; i++)
        if (this.records[i].IsValid == true)
          this.records[i].AABB.GizmoDraw(aabbColorBody);
      foreach (ShapeLogger shape in this.shapes)
        shape.GizmoDraw(aabbColorShape);
    }
    #endregion
  }
}
