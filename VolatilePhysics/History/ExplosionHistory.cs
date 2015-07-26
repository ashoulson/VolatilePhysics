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

using Volatile;

namespace Volatile.History
{
  public static class ExplosionHistory
  {
    /// <summary>
    /// N.B.: These callbacks will fire while the body is still in its
    /// rolled-back state. You can retrieve what frame the body is in using
    /// body.GetCurrentFrame().
    /// </summary>
    public static void Perform(
      this Explosion explosion,
      int frame,
      int rayBudget, 
      int minRays,
      Explosion.HitCallback callback)
    {
      List<Body> closeBodies =
        new List<Body>(
          explosion.world.QueryDynamic(
            frame,
            explosion.aabb, 
            explosion.filter));

      int count = closeBodies.Count;
      if (count == 0)
        return;

      // Since we're doing so many raycasts at once, it makes more sense to 
      // just move the body rather than do all the space transformations
      for (int i = 0; i < count; i++)
        closeBodies[i].Rollback(frame);

      for (int i = 0; i < count; i++)
        explosion.DoPerformOnBody(
          closeBodies[i],
          explosion.ComputeBudget(rayBudget, minRays, count), 
          callback);

      // Restore all the bodies we rolled back
      for (int i = 0; i < count; i++)
        closeBodies[i].Restore();
    }

    /// <summary>
    /// N.B.: These callbacks will fire while the body is still in its
    /// rolled-back state. You can retrieve what frame the body is in using
    /// body.GetCurrentFrame().
    /// </summary>
    public static void Perform(
      this Explosion explosion,
      int frame,
      Body body,
      int numRays,
      Explosion.HitCallback callback)
    {
      if (body.Query(frame, explosion.aabb) == true)
      {
        // Since we're doing so many raycasts at once, it makes more sense to 
        // just move the body rather than do all the space transformations
        body.Rollback(frame);
        explosion.DoPerformOnBody(body, numRays, callback);
        body.Restore();
      }
    }
  }
}