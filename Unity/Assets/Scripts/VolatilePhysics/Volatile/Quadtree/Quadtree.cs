using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

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
      internal BodyHandle listFirst;
      internal BodyHandle listLast;
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

      internal bool ListContains(BodyHandle entry)
      {
        for (var iter = this.listFirst; iter != null; iter = iter.Next)
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

    // Hashtable data
    protected int[] buckets;
    protected Node[] nodes;
    protected int count;
    protected int freeList;
    protected int freeCount;

    internal int HashCount
    {
      get { return this.count - this.freeCount; }
    }

    internal int HashCapacity
    {
      get { return this.nodes.Length; }
    }

    /// <summary>
    /// Blits the other quadtree onto this one.
    /// </summary>
    protected void ReceiveBlit(Quadtree other)
    {
      if (this.buckets.Length != other.buckets.Length)
        this.buckets = new int[other.buckets.Length];
      if (this.nodes.Length != other.nodes.Length)
        this.nodes = new Node[other.nodes.Length];

      Array.Copy(other.buckets, this.buckets, other.buckets.Length);
      Array.Copy(other.nodes, this.nodes, other.nodes.Length);

      this.count = other.count;
      this.freeList = other.freeList;
      this.freeCount = other.freeCount;
    }

    #region Node Functionality
    #endregion

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
    public void GizmoDraw(int time, bool drawGrid)
    {
      int key = this.HashFind(ROOT_KEY);
      this.GizmoDraw(time, ref this.nodes[key], drawGrid);
    }

    private void GizmoDraw(int time, bool drawGrid, Color boxColor)
    {
      int key = this.HashFind(ROOT_KEY);
      this.GizmoDraw(time, ref this.nodes[key], drawGrid, boxColor);
    }

    private void GizmoDraw(
      int time,
      ref Node node,
      bool drawGrid)
    {
      this.GizmoDraw(time, ref node, drawGrid, new Color(0f, 1f, 0f, 0.3f));
    }

    private void GizmoDraw(
      ref Node node,
      int time,
      Color boxColor)
    {
      Gizmos.color = new Color(1f, 1f, 1f, 1f);

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
    }

    private void GizmoDraw(
      int time,
      ref Node node,
      bool drawGrid,
      Color boxColor)
    {
      if (drawGrid == true)
      {
        this.GizmoDraw(ref node, time, boxColor);
      }

      if (node.hasChildren == true)
      {
        for (int i = 0; i < 4; i++)
        {
          int key = this.HashFind(node.ChildKey(i));
          this.GizmoDraw(time, ref this.nodes[key], drawGrid, boxColor);
        }
      }

      //for (var iter = node.ListFirst; iter != null; iter = iter.HistNext(time))
      //  iter.GizmoDraw(time);
    }
    #endregion
  }
}