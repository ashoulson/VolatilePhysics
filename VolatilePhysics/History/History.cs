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
  public static class History
  {
    #region Body Extensions
    public static void BeginLogging(this Body body, int capacity)
    {
      body.logger = new BodyLogger(body, capacity);
    }

    public static void Store(this Body body, int frame)
    {
      if (body.logger != null)
        body.logger.Store(frame);
    }

    public static bool MinDistance(
      this Body body,
      int frame, 
      Vector2 point,
      float maxDistance,
      out float minDistance,
      Func<Shape, bool> filter = null)
    {
      BodyLogger logger = body.logger;
      if (logger == null) // Fallback to present time if not a logging body
        return body.MinDistance(point, maxDistance, out minDistance, filter);
      return 
        logger.MinDistance(frame, point, maxDistance, out minDistance, filter);
    }

    public static bool Query(
      this Body body,
      int frame,
      AABB area)
    {
      BodyLogger logger = body.logger;
      if (logger == null) // Fallback to present time if not a logging body
        return body.Query(area);
      return logger.Query(frame, area);
    }

    public static bool Query(
      this Body body,
      int frame, 
      Vector2 point,
      Func<Shape, bool> filter = null)
    {
      BodyLogger logger = body.logger;
      if (logger == null) // Fallback to present time if not a logging body
        return body.Query(point, filter);
      return logger.Query(frame, point, filter);
    }

    public static bool Query(
      this Body body,
      int frame, 
      Vector2 point,
      float radius,
      Func<Shape, bool> filter = null)
    {
      BodyLogger logger = body.logger;
      if (logger == null) // Fallback to present time if not a logging body
        return body.Query(point, radius, filter);
      return logger.Query(frame, point, radius, filter);
    }

    public static bool RayCast(
      this Body body, 
      int frame, 
      ref RayCast ray, 
      ref RayResult result,
      Func<Shape, bool> filter = null)
    {
      BodyLogger logger = body.logger;
      if (logger == null) // Fallback to present time if not a logging body
        return body.RayCast(ref ray, ref result, filter);
      return logger.RayCast(frame, ref ray, ref result, filter);
    }

    public static bool CircleCast(
      this Body body,
      int frame,
      ref RayCast ray,
      float radius,
      ref RayResult result,
      Func<Shape, bool> filter = null)
    {
      BodyLogger logger = body.logger;
      if (logger == null) // Fallback to present time if not a logging body
        return body.CircleCast(ref ray, radius, ref result, filter);
      return logger.CircleCast(frame, ref ray, radius, ref result, filter);
    }
    #endregion

    #region World Extensions
    /// <summary>
    /// Returns all bodies whose bounding boxes overlap an area.
    /// </summary>
    public static IEnumerable<Body> QueryBodies(
      this World world,
      int frame,
      AABB area,
      Func<Body, bool> filter = null)
    {
      for (int i = 0; i < world.bodies.Count; i++)
      {
        Body body = world.bodies[i];
        if (filter == null || filter(body) == true)
        {
          if (body.Query(frame, area))
          {
            yield return body;
          }
        }
      }
    }

    /// <summary>
    /// Returns all bodies containing a point.
    /// </summary>
    public static IEnumerable<Body> QueryBodies(
      this World world,
      int frame,
      Vector2 point,
      Func<Body, bool> bodyFilter = null,
      Func<Shape, bool> shapeFilter = null)
    {
      for (int i = 0; i < world.bodies.Count; i++)
      {
        Body body = world.bodies[i];
        if (bodyFilter == null || bodyFilter(body) == true)
        {
          if (body.Query(frame, point, shapeFilter) == true)
          {
            yield return body;
          }
        }
      }
    }

    /// <summary>
    /// Returns all bodies overlapping with a circle.
    /// </summary>
    public static IEnumerable<Body> QueryBodies(
      this World world,
      int frame,
      Vector2 point,
      float radius,
      Func<Body, bool> bodyFilter = null,
      Func<Shape, bool> shapeFilter = null)
    {
      for (int i = 0; i < world.bodies.Count; i++)
      {
        Body body = world.bodies[i];
        if (bodyFilter == null || bodyFilter(body) == true)
        {
          if (body.Query(frame, point, radius, shapeFilter) == true)
          {
            yield return body;
          }
        }
      }
    }

    /// <summary>
    /// Returns all bodies overlapping with a circle, with distance.
    /// More expensive than a simple circle overlap query.
    /// </summary>
    public static IEnumerable<KeyValuePair<Body, float>> MinDistanceBodies(
      this World world,
      int frame,
      Vector2 point,
      float radius,
      Func<Body, bool> bodyFilter = null,
      Func<Shape, bool> shapeFilter = null)
    {
      float dist;
      for (int i = 0; i < world.bodies.Count; i++)
      {
        Body body = world.bodies[i];
        if (bodyFilter == null || bodyFilter(body) == true)
        {
          if (body.MinDistance(frame, point, radius, out dist) == true)
          {
            yield return new KeyValuePair<Body, float>(body, dist);
          }
        }
      }
    }

    /// <summary>
    /// Performs a raycast on all bodies contained in the world in a past
    /// world state. Filters by body or shape. Falls back to present-time
    /// cast for any bodies without an accurate history.
    /// </summary>
    public static bool RayCast(
      this World world,
      int frame,
      RayCast ray,
      out RayResult result,
      Func<Body, bool> bodyFilter = null,
      Func<Shape, bool> shapeFilter = null)
    {
      result = new RayResult();
      for (int i = 0; i < world.bodies.Count; i++)
      {
        Body body = world.bodies[i];
        if (bodyFilter == null || bodyFilter(body) == true)
        {
          body.RayCast(frame, ref ray, ref result, shapeFilter);
          if (result.IsContained == true)
            return true;
        }
      }
      return result.IsValid;
    }

    /// <summary>
    /// Performs a swept circle cast on all bodies contained in the world 
    /// in a past world state. Filters by body or shape. Falls back to 
    /// present-time cast for any bodies without an accurate history.
    /// </summary>
    public static bool CircleCast(
      this World world,
      int frame,
      RayCast ray,
      float radius,
      out RayResult result,
      Func<Body, bool> bodyFilter = null,
      Func<Shape, bool> shapeFilter = null)
    {
      result = new RayResult();
      for (int i = 0; i < world.bodies.Count; i++)
      {
        Body body = world.bodies[i];
        if (bodyFilter == null || bodyFilter(body) == true)
        {
          body.CircleCast(frame, ref ray, radius, ref result, shapeFilter);
          if (result.IsContained == true)
            return true;
        }
      }
      return result.IsValid;
    }
    #endregion

    public static void GizmoDrawHistory(
      this Body body,
      Color aabbColorBody,
      Color aabbColorShape)
    {
      body.logger.GizmoDraw(aabbColorBody, aabbColorShape);
    }
  }
}
