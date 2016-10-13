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

#if UNITY
using UnityEngine;
#endif

namespace Volatile
{
  /// <summary>
  /// A dynamic tree bounding volume hierarchy used for collision detection.
  /// Does not support raycasts (for now) as they would not be compatible with
  /// Volatile's historical raycast capability -- no past-step data is preserved
  /// in this tree.
  /// 
  /// Bounding AABBs are expanded to allow for movement room according to
  /// the VoltConfig.AABB_EXTENSION setting.
  ///
  /// Nodes are pooled and relocatable, so we use node indices rather than pointers.
  /// </summary>
  internal class TreeBroadphase : IBroadPhase
  {
    private delegate bool AABBTest(ref VoltAABB aabb);

    internal const int NULL_NODE = -1;

    /// <summary>
    /// A node in the dynamic tree.
    /// </summary>
    private class Node
    {
      internal bool IsLeaf
      {
        get { return this.left == TreeBroadphase.NULL_NODE; }
      }

      /// <summary>
      /// Expanded AABB.
      /// </summary>
      internal VoltAABB aabb;

      internal int left;
      internal int right;
      internal int height;
      internal int parentOrNext;
      internal VoltBody body;

      internal Node()
      {
        this.Reset();
      }

      internal Node(int parentOrNext, int height)
      {
        this.parentOrNext = parentOrNext;
        this.height = height;
      }

      internal void Initialize(int parentOrNext, int height)
      {
        this.parentOrNext = parentOrNext;
        this.height = height;
      }

      internal void Reset()
      {
        this.aabb = default(VoltAABB);
        this.left = NULL_NODE;
        this.right = NULL_NODE;
        this.height = 0;
        this.parentOrNext = NULL_NODE;
        this.body = null;
      }
    }

    private readonly Stack<int> queryStack;

    private int freeList;
    private int nodeCapacity;
    private int nodeCount;
    private Node[] nodes;
    private int rootId;

    public TreeBroadphase()
    {
      this.rootId = NULL_NODE;

      this.queryStack = new Stack<int>(256);

      this.nodeCapacity = 16;
      this.nodeCount = 0;
      this.nodes = new Node[this.nodeCapacity];

      // Build a linked list for the free list
      for (int i = 0; i < this.nodeCapacity - 1; ++i)
        this.nodes[i] = new Node(i + 1, 1);
      this.nodes[this.nodeCapacity - 1] = new Node(NULL_NODE, 1);
      this.freeList = 0;
    }

    /// <summary>
    /// Compute the height of the binary tree in O(N) time.
    /// Should not be called often.
    /// </summary>
    public int Height
    {
      get
      {
        if (this.rootId == NULL_NODE)
          return 0;
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
        if (this.rootId == NULL_NODE)
          return 0.0f;

        Node root = this.nodes[this.rootId];
        float rootArea = root.aabb.Perimeter;

        float totalArea = 0.0f;
        for (int i = 0; i < this.nodeCapacity; ++i)
        {
          Node node = this.nodes[i];
          if (node.height < 0)
            continue;
          totalArea += node.aabb.Perimeter;
        }

        return totalArea / rootArea;
      }
    }

    /// <summary>
    /// Get the maximum balance of an node in the tree. The balance is the
    /// difference in height of the two children of a node.
    /// </summary>
    public int MaxBalance
    {
      get
      {
        int maxBalance = 0;
        for (int i = 0; i < this.nodeCapacity; ++i)
        {
          Node node = this.nodes[i];
          if (node.height <= 1)
            continue;

          VoltDebug.Assert(node.IsLeaf == false);
          int balance =
            Math.Abs(
              this.nodes[node.right].height -
              this.nodes[node.left].height);

          maxBalance = Math.Max(maxBalance, balance);
        }

        return maxBalance;
      }
    }

    /// <summary>
    /// Compute the height of the entire tree.
    /// </summary>
    public int ComputeHeight()
    {
      return this.ComputeHeight(this.rootId);
    }

    /// <summary>
    /// Adds a body to the tree.     
    /// </summary>
    public void AddBody(VoltBody body)
    {
      VoltDebug.Assert(body.ProxyId == TreeBroadphase.NULL_NODE);

      int proxyId;
      Node proxyNode = this.AllocateNode(out proxyId);

      // Expand the aabb
      proxyNode.aabb =
        VoltAABB.CreateExpanded(
          body.AABB,
          VoltConfig.AABB_EXTENSION);

      proxyNode.body = body;
      proxyNode.height = 0;

      this.InsertLeaf(proxyId);
      body.ProxyId = proxyId;
    }

    /// <summary>
    /// Removes a body from the tree.
    /// </summary>
    public void RemoveBody(VoltBody body)
    {
      int proxyId = body.ProxyId;
      VoltDebug.Assert((0 <= proxyId) && (proxyId < this.nodeCapacity));
      VoltDebug.Assert(this.nodes[proxyId].IsLeaf);

      this.RemoveLeaf(proxyId);
      this.FreeNode(proxyId);

      body.ProxyId = TreeBroadphase.NULL_NODE;
    }

    /// <summary>
    /// Updates a body's position. If the body has moved outside of its
    /// expanded AABB, then the body is removed from the tree and re-inserted.
    /// Otherwise the function returns immediately.
    /// </summary>
    public void UpdateBody(VoltBody body)
    {
      int proxyId = body.ProxyId;
      VoltDebug.Assert((0 <= proxyId) && (proxyId < this.nodeCapacity));

      Node proxyNode = this.nodes[proxyId];
      VoltDebug.Assert(proxyNode.IsLeaf);

      if (proxyNode.aabb.Contains(body.AABB))
        return;
      this.RemoveLeaf(proxyId);

      // Extend AABB
      VoltAABB expanded =
        VoltAABB.CreateExpanded(body.AABB, VoltConfig.AABB_EXTENSION);

      // Predict AABB displacement and sweep the AABB
      //Vector2 sweep = VoltConfig.AABB_MULTIPLIER * displacement;
      //VoltAABB swept = VoltAABB.CreateSwept(expanded, sweep);
      //this.nodes[proxyId].aabb = swept;

      proxyNode.aabb = expanded;
      this.InsertLeaf(proxyId);
      return;
    }

    #region Tests
    public void QueryOverlap(
      VoltAABB aabb,
      VoltBuffer<VoltBody> outBuffer)
    {
      this.StartQuery(outBuffer);
      while (this.queryStack.Count > 0)
      {
        Node node = this.GetNextNode();
        if (node.aabb.Intersect(aabb))
          this.ExpandNode(node, outBuffer);
      }
    }

    public void QueryPoint(
      Vector2 point,
      VoltBuffer<VoltBody> outBuffer)
    {
      this.StartQuery(outBuffer);
      while (this.queryStack.Count > 0)
      {
        Node node = this.GetNextNode();
        if (node.aabb.QueryPoint(point))
          this.ExpandNode(node, outBuffer);
      }
    }

    public void QueryCircle(
      Vector2 point, 
      float radius,
      VoltBuffer<VoltBody> outBuffer)
    {
      this.StartQuery(outBuffer);
      while (this.queryStack.Count > 0)
      {
        Node node = this.GetNextNode();
        if (node.aabb.QueryCircleApprox(point, radius))
          this.ExpandNode(node, outBuffer);
      }
    }

    public void RayCast(
      ref VoltRayCast ray,
      VoltBuffer<VoltBody> outBuffer)
    {
      this.StartQuery(outBuffer);
      while (this.queryStack.Count > 0)
      {
        Node node = this.GetNextNode();
        if (node.aabb.RayCast(ref ray))
          this.ExpandNode(node, outBuffer);
      }
    }

    public void CircleCast(
      ref VoltRayCast ray, 
      float radius,
      VoltBuffer<VoltBody> outBuffer)
    {
      this.StartQuery(outBuffer);
      while (this.queryStack.Count > 0)
      {
        Node node = this.GetNextNode();
        if (node.aabb.CircleCastApprox(ref ray, radius))
          this.ExpandNode(node, outBuffer);
      }
    }
    #endregion

    #region Internals

    #region Query Internals
    private void StartQuery(VoltBuffer<VoltBody> outBuffer)
    {
      this.queryStack.Clear();
      this.ExpandChild(this.rootId, outBuffer);
    }

    private Node GetNextNode()
    {
      return this.nodes[this.queryStack.Pop()];
    }

    private void ExpandNode(Node node, VoltBuffer<VoltBody> outBuffer)
    {
      VoltDebug.Assert(node.IsLeaf == false);
      this.ExpandChild(node.left, outBuffer);
      this.ExpandChild(node.right, outBuffer);
    }

    /// <summary>
    /// If the node is a leaf, we do not test the actual proxy bounding box.
    /// This is redundant since we will be testing the body's bounding box in
    /// the first step of the narrowphase, and the two are almost equivalent.
    /// </summary>
    private void ExpandChild(int query, VoltBuffer<VoltBody> outBuffer)
    {
      if (query != TreeBroadphase.NULL_NODE)
      {
        Node node = this.nodes[query];
        if (node.IsLeaf)
          outBuffer.Add(node.body);
        else
          this.queryStack.Push(query);
      }
    }
    #endregion

    private Node AllocateNode(out int nodeId)
    {
      // Expand the node pool as needed
      if (this.freeList == NULL_NODE)
      {
        VoltDebug.Assert(this.nodeCount == this.nodeCapacity);

        // The free list is empty -- rebuild a bigger pool
        Node[] oldNodes = this.nodes;
        this.nodeCapacity = VoltUtil.ExpandArray(ref this.nodes);
        Array.Copy(oldNodes, this.nodes, this.nodeCount);

        // Build a linked list for the free list
        // The parent pointer becomes the "next" pointer
        for (int i = this.nodeCount; i < this.nodeCapacity - 1; ++i)
          this.nodes[i] = new Node(i + 1, -1);

        this.nodes[this.nodeCapacity - 1] = new Node(NULL_NODE, -1);
        this.freeList = this.nodeCount;
      }

      // Peel a node off the free list.
      nodeId = this.freeList;
      Node result = this.nodes[nodeId];

      this.freeList = result.parentOrNext;
      result.Reset();

      this.nodeCount++;
      return result;
    }

    private void FreeNode(int nodeId)
    {
      VoltDebug.Assert((0 <= nodeId) && (nodeId < this.nodeCapacity));
      VoltDebug.Assert(0 < this.nodeCount);

      this.nodes[nodeId].Initialize(this.freeList, -1);
      this.freeList = nodeId;

      this.nodeCount--;
    }

    private void InsertLeaf(int leafId)
    {
      if (this.rootId == NULL_NODE)
      {
        this.rootId = leafId;
        this.nodes[this.rootId].parentOrNext = NULL_NODE;
        return;
      }

      // Find the best sibling for this node
      Node leafNode = this.nodes[leafId];
      VoltAABB leafAABB = leafNode.aabb;
      int siblingId = this.FindBestSibling(ref leafAABB);
      Node sibling = this.nodes[siblingId];

      // Create a new parent
      int oldParentId = sibling.parentOrNext;
      int newParentId;

      Node newParent = this.AllocateNode(out newParentId);
      newParent.Initialize(oldParentId, sibling.height + 1);
      newParent.aabb = VoltAABB.CreateMerged(leafAABB, sibling.aabb);

      if (oldParentId != NULL_NODE)
      {
        Node oldParent = this.nodes[oldParentId];
        // The sibling was not the root
        if (oldParent.left == siblingId)
          oldParent.left = newParentId;
        else
          oldParent.right = newParentId;
      }
      else
      {
        // The sibling was the root
        this.rootId = newParentId;
      }

      newParent.left = siblingId;
      newParent.right = leafId;
      sibling.parentOrNext = newParentId;
      leafNode.parentOrNext = newParentId;

      // Walk back up the tree fixing heights and AABBs
      this.FixAncestors(leafNode.parentOrNext);
    }

    private int FindBestSibling(ref VoltAABB leafAABB)
    {
      int index = this.rootId;
      while (this.nodes[index].IsLeaf == false)
      {
        Node indexNode = this.nodes[index];

        int child1 = indexNode.left;
        int child2 = indexNode.right;

        float area = indexNode.aabb.Perimeter;

        VoltAABB combinedAABB = new VoltAABB();
        VoltAABB.CreateMerged(indexNode.aabb, leafAABB);
        float combinedArea = combinedAABB.Perimeter;

        // Cost of creating a new parent for this node and the new leaf
        float cost = 2.0f * combinedArea;

        // Minimum cost of pushing the leaf further down the tree
        float inheritanceCost = 2.0f * (combinedArea - area);
        float cost1 = this.GetCost(child1, ref leafAABB) + inheritanceCost;
        float cost2 = this.GetCost(child2, ref leafAABB) + inheritanceCost;

        // Descend according to the minimum cost.
        if ((cost < cost1) && (cost1 < cost2))
          break;

        // Descend
        if (cost1 < cost2)
          index = child1;
        else
          index = child2;
      }
      return index;
    }

    private void FixAncestors(int index)
    {
      while (index != NULL_NODE)
      {
        index = this.Balance(index);

        Node indexNode = this.nodes[index];
        Node left = this.nodes[indexNode.left];
        Node right = this.nodes[indexNode.right];

        indexNode.aabb = VoltAABB.CreateMerged(left.aabb, right.aabb);
        indexNode.height = 1 + Math.Max(left.height, right.height);
        index = indexNode.parentOrNext;
      }
    }

    private float GetCost(int index, ref VoltAABB leafAABB)
    {
      if (this.nodes[index].IsLeaf)
      {
        VoltAABB aabb =
          VoltAABB.CreateMerged(leafAABB, this.nodes[index].aabb);
        return aabb.Perimeter;
      }
      else
      {
        VoltAABB aabb =
          VoltAABB.CreateMerged(leafAABB, this.nodes[index].aabb);
        float oldArea = this.nodes[index].aabb.Perimeter;
        float newArea = aabb.Perimeter;
        return newArea - oldArea;
      }
    }

    private void RemoveLeaf(int leafId)
    {
      if (leafId == this.rootId)
      {
        this.rootId = NULL_NODE;
        return;
      }

      int parentId = this.nodes[leafId].parentOrNext;
      Node parent = this.nodes[parentId];

      int siblingId = (parent.left == leafId) ? parent.right : parent.left;
      Node sibling = this.nodes[siblingId];

      int grandParentId = parent.parentOrNext;
      if (grandParentId != NULL_NODE)
      {
        // Destroy parent and connect sibling to grandParent
        Node grandparent = this.nodes[grandParentId];
        if (grandparent.left == parentId)
          grandparent.left = siblingId;
        else
          grandparent.right = siblingId;
        sibling.parentOrNext = grandParentId;
        this.FreeNode(parentId);

        // Fix the tree going up
        this.FixAncestors(grandParentId);
      }
      else
      {
        this.rootId = siblingId;
        sibling.parentOrNext = NULL_NODE;
        this.FreeNode(parentId);
      }
    }

    /// <summary>
    /// Perform a left or right rotation if node A is imbalanced.
    /// </summary>
    private int Balance(int iA)
    {
      VoltDebug.Assert(iA != NULL_NODE);

      Node A = this.nodes[iA];
      if (A.IsLeaf || A.height < 2)
        return iA;

      int iB = A.left;
      int iC = A.right;
      VoltDebug.Assert(0 <= iB && iB < this.nodeCapacity);
      VoltDebug.Assert(0 <= iC && iC < this.nodeCapacity);

      Node B = this.nodes[iB];
      Node C = this.nodes[iC];
      int balance = C.height - B.height;

      if (balance > 1) // Rotate C up
        return this.Rotate(A, B, C, iA, iC, false);
      if (balance < -1) // Rotate B up
        return this.Rotate(A, C, B, iA, iB, true);
      return iA;
    }

    private int Rotate(
      Node P,
      Node Q,
      Node R,
      int iP,
      int iR,
      bool left)
    {
      int iX = R.left;
      int iY = R.right;
      Node X = this.nodes[iX];
      Node Y = this.nodes[iY];
      VoltDebug.Assert((0 <= iX) && (iX < this.nodeCapacity));
      VoltDebug.Assert((0 <= iY) && (iY < this.nodeCapacity));

      // Swap P and R
      R.left = iP;
      R.parentOrNext = P.parentOrNext;
      P.parentOrNext = iR;

      // P's old parent should point to R
      if (R.parentOrNext != NULL_NODE)
      {
        if (this.nodes[R.parentOrNext].left == iP)
        {
          this.nodes[R.parentOrNext].left = iR;
        }
        else
        {
          VoltDebug.Assert(this.nodes[R.parentOrNext].right == iP);
          this.nodes[R.parentOrNext].right = iR;
        }
      }
      else
      {
        this.rootId = iR;
      }

      // Rotate
      if (X.height > Y.height)
        this.UpdateRotated(P, Q, R, iP, iX, iY, left);
      else
        this.UpdateRotated(P, Q, R, iP, iY, iX, left);

      return iR;
    }

    private void UpdateRotated(
      Node P,
      Node Q,
      Node R,
      int iP,
      int iX,
      int iY,
      bool left)
    {
      Node X = this.nodes[iX];
      Node Y = this.nodes[iY];

      R.right = iX;
      if (left)
        P.left = iY;
      else
        P.right = iY;

      Y.parentOrNext = iP;
      P.aabb = VoltAABB.CreateMerged(Q.aabb, Y.aabb);
      R.aabb = VoltAABB.CreateMerged(P.aabb, X.aabb);

      P.height = 1 + Math.Max(Q.height, Y.height);
      R.height = 1 + Math.Max(P.height, X.height);
    }

    /// <summary>
    /// Compute the height of a sub-tree.
    /// </summary>
    private int ComputeHeight(int nodeId)
    {
      VoltDebug.Assert((0 <= nodeId) && (nodeId < this.nodeCapacity));
      Node node = this.nodes[nodeId];
      if (node.IsLeaf)
        return 0;

      int height1 = ComputeHeight(node.left);
      int height2 = ComputeHeight(node.right);
      return 1 + Math.Max(height1, height2);
    }
    #endregion

    #region Debug
#if UNITY && DEBUG
    public void GizmoDraw(Color aabbColor)
    {
      this.DoGizmoDraw(aabbColor, this.rootId);
    }

    private void DoGizmoDraw(Color color, int nodeId)
    {
      if (nodeId == TreeBroadphase.NULL_NODE)
        return;

      Node node = this.nodes[nodeId];
      node.aabb.GizmoDraw(color);

      this.DoGizmoDraw(color, node.left);
      this.DoGizmoDraw(color, node.right);
    }
#endif
    #endregion
  }
}