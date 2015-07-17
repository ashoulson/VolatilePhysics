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
  public class Body
  {
    /// <summary>
    /// User token, for attaching arbitrary data to this body.
    /// </summary>
    public object Token { get; set; }

    public World World { get; internal set; }
    public bool UseGravity { get; set; }

    public bool IsStatic 
    { 
      get
      {
        return this.isStatic;
      }
      set 
      {
        if (Mathf.Approximately(this.mass, 0.0f) == false)
          this.isStatic = value;
      }
    }

    public IList<Shape> Shapes 
    { 
      get { return this.shapes.AsReadOnly(); } 
    }

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

    public float Mass 
    {
      get { return this.IsStatic ? 0.0f : this.mass; } 
    }

    public float Inertia 
    {
      get { return this.IsStatic ? 0.0f : this.inertia; } 
    }

    public float InvMass 
    {
      get { return this.IsStatic ? 0.0f : this.invMass; } 
    }

    public float InvInertia 
    {
      get { return this.IsStatic ? 0.0f : this.invInertia; } 
    }

    internal Vector2 BiasVelocity { get; private set; }
    internal float BiasRotation { get; private set; }

    private bool isStatic = false;
    private float mass;
    private float inertia;
    private float invMass;
    private float invInertia;

    private List<Shape> shapes;
    private List<Fixture> fixtures;
    private Dictionary<Shape, Fixture> shapeToFixture;

    internal Volatile.History.BodyLogger logger = null;

    #region Force and Impulse Application
    public void AddForce(Vector2 force)
    {
      this.Force += force;
    }

    public void AddTorque(float torque)
    {
      this.Torque += torque;
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
      this.ApplyPosition();
    }

    public void SetWorld(float radians)
    {
      this.Angle = radians;
      this.ApplyPosition();
    }

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
    public bool Query(
      Vector2 point,
      Func<Shape, bool> filter = null)
    {
      if (this.AABB.Query(point) == true)
      {
        for (int i = 0; i < this.shapes.Count; i++)
        {
          Shape shape = this.shapes[i];
          if (filter == null || filter(shape) == true)
          {
            if (shape.Query(point) == true)
            {
              return true;
            }
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Checks if a circle overlaps with this body. 
    /// Begins with AABB checks.
    /// </summary>
    public bool Query(
      Vector2 point, 
      float radius,
      Func<Shape, bool> filter = null)
    {
      if (this.AABB.Query(point, radius) == true)
      {
        for (int i = 0; i < this.shapes.Count; i++)
        {
          Shape shape = this.shapes[i];
          if (filter == null || filter(shape) == true)
          {
            if (shape.Query(point, radius) == true)
            {
              return true;
            }
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Returns the minimum distance between this body and the point.
    /// </summary>
    public bool MinDistance(
      Vector2 point,
      float maxDistance,
      out float minDistance,
      Func<Shape, bool> filter = null)
    {
      minDistance = float.PositiveInfinity;
      bool result = false;

      if (this.AABB.Query(point, maxDistance) == true)
      {
        for (int i = 0; i < this.shapes.Count; i++)
        {
          Shape shape = this.shapes[i];
          if (filter == null || filter(shape) == true)
          {
            float distance;
            result |= shape.MinDistance(point, maxDistance, out distance);
            if (distance < minDistance)
              minDistance = distance;
          }
        }
      }

      return result;
    }

    /// <summary>
    /// Performs a ray cast check on this body. 
    /// Begins with AABB checks.
    /// </summary>
    public bool RayCast(
      ref RayCast ray, 
      ref RayResult result, 
      Func<Shape, bool> filter = null)
    {
      bool hit = false;
      if (this.AABB.RayCast(ref ray) == true)
      {
        for (int i = 0; i < this.shapes.Count; i++)
        {
          Shape shape = this.shapes[i];
          if (filter == null || filter(shape) == true)
          {
            if (shape.RayCast(ref ray, ref result) == true)
            {
              if (result.IsContained == true)
              {
                return true;
              }
              hit = true;
            }
          }
        }
      }
      return hit;
    }

    /// <summary>
    /// Performs a circle cast check on this body. 
    /// Begins with AABB checks.
    /// </summary>
    public bool CircleCast(
      ref RayCast ray,
      float radius,
      ref RayResult result,
      Func<Shape, bool> filter = null)
    {
      bool hit = false;
      if (this.AABB.CircleCast(ref ray, radius) == true)
      {
        for (int i = 0; i < this.shapes.Count; i++)
        {
          Shape shape = this.shapes[i];
          if (filter == null || filter(shape) == true)
          {
            if (shape.CircleCast(ref ray, radius, ref result) == true)
            {
              if (result.IsContained == true)
              {
                return true;
              }
              hit = true;
            }
          }
        }
      }
      return hit;
    }
    #endregion

    public Body(IEnumerable<Shape> shapesToAdd)
      : this(Vector2.zero, 0.0f, shapesToAdd) { }

    public Body(params Shape[] shapesToAdd)
      : this(Vector2.zero, 0.0f, shapesToAdd) { }

    public Body(Vector2 position, IEnumerable<Shape> shapesToAdd)
      : this(position, 0.0f, shapesToAdd) { }

    public Body(Vector2 position, params Shape[] shapesToAdd)
      : this(position, 0.0f, shapesToAdd) { }

    public Body(Vector2 position, float radians, params Shape[] shapesToAdd)
      : this(position, radians, (IEnumerable<Shape>)shapesToAdd) { }

    public Body(
      Vector2 position, 
      float radians, 
      IEnumerable<Shape> shapesToAdd)
    {
      this.Position = position;
      this.Angle = radians;
      this.Facing = VolatileUtil.Polar(radians);

      this.UseGravity = false;

      this.isStatic = false;
      this.shapes = new List<Shape>();
      this.fixtures = new List<Fixture>();
      this.shapeToFixture = new Dictionary<Shape, Fixture>();

      foreach (Shape shape in shapesToAdd)
        this.AddShape(shape);
      this.FinalizeShapes();
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
      this.shapeToFixture.Add(shape, fixture);
      shape.Body = this;
    }

    /// <summary>
    /// This function should be called after all shapes have been added.
    /// </summary>
    private void FinalizeShapes()
    {
      this.ComputeDynamics();
      this.ApplyPosition();
    }
    #endregion

    #region Collision
    internal bool CanCollide(Body other, bool allowDynamic)
    {
      // TODO: Layers, flags, etc.
      if (this == other)
        return false;
      if (this.IsStatic && other.IsStatic)
        return false;
      if (allowDynamic == false)
        if (this.IsStatic == false && other.IsStatic == false)
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
    /// Use this function for resetting shapes if they were moved for tests.
    /// </summary>
    internal void ApplyPosition()
    {
      this.Facing = VolatileUtil.Polar(this.Angle);
      for (int i = 0; i < this.fixtures.Count; i++)
        this.fixtures[i].Apply(this.Position, this.Facing);
      this.UpdateAABB();
    }

    /// <summary>
    /// Returns a shape to its original body-relative position. Note that
    /// this function does not recompute body facing or the AABB.
    /// </summary>
    internal void ResetShape(Shape shape)
    {
      this.shapeToFixture[shape].Apply(this.Position, this.Facing);
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
      if (this.UseGravity == true)
        totalForce += this.World.gravity;
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
      float mass = 0.0f;
      float inertia = 0.0f;

      for (int i = 0; i < this.fixtures.Count; i++)
      {
        Fixture fixture = this.fixtures[i];
        if (fixture.Shape.Density == 0.0f)
          continue;
        float curMass = fixture.ComputeMass();
        float curInertia = fixture.ComputeInertia(this.Facing);

        mass += curMass;
        inertia += curMass * curInertia;
      }

      if (Mathf.Approximately(mass, 0.0f) == true)
      {
        this.isStatic = true;
        this.mass = 0.0f;
        this.inertia = 0.0f;
        this.invMass = 0.0f;
        this.invInertia = 0.0f;
      }
      else
      {
        this.isStatic = false;
        this.mass = mass;
        this.inertia = inertia;
        this.invMass = 1.0f / mass;
        this.invInertia = 1.0f / inertia;
      }
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
