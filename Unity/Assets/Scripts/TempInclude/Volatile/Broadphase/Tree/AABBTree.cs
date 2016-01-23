﻿/*
 *  VolatilePhysics - A 2D Physics Library for Networked Games
 *  Copyright (c) 2015-2016 - Alexander Shoulson - http://ashoulson.com
 *
 *  Original sources:
 *  Farseer Physics Engine: Copyright (c) 2012 Ian Qvist
 *  Box2D: Copyright (c) 2006-2011 Erin Catto http://www.box2d.org 
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
  /// <summary>
  /// A dynamic tree arranges data in a binary tree to accelerate queries and
  /// ray/circle casts. Each node contains an AABB encompassing all of its 
  /// children. Leaves contain a reference to a body inside of a "fat" AABB
  /// expanded with an extra margin around the body's AABB, configured by
  /// Config.AABB_PADDING. This allows the body object to move by small 
  /// amounts without triggering a tree update.
  ///
  /// Nodes are stored in a contiguous array block, both for cache
  /// accessibility, and to make it easy to keep a rolling buffer of
  /// these tree structures in a history tracker.
  /// </summary>
  public class AABBTree
  {
    internal const int NULL_NODE_ID = -1;
    internal const int EMPTY_HEIGHT = -1;
    internal const int DEFAULT_CAPACITY = 16;

    /// <summary>
    /// A node in the dynamic tree.
    /// </summary>
    private struct TreeNode
    {
      /// <summary>
      /// "Fat" AABB in world space.
      /// </summary>
      internal AABB AABB;

      internal int leftId;
      internal int rightId;
      internal int parentOrNextId;

      internal int height;
      internal Body body;

      internal bool IsLeaf
      {
        get { return (leftId == AABBTree.NULL_NODE_ID); }
      }

      internal void Initialize()
      {
        this.parentOrNextId = AABBTree.NULL_NODE_ID;
        this.leftId = AABBTree.NULL_NODE_ID;
        this.rightId = AABBTree.NULL_NODE_ID;
        this.height = AABBTree.EMPTY_HEIGHT;
        this.body = null;
      }
    }

    private int frame;
    private int freeList;
    private int nodeCapacity;
    private int nodeCount;

    private TreeNode[] nodes;
    private int rootId;
    private Dictionary<Body, int> bodyToId;

    private Stack<int> testStack = new Stack<int>(256);

    public int Height
    {
      get
      {
        if (this.rootId == AABBTree.NULL_NODE_ID)
          return 0;
        return this.nodes[rootId].height;
      }
    }

    /// <summary>
    /// Constructing the tree initializes the node pool.
    /// </summary>
    public AABBTree(int capacity = AABBTree.DEFAULT_CAPACITY)
    {
      this.frame = History.CURRENT_FRAME;
      this.rootId = AABBTree.NULL_NODE_ID;

      this.nodeCapacity = capacity;
      this.nodeCount = 0;
      this.nodes = new TreeNode[capacity];
      this.bodyToId = new Dictionary<Body, int>();

      // Build a linked list for the free list
      for (int i = 0; i < capacity; i++)
      {
        this.nodes[i].Initialize();
        if (i < (capacity - 1))
          this.nodes[i].parentOrNextId = i + 1;
      }

      this.freeList = 0;
    }

    #region External Access
    /// <summary>
    /// Add a body to the tree as a leaf node.
    /// </summary>
    public void AddBody(Body body)
    {
      int nodeId = this.AllocateNode();

      // Fatten the aabb
      this.nodes[nodeId].AABB =
        AABB.CreateExpanded(body.AABB, Config.AABB_PADDING);
      this.nodes[nodeId].body = body;
      this.nodes[nodeId].height = 0;

      this.InsertLeaf(nodeId);
      this.bodyToId.Add(body, nodeId);
    }

    /// <summary>
    /// Removes a body from the tree.
    /// </summary>
    public void RemoveBody(Body body)
    {
      int nodeId = this.bodyToId[body];
      Debug.Assert((0 <= nodeId) && (nodeId < this.nodeCapacity));
      Debug.Assert(this.nodes[nodeId].IsLeaf);

      this.RemoveLeaf(nodeId);
      this.FreeNode(nodeId);
      this.bodyToId.Remove(body);
    }

    /// <summary>
    /// Updates a node in the tree based on its body's position. If the body 
    /// has moved outside of its fattened AABB, then the body is removed from
    /// the tree and re-inserted. Otherwise the function returns immediately.
    /// </summary>
    public void UpdateBody(Body body)
    {
      int nodeId = this.bodyToId[body];
      Debug.Assert((0 <= nodeId) && (nodeId < this.nodeCapacity));
      Debug.Assert(this.nodes[nodeId].IsLeaf);

      if (this.nodes[nodeId].AABB.Contains(body.AABB))
        return;

      this.RemoveLeaf(nodeId);
      this.nodes[nodeId].AABB =
        AABB.CreateExpanded(body.AABB, Config.AABB_PADDING);
      this.InsertLeaf(nodeId);
    }
    #endregion

    #region Tests
    /// <summary>
    /// Query an AABB for any bodies stored in leaves overlapping the provided
    /// AABB and returns them in the given list. Note that this does not check
    /// the bodies' bounding AABBs themselves, just the "fat" AABBs 
    /// surrounding those bodies in the tree.
    /// </summary>
    public void Query(AABB aabb, IList<Body> foundBodies)
    {
      this.testStack.Clear();
      this.testStack.Push(this.rootId);

      while (this.testStack.Count > 0)
      {
        int nodeId = this.testStack.Pop();
        if (nodeId == AABBTree.NULL_NODE_ID)
          continue;

        TreeNode node = this.nodes[nodeId];

        if (node.AABB.Intersect(aabb))
        {
          if (node.IsLeaf)
          {
            Body body = node.body;
            if (node.body != null)
              foundBodies.Add(body);
          }
          else
          {
            this.testStack.Push(node.leftId);
            this.testStack.Push(node.rightId);
          }
        }
      }
    }

    public void RayCast(
     ref RayCast ray,
     ref RayResult result,
     BodyFilter filter = null)
    {
      this.testStack.Clear();
      this.testStack.Push(this.rootId);

      while (this.testStack.Count > 0)
      {
        int nodeId = this.testStack.Pop();
        if (nodeId == AABBTree.NULL_NODE_ID)
          continue;

        TreeNode node = this.nodes[nodeId];

        if (node.AABB.RayCast(ref ray))
        {
          if (node.IsLeaf)
          {
            Body body = node.body;
            if (Body.Filter(body, filter) == true)
              body.RayCast(ref ray, ref result);
          }
          else
          {
            this.testStack.Push(node.leftId);
            this.testStack.Push(node.rightId);
          }
        }
      }
    }
    #endregion

    #region Internals

    #region Free List
    /// <summary>
    /// Allocates a free space from the buffer for a node.
    /// </summary>
    private int AllocateNode()
    {
      // Expand the node pool as needed
      if (this.freeList == AABBTree.NULL_NODE_ID)
        this.Expand();

      // Peel a node off the free list
      int nodeId = this.freeList;
      this.freeList = this.nodes[nodeId].parentOrNextId;
      this.nodeCount++;
      return nodeId;
    }

    /// <summary>
    /// Allocates more space on the heap for storing tree nodes.
    /// </summary>
    private void Expand()
    {
      Debug.Assert(this.nodeCount == this.nodeCapacity);

      // The free list is empty, rebuild a bigger pool
      TreeNode[] oldNodes = this.nodes;
      this.nodeCapacity *= 2;
      this.nodes = new TreeNode[this.nodeCapacity];
      Array.Copy(oldNodes, this.nodes, this.nodeCount);

      // Build a linked list for the free list
      // The parent pointer becomes the "next" pointer
      for (int i = this.nodeCount; i < this.nodeCapacity; ++i)
      {
        this.nodes[i].Initialize();
        if (i < (this.nodeCapacity - 1))
          this.nodes[i].parentOrNextId = i + 1;
      }
      this.freeList = this.nodeCount;
    }

    /// <summary>
    /// Returns a node to the buffer to be used later.
    /// </summary>
    private void FreeNode(int nodeId)
    {
      Debug.Assert((0 <= nodeId) && (nodeId < this.nodeCapacity));
      Debug.Assert(0 < this.nodeCount);

      this.nodes[nodeId].parentOrNextId = this.freeList;
      this.nodes[nodeId].height = AABBTree.EMPTY_HEIGHT;
      this.freeList = nodeId;
      this.nodeCount--;
    }
    #endregion

    #region AABB Tree
    private void InsertLeaf(int nodeId)
    {
      if (this.rootId == AABBTree.NULL_NODE_ID)
      {
        this.rootId = nodeId;
        this.nodes[this.rootId].parentOrNextId = AABBTree.NULL_NODE_ID;
        return;
      }

      // Find the best sibling for this node
      AABB leafAABB = this.nodes[nodeId].AABB;
      int bestSibling = this.FindBestSibling(ref leafAABB);

      // Create a new parent
      int oldParent = this.nodes[bestSibling].parentOrNextId;
      int newParent = AllocateNode();
      this.nodes[newParent].parentOrNextId = oldParent;
      this.nodes[newParent].body = null;
      this.nodes[newParent].AABB =
        AABB.CreateMerged(leafAABB, this.nodes[bestSibling].AABB);
      this.nodes[newParent].height = this.nodes[bestSibling].height + 1;

      // Update the parent, if any
      if (oldParent != AABBTree.NULL_NODE_ID)
      {
        // The sibling was not the root
        if (this.nodes[oldParent].leftId == bestSibling)
          this.nodes[oldParent].leftId = newParent;
        else
          this.nodes[oldParent].rightId = newParent;
      }
      else
      {
        // The sibling was the root
        this.rootId = newParent;
      }

      // Insert the new parent node
      this.nodes[newParent].leftId = bestSibling;
      this.nodes[newParent].rightId = nodeId;
      this.nodes[bestSibling].parentOrNextId = newParent;
      this.nodes[nodeId].parentOrNextId = newParent;

      // Walk back up the tree fixing heights and AABBs
      this.UpdateTreeRoots(this.nodes[nodeId].parentOrNextId);

      //Validate();
    }

    /// <summary>
    /// Removes a leaf from the tree
    /// </summary>
    private void RemoveLeaf(int nodeId)
    {
      if (nodeId == this.rootId)
      {
        this.rootId = AABBTree.NULL_NODE_ID;
        return;
      }

      int parent = this.nodes[nodeId].parentOrNextId;
      int grandParent = this.nodes[parent].parentOrNextId;

      int sibling;
      if (this.nodes[parent].leftId == nodeId)
        sibling = this.nodes[parent].rightId;
      else
        sibling = this.nodes[parent].leftId;

      if (grandParent != AABBTree.NULL_NODE_ID)
      {
        // Destroy parent and connect sibling to grandParent
        if (this.nodes[grandParent].leftId == parent)
          this.nodes[grandParent].leftId = sibling;
        else
          this.nodes[grandParent].rightId = sibling;
        this.nodes[sibling].parentOrNextId = grandParent;
        this.FreeNode(parent);

        // Adjust ancestor bounds
        int index = grandParent;
        while (index != AABBTree.NULL_NODE_ID)
        {
          index = this.Balance(index);

          int left = this.nodes[index].leftId;
          int right = this.nodes[index].rightId;

          this.nodes[index].AABB =
            AABB.CreateMerged(this.nodes[left].AABB, this.nodes[right].AABB);
          this.nodes[index].height =
            Math.Max(this.nodes[left].height, this.nodes[right].height) + 1;

          index = this.nodes[index].parentOrNextId;
        }
      }
      else
      {
        this.rootId = sibling;
        this.nodes[sibling].parentOrNextId = AABBTree.NULL_NODE_ID;
        this.FreeNode(parent);
      }

      //Validate();
    }

    /// <summary>
    /// Walks up the tree from an index fixing heights and AABBs
    /// </summary>
    private void UpdateTreeRoots(int index)
    {
      while (index != AABBTree.NULL_NODE_ID)
      {
        index = this.Balance(index);

        int left = this.nodes[index].leftId;
        int right = this.nodes[index].rightId;

        Debug.Assert(left != AABBTree.NULL_NODE_ID);
        Debug.Assert(right != AABBTree.NULL_NODE_ID);

        this.nodes[index].height =
          Math.Max(this.nodes[left].height, this.nodes[right].height) + 1;
        this.nodes[index].AABB =
          AABB.CreateMerged(this.nodes[left].AABB, this.nodes[right].AABB);

        index = this.nodes[index].parentOrNextId;
      }
    }

    /// <summary>
    /// Finds the best sibling for a given AABB based on area comparison
    /// </summary>
    private int FindBestSibling(ref AABB aabb)
    {
      int index = this.rootId;

      while (this.nodes[index].IsLeaf == false)
      {
        int left = this.nodes[index].leftId;
        int right = this.nodes[index].rightId;

        float area = this.nodes[index].AABB.Area;

        AABB combinedAABB =
          AABB.CreateMerged(this.nodes[index].AABB, aabb);
        float combinedArea = combinedAABB.Area;

        // Cost of creating a new parent for this node and the new leaf
        float cost = 2.0f * combinedArea;

        // Minimum cost of pushing the leaf further down the tree
        float inheritanceCost = 2.0f * (combinedArea - area);

        // Cost of descending into left
        float costLeft = ComputeNodeCost(left, aabb) + inheritanceCost;
        float costRight = ComputeNodeCost(right, aabb) + inheritanceCost;

        // Descend according to the minimum cost
        if (cost < costLeft && costLeft < costRight)
          break;

        // Descend
        if (costLeft < costRight)
          index = left;
        else
          index = right;
      }

      return index;
    }

    private float ComputeNodeCost(int index, AABB leafAABB)
    {
      AABB aabb = AABB.CreateMerged(leafAABB, this.nodes[index].AABB);
      if (this.nodes[index].IsLeaf)
        return aabb.Area;
      return aabb.Area - this.nodes[index].AABB.Area;
    }

    /// <summary>
    /// Perform a left or right rotation if node A is imbalanced.
    /// </summary>
    /// <returns>The new root index.</returns>
    private int Balance(int iA)
    {
      Debug.Assert(iA != AABBTree.NULL_NODE_ID);

      TreeNode A = this.nodes[iA];
      if (A.IsLeaf || A.height < 2)
        return iA;

      int iB = A.leftId;
      int iC = A.rightId;
      Debug.Assert(0 <= iB && iB < this.nodeCapacity);
      Debug.Assert(0 <= iC && iC < this.nodeCapacity);

      TreeNode B = this.nodes[iB];
      TreeNode C = this.nodes[iC];

      int balance = C.height - B.height;
      int returnVal = iA;

      if (balance < -1) // Rotate B up
        return this.Rotate(ref A, ref B, ref C, iA, iB, iC, false);
      else if (balance > 1) // Rotate C up
        return this.Rotate(ref A, ref C, ref B, iA, iC, iB, true);
      return iA;
    }

    /// <summary>
    /// Rotates node X up in the tree, either right (CW) or left (CCW).
    /// </summary>
    private int Rotate(
      ref TreeNode A,
      ref TreeNode X,
      ref TreeNode Y,
      int iA,
      int iX,
      int iY,
      bool right)
    {
      int iD = X.leftId;
      int iE = X.rightId;
      TreeNode D = this.nodes[iD];
      TreeNode E = this.nodes[iE];
      Debug.Assert(0 <= iD && iD < this.nodeCapacity);
      Debug.Assert(0 <= iE && iE < this.nodeCapacity);

      // Swap A and X
      X.leftId = iA;
      X.parentOrNextId = A.parentOrNextId;
      A.parentOrNextId = iX;

      // A's old parent should point to X
      if (X.parentOrNextId != AABBTree.NULL_NODE_ID)
      {
        if (this.nodes[X.parentOrNextId].leftId == iA)
        {
          this.nodes[X.parentOrNextId].leftId = iX;
        }
        else
        {
          Debug.Assert(this.nodes[X.parentOrNextId].rightId == iA);
          this.nodes[X.parentOrNextId].rightId = iX;
        }
      }
      else
      {
        this.rootId = iX;
      }

      // Swap the children if necessary
      if (E.height > D.height)
      {
        VolatileUtil.Swap<TreeNode>(ref E, ref D);
        VolatileUtil.Swap<int>(ref iE, ref iD);
      }

      // Rotate
      X.rightId = iD;
      if (right == true)
        A.rightId = iE;
      else
        A.leftId = iE;

      E.parentOrNextId = iA;
      A.AABB = AABB.CreateMerged(Y.AABB, E.AABB);
      X.AABB = AABB.CreateMerged(A.AABB, D.AABB);

      A.height = Math.Max(Y.height, E.height) + 1;
      X.height = Math.Max(A.height, D.height) + 1;

      // Store the intermediate values
      this.nodes[iA] = A;
      this.nodes[iX] = X;
      this.nodes[iY] = Y;
      this.nodes[iD] = D;
      this.nodes[iE] = E;

      return iX;
    }

    /// <summary>
    /// Compute the height of a subtree.
    /// </summary>
    public int ComputeHeight(int nodeId)
    {
      Debug.Assert((0 <= nodeId) && (nodeId < this.nodeCapacity));

      TreeNode node = this.nodes[nodeId];
      if (node.IsLeaf)
        return 0;

      int heightLeft = ComputeHeight(node.leftId);
      int heightRight = ComputeHeight(node.rightId);
      return Mathf.Max(heightLeft, heightRight) + 1;
    }
    #endregion

    #endregion

    #region Debug
    /// <summary>
    /// Get the ratio of the sum of the node areas to the root area.
    /// </summary>
    public float ComputeAreaRatio()
    {
      if (this.rootId == AABBTree.NULL_NODE_ID)
        return 0.0f;

      TreeNode root = this.nodes[this.rootId];
      float rootArea = root.AABB.Area;

      float totalArea = 0.0f;
      for (int i = 0; i < this.nodeCapacity; i++)
      {
        if (this.nodes[i].height < 0)
          continue;
        totalArea += this.nodes[i].AABB.Area;
      }

      return totalArea / rootArea;
    }

    /// <summary>
    /// Get the maximum balance of an node in the tree. The balance is the difference
    /// in height of the two children of a node.
    /// </summary>
    public int ComputeMaxBalance()
    {
      int maxBalance = 0;
      for (int i = 0; i < this.nodeCapacity; ++i)
      {
        TreeNode node = this.nodes[i];
        if (node.height <= 1)
          continue;

        Debug.Assert(node.IsLeaf == false);

        int balance =
          Math.Abs(
            this.nodes[node.rightId].height -
            this.nodes[node.leftId].height);
        maxBalance = Math.Max(maxBalance, balance);
      }

      return maxBalance;
    }

    /// <summary>
    /// Validate this tree. For testing.
    /// </summary>
    public void Validate()
    {
      ValidateStructure(this.rootId);
      ValidateMetrics(this.rootId);

      int freeCount = 0;
      int freeIndex = this.freeList;
      while (freeIndex != AABBTree.NULL_NODE_ID)
      {
        Debug.Assert(0 <= freeIndex && freeIndex < this.nodeCapacity);
        freeIndex = this.nodes[freeIndex].parentOrNextId;
        freeCount++;
      }

      int expectedheight = 0;
      if (this.rootId != AABBTree.NULL_NODE_ID)
        expectedheight = this.nodes[this.rootId].height;
      Debug.Assert(expectedheight == this.ComputeHeight());

      Debug.Assert(this.nodeCount + freeCount == this.nodeCapacity);
    }

    /// <summary>
    /// Compute the height of the entire tree.
    /// </summary>
    public int ComputeHeight()
    {
      return this.ComputeHeight(this.rootId);
    }

    public void ValidateStructure(int index)
    {
      if (index == AABBTree.NULL_NODE_ID)
        return;

      if (index == this.rootId)
        Debug.Assert(this.nodes[index].parentOrNextId == AABBTree.NULL_NODE_ID);

      TreeNode node = this.nodes[index];
      int left = node.leftId;
      int right = node.rightId;

      if (node.IsLeaf)
      {
        Debug.Assert(left == AABBTree.NULL_NODE_ID);
        Debug.Assert(right == AABBTree.NULL_NODE_ID);
        Debug.Assert(node.height == 0);
        return;
      }

      Debug.Assert(0 <= left && left < this.nodeCapacity);
      Debug.Assert(0 <= right && right < this.nodeCapacity);

      Debug.Assert(this.nodes[left].parentOrNextId == index);
      Debug.Assert(this.nodes[right].parentOrNextId == index);

      this.ValidateStructure(left);
      this.ValidateStructure(right);
    }

    public void ValidateMetrics(int index)
    {
      if (index == AABBTree.NULL_NODE_ID)
        return;

      TreeNode node = this.nodes[index];

      int left = node.leftId;
      int right = node.rightId;
      if (node.IsLeaf)
      {
        Debug.Assert(left == AABBTree.NULL_NODE_ID);
        Debug.Assert(right == AABBTree.NULL_NODE_ID);
        Debug.Assert(node.height == 0);
        return;
      }

      Debug.Assert(0 <= left && left < this.nodeCapacity);
      Debug.Assert(0 <= right && right < this.nodeCapacity);

      int heightLeft = this.nodes[left].height;
      int heightRight = this.nodes[right].height;
      int height = Math.Max(heightLeft, heightRight) + 1;
      Debug.Assert(node.height == height);

      AABB testAABB =
        AABB.CreateMerged(this.nodes[left].AABB, this.nodes[right].AABB);
      Debug.Assert(testAABB.BottomLeft == node.AABB.BottomLeft);
      Debug.Assert(testAABB.TopRight == node.AABB.TopRight);

      this.ValidateMetrics(left);
      this.ValidateMetrics(right);
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
      node.AABB.GizmoDraw(color);

      this.DoGizmoDraw(color, node.leftId);
      this.DoGizmoDraw(color, node.rightId);
    }

    ///// <summary>
    ///// Build an optimal tree. Very expensive. For testing.
    ///// </summary>
    //public void RebuildBottomUp()
    //{
    //  int[] nodes = new int[_nodeCount];
    //  int count = 0;

    //  // Build array of leaves. Free the rest.
    //  for (int i = 0; i < this.nodeCapacity; ++i)
    //  {
    //    if (this.nodes[i].height < 0)
    //    {
    //      // free node in pool
    //      continue;
    //    }

    //    if (this.nodes[i].IsLeaf())
    //    {
    //      this.nodes[i].parentOrNext = DynamicTree<T>.NULL_NODE;
    //      nodes[count] = i;
    //      ++count;
    //    }
    //    else
    //    {
    //      FreeNode(i);
    //    }
    //  }

    //  while (count > 1)
    //  {
    //    float minCost = Settings.MaxFloat;
    //    int iMin = -1, jMin = -1;
    //    for (int i = 0; i < count; ++i)
    //    {
    //      AABB AABBi = this.nodes[nodes[i]].AABB;

    //      for (int j = i + 1; j < count; ++j)
    //      {
    //        AABB AABBj = this.nodes[nodes[j]].AABB;
    //        AABB b = new AABB();
    //        b.Combine(ref AABBi, ref AABBj);
    //        float cost = b.Perimeter;
    //        if (cost < minCost)
    //        {
    //          iMin = i;
    //          jMin = j;
    //          minCost = cost;
    //        }
    //      }
    //    }

    //    int index1 = nodes[iMin];
    //    int index2 = nodes[jMin];
    //    TreeNode<T> left = this.nodes[index1];
    //    TreeNode<T> right = this.nodes[index2];

    //    int parentIndex = AllocateNode();
    //    TreeNode<T> parent = this.nodes[parentIndex];
    //    parent.left = index1;
    //    parent.right = index2;
    //    parent.height = 1 + Math.Max(left.height, right.height);
    //    parent.AABB.Combine(ref left.AABB, ref right.AABB);
    //    parent.parentOrNext = DynamicTree<T>.NULL_NODE;

    //    left.parentOrNext = parentIndex;
    //    right.parentOrNext = parentIndex;

    //    nodes[jMin] = nodes[count - 1];
    //    nodes[iMin] = parentIndex;
    //    --count;
    //  }

    //  this.root = nodes[0];

    //  Validate();
    //}
    #endregion
  }
}