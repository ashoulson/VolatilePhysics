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
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace Volatile
{
  class NaiveBroadphase : IBroadPhase
  {
    private List<Shape> shapes;

    public NaiveBroadphase()
    {
      this.shapes = new List<Shape>();
    }

    public void Add(Body body)
    {
      foreach (Shape shape in body.shapes)
        this.shapes.Add(shape);
    }

    public void Collision(
      Body body, 
      Action<Shape, Shape> narrowPhase)
    {
      foreach (Shape staticShape in this.shapes)
        if (staticShape.AABB.Intersect(body.AABB))
          foreach (Shape dynamicShape in body.shapes)
            if (staticShape.Query(dynamicShape.AABB))
              narrowPhase.Invoke(staticShape, dynamicShape);
    }

    public IEnumerable<Body> Query(
      AABB area, 
      BodyFilter filter)
    {
      HashSet<Body> foundBodies = new HashSet<Body>();
      foreach (Shape staticShape in this.shapes)
      {
        if (Body.Filter(staticShape.Body, filter) == false)
          continue;

        if (foundBodies.Contains(staticShape.Body) == false)
          if (staticShape.Query(area) == true)
            foundBodies.Add(staticShape.Body);
      }
      return foundBodies;
    }

    public IEnumerable<Body> Query(
      Vector2 point, 
      BodyFilter filter)
    {
      HashSet<Body> foundBodies = new HashSet<Body>();
      foreach (Shape staticShape in this.shapes)
      {
        if (Body.Filter(staticShape.Body, filter) == false)
          continue;

        if (foundBodies.Contains(staticShape.Body) == false)
          if (staticShape.Query(point) == true)
            foundBodies.Add(staticShape.Body);
      }
      return foundBodies;
    }

    public IEnumerable<Body> Query(
      Vector2 point, 
      float radius, 
      BodyFilter filter = null)
    {
      HashSet<Body> foundBodies = new HashSet<Body>();
      foreach (Shape staticShape in this.shapes)
      {
        if (Body.Filter(staticShape.Body, filter) == false)
          continue;

        if (foundBodies.Contains(staticShape.Body) == false)
          if (staticShape.Query(point, radius) == true)
            foundBodies.Add(staticShape.Body);
      }
      return foundBodies;
    }

    public bool RayCast(
      ref RayCast ray, 
      ref RayResult result, 
      BodyFilter filter = null)
    {
      foreach (Shape staticShape in this.shapes)
      {
        if (Body.Filter(staticShape.Body, filter) == false)
          continue;
        staticShape.RayCast(ref ray, ref result);
        if (result.IsContained == true)
          return true;
      }

      return result.IsValid;
    }

    public bool CircleCast(
      ref RayCast ray, 
      float radius, 
      ref RayResult result, 
      BodyFilter filter = null)
    {
      foreach (Shape staticShape in this.shapes)
      {
        if (Body.Filter(staticShape.Body, filter) == false)
          continue;
        staticShape.CircleCast(ref ray, radius, ref result);
        if (result.IsContained == true)
          return true;
      }

      return result.IsValid;
    }
  }
}