using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Volatile.History
{
  internal partial class Quadtree
  {
    // Unhashed keys
    private const int INVALID_KEY = 0;
    private const int ROOT_KEY = 1;

    private struct Node
    {
      internal BodyHandle ListFirst { get { return this.listFirst; } }
      internal int ListCount { get { return this.listCount; } }

      // Cell bounds for checking (world space)
      private AABB aabb;

      private bool hasChildren;
      private short totalContained;
      private byte depth;

      // Used for storing the contained bodies
      private BodyHandle listFirst;
      private BodyHandle listLast;
      private int listCount;

      // Index of next entry for hash table, -1 if last
      private int hashNext;

      // Identifier key for this cell, according to Morton order
      // Also serves as the key for hashing
      private int key;

      internal void Set(
        byte depth,
        bool hasChildren,
        AABB aabb,
        int totalContained)
      {
        this.depth = depth;
        this.hasChildren = false;
        this.aabb = aabb;
        this.totalContained = 0;
        this.ListClear();
      }

      #region Geometry
      public bool IsInBounds(AABB aabb)
      {
        return this.aabb.Contains(aabb);
      }

      public bool CouldFit(AABB aabb, float scaleW, float scaleH)
      {
        return this.aabb.CouldFit(aabb, scaleW, scaleH);
      }
      #endregion

      #region Hashtable-related Functions
      public int HashNext
      {
        get { return this.hashNext; }
        set { this.hashNext = value; }
      }

      public bool HashIsValid { get { return this.Key != INVALID_KEY; } }
      #endregion

      #region Tree-related Functions
      public int Key
      {
        get { return this.key; }
        set { this.key = value; }
      }

      public short TotalContained
      {
        get { return this.totalContained; }
        set { this.totalContained = value; }
      }

      public bool HasChildren
      {
        get { return this.hasChildren; }
        set { this.hasChildren = value; }
      }

      public bool IsRoot { get { return this.Key == ROOT_KEY; } }
      public bool HasBodies { get { return this.listFirst != null; } }

      public byte Depth
      {
        get { return this.depth; }
      }

      public bool ShouldMerge
      {
        get { return this.IsRoot == false && this.TotalContained == 0; }
      }

      public int ParentKey
      {
        get { Debug.Assert(this.IsRoot == false); return this.Key >> 2; }
      }

      public int GetChildKey(int which)
      {
        return (this.Key << 2) + which;
      }

      public bool ShouldSplit(float maxBodiesPerCell, float maxDepth)
      {
        return this.ListCount > maxBodiesPerCell && this.Depth < maxDepth;
      }

      public void Split(Quadtree tree)
      {
        // Set the hasChildren first because the array might copy during the
        // process of adding children, and the node reference could be invalidated
        this.HasChildren = true;

        byte newDepth = (byte)(this.Depth + 1);
        Vector2 center = this.aabb.Center;

        AABB topLeft = this.aabb.ComputeTopLeft(center);
        AABB topRight = this.aabb.ComputeTopRight(center);
        AABB bottomLeft = this.aabb.ComputeBottomLeft(center);
        AABB bottomRight = this.aabb.ComputeBottomRight(center);

        tree.HashAdd(this.GetChildKey(0), newDepth, topLeft);
        tree.HashAdd(this.GetChildKey(1), newDepth, topRight);
        tree.HashAdd(this.GetChildKey(2), newDepth, bottomLeft);
        tree.HashAdd(this.GetChildKey(3), newDepth, bottomRight);
      }
      #endregion

      #region Linked List Functions
      public void ListAdd(BodyHandle node)
      {
        node.Next = this.listFirst;
        if (this.listFirst != null)
          this.listFirst.Prev = node;
        this.listFirst = node;
        if (this.listLast == null)
          this.listLast = node;
        node.Prev = null;
        this.listCount++;
      }

      public void ListRemove(BodyHandle history)
      {
        BodyHandle nodeNext = history.Next;
        BodyHandle nodePrev = history.Prev;
        if (this.listFirst == history)
          this.listFirst = nodeNext;
        if (nodeNext != null)
          nodeNext.Prev = nodePrev;
        if (nodePrev != null)
          nodePrev.Next = nodeNext;
        this.listCount--;
      }

      public void ListClear()
      {
        // Note: This function very intentionally doesn't do any clearing
        //   of the actual nodes (preserving the chain between them). We abuse
        //   the hell out of this in Insert() for in-place reassignment, so
        //   don't do anything to that assumption.

        this.listFirst = null;
        this.listLast = null;
        this.listCount = 0;
      }
      #endregion

      #region Debug
      public void GizmoDraw(
        int time,
        Color boxColor)
      {
        Gizmos.color = new Color(1f, 1f, 1f, 1f);

        Vector2 topLeft = this.aabb.TopLeft;
        Vector2 topRight = this.aabb.TopRight;
        Vector2 bottomLeft = this.aabb.BottomLeft;
        Vector2 bottomRight = this.aabb.BottomRight;

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        //UnityEditor.Handles.Label(
        //  new Vector3(center.x, 0.0f, center.y), 
        //  node.TotalContained + 
        //  "\n" + 
        //  System.Convert.ToString(node.Key, 16));

        if (this.HasBodies == true)
        {
          Gizmos.color = boxColor;
          Gizmos.DrawCube(this.aabb.Center, this.aabb.Extent * 2.0f);
        }
      }
      #endregion
    }
  }
}