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
  public sealed class World
  {
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

    internal List<Body> dynamicBodies;
    internal IBroadPhase staticBroad;

    internal float damping = 0.999f;

    // Each World instance should own its own object pools, in case
    // you want to run multiple World instances simultaneously.
    private Manifold.Pool manifoldPool;
    private Contact.Pool contactPool;
    private List<Manifold> manifolds;

    public World(float damping = 0.999f)
    {
      this.DeltaTime = Time.fixedDeltaTime;
      this.IterationCount = Config.DEFAULT_ITERATION_COUNT;

      this.dynamicBodies = new List<Body>();
      this.staticBroad = new NaiveBroadphase();

      this.damping = damping;

      this.contactPool = new Contact.Pool();
      this.manifoldPool = new Manifold.Pool(this.contactPool);
      this.manifolds = new List<Manifold>();
    }

    /// <summary>
    /// Adds a body to the world, dynamic or static.
    /// </summary>
    /// <param name="body"></param>
    public void AddBody(Body body)
    {
      if (body.IsStatic == true)
        this.staticBroad.Add(body);
      else
        this.dynamicBodies.Add(body);

      body.World = this;
    }

    /// <summary>
    /// Removes a body from the world. Dynamic bodies only.
    /// </summary>
    /// <param name="body"></param>
    public void RemoveBody(Body body)
    {
      if (body.IsStatic == true)
        throw new InvalidOperationException("Can't remove static bodies");
      this.dynamicBodies.Remove(body);
      body.World = null;
    }

    /// <summary>
    /// Ticks the world, updating all dynamic bodies and resolving collisions.
    /// </summary>
    public void Update()
    {
      foreach (Body body in this.dynamicBodies)
        body.Update();

      this.BroadPhase();
      this.UpdateCollision();
      this.CleanupManifolds();
    }

    /// <summary>
    /// Updates a single body. Does not allow dynamic-dynamic collisions.
    /// </summary>
    public void Update(Body body)
    {
      body.Update();

      this.BroadPhase(body);
      this.UpdateCollision();
      this.CleanupManifolds();
    }

    #region Tests
    public IEnumerable<Body> Query(
      AABB area,
      BodyFilter filter = null)
    {
      return
        this.QueryDynamic(area, filter).Concat(
          this.staticBroad.Query(area, filter));
    }

    public IEnumerable<Body> Query(
      Vector2 point,
      BodyFilter filter = null)
    {
      return
        this.QueryDynamic(point, filter).Concat(
          this.staticBroad.Query(point, filter));
    }

    public IEnumerable<Body> Query(
      Vector2 point,
      float radius,
      BodyFilter filter = null)
    {
      return
        this.QueryDynamic(point, radius, filter).Concat(
          this.staticBroad.Query(point, radius, filter));
    }

    public bool RayCast(
      ref RayCast ray,
      ref RayResult result,
      BodyFilter filter = null)
    {
      return
        this.RayCastDynamic(ref ray, ref result, filter) ||
        this.staticBroad.RayCast(ref ray, ref result, filter);
    }

    public bool CircleCast(
      ref RayCast ray,
      float radius,
      ref RayResult result,
      BodyFilter filter = null)
    {
      return
        this.CircleCastDynamic(ref ray, radius, ref result, filter) ||
        this.staticBroad.CircleCast(ref ray, radius, ref result, filter);
    }

    #region Dynamic
    private IEnumerable<Body> QueryDynamic(
      AABB area,
      BodyFilter filter = null)
    {
      for (int i = 0; i < this.dynamicBodies.Count; i++)
      {
        Body body = this.dynamicBodies[i];
        if (Body.Filter(body, filter) && body.Query(area))
          yield return body;
      }
    }

    private IEnumerable<Body> QueryDynamic(
      Vector2 point,
      BodyFilter filter = null)
    {
      for (int i = 0; i < this.dynamicBodies.Count; i++)
      {
        Body body = this.dynamicBodies[i];
        if (Body.Filter(body, filter) && body.Query(point))
          yield return body;
      }
    }

    private IEnumerable<Body> QueryDynamic(
      Vector2 point,
      float radius,
      BodyFilter filter = null)
    {
      for (int i = 0; i < this.dynamicBodies.Count; i++)
      {
        Body body = this.dynamicBodies[i];
        if (Body.Filter(body, filter) && body.Query(point, radius))
          yield return body;
      }
    }

    private bool RayCastDynamic(
      ref RayCast ray,
      ref RayResult result,
      BodyFilter filter = null)
    {
      for (int i = 0; i < this.dynamicBodies.Count; i++)
      {
        Body body = this.dynamicBodies[i];
        if (Body.Filter(body, filter) == true)
        {
          body.RayCast(ref ray, ref result);
          if (result.IsContained == true)
            return true;
        }
      }
      return result.IsValid;
    }

    private bool CircleCastDynamic(
      ref RayCast ray,
      float radius,
      ref RayResult result,
      BodyFilter filter = null)
    {
      for (int i = 0; i < this.dynamicBodies.Count; i++)
      {
        Body body = this.dynamicBodies[i];
        if (Body.Filter(body, filter) == true)
        {
          body.CircleCast(ref ray, radius, ref result);
          if (result.IsContained == true)
            return true;
        }
      }
      return result.IsValid;
    }
    #endregion

    #endregion

    #region Internals
    private void BroadPhase()
    {
      for (int i = 0; i < this.dynamicBodies.Count; i++)
        this.staticBroad.Collision(
          this.dynamicBodies[i],
          this.NarrowPhase);
    }

    private void BroadPhase(Body body)
    {
      this.staticBroad.Collision(body, this.NarrowPhase);
    }

    private void NarrowPhase(
      Shape sa,
      Shape sb)
    {
      if (sa.Body.CanCollide(sb.Body) == false)
        return;
      if (sa.AABB.Intersect(sb.AABB) == false)
        return;

      Shape.OrderShapes(ref sa, ref sb);
      Manifold manifold = Collision.Dispatch(sa, sb, this.manifoldPool);
      if (manifold != null)
        this.manifolds.Add(manifold);
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
        this.manifolds[i].PreStep();

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