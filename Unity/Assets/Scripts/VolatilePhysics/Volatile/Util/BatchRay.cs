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

namespace Volatile
{
  /// <summary>
  /// A semi-precomputed ray optimized for many tests
  /// </summary>
  public struct BatchRay
  {
    public Vector2 origin;
    public Vector2 direction;
    public Vector2 invDirection;
    public bool signX;
    public bool signY;

    public BatchRay(Vector2 origin, Vector2 direction)
    {
      this.origin = origin;
      this.direction = direction;
      this.invDirection =
        new Vector2(
          1.0f / direction.x,
          1.0f / direction.y);
      this.signX = invDirection.x < 0.0f;
      this.signY = invDirection.y < 0.0f;
    }
  }
}