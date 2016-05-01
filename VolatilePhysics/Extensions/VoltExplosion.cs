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

using UnityEngine;

namespace Volatile.Extensions
{
  public delegate void VoltExplosionCallback(
    VoltShape shape, 
    Vector2 normal,
    float increment, 
    float normalizedDistance);

  public static class VoltExplosion
  {
    public static void ResolveExplosion(
      this VoltWorld world, 
      Vector2 origin, 
      float radius, 
      VoltExplosionCallback callback,
      VoltBodyFilter filter = null, 
      int ticksBehind = 0,
      int rayCount = 32)
    {
      VoltAABB worldBounds = new VoltAABB(origin, radius);
      IEnumerable<VoltBody> bodies = 
        world.QueryBounds(worldBounds, filter, ticksBehind);

      VoltRayCast ray;
      VoltRayResult result;
      float increment = 1.0f / rayCount;
      float angleIncrement = (Mathf.PI * 2.0f) * increment;

      for (int i = 0; i < rayCount; i++)
      {
        Vector2 normal = VoltMath.Polar(angleIncrement * i);
        ray = new VoltRayCast(origin, normal, radius);
        result = default(VoltRayResult);

        foreach (VoltBody body in bodies)
        {
          body.RayCast(ref ray, ref result, ticksBehind);
          if (result.IsContained)
            break;
        }

        if (result.IsValid)
          callback.Invoke(
            result.Shape, 
            normal, 
            increment, 
            result.Distance / radius);
      }
    }
  }
}
