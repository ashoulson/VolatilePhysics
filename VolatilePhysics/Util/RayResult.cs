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
  public struct RayResult
  {
    public bool IsValid { get { return this.shape != null; } }
    public bool IsContained 
    { 
      get { return this.IsValid && this.distance == 0.0f; } 
    }

    public Shape Shape { get { return this.shape; } }
    public float Distance { get { return this.distance; } }
    public Vector2 Normal { get { return this.normal; } }
    public Vector2 Point { get { return this.point; } }

    private Shape shape;
    private float distance;
    private Vector2 normal;
    private Vector2 point;

    internal void Set(
      Shape shape, 
      float distance, 
      Vector2 normal,
      Vector2 point)
    {
      if (this.IsValid == false || distance < this.distance)
      {
        this.shape = shape;
        this.distance = distance;
        this.normal = normal;
        this.point = point;
      }
    }

    internal void SetContained(Shape shape)
    {
      this.shape = shape;
      this.distance = 0.0f;
      this.normal = Vector2.zero;
    }
  }
}