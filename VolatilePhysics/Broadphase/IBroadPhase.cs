﻿/*
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
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace Volatile
{
  public interface IBroadPhase
  {
    void Add(Body body);

    void Collision(Body body, Action<Shape, Shape> narrowPhase);

    IEnumerable<Body> Query(
      AABB area,
      BodyFilter filter);

    IEnumerable<Body> Query(
      Vector2 point,
      BodyFilter filter);

    IEnumerable<Body> Query(
      Vector2 point,
      float radius,
      BodyFilter filter = null);

    bool RayCast(
      ref RayCast ray,
      ref RayResult result,
      BodyFilter filter = null);

    bool CircleCast(
      ref RayCast ray,
      float radius,
      ref RayResult result,
      BodyFilter filter = null);
  }
}