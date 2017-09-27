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

#if UNITY
using UnityEngine;
#endif

namespace Volatile
{
  internal interface IBroadPhase
  {
    void AddBody(VoltBody body);
    void RemoveBody(VoltBody body);
    void UpdateBody(VoltBody body);

    // Note that these should return bodies that meet the criteria within the
    // spaces defined by the structure itself. These tests should not test the
    // actual body's bounding box, as that will happen in the beginning of the
    // narrowphase test.
    void QueryOverlap(VoltAABB aabb, VoltBuffer<VoltBody> outBuffer);
    void QueryPoint(Vector2 point, VoltBuffer<VoltBody> outBuffer);
    void QueryCircle(Vector2 point, float radius, VoltBuffer<VoltBody> outBuffer);
    void RayCast(ref VoltRayCast ray, VoltBuffer<VoltBody> outBuffer);
    void CircleCast(ref VoltRayCast ray, float radius, VoltBuffer<VoltBody> outBuffer);
  }
}