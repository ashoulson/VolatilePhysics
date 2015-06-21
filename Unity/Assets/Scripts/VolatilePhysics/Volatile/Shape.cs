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
    public abstract Vector2 Position { get; }
    public abstract Vector2 Facing { get; }

    /// <summary>
    /// User token, for attaching arbitrary data to this shape.
    /// </summary>
    public object Token { get; set; }

    public Body Body { get; internal set; }
    public AABB AABB { get; protected set; }

    public float Area { get; protected set; }
    public float Density { get; private set; }

    // TODO: Clean these values up and make them accessible
    internal float friction = Config.DEFAULT_FRICTION;
    internal float restitution = Config.DEFAULT_RESTITUTION;

    // TODO: Remove static here
    protected static uint nextId = 0;
    internal uint id;

    public abstract bool ContainsPoint(Vector2 v);

    protected Shape(float density)
    {
      this.id = nextId++;
      this.Density = density;
    }

    public void SetWorld(Vector2 position, float radians)
    {
      this.SetWorld(position, Util.Polar(radians));
    }

    public abstract void SetWorld(Vector2 position, Vector2 facing);

    internal float ComputeMass()
    {
      return this.Area * this.Density * Config.AREA_MASS_RATIO;
    }

    internal abstract float ComputeInertia(Vector2 offset);
  }
}