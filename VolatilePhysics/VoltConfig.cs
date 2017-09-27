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
  public static class VoltConfig
  {
    public static float ResolveSlop = 0.01f;
    public static float ResolveRate = 0.1f;
    public static float AreaMassRatio = 0.01f;

    // Defaults
    public const float DEFAULT_DENSITY = 1.0f;
    public const float DEFAULT_RESTITUTION = 0.5f;
    public const float DEFAULT_FRICTION = 0.8f;

    internal const float DEFAULT_DELTA_TIME = 0.02f;
    internal const float DEFAULT_DAMPING = 0.999f;
    internal const int DEFAULT_ITERATION_COUNT = 20;

    // AABB extension for the dynamic tree
    internal const float AABB_EXTENSION = 0.2f;

    // Maximum contacts for collision resolution.
    internal const int MAX_CONTACTS = 3;

    // Used for initializing timesteps
    internal const int INVALID_TIME = -1;

    // The minimum mass a dynamic object can have before it is
    // converted to a static object
    internal const float MINIMUM_DYNAMIC_MASS = 0.00001f;
  }
}
