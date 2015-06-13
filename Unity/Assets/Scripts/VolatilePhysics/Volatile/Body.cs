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
  public sealed class Body
  {
    public IEnumerable<Shape> Shapes
    {
      get { return this.shapes.AsReadOnly(); }
    }

    public bool UseGravity { get; set; }

    public Vector2 Position { get; private set; }
    public Vector2 LinearVelocity { get; private set; }
    public float Angle { get; private set; }
    public float AngularVelocity { get; private set; }
    public Vector2 Facing { get; private set; }

    // Immutable
    internal float massInv;
    internal float inertiaInv;

    // Dynamics
    private Vector2 force;
    private float torque;

    // TODO: Make these properties
    internal Vector2 velBias;
    internal float rotBias;

    internal World world;

    private List<Shape> shapes;

    #region Shape Management
    public void AddShape(Shape shape)
    {
      this.shapes.Add(shape);
      shape.Body = this;
    }

    public void RemoveShape(Shape shape)
    {
      this.shapes.Remove(shape);
      shape.Body = null;
    }

    /// <summary>
    /// This function should be called after all shapes have been added.
    /// </summary>
    public void Finalize()
    {
      this.ComputeBodyProperties();
      this.UpdateWorldCache();
    }
    #endregion

    #region Force and Impulse Application
    public void AddForce(Vector2 force)
    {
      this.force += force;
    }

    public void AddTorque(float torque)
    {
      this.torque = torque;
    }

    public void AddImpulse(Vector2 impulse)
    {
      this.ApplyImpulse(impulse, this.Position);
    }

    public void AddImpulseAtPoint(Vector2 impulse, Vector2 point)
    {
      this.ApplyImpulse(impulse, point - this.Position);
    }
    #endregion

    #region Position and Orientation
    public void Set(Vector2 position)
    {
      this.Position = position;
      this.UpdateWorldCache();
    }

    public void Set(float radians)
    {
      this.Angle = radians;
      this.UpdateWorldCache();
    }

    public void Set(Vector2 position, float radians)
    {
      this.Position = position;
      this.Angle = radians;
      this.UpdateWorldCache();
    }
    #endregion

    public Body(Vector2 position, bool useGravity = true)
    {
      this.Position = position;
      this.UseGravity = useGravity;
      this.shapes = new List<Shape>();
    }

    internal void Update(float deltaTime)
    {
      this.Integrate(deltaTime);
      this.UpdateWorldCache();
    }

    #region Collision
    internal bool CanCollide(Body other)
    {
      // TODO: Groups/Layers/etc.
      return this != other;
    }

    internal void ApplyImpulse(Vector2 j, Vector2 r)
    {
      this.LinearVelocity += this.massInv * j;
      this.AngularVelocity -= this.inertiaInv * Util.Cross(j, r);
    }

    internal void ApplyBias(Vector2 j, Vector2 r)
    {
      velBias += massInv * j;
      rotBias -= inertiaInv * Util.Cross(j, r);
    }
    #endregion

    #region Helper Functions
    private void UpdateWorldCache()
    {
      this.Facing = Util.Polar(this.Angle);
      foreach (Shape s in this.shapes)
        s.UpdateWorldCache(this.Position, this.Facing);
    }

    private void Integrate(float deltaTime)
    {
      // Apply damping
      this.LinearVelocity *= this.world.damping;
      this.AngularVelocity *= this.world.damping;

      // Calculate total force and torque
      Vector2 totalForce = (this.force * this.massInv);
      if (this.UseGravity == true)
        totalForce += this.world.gravity;
      float totalTorque = this.torque * this.inertiaInv;

      // See http://www.niksula.hut.fi/~hkankaan/Homepages/gravity.html
      this.IntegrateForces(totalForce, totalTorque, deltaTime, 0.5f);
      this.IntegrateVelocity(deltaTime);
      this.IntegrateForces(totalForce, totalTorque, deltaTime, 0.5f);

      this.ClearForces();
    }

    private void IntegrateForces(
      Vector2 force,
      float torque,
      float deltaTime,
      float mult)
    {
      this.LinearVelocity += force * deltaTime * mult;
      this.AngularVelocity -= torque * deltaTime * mult;
    }

    private void IntegrateVelocity(float dt)
    {
      this.Position += dt * this.LinearVelocity + this.velBias;
      this.Angle += dt * this.AngularVelocity + this.rotBias;
    }

    private void ClearForces()
    {
      this.force = Vector2.zero;
      this.torque = 0.0f;

      this.velBias.x = 0.0f;
      this.velBias.y = 0.0f;
      this.rotBias = 0.0f;
    }

    private void ComputeBodyProperties()
    {
      float mass = 0.0f;
      float inertia = 0.0f;

      foreach (Shape s in this.shapes)
      {
        mass += s.Mass;
        inertia += s.Mass * s.Inertia;
      }

      this.massInv = 1.0f / mass;
      this.inertiaInv = 1.0f / inertia;
    }
    #endregion
  }
}
