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

    /// <summary>
    /// Fixed update delta time for body integration. 
    /// Defaults to Time.fixedDeltaTime.
    /// </summary>
    public float DeltaTime { get; set; }

    /// <summary>
    /// Number of iterations when updating the world.
    /// Defaults to Config.DEFAULT_ITERATION_COUNT.
    /// </summary>
    public int IterationCount { get; set; }

    internal float Elasticity { get; private set; }

    internal List<Body> bodies;
    internal List<Shape> shapes;

    internal Vector2 gravity;
    internal float damping = 0.999f;

    // Each World instance should own its own object pools, in case
    // you want to run multiple World instances simultaneously.
    private Manifold.Pool manifoldPool;
    private Contact.Pool contactPool;
    private List<Manifold> manifolds;

    public World(Vector2 gravity, float damping = 0.999f)
    {
      this.DeltaTime = Time.fixedDeltaTime;
      this.IterationCount = Config.DEFAULT_ITERATION_COUNT;

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

    /// <summary>
    /// Ticks the world, updating all bodies and resolving collisions.
    /// </summary>
    /// <param name="allowDynamic">Allow dynamic-dynamic collisions.</param>
    public void Update(bool allowDynamic = true)
    {
      foreach (Body body in this.bodies)
        body.Update();

      this.BroadPhase(allowDynamic);
      this.UpdateCollision();
      this.CleanupManifolds();
    }

    /// <summary>
    /// Updates a single body. Does not allow dynamic-dynamic collisions.
    /// </summary>
    public void Update(Body body)
    {
      body.Update();

      this.BroadPhase(body, false);
      this.UpdateCollision();
      this.CleanupManifolds();
    }

    #region Tests

    #region Shape Queries
    /// <summary>
    /// Returns all shapes whose bounding boxes overlap an area.
    /// </summary>
    public IEnumerable<Shape> QueryShapes(
      AABB area,
      Func<Shape, bool> filter = null)
    {
      for (int i = 0; i < this.shapes.Count; i++)
      {
        Shape shape = this.shapes[i];
        if (filter == null || filter(shape) == true)
        {
          if (shape.AABB.Intersect(area))
          {
            yield return shape;
          }
        }
      }
    }

    /// <summary>
    /// Returns all shapes containing a point.
    /// </summary>
    public IEnumerable<Shape> QueryShapes(
      Vector2 point,
      Func<Shape, bool> filter = null)
    {
      for (int i = 0; i < this.shapes.Count; i++)
      {
        Shape shape = this.shapes[i];
        if (filter == null || filter(shape) == true)
        {
          if (shape.Query(point) == true)
          {
            yield return shape;
          }
        }
      }
    }

    /// <summary>
    /// Returns all shapes overlapping with a circle.
    /// </summary>
    public IEnumerable<Shape> QueryShapes(
      Vector2 point,
      float radius,
      Func<Shape, bool> filter = null)
    {
      for (int i = 0; i < this.shapes.Count; i++)
      {
        Shape shape = this.shapes[i];
        if (filter == null || filter(shape) == true)
        {
          if (shape.Query(point, radius) == true)
          {
            yield return shape;
          }
        }
      }
    }

    /// <summary>
    /// Returns all shapes overlapping with a circle, with distance.
    /// More expensive than a simple circle overlap query.
    /// </summary>
    public IEnumerable<KeyValuePair<Shape, float>> MinDistanceShapes(
      Vector2 point, 
      float radius,
      Func<Shape, bool> filter = null)
    {
      float dist;
      for (int i = 0; i < this.shapes.Count; i++)
      {
        Shape shape = this.shapes[i];
        if (filter == null || filter(shape) == true)
        {
          if (shape.MinDistance(point, radius, out dist) == true)
          {
            yield return new KeyValuePair<Shape, float>(shape, dist);
          }
        }
      }
    }
    #endregion

    #region Body Queries
    /// <summary>
    /// Returns all bodies whose bounding boxes overlap an area.
    /// </summary>
    public IEnumerable<Body> QueryBodies(
      AABB area,
      Func<Body, bool> filter = null)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (filter == null || filter(body) == true)
        {
          if (body.Query(area))
          {
            yield return body;
          }
        }
      }
    }

    /// <summary>
    /// Returns all bodies containing a point.
    /// </summary>
    public IEnumerable<Body> QueryBodies(
      Vector2 point,
      Func<Body, bool> bodyFilter = null,
      Func<Shape, bool> shapeFilter = null)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (bodyFilter == null || bodyFilter(body) == true)
        {
          if (body.Query(point, shapeFilter) == true)
          {
            yield return body;
          }
        }
      }
    }

    /// <summary>
    /// Returns all bodies overlapping with a circle.
    /// </summary>
    public IEnumerable<Body> QueryBodies(
      Vector2 point,
      float radius,
      Func<Body, bool> bodyFilter = null,
      Func<Shape, bool> shapeFilter = null)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (bodyFilter == null || bodyFilter(body) == true)
        {
          if (body.Query(point, radius, shapeFilter) == true)
          {
            yield return body;
          }
        }
      }
    }

    /// <summary>
    /// Returns all bodies overlapping with a circle, with distance.
    /// More expensive than a simple circle overlap query.
    /// </summary>
    public IEnumerable<KeyValuePair<Body, float>> MinDistanceBodies(
      Vector2 point,
      float radius,
      Func<Body, bool> bodyFilter = null,
      Func<Shape, bool> shapeFilter = null)
    {
      float dist;
      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (bodyFilter == null || bodyFilter(body) == true)
        {
          if (body.MinDistance(point, radius, out dist, shapeFilter) == true)
          {
            yield return new KeyValuePair<Body, float>(body, dist);
          }
        }
      }
    }
    #endregion

    #region Line/Sweep Tests
    /// <summary>
    /// Performs a raycast on all bodies contained in the world.
    /// Filters by body or shape.
    /// </summary>
    public bool RayCast(
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
          body.RayCast(ref ray, ref result, shapeFilter);
          if (result.IsContained == true)
            return true;
        }
      }
      return result.IsValid;
    }

    /// <summary>
    /// Performs a swept circle cast on all bodies contained in the world.
    /// Filters by body or shape.
    /// </summary>
    public bool CircleCast(
      RayCast ray,
      float radius,
      out RayResult result,
      Func<Body, bool> bodyFilter = null,
      Func<Shape, bool> shapeFilter = null)
    {
      result = new RayResult();
      foreach (Body body in this.bodies)
      {
        if (bodyFilter == null || bodyFilter(body) == true)
        {
          body.CircleCast(ref ray, radius, ref result, shapeFilter);
          if (result.IsContained == true)
            return true;
        }
      }
      return result.IsValid;
    }
    #endregion

    #endregion

    #region Internals
    private void BroadPhase(bool allowDynamic)
    {
      // TODO: Extensible Broadphase
      for (int i = 0; i < this.shapes.Count; i++)
        for (int j = i + 1; j < this.shapes.Count; j++)
          this.NarrowPhase(
            this.shapes[i], 
            this.shapes[j], 
            this.manifolds,
            allowDynamic);
    }

    private void BroadPhase(Body body, bool allowDynamic)
    {
      // TODO: Extensible Broadphase
      foreach (Shape shape in body.Shapes)
        for (int i = 0; i < this.shapes.Count; i++)
          if (this.shapes[i].Body != body)
            this.NarrowPhase(
              shape, 
              this.shapes[i], 
              this.manifolds,
              allowDynamic);
    }

    private void NarrowPhase(
      Shape sa,
      Shape sb,
      List<Manifold> manifolds,
      bool allowDynamic)
    {
      if (sa.Body.CanCollide(sb.Body, allowDynamic) == false)
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
      for (int j = 0; j < this.IterationCount * 1 / 3; j++)
        for (int i = 0; i < this.manifolds.Count; i++)
          this.manifolds[i].Solve();

      for (int i = 0; i < this.manifolds.Count; i++)
        this.manifolds[i].SolveCached();

      this.Elasticity = 0.0f;
      for (int j = 0; j < this.IterationCount * 2 / 3; j++)
        for (int i = 0; i < this.manifolds.Count; i++)
          this.manifolds[i].Solve();
    }
    #endregion
  }
}
