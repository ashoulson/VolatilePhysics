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
  internal static class Config
  {
    internal const float AREA_MASS_RATIO = 0.01f;

    internal const float RESOLVE_SLOP = 0.01f;
    internal const float RESOLVE_RATE = 0.1f;

    internal const float DEFAULT_RESTITUTION = 0.5f;
    internal const float DEFAULT_FRICTION = 0.8f;

    /// <summary>
    /// Maximum contacts for collision resolution.
    /// </summary>
    internal const int MAX_CONTACTS = 3;

    /// <summary>
    /// Maximum history records stored by the rolling buffer.
    /// </summary>
    internal const int MAX_HISTORY = 64;

    /// <summary>
    /// Invalid time for initialization purposes.
    /// </summary>
    internal const uint INVALID_TIME = uint.MaxValue;
  }
}
