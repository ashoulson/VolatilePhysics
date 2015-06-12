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
  public struct AABB
  {
    public float Top { get { return this.top; } }
    public float Bottom { get { return this.bottom; } }
    public float Left { get { return this.left; } }
    public float Right { get { return this.right; } }

    public float Width { get { return this.Right - this.Left; } }
    public float Height { get { return this.Bottom - this.Top; } }

    private readonly float top;
    private readonly float bottom;
    private readonly float left;
    private readonly float right;

    public AABB(float top, float bottom, float left, float right)
    {
      this.top = top;
      this.bottom = bottom;
      this.left = left;
      this.right = right;
    }

    public AABB(Vector2 center, Vector2 extents)
    {
      Vector2 topLeft = center - extents;
      Vector2 bottomRight = center + extents;

      this.top = topLeft.y;
      this.bottom = bottomRight.y;
      this.left = topLeft.x;
      this.right = bottomRight.x;
    }

    public bool Intersect(AABB aabb)
    {
      return
        aabb.Left < this.Right &&
        this.Left < aabb.Right &&
        aabb.Top < this.Bottom &&
        this.Top < aabb.Bottom;
    }
  }
}
