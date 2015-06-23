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
  internal class Quadtree
  {
    /// <summary>
    /// There's a lot of C-style stuff going on with this struct. Within the
    /// base Quadtree class itself, though, none of Node's fields should be
    /// written to. MutableQuadtree contains all the methods that write to
    /// any of Node's fields. This violates most of C#'s struct guidelines.
    /// </summary>
    protected struct Node
    {
      // Cell bounds for checking (world space)
      internal AABB aabb;

      internal bool hasChildren;
      internal short totalContained;
      internal byte depth;

      // Used for storing the contained bodies
      internal ShapeHandle listFirst;
      internal ShapeHandle listLast;
      internal int listCount;

      // Index of next entry for hash table, -1 if last
      internal int hashNext;

      // Identifier key for this cell, according to Morton order
      // Also serves as the key for hashing
      internal int key;

      #region Helpers
      internal bool IsValid
      {
        get { return this.key != INVALID_KEY; }
      }

      internal int ParentKey
      {
        get { return this.key >> 2; }
      }

      public bool IsRoot 
      { 
        get { return this.key == ROOT_KEY; } 
      }

      internal bool ShouldMerge
      {
        get { return this.IsRoot == false && this.totalContained == 0; }
      }

      internal int ChildKey(int which)
      {
        return (this.key << 2) + which;
      }

      internal bool ListContains(ShapeHandle entry)
      {
        for (var iter = this.listFirst; iter != null; iter = iter.next)
          if (iter == entry)
            return true;
        return false;
      }

      internal bool AABBContains(AABB aabb)
      {
        return this.aabb.Contains(aabb);
      }

      internal bool AABBCouldFit(AABB aabb, float scaleW, float scaleH)
      {
        return this.aabb.CouldFit(aabb, scaleW, scaleH);
      }
      #endregion
    }

    // Unhashed keys for nodes
    protected const int INVALID_KEY = 0;
    protected const int ROOT_KEY = 1;

    // Hashing constants
    protected const int HASH_MASK = 0x7FFFFFFF;

    // Quadtree data
    protected int time;

    // Hashtable data
    protected int[] buckets;
    protected Node[] nodes;
    protected int count;
    protected int freeList;
    protected int freeCount;

    internal bool IsValid 
    {
      get { return this.time != Config.INVALID_TIME; } 
    }

    internal int HashCount
    {
      get { return this.count - this.freeCount; }
    }

    internal int HashCapacity
    {
      get { return this.nodes.Length; }
    }

    public Quadtree()
    {
      this.time = Config.INVALID_TIME;
      this.buckets = new int[0];
      this.nodes = new Node[0];
      this.count = 0;
      this.freeList = -1;
      this.freeCount = 0;
    }

    /// <summary>
    /// Blits the other quadtree onto this one.
    /// </summary>
    internal void ReceiveBlit(Quadtree other)
    {
      Debug.Assert(other.IsValid == true);

      if (this.buckets.Length != other.buckets.Length)
        this.buckets = new int[other.buckets.Length];
      if (this.nodes.Length != other.nodes.Length)
        this.nodes = new Node[other.nodes.Length];

      Array.Copy(other.buckets, this.buckets, other.buckets.Length);
      Array.Copy(other.nodes, this.nodes, other.nodes.Length);

      this.count = other.count;
      this.freeList = other.freeList;
      this.freeCount = other.freeCount;
      this.time = other.time;
    }

    /// <summary>
    /// Returns all shapes in a given cell or higher in the hierarchy.
    /// </summary>
    internal IEnumerable<ShapeHandle> GetShapesInCell(int cellKey)
    {
      int nodeIndex = this.HashFind(cellKey);
      Debug.Assert(nodeIndex != -1);

      ShapeHandle shape = this.nodes[nodeIndex].listFirst;
      for (; shape != null; shape = shape.Next(this.time))
        yield return shape;

      if (cellKey != ROOT_KEY)
      {
        int parentKey = this.nodes[nodeIndex].ParentKey;
        foreach (ShapeHandle parentShape in this.GetShapesInCell(parentKey))
          yield return parentShape;
      }
    }

    #region Hashing Functionality
    /// <summary>
    /// Takes in a non-hashed key and returns the hash array index.
    /// </summary>
    protected int HashFind(int key)
    {
      if (this.buckets != null)
      {
        int bucket = this.GetBucket(key);
        for (int i = this.buckets[bucket]; i >= 0; i = this.nodes[i].hashNext)
          if (this.nodes[i].key == key)
            return i;
      }
      return -1;
    }

    protected int GetBucket(int key)
    {
      return GetBucket(key, this.buckets.Length);
    }

    protected int GetBucket(int key, int numBuckets)
    {
      // Surprise! We don't actually hash the number, maybe later if needed
      int hashedKey = key;
      return hashedKey % numBuckets;
    }
    #endregion

    #region Debug
    public void GizmoDraw(
      Color gridColor,
      Color boxColor)
    {
      if (this.nodes.Length > 0)
      {
        int key = this.HashFind(ROOT_KEY);
        this.DrawRecursive(ref this.nodes[key], gridColor, boxColor);
      }
    }

    private void DrawRecursive(
      ref Node node,
      Color gridColor,
      Color boxColor)
    {
      this.DrawBox(ref node, time, gridColor, boxColor);

      if (node.hasChildren == true)
      {
        for (int i = 0; i < 4; i++)
        {
          int key = this.HashFind(node.ChildKey(i));
          this.DrawRecursive(ref this.nodes[key], gridColor, boxColor);
        }
      }

      this.DrawShapes(ref node);
    }

    private void DrawShapes(ref Node node)
    {
      ShapeHandle shape = node.listFirst;
      for (; shape != null; shape = shape.Next(this.time))
        shape.GizmoDraw(this.time);
    }

    private void DrawBox(
      ref Node node,
      int time,
      Color gridColor,
      Color boxColor)
    {
      Color current = Gizmos.color;
      Gizmos.color = gridColor;

      Vector2 topLeft = node.aabb.TopLeft;
      Vector2 topRight = node.aabb.TopRight;
      Vector2 bottomLeft = node.aabb.BottomLeft;
      Vector2 bottomRight = node.aabb.BottomRight;

      Gizmos.DrawLine(topLeft, topRight);
      Gizmos.DrawLine(topRight, bottomRight);
      Gizmos.DrawLine(bottomRight, bottomLeft);
      Gizmos.DrawLine(bottomLeft, topLeft);

      //UnityEditor.Handles.Label(
      //  new Vector3(center.x, 0.0f, center.y), 
      //  node.TotalContained + 
      //  "\n" + 
      //  System.Convert.ToString(node.Key, 16));

      if (node.listCount > 0)
      {
        Gizmos.color = boxColor;
        Gizmos.DrawCube(node.aabb.Center, node.aabb.Extent * 2.0f);
      }

      Gizmos.color = current;
    }
    #endregion
  }
}