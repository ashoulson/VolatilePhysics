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

namespace Volatile.History
{
  public class HistoryWorld : World
  {
    public int CurrentTime { get { return this.time; } }

    private Broadphase buffer;
    private int time;
    private int historyLength;

    /// <summary>
    /// Instantiates a new world with historical query capabilities.
    /// </summary>
    /// <param name="startingTime">The initial tick time.</param>
    /// <param name="historyLength">Historical length, in ticks.</param>
    /// <param name="initialCapacity">Initial quadtree capacity.</param>
    /// <param name="maxDepth">Maximum quadtree depth.</param>
    /// <param name="maxBodiesPerCell">Max bodies per quadtree cell.</param>
    /// <param name="extent">Size extent of the quadtree.</param>
    /// <param name="gravity">Gravity vector.</param>
    /// <param name="damping">Damping quotient.</param>
    public HistoryWorld(
      int startingTime,
      int historyLength,
      int initialCapacity,
      int maxDepth,
      int maxBodiesPerCell,
      float extent,
      Vector2 gravity, 
      float damping = 0.999f)
      : base(gravity, damping)
    {
      this.time = startingTime;
      this.historyLength = historyLength;
      this.buffer =
        new Broadphase(
          startingTime,
          historyLength,
          initialCapacity,
          maxDepth,
          maxBodiesPerCell,
          extent);
    }

    /// <summary>
    /// Call this after adding all bodies.
    /// </summary>
    public override void Initialize()
    {
      this.buffer.Update(this.time);
    }

    public override void AddBody(Body body)
    {
      base.AddBody(body);
      foreach (Shape shape in body.Shapes)
        this.buffer.AddShape(shape);
    }

    public override void Update()
    {
      this.time++;
      this.UpdateBodies();
      this.buffer.Update(this.time);
      this.UpdateCollision();
      this.CleanupManifolds();
    }

    internal override void BroadPhase(List<Manifold> manifolds)
    {
      System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
      watch.Start();

      //Dictionary<Shape, HashSet<Shape>> adjacencies =
      //  this.PopulateAdjacencies();
      //HashSet<Shape> done = new HashSet<Shape>();

      //foreach (KeyValuePair<Shape, HashSet<Shape>> pair in adjacencies)
      //{
      //  foreach (Shape value in pair.Value)
      //  {
      //    if (pair.Key != value && done.Contains(value) == false)
      //      this.NarrowPhase(pair.Key, value, manifolds);
      //  }
      //  done.Add(pair.Key);
      //}

      base.BroadPhase(manifolds);

      watch.Stop();
      Debug.Log(watch.ElapsedMilliseconds);
    }

    private Dictionary<Shape, HashSet<Shape>> PopulateAdjacencies()
    {
      Dictionary<Shape, HashSet<Shape>> adjacencies =
        new Dictionary<Shape, HashSet<Shape>>();

      for (int i = 0; i < this.shapes.Count; i++)
      {
        Shape current = this.shapes[i];
        foreach (Shape adjacent in this.buffer.GetAdjacentShapes(current))
        {
          this.AddEntry(adjacencies, current, adjacent);
          this.AddEntry(adjacencies, adjacent, current);
        }
      }

      return adjacencies;
    }

    private void AddEntry(
      Dictionary<Shape, HashSet<Shape>> adjacent, 
      Shape key,
      Shape value)
    {
      if (adjacent.ContainsKey(key) == false)
        adjacent[key] = new HashSet<Shape>();
      adjacent[key].Add(value);
    }

    internal Quadtree GetTree(int time)
    {
      if (this.IsTimeInBounds(time) == true)
        return this.buffer.GetTree(time);
      return null;
    }

    private bool IsTimeInBounds(int time)
    {
      return
        time >= 0 &&
        time <= this.time &&
        time > (this.time - this.historyLength);
    }
  }
}