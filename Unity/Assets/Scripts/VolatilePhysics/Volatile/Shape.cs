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
  public abstract class Shape
  {
    #region Static Methods
    internal static void OrderShapes(ref Shape sa, ref Shape sb)
    {
      if (sa.Type > sb.Type)
      {
        Shape temp = sa;
        sa = sb;
        sb = temp;
      }
    }
    #endregion

    public enum ShapeType
    {
      Circle,
      Polygon,
    }

    public abstract ShapeType Type { get; }
    public abstract float Area { get; }
    public abstract float Inertia { get; }

    public Body body;
    public AABB aabb;
    public float mass;

    public float friction = Config.DEFAULT_FRICTION;
    public float restitution = Config.DEFAULT_RESTITUTION;

    internal uint id;
    internal static uint next = 0;

    public Shape()
    {
      this.id = next++;
    }

    public void ComputeMass(float density)
    {
      this.mass = density * this.Area * Config.AREA_MASS_RATIO;
    }

    public abstract bool ContainsPoint(Vector2 v);
    internal abstract void UpdateCache();
    protected abstract void CacheWorldData();
  }
}