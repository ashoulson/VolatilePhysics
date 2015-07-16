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
  internal class ShapeLogger
  {
    private struct ShapeRecord
    {
      public int Frame { get { return this.frame; } }
      public AABB AABB { get { return this.aabb; } }
      public Vector2 Position { get { return this.position; } }
      public Vector2 Facing { get { return this.facing; } }

      public bool IsValid { get { return this.frame >= 0; } }

      private int frame;
      private AABB aabb;
      private Vector2 position;
      private Vector2 facing;

      public void Set(int frame, Shape shape)
      {
        this.frame = frame;
        this.aabb = shape.AABB;
        this.position = shape.Position;
        this.facing = shape.Facing;
      }

      public void Invalidate()
      {
        this.frame = -1;
      }

      public bool QueryAABB(Vector2 point)
      {
        return this.aabb.Query(point);
      }

      public bool QueryAABB(Vector2 point, float radius)
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

    internal Shape Shape { get { return this.shape; } }

    private ShapeRecord[] records;
    private Shape shape;

    public ShapeLogger(Shape shape, int capacity)
    {
      this.shape = shape;
      this.records = new ShapeRecord[capacity];
      for (int i = 0; i < capacity; i++)
        this.records[i].Invalidate();
    }

    public void Store(int frame)
    {
      this.records[this.FrameToIndex(frame)].Set(frame, this.shape);
    }

    internal bool RayCast(
      int frame, 
      ref RayCast ray, 
      ref RayResult result)
    {
      ShapeRecord record = this.records[this.FrameToIndex(frame)];

      if (record.Frame == frame)
      {
        // Record is valid, create a local mask
        ray.CreateMask(record.Position, record.Facing);
        ray.DisableMask();

        // Check containment of the ray origin in world space
        if (record.QueryAABB(ray.Origin) == true)
        {
          // Perform shape/point query in local space
          ray.EnableMask();
          if (this.shape.ShapeQuery(ray.Origin, true))
          {
            result.SetContained(this.shape);
            ray.ClearMask();
            return true;
          }
        }

        // Perform aabb raycast in world space
        ray.DisableMask();
        if (record.RayCastAABB(ref ray) == true)
        {
          // Perform shape raycast in local space
          ray.EnableMask();
          if (this.shape.RayCast(ref ray, ref result) == true)
          {
            // Invalidate the normal since it isn't worth transforming it back
            result.InvalidateNormal();

            ray.ClearMask();
            return true;
          }
        }

        // Clear the mask since we're now done with it
        ray.ClearMask();
      }
      else
      {
        // If the record is invalid, fall back to a current-time ray cast
        return this.shape.RayCast(ref ray, ref result);
      }

      return false;
    }

    internal bool CircleCast(
      int frame,
      ref RayCast ray,
      float radius,
      ref RayResult result)
    {
      ShapeRecord record = this.records[this.FrameToIndex(frame)];

      if (record.Frame == frame)
      {
        // Record is valid, create a local mask
        ray.CreateMask(record.Position, record.Facing);
        ray.DisableMask();

        // Check overlap of the circle origin with the shape in world space
        if (record.QueryAABB(ray.Origin, radius) == true)
        {
          // Perform shape/circle overlap query in local space
          ray.EnableMask();
          if (this.shape.ShapeQuery(ray.Origin, radius, true))
          {
            result.SetContained(this.shape);
            ray.ClearMask();
            return true;
          }
        }

        // Perform aabb circlecast in world space
        ray.DisableMask();
        if (record.CircleCastAABB(ref ray, radius) == true)
        {
          // Perform shape circlecast in local space
          ray.EnableMask();
          if (this.shape.CircleCast(ref ray, radius, ref result) == true)
          {
            // Invalidate the normal since it isn't worth transforming it back
            result.InvalidateNormal();

            ray.ClearMask();
            return true;
          }
        }

        // Clear the mask since we're now done with it
        ray.ClearMask();

        // Invalidate the normal since it isn't worth transforming it back
        result.InvalidateNormal();
      }
      else
      {
        // If the record is invalid, fall back to a current-time ray cast
        return this.shape.CircleCast(ref ray, radius, ref result);
      }

      return false;
    }

    private int FrameToIndex(int frame)
    {
      return frame % this.records.Length;
    }
  }
}
