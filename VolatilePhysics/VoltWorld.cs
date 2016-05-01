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

using UnityEngine;
using CommonUtil;

namespace Volatile
{
  public sealed class VoltWorld
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

    public IEnumerable<VoltBody> Bodies 
    { 
      get 
      {
        for (int i = 0; i < this.bodies.Count; i++)
          yield return this.bodies[i];
      } 
    }

    internal float Elasticity { get; private set; }
    internal float Damping { get; private set; }

    private CheapList<VoltBody> bodies;
    private List<Manifold> manifolds;

    // Each World instance should own its own object pools, in case
    // you want to run multiple World instances simultaneously.
    private IUtilPool<VoltBody> bodyPool;
    private IUtilPool<VoltShape> circlePool;
    private IUtilPool<VoltShape> polygonPool;

    private IUtilPool<Contact> contactPool;
    private IUtilPool<Manifold> manifoldPool;
    private IUtilPool<HistoryBuffer> historyPool;

    public VoltWorld(
      int historyLength = 0,
      float damping = VoltConfig.DEFAULT_DAMPING)
    {
      this.HistoryLength = historyLength;
      this.Damping = damping;

      this.IterationCount = VoltConfig.DEFAULT_ITERATION_COUNT;
      this.DeltaTime = VoltConfig.DEFAULT_DELTA_TIME;

      this.bodies = new CheapList<VoltBody>();
      this.manifolds = new List<Manifold>();

      this.bodyPool = new UtilPool<VoltBody>();
      this.circlePool = new UtilPool<VoltShape, VoltCircle>();
      this.polygonPool = new UtilPool<VoltShape, VoltPolygon>();

      this.contactPool = new UtilPool<Contact>();
      this.manifoldPool = new UtilPool<Manifold>();
      this.historyPool = new UtilPool<HistoryBuffer>();
    }

    /// <summary>
    /// Creates a new polygon shape. Must be initialized afterwards.
    /// </summary>
    public VoltPolygon CreatePolygon()
    {
      return (VoltPolygon)this.polygonPool.Allocate();
    }

    /// <summary>
    /// Creates a new circle shape. Must be initialized afterwards.
    /// </summary>
    public VoltCircle CreateCircle()
    {
      return (VoltCircle)this.circlePool.Allocate();
    }

    /// <summary>
    /// Creates a new static body and adds it to the world.
    /// </summary>
    public VoltBody CreateStaticBody(
      Vector2 position,
      float radians,
      params VoltShape[] shapesToAdd)
    {
      VoltBody body = this.bodyPool.Allocate();
      body.InitializeStatic(position, radians, shapesToAdd);
      this.AddBody(body);
      return body;
    }

    /// <summary>
    /// Creates a new dynamic body and adds it to the world.
    /// </summary>
    public VoltBody CreateDynamicBody(
      Vector2 position,
      float radians,
      params VoltShape[] shapesToAdd)
    {
      VoltBody body = this.bodyPool.Allocate();
      body.InitializeDynamic(position, radians, shapesToAdd);
      this.AddBody(body);
      return body;
    }

    /// <summary>
    /// Adds a body to the world. Used for reintroducing bodies that 
    /// have been removed. For new bodies, use CreateBody.
    /// </summary>
    public void AddBody(
      VoltBody body,
      Vector2 position,
      float radians)
    {
#if DEBUG
      UtilDebug.Assert(body.IsInitialized);
#endif
      UtilDebug.Assert(body.World == null);
      this.AddBody(body);
      body.Set(position, radians);
    }

    /// <summary>
    /// Removes a body from the world. The body will be partially reset so it
    /// can be added later. The pointer is still valid and the body can be
    /// returned to the world using AddBody.
    /// </summary>
    public void RemoveBody(VoltBody body)
    {
      UtilDebug.Assert(body.World == this);
      this.bodies.Remove(body);

      body.FreeHistory();
      body.PartialReset();
      body.AssignWorld(null);
    }

    /// <summary>
    /// Removes a body from the world and deallocates it. The pointer is
    /// invalid after this point.
    /// </summary>
    public void DestroyBody(VoltBody body)
    {
      UtilDebug.Assert(body.World == this);
      this.bodies.Remove(body);

      body.FreeHistory();
      body.FreeShapes();
      body.AssignWorld(null);

      this.FreeBody(body);
    }

    /// <summary>
    /// Ticks the world, updating all dynamic bodies and resolving collisions.
    /// If a frame number is provided, all dynamic bodies will store their
    /// state for that frame for later testing.
    /// </summary>
    public void Update()
    {
      for (int i = 0; i < this.bodies.Count; i++)
        this.bodies[i].Update();
      this.BroadPhase();

      this.UpdateCollision();
      this.FreeManifolds();
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
    public void Update(VoltBody body)
    {
      body.Update();
      this.BroadPhase(body);

      this.UpdateCollision();
      this.FreeManifolds();
    }

    /// <summary>
    /// Finds all bodies containing a given point.
    /// </summary>
    public IEnumerable<VoltBody> QueryPoint(
      Vector2 point,
      VoltBodyFilter filter = null,
      int ticksBehind = 0)
    {
      if (ticksBehind < 0)
        throw new ArgumentOutOfRangeException("ticksBehind");

      for (int i = 0; i < this.bodies.Count; i++)
      {
        VoltBody body = this.bodies[i];
        if (VoltBody.Filter(body, filter))
          if (body.QueryPoint(point, ticksBehind))
            yield return body;
      }
    }

    /// <summary>
    /// Finds all bodies intersecting with a given circle.
    /// </summary>
    public IEnumerable<VoltBody> QueryCircle(
      Vector2 origin,
      float radius,
      VoltBodyFilter filter = null,
      int ticksBehind = 0)
    {
      if (ticksBehind < 0)
        throw new ArgumentOutOfRangeException("ticksBehind");

      for (int i = 0; i < this.bodies.Count; i++)
      {
        VoltBody body = this.bodies[i];
        if (VoltBody.Filter(body, filter))
          if (body.QueryCircle(origin, radius, ticksBehind))
           yield return body;
      }
    }

    /// <summary>
    /// Performs a raycast on all world bodies.
    /// </summary>
    public bool RayCast(
      ref VoltRayCast ray,
      ref VoltRayResult result,
      VoltBodyFilter filter = null,
      int ticksBehind = 0)
    {
      if (ticksBehind < 0)
        throw new ArgumentOutOfRangeException("ticksBehind");

      for (int i = 0; i < this.bodies.Count; i++)
      {
        VoltBody body = this.bodies[i];
        if (VoltBody.Filter(body, filter) == true)
        {
          body.RayCast(ref ray, ref result, ticksBehind);
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
      ref VoltRayCast ray,
      float radius,
      ref VoltRayResult result,
      VoltBodyFilter filter = null,
      int ticksBehind = 0)
    {
      if (ticksBehind < 0)
        throw new ArgumentOutOfRangeException("ticksBehind");

      for (int i = 0; i < this.bodies.Count; i++)
      {
        VoltBody body = this.bodies[i];
        if (VoltBody.Filter(body, filter) == true)
        {
          body.CircleCast(ref ray, radius, ref result, ticksBehind);
          if (result.IsContained == true)
            return true;
        }
      }
      return result.IsValid;
    }

    #region Internals
    private void AddBody(VoltBody body)
    {
      this.bodies.Add(body);
      body.AssignWorld(this);
      if ((this.HistoryLength > 0) && (body.IsStatic == false))
        body.AssignHistory(this.AllocateHistory());
    }

    /// <summary>
    /// Identifies collisions for all bodies, ignoring symmetrical duplicates.
    /// </summary>
    private void BroadPhase()
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        for (int j = i + 1; j < this.bodies.Count; j++)
        {
          VoltBody ba = this.bodies[i];
          VoltBody bb = this.bodies[j];

          if (ba.CanCollide(bb) && bb.CanCollide(ba) && ba.AABB.Intersect(bb.AABB))
            for (int i_s = 0; i_s < ba.shapeCount; i_s++)
              for (int j_s = 0; j_s < bb.shapeCount; j_s++)
                this.NarrowPhase(ba.shapes[i_s], bb.shapes[j_s]);
        }
      }
    }

    /// <summary>
    /// Identifies collisions for a single body. Does not keep track of 
    /// symmetrical duplicates (they could be counted twice).
    /// </summary>
    private void BroadPhase(VoltBody bb)
    {
      for (int i = 0; i < this.bodies.Count; i++)
      {
        VoltBody ba = this.bodies[i];
        if (ba.CanCollide(bb) && bb.CanCollide(ba) && ba.AABB.Intersect(bb.AABB))
          for (int i_s = 0; i_s < ba.shapeCount; i_s++)
            for (int j_s = 0; j_s < bb.shapeCount; j_s++)
              this.NarrowPhase(ba.shapes[i_s], bb.shapes[j_s]);
      }
    }

    /// <summary>
    /// Creates a manifold for two shapes if they collide.
    /// </summary>
    private void NarrowPhase(
      VoltShape sa,
      VoltShape sb)
    {
      if (sa.AABB.Intersect(sb.worldSpaceAABB) == false)
        return;

      VoltShape.OrderShapes(ref sa, ref sb);
      Manifold manifold = Collision.Dispatch(this, sa, sb);
      if (manifold != null)
        this.manifolds.Add(manifold);
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

    #region Pooling
    internal Contact AllocateContact()
    {
      return this.contactPool.Allocate();
    }

    internal Manifold AllocateManifold()
    {
      return this.manifoldPool.Allocate();
    }

    private HistoryBuffer AllocateHistory()
    {
      HistoryBuffer history = this.historyPool.Allocate();
      history.Initialize(this.HistoryLength);
      return history;
    }

    private void FreeBody(VoltBody body)
    {
      this.bodyPool.Deallocate(body);
    }

    private void FreeManifolds()
    {
      for (int i = 0; i < this.manifolds.Count; i++)
        this.manifoldPool.Deallocate(this.manifolds[i]);
      this.manifolds.Clear();
    }

    internal void FreeContacts(IList<Contact> contacts)
    {
      for (int i = 0; i < contacts.Count; i++)
        this.contactPool.Deallocate(contacts[i]);
    }

    internal void FreeHistory(HistoryBuffer history)
    {
      this.historyPool.Deallocate(history);
    }

    internal void FreeShapes(IList<VoltShape> shapes)
    {
      for (int i = 0; i < shapes.Count; i++)
      {
        VoltShape shape = shapes[i];
        switch (shape.Type)
        {
          case VoltShape.ShapeType.Circle:
            this.circlePool.Deallocate(shape);
            break;

          case VoltShape.ShapeType.Polygon:
            this.polygonPool.Deallocate(shape);
            break;

          default:
            UtilDebug.LogError("Unknown shape for deallocation");
            break;
        }
      }
    }
    #endregion
    #endregion
  }
}