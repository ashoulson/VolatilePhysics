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

namespace Volatile
{
  /// <summary>
  /// A stored historical image of a past body state, used for historical
  /// queries and raycasts. Rather than actually rolling the body back to
  /// its old position (expensive), we transform the ray into the body's
  /// local space based on the body's old position/axis. Then all casts
  /// on shapes use the local-space ray (this applies both for current-
  /// time and past-time raycasts and point queries).
  /// </summary>
  internal struct HistoryRecord
  {
    internal VoltAABB aabb;
    internal VoltVec2 position;
    internal VoltVec2 facing;

    internal void Store(ref HistoryRecord other)
    {
      this.aabb = other.aabb;
      this.position = other.position;
      this.facing = other.facing;
    }

    #region World-Space to Body-Space Transformations
    internal VoltVec2 WorldToBodyPoint(VoltVec2 vector)
    {
      return VoltMath.WorldToBodyPoint(this.position, this.facing, vector);
    }

    internal VoltVec2 WorldToBodyDirection(VoltVec2 vector)
    {
      return VoltMath.WorldToBodyDirection(this.facing, vector);
    }

    internal VoltRayCast WorldToBodyRay(ref VoltRayCast rayCast)
    {
      return new VoltRayCast(
        this.WorldToBodyPoint(rayCast.origin),
        this.WorldToBodyDirection(rayCast.direction),
        rayCast.distance);
    }
    #endregion

    #region Body-Space to World-Space Transformations
    internal VoltVec2 BodyToWorldPoint(VoltVec2 vector)
    {
      return VoltMath.BodyToWorldPoint(this.position, this.facing, vector);
    }

    internal VoltVec2 BodyToWorldDirection(VoltVec2 vector)
    {
      return VoltMath.BodyToWorldDirection(this.facing, vector);
    }

    internal Axis BodyToWorldAxis(Axis axis)
    {
      VoltVec2 normal = axis.Normal.Rotate(this.facing);
      float width = VoltVec2.Dot(normal, this.position) + axis.Width;
      return new Axis(normal, width);
    }
    #endregion
  }
}
