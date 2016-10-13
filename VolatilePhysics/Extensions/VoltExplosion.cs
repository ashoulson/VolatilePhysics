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

#if UNITY
using UnityEngine;
#endif

namespace Volatile
{
  public delegate void VoltExplosionCallback(
    VoltRayCast rayCast,
    VoltRayResult rayResult,
    float rayWeight);

  public partial class VoltWorld
  {
    // We'll increase the minimum occluder range by this amount when testing.
    // This way, if an occluder is also a target, we will catch that target
    // within the occluder range. Also allows us to handle the case where the
    // explosion origin is within both targets' and occluders' shapes.
    private const float EXPLOSION_OCCLUDER_SLOP = 0.05f;

    private VoltBuffer<VoltBody> targetBodies;
    private VoltBuffer<VoltBody> occludingBodies;

    public void PerformExplosion(
      Vector2 origin,
      float radius,
      VoltExplosionCallback callback,
      VoltBodyFilter targetFilter = null,
      VoltBodyFilter occlusionFilter = null,
      int ticksBehind = 0,
      int rayCount = 32)
    {
      if (ticksBehind < 0)
        throw new ArgumentOutOfRangeException("ticksBehind");

      // Get all target bodies
      this.PopulateFiltered(
        origin, 
        radius, 
        targetFilter, 
        ticksBehind, 
        ref this.targetBodies);

      // Get all occluding bodies
      this.PopulateFiltered(
        origin,
        radius,
        occlusionFilter,
        ticksBehind,
        ref this.occludingBodies);

      VoltRayCast ray;
      float rayWeight = 1.0f / rayCount;
      float angleIncrement = (Mathf.PI * 2.0f) * rayWeight;

      for (int i = 0; i < rayCount; i++)
      {
        Vector2 normal = VoltMath.Polar(angleIncrement * i);
        ray = new VoltRayCast(origin, normal, radius);

        float minDistance = 
          this.GetOccludingDistance(ray, ticksBehind);
        minDistance += VoltWorld.EXPLOSION_OCCLUDER_SLOP;

        this.TestTargets(ray, callback, ticksBehind, minDistance, rayWeight);
      }
    }

    /// <summary>
    /// Gets the distance to the closest occluder for the given ray.
    /// </summary>
    private float GetOccludingDistance(
      VoltRayCast ray,
      int ticksBehind)
    {
      float distance = float.MaxValue;
      VoltRayResult result = default(VoltRayResult);

      for (int i = 0; i < this.occludingBodies.Count; i++)
      {
        if (this.occludingBodies[i].RayCast(ref ray, ref result, ticksBehind))
          distance = result.Distance;
        if (result.IsContained)
          break;
      }

      return distance;
    }

    /// <summary>
    /// Tests all valid explosion targets for a given ray.
    /// </summary>
    private void TestTargets(
      VoltRayCast ray,
      VoltExplosionCallback callback,
      int ticksBehind,
      float minOccluderDistance,
      float rayWeight)
    {
      for (int i = 0; i < this.targetBodies.Count; i++)
      {
        VoltBody targetBody = this.targetBodies[i];
        VoltRayResult result = default(VoltRayResult);

        if (targetBody.RayCast(ref ray, ref result, ticksBehind))
          if (result.Distance < minOccluderDistance)
            callback.Invoke(ray, result, rayWeight);
      }
    }

    /// <summary>
    /// Finds all dynamic bodies that overlap with the explosion AABB
    /// and pass the target filter test. Does not test actual shapes.
    /// </summary>
    private void PopulateFiltered(
      Vector2 origin,
      float radius,
      VoltBodyFilter targetFilter,
      int ticksBehind,
      ref VoltBuffer<VoltBody> filterBuffer)
    {
      if (filterBuffer == null)
        filterBuffer = new VoltBuffer<VoltBody>();
      filterBuffer.Clear();

      this.reusableBuffer.Clear();
      this.staticBroadphase.QueryCircle(origin, radius, this.reusableBuffer);
      this.dynamicBroadphase.QueryCircle(origin, radius, this.reusableBuffer);

      VoltAABB aabb = new VoltAABB(origin, radius);
      for (int i = 0; i < this.reusableBuffer.Count; i++)
      {
        VoltBody body = this.reusableBuffer[i];
        if ((targetFilter == null) || targetFilter.Invoke(body))
          if (body.QueryAABBOnly(aabb, ticksBehind))
            filterBuffer.Add(body);
      }
    }
  }
}
