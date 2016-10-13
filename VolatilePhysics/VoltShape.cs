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

#if UNITY
using UnityEngine;
#endif

namespace Volatile
{
  public abstract class VoltShape
    : IVoltPoolable<VoltShape>
  {
    #region Interface
    IVoltPool<VoltShape> IVoltPoolable<VoltShape>.Pool { get; set; }
    void IVoltPoolable<VoltShape>.Reset() { this.Reset(); }
    #endregion

    #region Static Methods
    internal static void OrderShapes(ref VoltShape sa, ref VoltShape sb)
    {
      if (sa.Type > sb.Type)
      {
        VoltShape temp = sa;
        sa = sb;
        sb = temp;
      }
    }
    #endregion

    public enum ShapeType
    {
      Circle,
      Polygon,
    }

#if DEBUG
    internal bool IsInitialized { get; set; }
#endif

    public abstract ShapeType Type { get; }

    /// <summary>
    /// For attaching arbitrary data to this shape.
    /// </summary>
    public object UserData { get; set; }
    public VoltBody Body { get; private set; }

    internal float Density { get; private set; }
    internal float Friction { get; private set; }
    internal float Restitution { get; private set; }

    /// <summary>
    /// The world-space bounding AABB for this shape.
    /// </summary>
    public VoltAABB AABB { get { return this.worldSpaceAABB; } }

    /// <summary>
    /// Total area of the shape.
    /// </summary>
    public float Area { get; protected set; }

    /// <summary>
    /// Total mass of the shape (area * density).
    /// </summary>
    public float Mass { get; protected set; }

    /// <summary>
    /// Total inertia of the shape relative to the body's origin.
    /// </summary>
    public float Inertia { get; protected set; }

    // Body-space bounding AABB for pre-checks during queries/casts
    internal VoltAABB worldSpaceAABB;
    internal VoltAABB bodySpaceAABB;

    #region Body-Related
    internal void AssignBody(VoltBody body)
    {
      this.Body = body;
      this.ComputeMetrics();
    }

    internal void OnBodyPositionUpdated()
    {
      this.ApplyBodyPosition();
    }
    #endregion

    #region Tests
    /// <summary>
    /// Checks if a point is contained in this shape. 
    /// Begins with an AABB check.
    /// </summary>
    internal bool QueryPoint(Vector2 bodySpacePoint)
    {
      // Queries and casts on shapes are always done in body space
      if (this.bodySpaceAABB.QueryPoint(bodySpacePoint))
        return this.ShapeQueryPoint(bodySpacePoint);
      return false;
    }

    /// <summary>
    /// Checks if a circle overlaps with this shape. 
    /// Begins with an AABB check.
    /// </summary>
    internal bool QueryCircle(Vector2 bodySpaceOrigin, float radius)
    {
      // Queries and casts on shapes are always done in body space
      if (this.bodySpaceAABB.QueryCircleApprox(bodySpaceOrigin, radius))
        return this.ShapeQueryCircle(bodySpaceOrigin, radius);
      return false;
    }

    /// <summary>
    /// Performs a raycast check on this shape. 
    /// Begins with an AABB check.
    /// </summary>
    internal bool RayCast(
      ref VoltRayCast bodySpaceRay, 
      ref VoltRayResult result)
    {
      // Queries and casts on shapes are always done in body space
      if (this.bodySpaceAABB.RayCast(ref bodySpaceRay))
        return this.ShapeRayCast(ref bodySpaceRay, ref result);
      return false;
    }

    /// <summary>
    /// Performs a circlecast check on this shape. 
    /// Begins with an AABB check.
    /// </summary>
    internal bool CircleCast(
      ref VoltRayCast bodySpaceRay, 
      float radius, 
      ref VoltRayResult result)
    {
      // Queries and casts on shapes are always done in body space
      if (this.bodySpaceAABB.CircleCastApprox(ref bodySpaceRay, radius))
        return this.ShapeCircleCast(ref bodySpaceRay, radius, ref result);
      return false;
    }
    #endregion

    protected void Initialize(
      float density, 
      float friction, 
      float restitution)
    {
      this.Density = density;
      this.Friction = friction;
      this.Restitution = restitution;

#if DEBUG
      this.IsInitialized = true;
#endif
    }

    protected virtual void Reset()
    {
#if DEBUG
      this.IsInitialized = false;
#endif

      this.UserData = null;
      this.Body = null;

      this.Density = 0.0f;
      this.Friction = 0.0f;
      this.Restitution = 0.0f;

      this.Area = 0.0f;
      this.Mass = 0.0f;
      this.Inertia = 0.0f;

      this.bodySpaceAABB = default(VoltAABB);
      this.worldSpaceAABB = default(VoltAABB);
    }

    #region Functionality Overrides
    protected abstract void ComputeMetrics();
    protected abstract void ApplyBodyPosition();
    #endregion

    #region Test Overrides
    protected abstract bool ShapeQueryPoint(
      Vector2 bodySpacePoint);

    protected abstract bool ShapeQueryCircle(
      Vector2 bodySpaceOrigin,
      float radius);

    protected abstract bool ShapeRayCast(
      ref VoltRayCast bodySpaceRay,
      ref VoltRayResult result);

    protected abstract bool ShapeCircleCast(
      ref VoltRayCast bodySpaceRay,
      float radius,
      ref VoltRayResult result);
    #endregion

    #region Debug
#if UNITY && DEBUG
    public abstract void GizmoDraw(
      Color edgeColor,
      Color normalColor,
      Color originColor,
      Color aabbColor,
      float normalLength);
#endif
    #endregion
  }
}