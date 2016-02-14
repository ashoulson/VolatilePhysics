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
  public delegate bool BodyFilter(Body body);

  public class Body
  {
    /// <summary>
    /// A predefined filter that disallows collisions between dynamic bodies.
    /// </summary>
    public static bool DisallowDynamic(Body a, Body b)
    {
      return
        (a != null) &&
        (b != null) &&
        (a.IsStatic || b.IsStatic);
    }

    #region History
    /// <summary>
    /// A stored historical image of a past body state, used for historical
    /// queries and raycasts. Rather than actually rolling the body back to
    /// its old position (expensive), we transform the ray into the body's
    /// local space based on the body's old position/axis. Then all casts
    /// on shapes use the local-space ray (this applies both for current-
    /// time and past-time raycasts and point queries).
    /// </summary>
    private struct State
    {
      internal int frame;
      internal AABB aabb;
      internal Vector2 position;
      internal Vector2 facing;

      internal void Store(ref State other, int validatedFrame)
      {
        this.frame = validatedFrame;
        this.aabb = other.aabb;
        this.position = other.position;
        this.facing = other.facing;
      }

      #region World-Space to Body-Space Transformations
      internal Vector2 WorldToBodyPoint(Vector2 vector)
      {
        return (vector - this.position).InvRotate(this.facing);
      }

      internal Vector2 WorldToBodyDirection(Vector2 vector)
      {
        return vector.InvRotate(this.facing);
      }

      internal RayCast WorldToBodyRay(ref RayCast rayCast)
      {
        return new RayCast(
          this.WorldToBodyPoint(rayCast.origin),
          this.WorldToBodyDirection(rayCast.direction),
          rayCast.distance);
      }
      #endregion

      #region Body-Space to World-Space Transformations
      internal Vector2 BodyToWorldPoint(Vector2 vector)
      {
        return vector.Rotate(this.facing) + this.position;
      }


      internal Vector2 BodyToWorldDirection(Vector2 vector)
      {
        return vector.Rotate(this.facing);
      }

      internal Axis BodyToWorldAxis(Axis axis)
      {
        Vector2 normal = axis.Normal.Rotate(this.facing);
        float width = Vector2.Dot(normal, this.position) + axis.Width;
        return new Axis(normal, width);
      }
      #endregion
    }

    private State[] historyStates;
    private State currentState;

    /// <summary>
    /// Initializes the buffer for storing past body states/spaces.
    /// </summary>
    internal void StartHistory(int length)
    {
      Debug.Assert(
        length >= 0,
        "StartHistory(): Length value must be >= 0");

      if (this.IsStatic == false)
      {
        this.historyStates = new State[length];
        for (int i = 0; i < length; i++)
          this.historyStates[i].frame = History.CURRENT_FRAME;
      }
    }

    /// <summary>
    /// Stores a snapshot of this body's current state/space to a frame.
    /// </summary>
    internal void StoreState(int frame)
    {
      Debug.Assert(
        frame >= 0, 
        "StoreState(): Frame value must be >= 0");

      if (this.historyStates != null)
      {
        int length = this.historyStates.Length;
        if (length > 0)
        {
          this.historyStates[frame % length].Store(
            ref this.currentState,
            frame);
          return;
        }
      }
    }

    /// <summary>
    /// Retrieves a snapshot of the body's state/space at a frame.
    /// Logs an error and defaults to the current state if it can't be found.
    /// </summary>
    private State GetState(int frame)
    {
      if (frame == History.CURRENT_FRAME)
        return this.currentState;

      Debug.Assert(
        frame >= 0, 
        "GetState(): Frame value must be -1, or >= 0");

      if (this.historyStates != null)
      {
        int length = this.historyStates.Length;
        if (length > 0)
        {
          State image = this.historyStates[frame % length];
          if (image.frame == frame)
            return image;

          Debug.LogWarning(
            "Wrong frame history access! Expected " + 
            frame +
            ", but got " + image.frame);
        }
      }

      return this.currentState;
    }
    #endregion

    public static bool Filter(Body body, BodyFilter filter)
    {
      return ((filter == null) || (filter.Invoke(body) == true));
    }

    #region Factory Functions
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
    #endregion

    /// <summary>
    /// For attaching arbitrary data to this body.
    /// </summary>
    public object UserData { get; set; }

    public World World { get; private set; }
    public IList<Shape> Shapes { get { return this.shapes.AsReadOnly(); } }

    /// <summary>
    /// Number of shapes in the body.
    /// </summary>
    public int Count { get { return this.shapes.Count; } }

    // Some basic properties are stored in an internal mutable
    // record to avoid code redundancy when performing conversions
    public Vector2 Position
    {
      get { return this.currentState.position; }
      private set { this.currentState.position = value; }
    }

    public Vector2 Facing
    {
      get { return this.currentState.facing; }
      private set { this.currentState.facing = value; }
    }

    public AABB AABB
    {
      get { return this.currentState.aabb; }
      private set { this.currentState.aabb = value; }
    }

    public float Angle { get; private set; }

    public Vector2 LinearVelocity { get; set; }
    public float AngularVelocity { get; set; }

    public Vector2 Force { get; private set; }
    public float Torque { get; private set; }

    public bool IsStatic { get; private set; }
    public float Mass { get; private set; }
    public float Inertia { get; private set; }
    public float InvMass { get; private set; }
    public float InvInertia { get; private set; }

    /// <summary>
    /// If we're doing historical queries or tests, the body may have since
    /// been removed from the world.
    /// </summary>
    public bool IsInWorld { get { return this.World != null; } }

    internal Vector2 BiasVelocity { get; private set; }
    internal float BiasRotation { get; private set; }

    internal List<Shape> shapes;

    #region Manipulation
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

    public void Set(Vector2 position, float radians)
    {
      this.Position = position;
      this.Angle = radians;
      this.Facing = VolatileUtil.Polar(radians);
      this.OnPositionUpdated();
    }
    #endregion

    #region Tests
    /// <summary>
    /// Checks if a point is contained in this body. 
    /// Begins with AABB checks.
    /// </summary>
    internal bool QueryPoint(
      Vector2 point, 
      int validatedFrame = History.CURRENT_FRAME)
    {
      // AABB check done in world space (because it keeps changing)
      State record = this.GetState(validatedFrame);
      if (record.aabb.QueryPoint(point) == false)
        return false;

      // Actual query on shapes done in body space
      Vector2 bodySpacePoint = record.WorldToBodyPoint(point);
      for (int i = 0; i < this.shapes.Count; i++)
        if (this.shapes[i].QueryPoint(bodySpacePoint))
          return true;
      return false;
    }

    /// <summary>
    /// Checks if a circle overlaps with this body. 
    /// Begins with AABB checks.
    /// </summary>
    internal bool QueryCircle(
      Vector2 origin, 
      float radius,
      int validatedFrame = History.CURRENT_FRAME)
    {
      // AABB check done in world space (because it keeps changing)
      State record = this.GetState(validatedFrame);
      if (record.aabb.QueryCircleApproximate(origin, radius) == false)
        return false;

      // Actual query on shapes done in body space
      Vector2 bodySpaceOrigin = record.WorldToBodyPoint(origin);
      for (int i = 0; i < this.shapes.Count; i++)
        if (this.shapes[i].QueryCircle(bodySpaceOrigin, radius))
          return true;
      return false;
    }

    /// <summary>
    /// Performs a ray cast check on this body. 
    /// Begins with AABB checks.
    /// </summary>
    internal bool RayCast(
      ref RayCast ray, 
      ref RayResult result,
      int validatedFrame = History.CURRENT_FRAME)
    {
      State record = this.GetState(validatedFrame);
      if (record.aabb.RayCast(ref ray) == false)
        return false;

      // Actual tests on shapes done in body space
      RayCast bodySpaceRay = record.WorldToBodyRay(ref ray);
      for (int i = 0; i < this.shapes.Count; i++)
        if (this.shapes[i].RayCast(ref bodySpaceRay, ref result))
          if (result.IsContained)
            return true;

      // We need to convert the results back to world space to be any use
      // (Doesn't matter if we were contained since there will be no normal)
      if (result.Body == this)
        result.normal = record.BodyToWorldDirection(result.normal);
      return result.IsValid;
    }

    /// <summary>
    /// Performs a circle cast check on this body. 
    /// Begins with AABB checks.
    /// </summary>
    internal bool CircleCast(
      ref RayCast ray,
      float radius,
      ref RayResult result,
      int validatedFrame = History.CURRENT_FRAME)
    {
      State record = this.GetState(validatedFrame);
      if (record.aabb.CircleCast(ref ray, radius) == false)
        return false;

      // Actual tests on shapes done in body space
      RayCast bodySpaceRay = record.WorldToBodyRay(ref ray);
      for (int i = 0; i < this.shapes.Count; i++)
        if (this.shapes[i].CircleCast(ref bodySpaceRay, radius, ref result))
          if (result.IsContained)
            return true;

      // We need to convert the results back to world space to be any use
      // (Doesn't matter if we were contained since there will be no normal)
      if (result.Body == this)
        result.normal = record.BodyToWorldDirection(result.normal);
      return result.IsValid;
    }
    #endregion

    private Body(
      Vector2 position, 
      float radians, 
      IEnumerable<Shape> shapesToAdd)
    {
      this.historyStates = null;
      this.currentState.frame = History.CURRENT_FRAME;
      this.Position = position;
      this.Angle = radians;
      this.Facing = VolatileUtil.Polar(radians);

      this.shapes = new List<Shape>();
      foreach (Shape shape in shapesToAdd)
        this.AddShape(shape);
      this.OnPositionUpdated();
    }

    internal void Update()
    {
      this.Integrate();
      this.OnPositionUpdated();
    }

    internal void AssignWorld(World world)
    {
      this.World = world;
    }

    #region Collision
    internal bool CanCollide(Body other)
    {
      // Ignore self and static-static collisions
      if ((this == other) || (this.IsStatic && other.IsStatic))
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

    #region Transformation Shortcuts
    internal Vector2 WorldToBodyPointCurrent(Vector2 vector)
    {
      return this.currentState.WorldToBodyPoint(vector);
    }

    internal Vector2 BodyToWorldPointCurrent(Vector2 vector)
    {
      return this.currentState.BodyToWorldPoint(vector);
    }

    internal Axis BodyToWorldAxisCurrent(Axis axis)
    {
      return this.currentState.BodyToWorldAxis(axis);
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Adds a shape and notifies it that it has a new body.
    /// </summary>
    private void AddShape(Shape shape)
    {
      this.shapes.Add(shape);
      shape.AssignBody(this);
    }

    /// <summary>
    /// Applies the current position and angle to shapes and the AABB.
    /// </summary>
    private void OnPositionUpdated()
    {
      for (int i = 0; i < this.shapes.Count; i++)
        this.shapes[i].OnBodyPositionUpdated();
      this.UpdateAABB();
    }

    /// <summary>
    /// Builds the AABB by combining all the shape AABBs.
    /// </summary>
    private void UpdateAABB()
    {
      float top = float.NegativeInfinity;
      float right = float.NegativeInfinity;
      float bottom = float.PositiveInfinity;
      float left = float.PositiveInfinity;

      for (int i = 0; i < this.shapes.Count; i++)
      {
        AABB aabb = this.shapes[i].AABB;
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
      this.LinearVelocity *= this.World.Damping;
      this.AngularVelocity *= this.World.Damping;

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
      this.Facing = VolatileUtil.Polar(this.Angle);
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

      for (int i = 0; i < this.shapes.Count; i++)
      {
        Shape shape = this.shapes[i];
        if (shape.Density == 0.0f)
          continue;
        float curMass = shape.Mass;
        float curInertia = shape.Inertia;

        this.Mass += curMass;
        this.Inertia += curMass * curInertia;
      }

      if (this.Mass < Config.MINIMUM_DYNAMIC_MASS)
      {
        Debug.LogWarning(
          "mass < MINIMUM_DYNAMIC_MASS, setting to static");
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
#if VOLATILE_UNITY
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

    public void GizmoDrawHistory(Color aabbColor)
    {
      Color current = Gizmos.color;

      if (this.historyStates != null)
      {
        for (int i = 0; i < this.historyStates.Length; i++)
        {
          State state = this.historyStates[i];
          if (state.frame != History.CURRENT_FRAME)
          {
            state.aabb.GizmoDraw(aabbColor);
          }
        }
      }

      Gizmos.color = current;
    }
#endif
    #endregion
  }
}