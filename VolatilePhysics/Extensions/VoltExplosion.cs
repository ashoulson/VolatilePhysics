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

using CommonUtil;

namespace Volatile
{
  public delegate void VoltExplosionCallback(
    VoltShape shape, 
    Vector2 normal,
    float increment, 
    float normalizedDistance);

  public partial class VoltWorld
  {
    private static void InitializeStack(ref Stack<VoltBody> stack)
    {
      if (stack == null)
        stack = new Stack<VoltBody>(256);
      stack.Clear();
    }

    private Stack<VoltBody> occludingBodies;
    private Stack<VoltBody> targetBodies;

    /// <summary>
    /// Resolves an explosion with a series of radial raycasts.
    /// Fires a callback for each ray that hits on each target.
    /// 
    /// By default (useOcclusion = true), these rays are blocked by
    /// static geometry.
    /// </summary>
    public IEnumerable<VoltBody> ResolveExplosion(
      Vector2 origin,
      float radius,
      VoltExplosionCallback callback,
      VoltBodyFilter targetFilter = null,
      int ticksBehind = 0,
      bool useOcclusion = true,
      int rayCount = 32)
    {
      VoltAABB worldBounds = new VoltAABB(origin, radius);
      this.PopulateExplosionBodies(
        ref worldBounds,
        targetFilter,
        ticksBehind,
        useOcclusion);

      VoltRayCast ray;
      VoltRayResult result;
      float increment = 1.0f / rayCount;
      float angleIncrement = (Mathf.PI * 2.0f) * increment;

      for (int i = 0; i < rayCount; i++)
      {
        Vector2 normal = VoltMath.Polar(angleIncrement * i);
        ray = new VoltRayCast(origin, normal, radius);
        result = default(VoltRayResult);

        // Prime the ray on the blockers
        foreach (VoltBody body in this.occludingBodies)
        {
          body.RayCast(ref ray, ref result, ticksBehind);
          if (result.IsContained)
            break;
        }

        // Check against the target bodies, starting from the blocker result
        VoltRayResult primed;
        foreach (VoltBody body in this.targetBodies)
        {
          primed = result;
          if (body.RayCast(ref ray, ref primed, ticksBehind))
          {
            // Did we hit the body or were we blocked?
            if (primed.Shape.Body == body)
            {
              callback.Invoke(
                primed.Shape,
                normal,
                increment,
                primed.Distance / radius);
            }
          }
        }
      }

      return targetBodies;
    }

    private void PopulateExplosionBodies(
      ref VoltAABB worldBounds,
      VoltBodyFilter targetFilter,
      int ticksBehind,
      bool useOcclusion)
    {
      if (ticksBehind < 0)
        throw new ArgumentOutOfRangeException("ticksBehind");

      VoltWorld.InitializeStack(ref this.occludingBodies);
      VoltWorld.InitializeStack(ref this.targetBodies);

      if (useOcclusion)
        this.CollectOccluders(ref worldBounds);
      this.CollectTargets(ref worldBounds, targetFilter, ticksBehind);
    }

    private void CollectOccluders(ref VoltAABB worldBounds)
    {
      this.reusableBuffer.Clear();
      this.staticBroadphase.QueryOverlap(worldBounds, this.reusableBuffer);
      for (int i = 0; i < this.reusableBuffer.Count; i++)
        this.occludingBodies.Push(this.reusableBuffer[i]);
    }

    private void CollectTargets(
      ref VoltAABB worldBounds,
      VoltBodyFilter targetFilter,
      int ticksBehind)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        VoltBody body = this.bodies[i];
        if (VoltBody.Filter(body, targetFilter))
          if (body.QueryOverlap(worldBounds, ticksBehind))
              this.targetBodies.Push(body);
      }
    }
  }
}
