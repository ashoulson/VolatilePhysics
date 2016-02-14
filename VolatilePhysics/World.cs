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

#if VOLATILE_UNITY
using UnityEngine;
#else
using VolatileEngine;
#endif

namespace Volatile
{
  public sealed class World
  {
    /// <summary>
    /// Fixed update delta time for body integration. 
    /// Defaults to Config.DEFAULT_DELTA_TIME.
    /// </summary>
    public float DeltaTime { get; set; }

    /// <summary>
    /// Number of iterations when updating the world.
    /// Defaults to Config.DEFAULT_ITERATION_COUNT.
    /// </summary>
    public int IterationCount { get; set; }

    /// <summary>
    /// How many frames of history this world is recording.
    /// </summary>
    public int HistoryLength { get; private set; }

    internal float Elasticity { get; private set; }
    internal float Damping { get; private set; }

    private List<Body> bodies;

    // Each World instance should own its own object pools, in case
    // you want to run multiple World instances simultaneously.
    private Manifold.Pool manifoldPool;
    private Contact.Pool contactPool;

    // TODO: Could convert to a linked list using the pool pointers
    private List<Manifold> manifolds;

    public World(
      int historyLength = 0,
      float damping = Config.DEFAULT_DAMPING)
    {
      this.HistoryLength = historyLength;

      this.Damping = damping;

      this.IterationCount = Config.DEFAULT_ITERATION_COUNT;
      this.DeltaTime = Config.DEFAULT_DELTA_TIME;

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
      body.AssignWorld(this);
      if (this.HistoryLength > 0)
        body.StartHistory(this.HistoryLength);
    }

    /// <summary>
    /// Removes a body from the world.
    /// </summary>
    public void RemoveBody(Body body)
    {
      this.bodies.Remove(body);
      body.AssignWorld(null);
    }

    /// <summary>
    /// Ticks the world, updating all dynamic bodies and resolving collisions.
    /// If a frame number is provided, all dynamic bodies will store their
    /// state for that frame for later testing.
    /// </summary>
    public void Update(int frame = History.CURRENT_FRAME)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        body.Update();
        if (History.ShouldStoreOnFrame(frame))
          body.StoreState(frame);
      }
      this.BroadPhase();

      this.UpdateCollision();
      this.CleanupManifolds();
    }

    /// <summary>
    /// Updates a single body, resolving only collisions with that body.
    /// If a frame number is provided, all dynamic bodies will store their
    /// state for that frame for later testing.
    /// 
    /// Note: This function is more efficient to use if you have only a single
    /// body in your world surrounded by static geometry (as is common for 
    /// client-side controller prediction in networked games). If you have 
    /// multiple dynamic bodies, using this function on each individual body 
    /// may result in collisions being resolved twice with double the force.
    /// </summary>
    public void Update(Body body, int frame = History.CURRENT_FRAME)
    {
      body.Update();
      if (History.ShouldStoreOnFrame(frame))
        body.StoreState(frame);
      this.BroadPhase(body);

      this.UpdateCollision();
      this.CleanupManifolds();
    }

    /// <summary>
    /// Finds all bodies containing a given point.
    /// </summary>
    public IEnumerable<Body> QueryPoint(
      Vector2 point,
      BodyFilter filter = null,
      int frame = History.CURRENT_FRAME)
    {
      // Validate user input
      frame = History.ValidateTestFrame(frame);

      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (Body.Filter(body, filter))
          if (body.QueryPoint(point, frame))
          yield return body;
      }
    }

    /// <summary>
    /// Finds all bodies intersecting with a given circle.
    /// </summary>
    public IEnumerable<Body> QueryCircle(
      Vector2 origin,
      float radius,
      BodyFilter filter = null,
      int frame = History.CURRENT_FRAME)
    {
      // Validate user input
      frame = History.ValidateTestFrame(frame);

      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (Body.Filter(body, filter))
          if (body.QueryCircle(origin, radius, frame))
           yield return body;
      }
    }

    /// <summary>
    /// Performs a raycast on all world bodies.
    /// </summary>
    public bool RayCast(
      ref RayCast ray,
      ref RayResult result,
      BodyFilter filter = null,
      int frame = History.CURRENT_FRAME)
    {
      // Validate user input
      frame = History.ValidateTestFrame(frame);

      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (Body.Filter(body, filter) == true)
        {
          body.RayCast(ref ray, ref result, frame);
          if (result.IsContained == true)
            return true;
        }
      }

      return result.IsValid;
    }

    /// <summary>
    /// Performs a circle cast on all world bodies.
    /// </summary>
    public bool CircleCast(
      ref RayCast ray,
      float radius,
      ref RayResult result,
      BodyFilter filter = null,
      int frame = History.CURRENT_FRAME)
    {
      // Validate user input
      frame = History.ValidateTestFrame(frame);

      for (int i = 0; i < this.bodies.Count; i++)
      {
        Body body = this.bodies[i];
        if (Body.Filter(body, filter) == true)
        {
          body.CircleCast(ref ray, radius, ref result, frame);
          if (result.IsContained == true)
            return true;
        }
      }
      return result.IsValid;
    }

    #region Internals
    /// <summary>
    /// Identifies collisions for all bodies, ignoring symmetrical duplicates.
    /// </summary>
    private void BroadPhase()
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        for (int j = i + 1; j < this.bodies.Count; j++)
        {
          Body ba = this.bodies[i];
          Body bb = this.bodies[j];

          if (ba.CanCollide(bb) && ba.AABB.Intersect(bb.AABB))
            for (int i_s = 0; i_s < ba.shapes.Count; i_s++)
              for (int j_s = 0; j_s < bb.shapes.Count; j_s++)
                this.NarrowPhase(ba.shapes[i_s], bb.shapes[j_s]);
        }
      }
    }

    /// <summary>
    /// Identifies collisions for a single body. Does not keep track of 
    /// symmetrical duplicates (they could be counted twice).
    /// </summary>
    private void BroadPhase(Body bb)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
          Body ba = this.bodies[i];
          if (ba.CanCollide(bb) && ba.AABB.Intersect(bb.AABB))
            for (int i_s = 0; i_s < ba.shapes.Count; i_s++)
              for (int j_s = 0; j_s < bb.shapes.Count; j_s++)
                this.NarrowPhase(ba.shapes[i_s], bb.shapes[j_s]);
      }
    }

    /// <summary>
    /// Creates a manifold for two shapes if they collide.
    /// </summary>
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