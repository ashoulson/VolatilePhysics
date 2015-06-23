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
  public struct AABB
  {
    public Vector2 TopLeft 
    { 
      get { return new Vector2(this.left, this.top); } 
    }

    public Vector2 TopRight 
    { 
      get { return new Vector2(this.right, this.top); } 
    }

    public Vector2 BottomLeft 
    { 
      get { return new Vector2(this.left, this.bottom); } 
    }

    public Vector2 BottomRight 
    { 
      get { return new Vector2(this.right, this.bottom); } 
    }

    public float Top { get { return this.top; } }
    public float Bottom { get { return this.bottom; } }
    public float Left { get { return this.left; } }
    public float Right { get { return this.right; } }

    public float Width { get { return this.Right - this.Left; } }
    public float Height { get { return this.Top - this.Bottom; } }

    public Vector2 Center { get { return this.ComputeCenter(); } }
    public Vector2 Extent 
    { 
      get { return new Vector2(this.Width * 0.5f, this.Height * 0.5f); } 
    }

    private readonly float top;
    private readonly float bottom;
    private readonly float left;
    private readonly float right;

    #region Tests
    /// <summary>
    /// A cheap ray test that requires some precomputed information.
    /// Adapted from: http://www.cs.utah.edu/~awilliam/box/box.pdf
    /// </summary>
    public bool Raycast(ref BatchRay ray, float distance = float.MaxValue)
    {
      float txmin =
        ((ray.signX ? this.right : this.left) - ray.origin.x) *
        ray.invDirection.x;
      float txmax =
        ((ray.signX ? this.left : this.right) - ray.origin.x) *
        ray.invDirection.x;

      float tymin =
        ((ray.signY ? this.top : this.bottom) - ray.origin.y) *
        ray.invDirection.y;
      float tymax =
        ((ray.signY ? this.bottom : this.top) - ray.origin.y) *
        ray.invDirection.y;

      if ((txmin > tymax) || (tymin > txmax))
        return false;
      if (tymin > txmin)
        txmin = tymin;
      if (tymax < txmax)
        txmax = tymax;
      return (txmax > 0.0f) && (txmin < distance);
    }

    public bool Intersect(AABB other)
    {
      bool outside =
        this.right <= other.left ||
        this.left >= other.right ||
        this.bottom >= other.top ||
        this.top <= other.bottom;
      return (outside == false);
    }

    public bool Contains(AABB other)
    {
      return
        this.top >= other.Top &&
        this.bottom <= other.Bottom &&
        this.right >= other.right &&
        this.left <= other.left;
    }

    /// <summary>
    /// Returns true iff the given AABB could fit into this AABB, optionally
    /// scaling width and height by a given value. Takes only width and height
    /// into account, not position.
    /// </summary>
    public bool CouldFit(AABB other, float scaleW = 1.0f, float scaleH = 1.0f)
    {
      float thisWidth = (this.right - this.left) * scaleW;
      float thisHeight = (this.top - this.bottom) * scaleH;
      float otherWidth = other.right - other.left;
      float otherHeight = other.top - other.bottom;

      return (thisWidth >= otherWidth && thisHeight >= otherHeight);
    }
    #endregion

    public AABB(float top, float bottom, float left, float right)
    {
      this.top = top;
      this.bottom = bottom;
      this.left = left;
      this.right = right;
    }

    public AABB(Vector2 center, Vector2 extents)
    {
      Vector2 topRight = center + extents;
      Vector2 bottomLeft = center - extents;

      this.top = topRight.y;
      this.right = topRight.x;
      this.bottom = bottomLeft.y;
      this.left = bottomLeft.x;
    }

    public AABB ComputeTopLeft(Vector2 center)
    {
      return new AABB(this.top, center.y, this.left, center.x);
    }

    public AABB ComputeTopRight(Vector2 center)
    {
      return new AABB(this.top, center.y, center.x, this.right);
    }

    public AABB ComputeBottomLeft(Vector2 center)
    {
      return new AABB(center.y, this.bottom, this.left, center.x);
    }

    public AABB ComputeBottomRight(Vector2 center)
    {
      return new AABB(center.y, this.bottom, center.x, this.right);
    }

    private Vector2 ComputeCenter()
    {
      return new Vector2(
        (this.Width * 0.5f) + this.left, 
        (this.Height * 0.5f) + this.bottom);
    }

    #region Debug
    public void GizmoDraw(Color aabbColor)
    {
      Color current = Gizmos.color;

      Vector2 A = new Vector2(this.Left, this.Top);
      Vector2 B = new Vector2(this.Right, this.Top);
      Vector2 C = new Vector2(this.Right, this.Bottom);
      Vector2 D = new Vector2(this.Left, this.Bottom);

      Gizmos.color = aabbColor;
      Gizmos.DrawLine(A, B);
      Gizmos.DrawLine(B, C);
      Gizmos.DrawLine(C, D);
      Gizmos.DrawLine(D, A);

      Gizmos.color = current;
    }
    #endregion
  }
}
