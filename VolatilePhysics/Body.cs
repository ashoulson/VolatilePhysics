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
  public delegate bool BodyFilter(Body body);

  public class Body
  {
    public static bool Filter(Body body, BodyFilter filter)
    {
      return ((filter == null) || (filter.Invoke(body) == true));
    }

    public static Body CreateDynamic(
      Vector2 position,
      float radians,
      IEnumerable<Shape> shapesToAdd)
    {
      Body body = new Body(position, radians, shapesToAdd);
      body.ComputeDynamics();
      return body;
    }

    public static Body CreateStatic(
      Vector2 position,
      float radians,
      IEnumerable<Shape> shapesToAdd)
    {
      Body body = new Body(position, radians, shapesToAdd);
      body.SetStatic();
      return body;
    }

    /// <summary>
    /// For attaching arbitrary data to this body.
    /// </summary>
    public object UserData { get; set; }

    public World World { get; internal set; }
    public IList<Shape> Shapes { get { return this.shapes.AsReadOnly(); } }

    /// <summary>
    /// Number of shapes in the body.
    /// </summary>
    public int Count { get { return this.shapes.Count; } }

    public Vector2 LinearVelocity { get; set; }
    public float AngularVelocity { get; set; }

    public Vector2 Position { get; private set; }
    public Vector2 Force { get; private set; }

    public float Angle { get; private set; }
    public float Torque { get; private set; }

    public Vector2 Facing { get; private set; }
    public AABB AABB { get; private set; }

    public bool IsStatic { get; private set; }
    public float Mass { get; private set; }
    public float Inertia { get; private set; }
    public float InvMass { get; private set; }
    public float InvInertia { get; private set; }

    internal Vector2 BiasVelocity { get; private set; }
    internal float BiasRotation { get; private set; }

    internal List<Shape> shapes;
    private List<Fixture> fixtures;

    internal Volatile.History.BodyLogger bodyLogger = null;

    #region Force and Impulse Application
    public void AddTorque(float torque)
    {
      this.Torque += torque;
    }

    public void AddForce(Vector2 force)
    {
      this.Force += force;
    }

    public void AddForce(Vector2 force, Vector2 point)
    {
      this.Force += force;
      this.Torque += VolatileUtil.Cross(this.Position - point, force);
    }
    #endregion

    #region Position and Orientation
    public void SetWorld(Vector2 position, float radians)
    {
      this.Position = position;
      this.Angle = radians;
      this.ApplyPosition();
    }
    #endregion

    #region Tests
    /// <summary>
    /// Returns true iff an area overlaps with our AABB.
    /// </summary>
    public bool Query(AABB area)
    {
      return this.AABB.Intersect(area);
    }

    /// <summary>
    /// Checks if a point is contained in this body. 
    /// Begins with AABB checks.
    /// </summary>
    public bool Query(Vector2 point)
    {
      if (this.AABB.Query(point) == true)
        for (int i = 0; i < this.shapes.Count; i++)
          if (this.shapes[i].Query(point) == true)
            return true;
      return false;
    }

    /// <summary>
    /// Checks if a circle overlaps with this body. 
    /// Begins with AABB checks.
    /// </summary>
    public bool Query(Vector2 point, float radius)
    {
      if (this.AABB.Query(point, radius) == true)
        for (int i = 0; i < this.shapes.Count; i++)
          if (this.shapes[i].Query(point, radius) == true)
            return true;
      return false;
    }

    /// <summary>
    /// Performs a ray cast check on this body. 
    /// Begins with AABB checks.
    /// </summary>
    public bool RayCast(ref RayCast ray, ref RayResult result)
    {
      if (this.AABB.RayCast(ref ray) == true)
        for (int i = 0; i < this.shapes.Count; i++)
          if (this.shapes[i].RayCast(ref ray, ref result) == true)
            if (result.IsContained == true)
              return true;
      return result.IsValid;
    }

    /// <summary>
    /// Performs a circle cast check on this body. 
    /// Begins with AABB checks.
    /// </summary>
    public bool CircleCast(
      ref RayCast ray,
      float radius,
      ref RayResult result)
    {
      if (this.AABB.CircleCast(ref ray, radius) == true)
        for (int i = 0; i < this.shapes.Count; i++)
          if (this.shapes[i].CircleCast(ref ray, radius, ref result) == true)
            if (result.IsContained == true)
              return true;
      return result.IsValid;
    }
    #endregion

    private Body(
      Vector2 position, 
      float radians, 
      IEnumerable<Shape> shapesToAdd)
    {
      this.Position = position;
      this.Angle = radians;
      this.Facing = VolatileUtil.Polar(radians);

      this.shapes = new List<Shape>();
      this.fixtures = new List<Fixture>();

      foreach (Shape shape in shapesToAdd)
        this.AddShape(shape);
      this.ApplyPosition();
    }

    internal void Update()
    {
      this.Integrate();
      this.ApplyPosition();
    }

    #region Fixture/Shape Management
    /// <summary>
    /// Adds a shape, using a fixture to "pin" that shape to the body
    /// relative to its current position and rotation offset from the body.
    /// Any subsequent movement of the body will also move the shape.
    /// </summary>
    private void AddShape(Shape shape)
    {
      Fixture fixture = Fixture.FromWorldSpace(this, shape);
      this.shapes.Add(shape);
      this.fixtures.Add(fixture);
      shape.Body = this;
    }
    #endregion

    #region Collision
    internal bool CanCollide(Body other)
    {
      // Ignore self, static-static, and dynamic-dynamic collisions
      if ((this == other) || (this.IsStatic == other.IsStatic))
        return false;
      return true;
    }

    internal void ApplyImpulse(Vector2 j, Vector2 r)
    {
      this.LinearVelocity += this.InvMass * j;
      this.AngularVelocity -= this.InvInertia * VolatileUtil.Cross(j, r);
    }

    internal void ApplyBias(Vector2 j, Vector2 r)
    {
      this.BiasVelocity += this.InvMass * j;
      this.BiasRotation -= this.InvInertia * VolatileUtil.Cross(j, r);
    }
    #endregion

    #region Helper Functions
    /// <summary>
    /// Applies the current position and angle to shapes and the AABB.
    /// </summary>
    internal void ApplyPosition()
    {
      this.Facing = VolatileUtil.Polar(this.Angle);
      for (int i = 0; i < this.fixtures.Count; i++)
        this.fixtures[i].Apply(this.Position, this.Facing);
      this.UpdateAABB();
    }

    /// <summary>
    /// Builds the AABB by combining all the shape AABBs.
    /// </summary>
    private void UpdateAABB()
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

    /// <summary>
    /// Computes forces and dynamics and applies them to position and angle.
    /// </summary>
    private void Integrate()
    {
      // Apply damping
      this.LinearVelocity *= this.World.damping;
      this.AngularVelocity *= this.World.damping;

      // Calculate total force and torque
      Vector2 totalForce = this.Force * this.InvMass;
      float totalTorque = this.Torque * this.InvInertia;

      // See http://www.niksula.hut.fi/~hkankaan/Homepages/gravity.html
      this.IntegrateForces(totalForce, totalTorque, 0.5f);
      this.IntegrateVelocity();
      this.IntegrateForces(totalForce, totalTorque, 0.5f);

      this.ClearForces();
    }

    private void IntegrateForces(
      Vector2 force,
      float torque,
      float mult)
    {
      this.LinearVelocity += this.World.DeltaTime * force * mult;
      this.AngularVelocity -= this.World.DeltaTime * torque * mult;
    }

    private void IntegrateVelocity()
    {
      this.Position +=
        this.World.DeltaTime * this.LinearVelocity + this.BiasVelocity;
      this.Angle +=
        this.World.DeltaTime * this.AngularVelocity + this.BiasRotation;
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
      this.Mass = 0.0f;
      this.Inertia = 0.0f;

      for (int i = 0; i < this.fixtures.Count; i++)
      {
        Fixture fixture = this.fixtures[i];
        if (fixture.Shape.Density == 0.0f)
          continue;
        float curMass = fixture.ComputeMass();
        float curInertia = fixture.ComputeInertia();

        this.Mass += curMass;
        this.Inertia += curMass * curInertia;
      }

      if (Mathf.Approximately(this.Mass, 0.0f) == true)
      {
        Debug.LogWarning("Zero mass on dynamic body, setting to static");
        this.SetStatic();
      }
      else
      {
        this.InvMass = 1.0f / this.Mass;
        this.InvInertia = 1.0f / this.Inertia;
        this.IsStatic = false;
      }
    }

    private void SetStatic()
    {
      this.IsStatic = true;
      this.Mass = 0.0f;
      this.Inertia = 0.0f;
      this.InvMass = 0.0f;
      this.InvInertia = 0.0f;
    }
    #endregion

    #region Debug
    public void GizmoDraw(
      Color edgeColor,
      Color normalColor,
      Color bodyOriginColor,
      Color shapeOriginColor,
      Color bodyAabbColor,
      Color shapeAabbColor,
      float normalLength)
    {
      Color current = Gizmos.color;

      // Draw origin
      Gizmos.color = bodyOriginColor;
      Gizmos.DrawWireSphere(this.Position, 0.1f);

      // Draw facing
      Gizmos.color = normalColor;
      Gizmos.DrawLine(
        this.Position,
        this.Position + this.Facing * normalLength);

      this.AABB.GizmoDraw(bodyAabbColor);

      foreach (Shape shape in this.Shapes)
        shape.GizmoDraw(
          edgeColor,
          normalColor,
          shapeOriginColor,
          shapeAabbColor,
          normalLength);

      Gizmos.color = current;
    }
    #endregion
  }
}