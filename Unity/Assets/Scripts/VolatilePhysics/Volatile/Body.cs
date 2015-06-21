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
    /// <summary>
    /// User token, for attaching arbitrary data to this body.
    /// </summary>
    public object Token { get; set; }

    public World World { get; internal set; }
    public bool UseGravity { get; set; }
    public bool IsStatic { get; private set; }
    public IEnumerable<Shape> Shapes { get { return this.GetShapes(); } }

    public Vector2 Position { get; private set; }
    public Vector2 LinearVelocity { get; private set; }
    public Vector2 Force { get; private set; }

    public float Angle { get; private set; }
    public float AngularVelocity { get; internal set; } // (TEMP) TODO: Make this private again 
    public float Torque { get; private set; }

    public Vector2 Facing { get; private set; }
    public AABB AABB { get; private set; }

    internal float InvMass { get; private set; }
    internal float InvInertia { get; private set; }
    internal Vector2 BiasVelocity { get; private set; }
    internal float BiasRotation { get; private set; }

    private List<Fixture> fixtures;

    #region Fixture/Shape Management
    /// <summary>
    /// Adds a shape, using a fixture to "pin" that shape to the body
    /// relative to its current position and rotation offset from the body.
    /// Any subsequent movement of the body will also move the shape.
    /// </summary>
    public void AddShape(Shape shape)
    {
      this.fixtures.Add(Fixture.FromWorldSpace(this, shape));
      shape.Body = this;
    }

    /// <summary>
    /// This function should be called after all shapes have been added.
    /// </summary>
    public void Initialize()
    {
      this.ComputeDynamics();
      this.ApplyPositions();
    }

    /// <summary>
    /// Extracts the shape from each fixture.
    /// </summary>
    private IEnumerable<Shape> GetShapes()
    {
      foreach (Fixture fixture in this.fixtures)
        yield return fixture.Shape;
    }
    #endregion

    #region Force and Impulse Application
    public void AddForce(Vector2 force)
    {
      this.Force += force;
    }

    public void AddTorque(float torque)
    {
      this.Torque = torque;
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
    public void SetWorld(Vector2 position)
    {
      this.Position = position;
      this.ApplyPositions();
    }

    public void SetWorld(float radians)
    {
      this.Angle = radians;
      this.ApplyPositions();
    }

    public void SetWorld(Vector2 position, float radians)
    {
      this.Position = position;
      this.Angle = radians;
      this.ApplyPositions();
    }
    #endregion

    public Body(Vector2 position, float radians, bool useGravity = true)
    {
      this.Position = position;
      this.Angle = radians;
      this.Facing = Util.Polar(radians);
      this.UseGravity = useGravity;
      this.fixtures = new List<Fixture>();
    }

    public Body(Vector2 position, bool useGravity = true)
      : this(position, 0.0f, useGravity)
    {
    }

    internal void Update(float deltaTime)
    {
      this.Integrate(deltaTime);
      this.ApplyPositions();
    }

    #region Collision
    internal bool CanCollide(Body other)
    {
      // TODO: Layers, flags, etc.
      if (this == other)
        return false;
      if (this.IsStatic && other.IsStatic)
        return false;
      return true;
    }

    internal void ApplyImpulse(Vector2 j, Vector2 r)
    {
      this.LinearVelocity += this.InvMass * j;
      this.AngularVelocity -= this.InvInertia * Util.Cross(j, r);
    }

    internal void ApplyBias(Vector2 j, Vector2 r)
    {
      BiasVelocity += InvMass * j;
      BiasRotation -= InvInertia * Util.Cross(j, r);
    }
    #endregion

    #region Helper Functions
    /// <summary>
    /// Takes the new position and angle and applies to the AABB and fixtures.
    /// </summary>
    private void ApplyPositions()
    {
      this.Facing = Util.Polar(this.Angle);
      for (int i = 0; i < this.fixtures.Count; i++)
        this.fixtures[i].Apply(this.Position, this.Facing);
      this.UpdateAABB();
    }

    /// <summary>
    /// Builds the AABB by combining all the shape AABBs.
    /// </summary>
    private void UpdateAABB()
    {
      if (this.fixtures.Count == 1)
      {
        this.AABB = this.fixtures[0].Shape.AABB;
      }
      else
      {
        float top = Mathf.NegativeInfinity;
        float right = Mathf.NegativeInfinity;
        float bottom = Mathf.Infinity;
        float left = Mathf.Infinity;

        for (int i = 0; i < this.fixtures.Count; i++)
        {
          AABB aabb = this.fixtures[i].Shape.AABB;

          top = Mathf.Max(top, aabb.Top);
          right = Mathf.Max(right, aabb.Right);
          bottom = Mathf.Min(bottom, aabb.Bottom);
          left = Mathf.Min(left, aabb.Left);
        }

        this.AABB = new AABB(top, bottom, left, right);
      }
    }

    /// <summary>
    /// Computes forces and dynamics and applies them to position and angle.
    /// </summary>
    private void Integrate(float deltaTime)
    {
      // Apply damping
      this.LinearVelocity *= this.World.damping;
      this.AngularVelocity *= this.World.damping;

      // Calculate total force and torque
      Vector2 totalForce = (this.Force * this.InvMass);
      if (this.UseGravity == true)
        totalForce += this.World.gravity;
      float totalTorque = this.Torque * this.InvInertia;

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
      this.Position += dt * this.LinearVelocity + this.BiasVelocity;
      this.Angle += dt * this.AngularVelocity + this.BiasRotation;
    }

    private void ClearForces()
    {
      this.Force = Vector2.zero;
      this.Torque = 0.0f;

      this.BiasVelocity = Vector2.zero;
      this.BiasRotation = 0.0f;
    }

    private void ComputeDynamics()
    {
      float mass = 0.0f;
      float inertia = 0.0f;

      foreach (Fixture f in this.fixtures)
      {
        float curMass = f.ComputeMass();
        float curInertia = f.ComputeInertia(this.Facing);

        mass += curMass;
        inertia += curMass * curInertia;
      }

      if (mass == 0.0f)
      {
        this.IsStatic = true;
        this.InvMass = 0.0f;
        this.InvInertia = 0.0f;
      }
      else
      {
        this.IsStatic = false;
        this.InvMass = 1.0f / mass;
        this.InvInertia = 1.0f / inertia;
      }
    }
    #endregion
  }
}
