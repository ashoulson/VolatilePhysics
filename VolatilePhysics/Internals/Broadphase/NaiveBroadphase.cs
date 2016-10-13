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

#if UNITY
using UnityEngine;
#endif

namespace Volatile
{
  internal class NaiveBroadphase : IBroadPhase
  {
    private VoltBody[] bodies;
    private int count;

    public NaiveBroadphase()
    {
      this.bodies = new VoltBody[256];
      this.count = 0;
    }

    public void AddBody(VoltBody body)
    {
      if (this.count >= this.bodies.Length)
        VoltUtil.ExpandArray(ref this.bodies);

      this.bodies[this.count] = body;
      body.ProxyId = this.count;
      this.count++;
    }

    public void RemoveBody(VoltBody body)
    {
      int index = body.ProxyId;
      VoltDebug.Assert(index >= 0);
      VoltDebug.Assert(index < this.count);

      int lastIndex = this.count - 1;
      if (index < lastIndex)
      {
        VoltBody lastBody = this.bodies[lastIndex];

        this.bodies[lastIndex].ProxyId = -1;
        this.bodies[lastIndex] = null;

        this.bodies[index] = lastBody;
        lastBody.ProxyId = index;
      }

      this.count--;
    }

    public void UpdateBody(VoltBody body)
    {
      // Do nothing
    }

    public void QueryOverlap(
      VoltAABB aabb,
      VoltBuffer<VoltBody> outBuffer)
    {
      outBuffer.Add(this.bodies, this.count);
    }

    public void QueryPoint(
      Vector2 point,
      VoltBuffer<VoltBody> outBuffer)
    {
      outBuffer.Add(this.bodies, this.count);
    }

    public void QueryCircle(
      Vector2 point,
      float radius,
      VoltBuffer<VoltBody> outBuffer)
    {
      outBuffer.Add(this.bodies, this.count);
    }

    public void RayCast(
      ref VoltRayCast ray,
      VoltBuffer<VoltBody> outBuffer)
    {
      outBuffer.Add(this.bodies, this.count);
    }

    public void CircleCast(
      ref VoltRayCast ray,
      float radius,
      VoltBuffer<VoltBody> outBuffer)
    {
      outBuffer.Add(this.bodies, this.count);
    }
  }
}
