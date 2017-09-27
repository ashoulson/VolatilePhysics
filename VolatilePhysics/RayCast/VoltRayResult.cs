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
  public struct VoltRayResult
  {
    public bool IsValid { get { return this.shape != null; } }
    public bool IsContained
    {
      get { return this.IsValid && this.distance == 0.0f; }
    }

    public VoltShape Shape { get { return this.shape; } }

    public VoltBody Body 
    { 
      get { return (this.shape == null) ? null : this.shape.Body; } 
    }

    public float Distance { get { return this.distance; } }
    public Vector2 Normal { get { return this.normal; } }

    private VoltShape shape;
    private float distance;
    internal Vector2 normal;

    public Vector2 ComputePoint(ref VoltRayCast cast)
    {
      return cast.origin + (cast.direction * this.distance);
    }

    internal void Set(
      VoltShape shape,
      float distance,
      Vector2 normal)
    {
      if (this.IsValid == false || distance < this.distance)
      {
        this.shape = shape;
        this.distance = distance;
        this.normal = normal;
      }
    }

    internal void Reset()
    {
      this.shape = null;
    }

    internal void SetContained(VoltShape shape)
    {
      this.shape = shape;
      this.distance = 0.0f;
      this.normal = Vector2.zero;
    }
  }
}