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
  /// <summary>
  /// The Axis data structure represents a "slab" between the given edge and
  /// a parallel edge drawn at the origin. The "width" value gives the width
  /// of that axis slab, defined as follows: For an edge AB with normal N, 
  /// this width w is given by Dot(A, N). If you take edge AB, and draw an 
  /// edge CD parallel to AB that intersects the origin, the width w is equal
  /// to the minimum distance between edges AB and CD.
  ///
  ///             |
  ///             |     C
  ///             |    /
  ///             |   /           A
  ///             |  /ヽ         /
  ///             | /   ヽ      /
  ///             |/    w ヽ   /
  ///  -----------+---------ヽ/----
  ///            /|          /
  ///           D |         /
  ///             |        /
  ///             |       B
  ///             |
  ///             
  /// </summary>
  internal struct Axis
  {
    internal Vector2 Normal { get { return this.normal; } }
    internal float Width { get { return this.width; } }

    private readonly Vector2 normal;
    private readonly float width;

    internal Axis(Vector2 normal, float width)
    {
      this.normal = normal;
      this.width = width;
    }
  }
}
