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
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace Volatile
{
  public sealed class World
  {
    // TEMP DEBUG
    private long[] windowUpdate = new long[100];
    private int windowUpdateIdx = 0;
    private long[] windowRay = new long[100];
    private int windowRayIdx = 0;

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

    internal AABBTree bodyTree;
    internal List<Body> bodies;

    internal float damping = 0.999f;

    // Each World instance should own its own object pools, in case
    // you want to run multiple World instances simultaneously.
    private Manifold.Pool manifoldPool;
    private Contact.Pool contactPool;

    // TODO: Could convert to a linked list using the pool pointers, maybe?
    private List<Manifold> manifolds;

    public World(float damping = 0.999f)
    {
      // DEBUG
      this.windowUpdate = new long[1000];
      for (int i = 0; i < this.windowUpdate.Length; i++)
        this.windowUpdate[i] = 0;
      this.windowUpdateIdx = 0;
      this.windowRay = new long[1000];
      for (int i = 0; i < this.windowRay.Length; i++)
        this.windowRay[i] = 0;
      this.windowRayIdx = 0;

      this.DeltaTime = Time.fixedDeltaTime;
      this.IterationCount = Config.DEFAULT_ITERATION_COUNT;
      this.damping = damping;

      this.bodyTree = new AABBTree();
      this.bodies = new List<Body>();
      this.contactPool = new Contact.Pool();
      this.manifoldPool = new Manifold.Pool(this.contactPool);
      this.manifolds = new List<Manifold>();
    }

    /// <summary>
    /// Adds a body to the world.
    /// </summary>
    public void AddBody(Body body)
    {
      this.bodies.Add(body);
      this.bodyTree.AddBody(body);
      body.AssignWorld(this);
    }

    /// <summary>
    /// Removes a body from the world.
    /// </summary>
    public void RemoveBody(Body body)
    {
      this.bodies.Remove(body);
      this.bodyTree.RemoveBody(body);
      body.AssignWorld(null);
    }

    /// <summary>
    /// Ticks the world, updating all dynamic bodies and resolving collisions.
    /// Does not allow dynamic-dynamic collisions.
    /// </summary>
    public void Update()
    {
      // Debug
      System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
      stopwatch.Start();

      for (int i = 0; i < this.bodies.Count; i++)
        this.bodies[i].Update();

      this.BroadPhase(true);
      this.UpdateCollision();
      this.CleanupManifolds();

      for (int i = 0; i < this.bodies.Count; i++)
        this.bodyTree.UpdateBody(this.bodies[i]);

      // Debug
      stopwatch.Stop();
      long elapsed = stopwatch.ElapsedTicks;
      this.windowUpdate[(windowUpdateIdx++) % this.windowUpdate.Length] = elapsed;
      double sum = 0.0f;
      for (int i = 0; i < this.windowUpdate.Length; i++)
        sum += this.windowUpdate[i];
      double avg = sum / (double)this.windowUpdate.Length;
      Debug.Log("Update: " + elapsed + " " + avg);
    }

    /// <summary>
    /// Updates a single body. Does not allow dynamic-dynamic collisions.
    /// </summary>
    public void Update(Body body)
    {
      body.Update();

      this.BroadPhase(false);
      this.UpdateCollision();
      this.CleanupManifolds();

      this.bodyTree.UpdateBody(body);
    }

    #region Tests
    public IEnumerable<Body> Query(
      Vector2 point,
      BodyFilter filter = null)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (Body.Filter(body, filter) && body.Query(point))
          yield return body;
      }
    }

    public IEnumerable<Body> Query(
      Vector2 point,
      float radius,
      BodyFilter filter = null)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (Body.Filter(body, filter) && body.Query(point, radius))
          yield return body;
      }
    }

    public bool RayCast(
      ref RayCast ray,
      ref RayResult result,
      BodyFilter filter = null)
    {
      // Debug
      System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
      stopwatch.Start();

      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (Body.Filter(body, filter) == true)
        {
          body.RayCast(ref ray, ref result);
          if (result.IsContained == true)
            return true;
        }
      }

      //this.bodyTree.RayCast(ref ray, ref result, filter);

      // Debug
      stopwatch.Stop();
      long elapsed = stopwatch.ElapsedTicks;
      this.windowRay[(windowRayIdx++) % this.windowRay.Length] = elapsed;
      double sum = 0.0f;
      for (int i = 0; i < this.windowRay.Length; i++)
        sum += this.windowRay[i];
      double avg = sum / (double)this.windowRay.Length;
      Debug.Log("Ray: " + elapsed + " " + avg);

      return result.IsValid;
    }

    public bool CircleCast(
      ref RayCast ray,
      float radius,
      ref RayResult result,
      BodyFilter filter = null)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
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

    #region Internals
    private void BroadPhase(bool allowDynamic)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        for (int j = i + 1; j < this.bodies.Count; j++)
        {
          Body ba = this.bodies[i];
          Body bb = this.bodies[j];

          if (ba.CanCollide(bb, allowDynamic) && ba.AABB.Intersect(bb.AABB))
            for (int i_s = 0; i_s < ba.shapes.Count; i_s++)
              for (int j_s = 0; j_s < bb.shapes.Count; j_s++)
                this.NarrowPhase(ba.shapes[i_s], bb.shapes[j_s]);
        }
      }

      //List<Body> foundBodies = new List<Body>(256);

      //for (int idxBodies = 0; idxBodies < this.bodies.Count; idxBodies++)
      //{

      //  foundBodies.Clear();
      //  Body current = this.bodies[idxBodies];
      //  this.bodyTree.Query(current.AABB, foundBodies);

      //  for (int idxFound = 0; idxFound < foundBodies.Count; idxFound++)
      //  {
      //    Body found = foundBodies[idxFound];
      //    if (found.CanCollide(current, allowDynamic) && found.AABB.Intersect(current.AABB))
      //      for (int i_s = 0; i_s < found.shapes.Count; i_s++)
      //        for (int j_s = 0; j_s < current.shapes.Count; j_s++)
      //          this.NarrowPhase(found.shapes[i_s], current.shapes[j_s]);
      //  }
      //}
    }

    private void NarrowPhase(
      Shape sa,
      Shape sb)
    {
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