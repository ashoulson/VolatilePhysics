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

namespace Volatile
{
  public struct VoltAABB
  {
    #region Static Methods
    public static VoltAABB CreateExpanded(VoltAABB aabb, float expansionAmount)
    {
      return new VoltAABB(
        aabb.Top + expansionAmount,
        aabb.Bottom - expansionAmount,
        aabb.Left - expansionAmount,
        aabb.Right + expansionAmount);
    }

    public static VoltAABB CreateMerged(VoltAABB aabb1, VoltAABB aabb2)
    {
      return new VoltAABB(
        VoltMath.Max(aabb1.Top, aabb2.Top),
        VoltMath.Min(aabb1.Bottom, aabb2.Bottom),
        VoltMath.Min(aabb1.Left, aabb2.Left),
        VoltMath.Max(aabb1.Right, aabb2.Right));
    }

    public static VoltAABB CreateSwept(VoltAABB source, VoltVec2 vector)
    {
      float top = source.Top;
      float bottom = source.Bottom;
      float left = source.Left;
      float right = source.Right;

      if (vector.X < 0.0f)
        left += vector.X;
      else
        right += vector.X;

      if (vector.Y < 0.0f)
        bottom += vector.Y;
      else
        top += vector.Y;

      return new VoltAABB(top, bottom, left, right);
    }

    /// <summary>
    /// A cheap ray test that requires some precomputed information.
    /// Adapted from: http://www.cs.utah.edu/~awilliam/box/box.pdf
    /// </summary>
    private static bool RayCast(
      ref VoltRayCast ray,
      float top,
      float bottom,
      float left,
      float right)
    {
      float txmin =
        ((ray.signX ? right : left) - ray.origin.X) *
        ray.invDirection.X;
      float txmax =
        ((ray.signX ? left : right) - ray.origin.X) *
        ray.invDirection.X;

      float tymin =
        ((ray.signY ? top : bottom) - ray.origin.Y) *
        ray.invDirection.Y;
      float tymax =
        ((ray.signY ? bottom : top) - ray.origin.Y) *
        ray.invDirection.Y;

      if ((txmin > tymax) || (tymin > txmax))
        return false;
      if (tymin > txmin)
        txmin = tymin;
      if (tymax < txmax)
        txmax = tymax;
      return (txmax > 0.0f) && (txmin < ray.distance);
    }
    #endregion

    public VoltVec2 TopLeft 
    { 
      get { return new VoltVec2(this.Left, this.Top); } 
    }

    public VoltVec2 TopRight 
    { 
      get { return new VoltVec2(this.Right, this.Top); } 
    }

    public VoltVec2 BottomLeft 
    { 
      get { return new VoltVec2(this.Left, this.Bottom); } 
    }

    public VoltVec2 BottomRight 
    { 
      get { return new VoltVec2(this.Right, this.Bottom); } 
    }

    public float Top { get; }
    public float Bottom { get; }
    public float Left { get; }
    public float Right { get; }

    public float Width { get { return this.Right - this.Left; } }
    public float Height { get { return this.Top - this.Bottom; } }

    public float Area { get { return this.Width * this.Height; } }
    public float Perimeter 
    { 
      get { return 2.0f * (this.Width + this.Height); } 
    }

    public VoltVec2 Center { get { return this.ComputeCenter(); } }
    public VoltVec2 Extent 
    { 
      get { return new VoltVec2(this.Width * 0.5f, this.Height * 0.5f); } 
    }

    #region Tests
    /// <summary>
    /// Performs a point test on the AABB.
    /// </summary>
    public bool QueryPoint(VoltVec2 point)
    {
      return 
        this.Left <= point.X && 
        this.Right >= point.X &&
        this.Bottom <= point.Y &&
        this.Top >= point.Y;
    }

    /// <summary>
    /// Note: This doesn't take rounded edges into account.
    /// </summary>
    public bool QueryCircleApprox(VoltVec2 origin, float radius)
    {
      return
        (this.Left - radius) <= origin.X &&
        (this.Right + radius) >= origin.X &&
        (this.Bottom - radius) <= origin.Y &&
        (this.Top + radius) >= origin.Y;
    }

    public bool RayCast(ref VoltRayCast ray)
    {
      return VoltAABB.RayCast(
        ref ray, 
        this.Top, 
        this.Bottom, 
        this.Left, 
        this.Right);
    }

    /// <summary>
    /// Note: This doesn't take rounded edges into account.
    /// </summary>
    public bool CircleCastApprox(ref VoltRayCast ray, float radius)
    {
      return VoltAABB.RayCast(
        ref ray,
        this.Top + radius,
        this.Bottom - radius,
        this.Left - radius,
        this.Right + radius);
    }

    public bool Intersect(VoltAABB other)
    {
      bool outside =
        this.Right <= other.Left ||
        this.Left >= other.Right ||
        this.Bottom >= other.Top ||
        this.Top <= other.Bottom;
      return (outside == false);
    }

    public bool Contains(VoltAABB other)
    {
      return
        this.Top >= other.Top &&
        this.Bottom <= other.Bottom &&
        this.Right >= other.Right &&
        this.Left <= other.Left;
    }
    #endregion

    public VoltAABB(float top, float bottom, float left, float right)
    {
      this.Top = top;
      this.Bottom = bottom;
      this.Left = left;
      this.Right = right;
    }

    public VoltAABB(VoltVec2 center, VoltVec2 extents)
    {
      VoltVec2 topRight = center + extents;
      VoltVec2 bottomLeft = center - extents;

      this.Top = topRight.Y;
      this.Right = topRight.X;
      this.Bottom = bottomLeft.Y;
      this.Left = bottomLeft.X;
    }

    public VoltAABB(VoltVec2 center, float radius)
      : this (center, new VoltVec2(radius, radius))
    {
    }

    public VoltAABB ComputeTopLeft(VoltVec2 center)
    {
      return new VoltAABB(this.Top, center.Y, this.Left, center.X);
    }

    public VoltAABB ComputeTopRight(VoltVec2 center)
    {
      return new VoltAABB(this.Top, center.Y, center.X, this.Right);
    }

    public VoltAABB ComputeBottomLeft(VoltVec2 center)
    {
      return new VoltAABB(center.Y, this.Bottom, this.Left, center.X);
    }

    public VoltAABB ComputeBottomRight(VoltVec2 center)
    {
      return new VoltAABB(center.Y, this.Bottom, center.X, this.Right);
    }

    private VoltVec2 ComputeCenter()
    {
      return new VoltVec2(
        (this.Width * 0.5f) + this.Left, 
        (this.Height * 0.5f) + this.Bottom);
    }
  }
}
