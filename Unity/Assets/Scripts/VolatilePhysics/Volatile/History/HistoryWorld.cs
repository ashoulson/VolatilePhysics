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

    private QuadtreeBuffer buffer;
    private int time;

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
      this.buffer =
        new QuadtreeBuffer(
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
      base.Update();
      this.buffer.Update(this.time);
    }

    internal Quadtree GetTree(int time)
    {
      return this.buffer.GetTree(time);
    }
  }
}