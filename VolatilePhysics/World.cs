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
  public sealed class World
  {
    public IEnumerable<Shape> Shapes 
    { 
      get { return this.shapes.AsReadOnly(); } 
    }

    public IEnumerable<Body> Bodies 
    {
      get { return this.bodies.AsReadOnly(); } 
    }

    internal float Elasticity { get; private set; }

    private List<Body> bodies;
    private List<Shape> shapes;

    internal Vector2 gravity;
    internal float damping = 0.999f;

    // Each World instance should own its own object pools, in case
    // you want to run multiple World instances simultaneously.
    private Manifold.Pool manifoldPool;
    private Contact.Pool contactPool;
    private List<Manifold> manifolds;

    public World(Vector2 gravity, float damping = 0.999f)
    {
      this.bodies = new List<Body>();
      this.shapes = new List<Shape>();

      this.gravity = gravity;
      this.damping = damping;

      this.contactPool = new Contact.Pool();
      this.manifoldPool = new Manifold.Pool(this.contactPool);
      this.manifolds = new List<Manifold>();
    }

    public void AddBody(Body body)
    {
      foreach (Shape s in body.Shapes)
        this.shapes.Add(s);
      this.bodies.Add(body);
      body.World = this;
    }

    public void RemoveBody(Body body)
    {
      // TODO: Ouch, this is costly.
      foreach (Shape s in body.Shapes)
        this.shapes.Remove(s);
      this.bodies.Remove(body);
      body.World = null;
    }

    public void Update()
    {
      this.UpdateBodies();
      this.UpdateCollision();
      this.CleanupManifolds();
    }

    #region Tests
    /// <summary>
    /// Returns all shapes whose bounding boxes overlap an area.
    /// </summary>
    public IEnumerable<Shape> Query(AABB area)
    {
      foreach (Shape shape in this.shapes)
        if (shape.AABB.Intersect(area))
          yield return shape;
    }

    /// <summary>
    /// Returns all shapes containing a point.
    /// </summary>
    public IEnumerable<Shape> Query(Vector2 point)
    {
      foreach (Shape shape in this.shapes)
        if (shape.Query(point) == true)
          yield return shape;
    }

    /// <summary>
    /// Performs a raycast on all bodies contained in the world.
    /// Filters by body or shape.
    /// </summary>
    public bool Raycast(
      RayCast ray,
      out RayResult result,
      Func<Body, bool> bodyFilter = null,
      Func<Shape, bool> shapeFilter = null)
    {
      result = new RayResult();
      foreach (Body body in this.bodies)
      {
        if (bodyFilter == null || bodyFilter(body) == true)
        {
          body.Raycast(ref ray, ref result, shapeFilter);
          if (result.IsContained == true)
            return true;
        }
      }
      return result.IsValid;
    }
    #endregion

    #region Internals
    private void UpdateBodies()
    {
      foreach (Body body in this.bodies)
        body.Update();
      this.BroadPhase(this.manifolds);
    }


    private void BroadPhase(List<Manifold> manifolds)
    {
      // TODO: Extensible Broadphase
      for (int i = 0; i < this.shapes.Count; i++)
        for (int j = i + 1; j < this.shapes.Count; j++)
          this.NarrowPhase(this.shapes[i], this.shapes[j], manifolds);
    }

    private void NarrowPhase(
      Shape sa,
      Shape sb,
      List<Manifold> manifolds)
    {
      if (sa.Body.CanCollide(sb.Body) == false)
        return;
      if (sa.AABB.Intersect(sb.AABB) == false)
        return;

      Shape.OrderShapes(ref sa, ref sb);
      Manifold manifold = Collision.Dispatch(sa, sb, this.manifoldPool);
      if (manifold != null)
        manifolds.Add(manifold);
    }

    private void CleanupManifolds()
    {
      for (int i = 0; i < this.manifolds.Count; i++)
      {
        this.manifolds[i].ReleaseContacts();
        this.manifoldPool.Release(this.manifolds[i]);
      }
      this.manifolds.Clear();
    }

    private void UpdateCollision()
    {
      for (int i = 0; i < this.manifolds.Count; i++)
        this.manifolds[i].Prestep();

      this.Elasticity = 1.0f;
      for (int j = 0; j < Config.NUM_ITERATIONS * 1 / 3; j++)
        for (int i = 0; i < this.manifolds.Count; i++)
          this.manifolds[i].Solve();

      for (int i = 0; i < this.manifolds.Count; i++)
        this.manifolds[i].SolveCached();

      this.Elasticity = 0.0f;
      for (int j = 0; j < Config.NUM_ITERATIONS * 2 / 3; j++)
        for (int i = 0; i < this.manifolds.Count; i++)
          this.manifolds[i].Solve();
    }
    #endregion
  }
}
