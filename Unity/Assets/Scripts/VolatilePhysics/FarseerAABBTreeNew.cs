/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
* 
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using System.Collections.Generic;

using UnityEngine;
using Volatile;

namespace FarseerPhysics.Collision
{
  /// <summary>
  /// A dynamic tree arranges data in a binary tree to accelerate
  /// queries such as volume queries and ray casts. Leafs are proxies
  /// with an AABB. In the tree we expand the proxy AABB by Settings.b2_fatAABBFactor
  /// so that the proxy AABB is bigger than the client object. This allows the client
  /// object to move by small amounts without triggering a tree update.
  ///
  /// Nodes are pooled and relocatable, so we use node indices rather than pointers.
  /// </summary>
  public class DynamicTreeNew
  {
    internal const int NULL_NODE_ID = -1;

    /// <summary>
    /// A node in the dynamic tree. The client does not interact with this directly.
    /// </summary>
    private class TreeNode
    {
      /// <summary>
      /// Enlarged AABB
      /// </summary>
      internal AABB aabb;

      internal int left;
      internal int right;

      internal int height;
      internal int parentOrNext;
      internal Body body;

      internal bool IsLeaf
      {
        get { return left == DynamicTreeNew.NULL_NODE_ID; }
      }
    }

    //private Stack<int> _raycastStack = new Stack<int>(256);
    //private Stack<int> _queryStack = new Stack<int>(256);

    private int freeListStartId;
    private int rootId;

    private int nodeCapacity;
    private int nodeCount;
    private TreeNode[] nodes;

    /// <summary>
    /// Constructing the tree initializes the node pool.
    /// </summary>
    public DynamicTreeNew()
    {
      this.rootId = NULL_NODE_ID;

      this.nodeCapacity = 16;
      this.nodeCount = 0;
      this.nodes = new TreeNode[this.nodeCapacity];

      // Build a linked list for the free list.
      for (int i = 0; i < this.nodeCapacity - 1; ++i)
      {
        this.nodes[i] = new TreeNode();
        this.nodes[i].parentOrNext = i + 1;
        this.nodes[i].height = 1;
      }
      this.nodes[this.nodeCapacity - 1] = new TreeNode();
      this.nodes[this.nodeCapacity - 1].parentOrNext = NULL_NODE_ID;
      this.nodes[this.nodeCapacity - 1].height = 1;
      this.freeListStartId = 0;
    }

    /// <summary>
    /// Compute the height of the binary tree in O(N) time. Should not be called often.
    /// </summary>
    public int Height
    {
      get
      {
        if (this.rootId == NULL_NODE_ID)
        {
          return 0;
        }

        return this.nodes[this.rootId].height;
      }
    }

    /// <summary>
    /// Get the ratio of the sum of the node areas to the root area.
    /// </summary>
    public float AreaRatio
    {
      get
      {
        if (this.rootId == NULL_NODE_ID)
        {
          return 0.0f;
        }

        TreeNode root = this.nodes[this.rootId];
        float rootArea = root.aabb.Perimeter;

        float totalArea = 0.0f;
        for (int i = 0; i < this.nodeCapacity; ++i)
        {
          TreeNode node = this.nodes[i];
          if (node.height < 0)
          {
            // Free node in pool
            continue;
          }

          totalArea += node.aabb.Perimeter;
        }

        return totalArea / rootArea;
      }
    }

    /// <summary>
    /// Get the maximum balance of an node in the tree. The balance is the difference
    /// in height of the two children of a node.
    /// </summary>
    public int MaxBalance
    {
      get
      {
        int maxBalance = 0;
        for (int i = 0; i < this.nodeCapacity; ++i)
        {
          TreeNode node = this.nodes[i];
          if (node.height <= 1)
          {
            continue;
          }

          Debug.Assert(node.IsLeaf == false);

          int child1 = node.left;
          int child2 = node.right;
          int balance = Math.Abs(this.nodes[child2].height - this.nodes[child1].height);
          maxBalance = Math.Max(maxBalance, balance);
        }

        return maxBalance;
      }
    }

    /// <summary>
    /// Create a proxy in the tree as a leaf node. We return the index
    /// of the node instead of a pointer so that we can grow
    /// the node pool.        
    /// /// </summary>
    /// <param name="aabb">The aabb.</param>
    /// <param name="userData">The user data.</param>
    /// <returns>Index of the created proxy</returns>
    public int AddProxy(AABB aabb, Body userData)
    {
      int proxyId = AllocateNode();

      // Fatten the aabb.
      this.nodes[proxyId].aabb = AABB.CreateExpanded(aabb, 0.2f);
      this.nodes[proxyId].body = userData;
      this.nodes[proxyId].height = 0;

      InsertLeaf(proxyId);

      return proxyId;
    }

    /// <summary>
    /// Destroy a proxy. This asserts if the id is invalid.
    /// </summary>
    /// <param name="proxyId">The proxy id.</param>
    public void RemoveProxy(int proxyId)
    {
      Debug.Assert(0 <= proxyId && proxyId < this.nodeCapacity);
      Debug.Assert(this.nodes[proxyId].IsLeaf);

      RemoveLeaf(proxyId);
      FreeNode(proxyId);
    }

    /// <summary>
    /// Move a proxy with a swepted AABB. If the proxy has moved outside of its fattened AABB,
    /// then the proxy is removed from the tree and re-inserted. Otherwise
    /// the function returns immediately.
    /// </summary>
    /// <param name="proxyId">The proxy id.</param>
    /// <param name="aabb">The aabb.</param>
    /// <param name="displacement">The displacement.</param>
    /// <returns>true if the proxy was re-inserted.</returns>
    public bool MoveProxy(int proxyId, AABB aabb)
    {
      Debug.Assert((0 <= proxyId) && (proxyId < this.nodeCapacity));
      Debug.Assert(this.nodes[proxyId].IsLeaf);

      if (this.nodes[proxyId].aabb.Contains(aabb))
        return false;

      this.RemoveLeaf(proxyId);
      this.nodes[proxyId].aabb = AABB.CreateExpanded(aabb, 0.2f);
      this.InsertLeaf(proxyId);

      return true;
    }

    ///// <summary>
    ///// Query an AABB for overlapping proxies. The callback class
    ///// is called for each proxy that overlaps the supplied AABB.
    ///// </summary>
    ///// <param name="callback">The callback.</param>
    ///// <param name="aabb">The aabb.</param>
    //public void Query(Func<int, bool> callback, ref AABB aabb)
    //{
    //  _queryStack.Clear();
    //  _queryStack.Push(this.rootId);

    //  while (_queryStack.Count > 0)
    //  {
    //    int nodeId = _queryStack.Pop();
    //    if (nodeId == NullNode)
    //    {
    //      continue;
    //    }

    //    TreeNode node = this.nodes[nodeId];

    //    if (AABB.TestOverlap(ref node.AABB, ref aabb))
    //    {
    //      if (node.IsLeaf)
    //      {
    //        bool proceed = callback(nodeId);
    //        if (proceed == false)
    //        {
    //          return;
    //        }
    //      }
    //      else
    //      {
    //        _queryStack.Push(node.Child1);
    //        _queryStack.Push(node.Child2);
    //      }
    //    }
    //  }
    //}

    ///// <summary>
    ///// Ray-cast against the proxies in the tree. This relies on the callback
    ///// to perform a exact ray-cast in the case were the proxy contains a Shape.
    ///// The callback also performs the any collision filtering. This has performance
    ///// roughly equal to k * log(n), where k is the number of collisions and n is the
    ///// number of proxies in the tree.
    ///// </summary>
    ///// <param name="callback">A callback class that is called for each proxy that is hit by the ray.</param>
    ///// <param name="input">The ray-cast input data. The ray extends from p1 to p1 + maxFraction * (p2 - p1).</param>
    //public void RayCast(Func<RayCastInput, int, float> callback, ref RayCastInput input)
    //{
    //  Vector2 p1 = input.Point1;
    //  Vector2 p2 = input.Point2;
    //  Vector2 r = p2 - p1;
    //  Debug.Assert(r.LengthSquared() > 0.0f);
    //  r.Normalize();

    //  // v is perpendicular to the segment.
    //  Vector2 absV = MathUtils.Abs(new Vector2(-r.Y, r.X)); //FPE: Inlined the 'v' variable

    //  // Separating axis for segment (Gino, p80).
    //  // |dot(v, p1 - c)| > dot(|v|, h)

    //  float maxFraction = input.MaxFraction;

    //  // Build a bounding box for the segment.
    //  AABB segmentAABB = new AABB();
    //  {
    //    Vector2 t = p1 + maxFraction * (p2 - p1);
    //    Vector2.Min(ref p1, ref t, out segmentAABB.LowerBound);
    //    Vector2.Max(ref p1, ref t, out segmentAABB.UpperBound);
    //  }

    //  _raycastStack.Clear();
    //  _raycastStack.Push(this.rootId);

    //  while (_raycastStack.Count > 0)
    //  {
    //    int nodeId = _raycastStack.Pop();
    //    if (nodeId == NullNode)
    //    {
    //      continue;
    //    }

    //    TreeNode node = this.nodes[nodeId];

    //    if (AABB.TestOverlap(ref node.AABB, ref segmentAABB) == false)
    //    {
    //      continue;
    //    }

    //    // Separating axis for segment (Gino, p80).
    //    // |dot(v, p1 - c)| > dot(|v|, h)
    //    Vector2 c = node.AABB.Center;
    //    Vector2 h = node.AABB.Extents;
    //    float separation = Math.Abs(Vector2.Dot(new Vector2(-r.Y, r.X), p1 - c)) - Vector2.Dot(absV, h);
    //    if (separation > 0.0f)
    //    {
    //      continue;
    //    }

    //    if (node.IsLeaf)
    //    {
    //      RayCastInput subInput;
    //      subInput.Point1 = input.Point1;
    //      subInput.Point2 = input.Point2;
    //      subInput.MaxFraction = maxFraction;

    //      float value = callback(subInput, nodeId);

    //      if (value == 0.0f)
    //      {
    //        // the client has terminated the raycast.
    //        return;
    //      }

    //      if (value > 0.0f)
    //      {
    //        // Update segment bounding box.
    //        maxFraction = value;
    //        Vector2 t = p1 + maxFraction * (p2 - p1);
    //        segmentAABB.LowerBound = Vector2.Min(p1, t);
    //        segmentAABB.UpperBound = Vector2.Max(p1, t);
    //      }
    //    }
    //    else
    //    {
    //      _raycastStack.Push(node.Child1);
    //      _raycastStack.Push(node.Child2);
    //    }
    //  }
    //}

    private int AllocateNode()
    {
      // Expand the node pool as needed.
      if (this.freeListStartId == NULL_NODE_ID)
      {
        Debug.Assert(this.nodeCount == this.nodeCapacity);

        // The free list is empty. Rebuild a bigger pool.
        TreeNode[] oldNodes = this.nodes;
        this.nodeCapacity *= 2;
        this.nodes = new TreeNode[this.nodeCapacity];
        Array.Copy(oldNodes, this.nodes, this.nodeCount);

        // Build a linked list for the free list. The parent
        // pointer becomes the "next" pointer.
        for (int i = this.nodeCount; i < this.nodeCapacity - 1; ++i)
        {
          this.nodes[i] = new TreeNode();
          this.nodes[i].parentOrNext = i + 1;
          this.nodes[i].height = -1;
        }
        this.nodes[this.nodeCapacity - 1] = new TreeNode();
        this.nodes[this.nodeCapacity - 1].parentOrNext = NULL_NODE_ID;
        this.nodes[this.nodeCapacity - 1].height = -1;
        this.freeListStartId = this.nodeCount;
      }

      // Peel a node off the free list.
      int nodeId = this.freeListStartId;
      this.freeListStartId = this.nodes[nodeId].parentOrNext;
      this.nodes[nodeId].parentOrNext = NULL_NODE_ID;
      this.nodes[nodeId].left = NULL_NODE_ID;
      this.nodes[nodeId].right = NULL_NODE_ID;
      this.nodes[nodeId].height = 0;
      this.nodes[nodeId].body = null;
      ++this.nodeCount;
      return nodeId;
    }

    private void FreeNode(int nodeId)
    {
      Debug.Assert(0 <= nodeId && nodeId < this.nodeCapacity);
      Debug.Assert(0 < this.nodeCount);
      this.nodes[nodeId].parentOrNext = this.freeListStartId;
      this.nodes[nodeId].height = -1;
      this.freeListStartId = nodeId;
      --this.nodeCount;
    }

    private void InsertLeaf(int leaf)
    {
      if (this.rootId == NULL_NODE_ID)
      {
        this.rootId = leaf;
        this.nodes[this.rootId].parentOrNext = NULL_NODE_ID;
        return;
      }

      // Find the best sibling for this node
      AABB leafAABB = this.nodes[leaf].aabb;
      int index = this.rootId;
      while (this.nodes[index].IsLeaf == false)
      {
        int child1 = this.nodes[index].left;
        int child2 = this.nodes[index].right;

        float area = this.nodes[index].aabb.Perimeter;

        AABB combinedAABB = new AABB();
        combinedAABB = AABB.CreateMerged(this.nodes[index].aabb, leafAABB);
        float combinedArea = combinedAABB.Perimeter;

        // Cost of creating a new parent for this node and the new leaf
        float cost = 2.0f * combinedArea;

        // Minimum cost of pushing the leaf further down the tree
        float inheritanceCost = 2.0f * (combinedArea - area);

        // Cost of descending into child1
        float cost1;
        if (this.nodes[child1].IsLeaf)
        {
          AABB aabb = new AABB();
          aabb = AABB.CreateMerged(leafAABB, this.nodes[child1].aabb);
          cost1 = aabb.Perimeter + inheritanceCost;
        }
        else
        {
          AABB aabb = new AABB();
          aabb = AABB.CreateMerged(leafAABB, this.nodes[child1].aabb);
          float oldArea = this.nodes[child1].aabb.Perimeter;
          float newArea = aabb.Perimeter;
          cost1 = (newArea - oldArea) + inheritanceCost;
        }

        // Cost of descending into child2
        float cost2;
        if (this.nodes[child2].IsLeaf)
        {
          AABB aabb = new AABB();
          aabb = AABB.CreateMerged(leafAABB, this.nodes[child2].aabb);
          cost2 = aabb.Perimeter + inheritanceCost;
        }
        else
        {
          AABB aabb = new AABB();
          aabb = AABB.CreateMerged(leafAABB, this.nodes[child2].aabb);
          float oldArea = this.nodes[child2].aabb.Perimeter;
          float newArea = aabb.Perimeter;
          cost2 = newArea - oldArea + inheritanceCost;
        }

        // Descend according to the minimum cost.
        if (cost < cost1 && cost1 < cost2)
        {
          break;
        }

        // Descend
        if (cost1 < cost2)
        {
          index = child1;
        }
        else
        {
          index = child2;
        }
      }

      int sibling = index;

      // Create a new parent.
      int oldParent = this.nodes[sibling].parentOrNext;
      int newParent = AllocateNode();
      this.nodes[newParent].parentOrNext = oldParent;
      this.nodes[newParent].body = null;
      this.nodes[newParent].aabb = AABB.CreateMerged(leafAABB, this.nodes[sibling].aabb);
      this.nodes[newParent].height = this.nodes[sibling].height + 1;

      if (oldParent != NULL_NODE_ID)
      {
        // The sibling was not the root.
        if (this.nodes[oldParent].left == sibling)
        {
          this.nodes[oldParent].left = newParent;
        }
        else
        {
          this.nodes[oldParent].right = newParent;
        }

        this.nodes[newParent].left = sibling;
        this.nodes[newParent].right = leaf;
        this.nodes[sibling].parentOrNext = newParent;
        this.nodes[leaf].parentOrNext = newParent;
      }
      else
      {
        // The sibling was the root.
        this.nodes[newParent].left = sibling;
        this.nodes[newParent].right = leaf;
        this.nodes[sibling].parentOrNext = newParent;
        this.nodes[leaf].parentOrNext = newParent;
        this.rootId = newParent;
      }

      // Walk back up the tree fixing heights and AABBs
      index = this.nodes[leaf].parentOrNext;
      while (index != NULL_NODE_ID)
      {
        index = Balance(index);

        int child1 = this.nodes[index].left;
        int child2 = this.nodes[index].right;

        Debug.Assert(child1 != NULL_NODE_ID);
        Debug.Assert(child2 != NULL_NODE_ID);

        this.nodes[index].height = 1 + Math.Max(this.nodes[child1].height, this.nodes[child2].height);
        this.nodes[index].aabb = AABB.CreateMerged(this.nodes[child1].aabb, this.nodes[child2].aabb);

        index = this.nodes[index].parentOrNext;
      }

      //Validate();
    }

    private void RemoveLeaf(int leaf)
    {
      if (leaf == this.rootId)
      {
        this.rootId = NULL_NODE_ID;
        return;
      }

      int parent = this.nodes[leaf].parentOrNext;
      int grandParent = this.nodes[parent].parentOrNext;
      int sibling;
      if (this.nodes[parent].left == leaf)
      {
        sibling = this.nodes[parent].right;
      }
      else
      {
        sibling = this.nodes[parent].left;
      }

      if (grandParent != NULL_NODE_ID)
      {
        // Destroy parent and connect sibling to grandParent.
        if (this.nodes[grandParent].left == parent)
        {
          this.nodes[grandParent].left = sibling;
        }
        else
        {
          this.nodes[grandParent].right = sibling;
        }
        this.nodes[sibling].parentOrNext = grandParent;
        FreeNode(parent);

        // Adjust ancestor bounds.
        int index = grandParent;
        while (index != NULL_NODE_ID)
        {
          index = Balance(index);

          int child1 = this.nodes[index].left;
          int child2 = this.nodes[index].right;

          this.nodes[index].aabb = AABB.CreateMerged(this.nodes[child1].aabb, this.nodes[child2].aabb);
          this.nodes[index].height = 1 + Math.Max(this.nodes[child1].height, this.nodes[child2].height);

          index = this.nodes[index].parentOrNext;
        }
      }
      else
      {
        this.rootId = sibling;
        this.nodes[sibling].parentOrNext = NULL_NODE_ID;
        FreeNode(parent);
      }

      //Validate();
    }

    /// <summary>
    /// Perform a left or right rotation if node A is imbalanced.
    /// </summary>
    /// <param name="iA"></param>
    /// <returns>the new root index.</returns>
    private int Balance(int iA)
    {
      Debug.Assert(iA != NULL_NODE_ID);

      TreeNode A = this.nodes[iA];
      if (A.IsLeaf || A.height < 2)
      {
        return iA;
      }

      int iB = A.left;
      int iC = A.right;
      Debug.Assert(0 <= iB && iB < this.nodeCapacity);
      Debug.Assert(0 <= iC && iC < this.nodeCapacity);

      TreeNode B = this.nodes[iB];
      TreeNode C = this.nodes[iC];

      int balance = C.height - B.height;

      // Rotate C up
      if (balance > 1)
      {
        int iF = C.left;
        int iG = C.right;
        TreeNode F = this.nodes[iF];
        TreeNode G = this.nodes[iG];
        Debug.Assert(0 <= iF && iF < this.nodeCapacity);
        Debug.Assert(0 <= iG && iG < this.nodeCapacity);

        // Swap A and C
        C.left = iA;
        C.parentOrNext = A.parentOrNext;
        A.parentOrNext = iC;

        // A's old parent should point to C
        if (C.parentOrNext != NULL_NODE_ID)
        {
          if (this.nodes[C.parentOrNext].left == iA)
          {
            this.nodes[C.parentOrNext].left = iC;
          }
          else
          {
            Debug.Assert(this.nodes[C.parentOrNext].right == iA);
            this.nodes[C.parentOrNext].right = iC;
          }
        }
        else
        {
          this.rootId = iC;
        }

        // Rotate
        if (F.height > G.height)
        {
          C.right = iF;
          A.right = iG;
          G.parentOrNext = iA;
          A.aabb = AABB.CreateMerged(B.aabb, G.aabb);
          C.aabb = AABB.CreateMerged(A.aabb, F.aabb);

          A.height = 1 + Math.Max(B.height, G.height);
          C.height = 1 + Math.Max(A.height, F.height);
        }
        else
        {
          C.right = iG;
          A.right = iF;
          F.parentOrNext = iA;
          A.aabb = AABB.CreateMerged(B.aabb, F.aabb);
          C.aabb = AABB.CreateMerged(A.aabb, G.aabb);

          A.height = 1 + Math.Max(B.height, F.height);
          C.height = 1 + Math.Max(A.height, G.height);
        }

        return iC;
      }

      // Rotate B up
      if (balance < -1)
      {
        int iD = B.left;
        int iE = B.right;
        TreeNode D = this.nodes[iD];
        TreeNode E = this.nodes[iE];
        Debug.Assert(0 <= iD && iD < this.nodeCapacity);
        Debug.Assert(0 <= iE && iE < this.nodeCapacity);

        // Swap A and B
        B.left = iA;
        B.parentOrNext = A.parentOrNext;
        A.parentOrNext = iB;

        // A's old parent should point to B
        if (B.parentOrNext != NULL_NODE_ID)
        {
          if (this.nodes[B.parentOrNext].left == iA)
          {
            this.nodes[B.parentOrNext].left = iB;
          }
          else
          {
            Debug.Assert(this.nodes[B.parentOrNext].right == iA);
            this.nodes[B.parentOrNext].right = iB;
          }
        }
        else
        {
          this.rootId = iB;
        }

        // Rotate
        if (D.height > E.height)
        {
          B.right = iD;
          A.left = iE;
          E.parentOrNext = iA;
          A.aabb = AABB.CreateMerged(C.aabb, E.aabb);
          B.aabb = AABB.CreateMerged(A.aabb, D.aabb);

          A.height = 1 + Math.Max(C.height, E.height);
          B.height = 1 + Math.Max(A.height, D.height);
        }
        else
        {
          B.right = iE;
          A.left = iD;
          D.parentOrNext = iA;
          A.aabb = AABB.CreateMerged(C.aabb, D.aabb);
          B.aabb = AABB.CreateMerged(A.aabb, E.aabb);

          A.height = 1 + Math.Max(C.height, D.height);
          B.height = 1 + Math.Max(A.height, E.height);
        }

        return iB;
      }

      return iA;
    }

    /// <summary>
    /// Compute the height of a sub-tree.
    /// </summary>
    /// <param name="nodeId">The node id to use as parent.</param>
    /// <returns>The height of the tree.</returns>
    public int ComputeHeight(int nodeId)
    {
      Debug.Assert(0 <= nodeId && nodeId < this.nodeCapacity);
      TreeNode node = this.nodes[nodeId];

      if (node.IsLeaf)
      {
        return 0;
      }

      int height1 = ComputeHeight(node.left);
      int height2 = ComputeHeight(node.right);
      return 1 + Math.Max(height1, height2);
    }

    /// <summary>
    /// Compute the height of the entire tree.
    /// </summary>
    /// <returns>The height of the tree.</returns>
    public int ComputeHeight()
    {
      int height = ComputeHeight(this.rootId);
      return height;
    }

    public void ValidateStructure(int index)
    {
      if (index == NULL_NODE_ID)
      {
        return;
      }

      if (index == this.rootId)
      {
        Debug.Assert(this.nodes[index].parentOrNext == NULL_NODE_ID);
      }

      TreeNode node = this.nodes[index];

      int child1 = node.left;
      int child2 = node.right;

      if (node.IsLeaf)
      {
        Debug.Assert(child1 == NULL_NODE_ID);
        Debug.Assert(child2 == NULL_NODE_ID);
        Debug.Assert(node.height == 0);
        return;
      }

      Debug.Assert(0 <= child1 && child1 < this.nodeCapacity);
      Debug.Assert(0 <= child2 && child2 < this.nodeCapacity);

      Debug.Assert(this.nodes[child1].parentOrNext == index);
      Debug.Assert(this.nodes[child2].parentOrNext == index);

      ValidateStructure(child1);
      ValidateStructure(child2);
    }

    public void ValidateMetrics(int index)
    {
      if (index == NULL_NODE_ID)
      {
        return;
      }

      TreeNode node = this.nodes[index];

      int child1 = node.left;
      int child2 = node.right;

      if (node.IsLeaf)
      {
        Debug.Assert(child1 == NULL_NODE_ID);
        Debug.Assert(child2 == NULL_NODE_ID);
        Debug.Assert(node.height == 0);
        return;
      }

      Debug.Assert(0 <= child1 && child1 < this.nodeCapacity);
      Debug.Assert(0 <= child2 && child2 < this.nodeCapacity);

      int height1 = this.nodes[child1].height;
      int height2 = this.nodes[child2].height;
      int height = 1 + Math.Max(height1, height2);
      Debug.Assert(node.height == height);

      AABB AABB = new AABB();
      AABB = AABB.CreateMerged(this.nodes[child1].aabb, this.nodes[child2].aabb);

      Debug.Assert(AABB.Top == node.aabb.Top);
      Debug.Assert(AABB.Bottom == node.aabb.Bottom);
      Debug.Assert(AABB.Left == node.aabb.Left);
      Debug.Assert(AABB.Right == node.aabb.Right);

      ValidateMetrics(child1);
      ValidateMetrics(child2);
    }

    /// <summary>
    /// Validate this tree. For testing.
    /// </summary>
    public void Validate()
    {
      ValidateStructure(this.rootId);
      ValidateMetrics(this.rootId);

      int freeCount = 0;
      int freeIndex = this.freeListStartId;
      while (freeIndex != NULL_NODE_ID)
      {
        Debug.Assert(0 <= freeIndex && freeIndex < this.nodeCapacity);
        freeIndex = this.nodes[freeIndex].parentOrNext;
        ++freeCount;
      }

      Debug.Assert(Height == ComputeHeight());

      Debug.Assert(this.nodeCount + freeCount == this.nodeCapacity);
    }

    public void GizmoDraw(Color aabbColor)
    {
      this.DoGizmoDraw(aabbColor, this.rootId);
    }

    private void DoGizmoDraw(Color color, int nodeId)
    {
      if (nodeId == AABBTree.NULL_NODE_ID)
        return;

      TreeNode node = this.nodes[nodeId];
      node.aabb.GizmoDraw(color);

      this.DoGizmoDraw(color, node.left);
      this.DoGizmoDraw(color, node.right);
    }

    //  /// <summary>
    //  /// Build an optimal tree. Very expensive. For testing.
    //  /// </summary>
    //  public void RebuildBottomUp()
    //  {
    //    int[] nodes = new int[this.nodeCount];
    //    int count = 0;

    //    // Build array of leaves. Free the rest.
    //    for (int i = 0; i < this.nodeCapacity; ++i)
    //    {
    //      if (this.nodes[i].Height < 0)
    //      {
    //        // free node in pool
    //        continue;
    //      }

    //      if (this.nodes[i].IsLeaf)
    //      {
    //        this.nodes[i].ParentOrNext = NullNode;
    //        nodes[count] = i;
    //        ++count;
    //      }
    //      else
    //      {
    //        FreeNode(i);
    //      }
    //    }

    //    while (count > 1)
    //    {
    //      float minCost = Settings.MaxFloat;
    //      int iMin = -1, jMin = -1;
    //      for (int i = 0; i < count; ++i)
    //      {
    //        AABB AABBi = this.nodes[nodes[i]].AABB;

    //        for (int j = i + 1; j < count; ++j)
    //        {
    //          AABB AABBj = this.nodes[nodes[j]].AABB;
    //          AABB b = new AABB();
    //          b = AABB.CreateMerged((ref AABBi, ref AABBj);
    //          float cost = b.Perimeter;
    //          if (cost < minCost)
    //          {
    //            iMin = i;
    //            jMin = j;
    //            minCost = cost;
    //          }
    //        }
    //      }

    //      int index1 = nodes[iMin];
    //      int index2 = nodes[jMin];
    //      TreeNode child1 = this.nodes[index1];
    //      TreeNode child2 = this.nodes[index2];

    //      int parentIndex = AllocateNode();
    //      TreeNode parent = this.nodes[parentIndex];
    //      parent.Child1 = index1;
    //      parent.Child2 = index2;
    //      parent.Height = 1 + Math.Max(child1.Height, child2.Height);
    //      parent.AABB = AABB.CreateMerged((ref child1.AABB, ref child2.AABB);
    //      parent.ParentOrNext = NullNode;

    //      child1.ParentOrNext = parentIndex;
    //      child2.ParentOrNext = parentIndex;

    //      nodes[jMin] = nodes[count - 1];
    //      nodes[iMin] = parentIndex;
    //      --count;
    //    }

    //    this.rootId = nodes[0];

    //    Validate();
    //  }

    //  /// <summary>
    //  /// Shift the origin of the nodes
    //  /// </summary>
    //  /// <param name="newOrigin">The displacement to use.</param>
    //  public void ShiftOrigin(Vector2 newOrigin)
    //  {
    //    // Build array of leaves. Free the rest.
    //    for (int i = 0; i < this.nodeCapacity; ++i)
    //    {
    //      this.nodes[i].AABB.LowerBound -= newOrigin;
    //      this.nodes[i].AABB.UpperBound -= newOrigin;
    //    }
    //  }
  }
}