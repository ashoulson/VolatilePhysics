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
      if (body.IsStatic == false)
        body.bodyLogger = new BodyLogger(body, capacity);
    }

    public static void Store(this Body body, int frame)
    {
      if (body.bodyLogger != null)
        body.bodyLogger.Store(frame);
    }

    public static bool Query(
      this Body body,
      int frame,
      AABB area)
    {
      BodyLogger logger = body.bodyLogger;
      if ((body.IsStatic == true) || (logger == null))
        return body.Query(area);
      return logger.Query(frame, area);
    }

    public static bool Query(
      this Body body,
      int frame,
      Vector2 point)
    {
      BodyLogger logger = body.bodyLogger;
      if ((body.IsStatic == true) || (logger == null))
        return body.Query(point);
      return logger.Query(frame, point);
    }

    public static bool Query(
      this Body body,
      int frame,
      Vector2 point,
      float radius)
    {
      BodyLogger logger = body.bodyLogger;
      if ((body.IsStatic == true) || (logger == null))
        return body.Query(point, radius);
      return logger.Query(frame, point, radius);
    }

    public static bool RayCast(
      this Body body,
      int frame,
      ref RayCast ray,
      ref RayResult result)
    {
      BodyLogger logger = body.bodyLogger;
      if ((body.IsStatic == true) || (logger == null))
        return body.RayCast(ref ray, ref result);
      return logger.RayCast(frame, ref ray, ref result);
    }

    public static bool CircleCast(
      this Body body,
      int frame,
      ref RayCast ray,
      float radius,
      ref RayResult result)
    {
      BodyLogger logger = body.bodyLogger;
      if ((body.IsStatic == true) || (logger == null))
        return body.CircleCast(ref ray, radius, ref result);
      return logger.CircleCast(frame, ref ray, radius, ref result);
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
      BodyFilter filter = null)
    {
      for (int i = 0; i < world.dynamicBodies.Count; i++)
      {
        Body body = world.dynamicBodies[i];
        if (Body.Filter(body, filter) && body.Query(frame, area))
          yield return body;
      }

      foreach (Body body in world.staticBroad.Query(area, filter))
        yield return body;
    }

    /// <summary>
    /// Returns all bodies containing a point.
    /// </summary>
    public static IEnumerable<Body> QueryBodies(
      this World world,
      int frame,
      Vector2 point,
      BodyFilter filter = null)
    {
      for (int i = 0; i < world.dynamicBodies.Count; i++)
      {
        Body body = world.dynamicBodies[i];
        if (Body.Filter(body, filter) && body.Query(frame, point))
          yield return body;
      }

      foreach (Body body in world.staticBroad.Query(point, filter))
        yield return body;
    }

    /// <summary>
    /// Returns all bodies overlapping with a circle.
    /// </summary>
    public static IEnumerable<Body> QueryBodies(
      this World world,
      int frame,
      Vector2 point,
      float radius,
      BodyFilter filter = null)
    {
      for (int i = 0; i < world.dynamicBodies.Count; i++)
      {
        Body body = world.dynamicBodies[i];
        if (Body.Filter(body, filter) && body.Query(frame, point, radius))
          yield return body;
      }

      foreach (Body body in world.staticBroad.Query(point, radius, filter))
        yield return body;
    }

    /// <summary>
    /// Performs a raycast on all bodies contained in the world in a past
    /// world state. Filters by body or shape. Falls back to present-time
    /// cast for any bodies without an accurate history.
    /// </summary>
    public static bool RayCast(
      this World world,
      int frame,
      ref RayCast ray,
      ref RayResult result,
      BodyFilter filter = null)
    {
      for (int i = 0; i < world.dynamicBodies.Count; i++)
      {
        Body body = world.dynamicBodies[i];
        if (Body.Filter(body, filter) == true)
        {
          body.RayCast(frame, ref ray, ref result);
          if (result.IsContained == true)
            return true;
        }
      }

      world.staticBroad.RayCast(ref ray, ref result, filter);
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
      ref RayCast ray,
      float radius,
      ref RayResult result,
      BodyFilter filter = null)
    {
      for (int i = 0; i < world.dynamicBodies.Count; i++)
      {
        Body body = world.dynamicBodies[i];
        if (Body.Filter(body, filter) == true)
        {
          body.CircleCast(frame, ref ray, radius, ref result);
          if (result.IsContained == true)
            return true;
        }
      }

      world.staticBroad.CircleCast(ref ray, radius, ref result, filter);
      return result.IsValid;
    }
    #endregion

    public static void GizmoDrawHistory(
      this Body body,
      Color aabbColorBody,
      Color aabbColorShape)
    {
      body.bodyLogger.GizmoDraw(aabbColorBody, aabbColorShape);
    }
  }
}