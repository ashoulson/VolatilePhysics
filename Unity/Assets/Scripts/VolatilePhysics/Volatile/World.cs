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
  public class World
  {
    public List<Body> bodies;
    public List<Shape> shapes;

    internal static float elasticity;
    internal Vector2 gravity;
    internal float damping = 0.999f;

    public World(Vector2 gravity, float damping = 0.999f)
    {
      this.gravity = gravity;
      this.damping = damping;
      this.bodies = new List<Body>();
      this.shapes = new List<Shape>();
    }

    public void AddBody(Body body)
    {
      foreach (Shape s in body.Shapes)
        this.shapes.Add(s);
      this.bodies.Add(body);
      body.world = this;
    }

    protected virtual void BroadPhase(List<Manifold> manifolds)
    {
      for (int i = 0; i < this.shapes.Count; i++)
      {
        for (int j = i + 1; j < this.shapes.Count; j++)
        {
          Shape a = this.shapes[i];
          Shape b = this.shapes[j];
          this.NarrowPhase(a, b, manifolds);
        }
      }
    }

    protected void NarrowPhase(Shape sa, Shape sb, List<Manifold> manifolds)
    {
      if (sa.Body.CanCollide(sb.Body) == false)
        return;

      Shape.OrderShapes(ref sa, ref sb);
      Manifold manifold = new Manifold(3);

      if (Collision.Dispatch(sa, sb, ref manifold))
      {
        if (manifold.shapeA == null)
        {
          manifold.shapeA = sa;
          manifold.shapeB = sb;
        }

        manifolds.Add(manifold);
      }
    }

    public virtual IEnumerable<Shape> Query(AABB area)
    {
      foreach (Shape shape in this.shapes)
        if (shape.AABB.Intersect(area))
          yield return shape;
    }

    public void RunPhysics(float dt, int iterations)
    {
      foreach (Body body in this.bodies)
        body.Update(dt);

      List<Manifold> manifolds = new List<Manifold>();
      this.BroadPhase(manifolds);

      foreach (Manifold arb in manifolds)
        arb.Prestep();

      elasticity = 1.0f;
      for (int i = 0; i < iterations * 1 / 3; i++)
        foreach (Manifold arb in manifolds)
          arb.Perform();

      foreach (Manifold arb in manifolds)
        arb.PerformCached();

      elasticity = 0.0f;
      for (int i = 0; i < iterations * 1 / 3; i++)
        foreach (Manifold arb in manifolds)
          arb.Perform();
    }
  }
}
