///*
// *  VolatilePhysics - A 2D Physics Library for Networked Games
// *  Copyright (c) 2015-2016 - Alexander Shoulson - http://ashoulson.com
// *
// *  Original sources:
// *  Farseer Physics Engine: Copyright (c) 2012 Ian Qvist
// *  Box2D: Copyright (c) 2006-2011 Erin Catto http://www.box2d.org 
// *  
// *  This software is provided 'as-is', without any express or implied
// *  warranty. In no event will the authors be held liable for any damages
// *  arising from the use of this software.
// *  Permission is granted to anyone to use this software for any purpose,
// *  including commercial applications, and to alter it and redistribute it
// *  freely, subject to the following restrictions:
// *  
// *  1. The origin of this software must not be misrepresented; you must not
// *     claim that you wrote the original software. If you use this software
// *     in a product, an acknowledgment in the product documentation would be
// *     appreciated but is not required.
// *  2. Altered source versions must be plainly marked as such, and must not be
// *     misrepresented as being the original software.
// *  3. This notice may not be removed or altered from any source distribution.
//*/

//using System;
//using System.Collections.Generic;

//using UnityEngine;

//namespace Volatile
//{
//  /// <summary>
//  /// A dynamic tree arranges data in a binary tree to accelerate
//  /// queries such as volume queries and ray casts. Leafs are proxies
//  /// with an AABB. In the tree we expand the proxy AABBs by Config.AABB_PADDING
//  /// so that the proxy AABB is bigger than the client object. This allows the client
//  /// object to move by small amounts without triggering a tree update.
//  ///
//  /// Nodes are pooled and relocatable, so we use node indices rather than pointers.
//  /// </summary>
//  public class AABBTree
//  {
//    internal const int NULL_NODE = -1;

//    /// <summary>
//    /// A node in the dynamic tree.
//    /// </summary>
//    private struct TreeNode
//    {
//      /// <summary>
//      /// "Fat" AABB
//      /// </summary>
//      internal AABB AABB;

//      internal int leftId;
//      internal int rightId;
//      internal int parentOrNextId;

//      internal int height;
//      internal Body body;

//      internal bool IsLeaf 
//      { 
//        get { return (leftId == AABBTree.NULL_NODE); } 
//      }

//      internal void Initialize()
//      {
//        this.parentOrNextId = AABBTree.NULL_NODE;
//        this.leftId = AABBTree.NULL_NODE;
//        this.rightId = AABBTree.NULL_NODE;
//        this.height = 0;
//        this.body = null;
//      }
//    }

//    private Stack<int> _raycastStack = new Stack<int>(256);
//    private Stack<int> _queryStack = new Stack<int>(256);

//    private int freeList;
//    private int nodeCapacity;
//    private int nodeCount;

//    private TreeNode[] nodes;
//    private int rootId;
//    private Dictionary<Body, int> bodyToId;

//    public int Height
//    {
//      get 
//      {
//        if (this.rootId == AABBTree.NULL_NODE)
//          return 0;
//        return this.nodes[rootId].height;
//      }
//    }

//    /// <summary>
//    /// Constructing the tree initializes the node pool.
//    /// </summary>
//    public AABBTree()
//    {
//      this.rootId = AABBTree.NULL_NODE;

//      this.nodeCapacity = 16;
//      this.nodeCount = 0;
//      this.nodes = new TreeNode[this.nodeCapacity];
//      this.bodyToId = new Dictionary<Body, int>();

//      // Build a linked list for the free list.
//      for (int i = 0; i < this.nodeCapacity - 1; ++i)
//      {
//        this.nodes[i] = new TreeNode();
//        this.nodes[i].height = 1;
//        this.nodes[i].parentOrNextId = i + 1;
//      }

//      this.nodes[this.nodeCapacity - 1] = new TreeNode();
//      this.nodes[this.nodeCapacity - 1].height = 1;
//      this.nodes[this.nodeCapacity - 1].parentOrNextId = AABBTree.NULL_NODE;

//      this.freeList = 0;
//    }

//    #region External Access
//    /// <summary>
//    /// Add a body to the tree as a leaf node. Returns the reference ID.
//    /// </summary>
//    public void AddBody(Body body)
//    {
//      int nodeId = this.AllocateNode();

//      // Fatten the aabb
//      this.nodes[nodeId].AABB = 
//        AABB.CreateExpanded(body.AABB, Config.AABB_PADDING);
//      this.nodes[nodeId].body = body;
//      this.nodes[nodeId].height = 0;

//      this.InsertLeaf(nodeId);
//      this.bodyToId.Add(body, nodeId);
//    }

//    /// <summary>
//    /// Removes a body from the tree by node ID.
//    /// </summary>
//    public void RemoveBody(Body body)
//    {
//      int nodeId = this.bodyToId[body];
//      Debug.Assert((0 <= nodeId) && (nodeId < this.nodeCapacity));
//      Debug.Assert(this.nodes[nodeId].IsLeaf);

//      this.RemoveLeaf(nodeId);
//      this.FreeNode(nodeId);
//      this.bodyToId.Remove(body);
//    }

//    /// <summary>
//    /// Updates a node in the tree based on its body's position. If the body 
//    /// has moved outside of its fattened AABB, then the body is removed from
//    /// the tree and re-inserted. Otherwise the function returns immediately.
//    /// Returns true iff the node was re-inserted.
//    /// </summary>
//    public bool UpdateBody(Body body)
//    {
//      int nodeId = this.bodyToId[body];
//      Debug.Assert((0 <= nodeId) && (nodeId < this.nodeCapacity));
//      Debug.Assert(this.nodes[nodeId].IsLeaf);

//      if (this.nodes[nodeId].AABB.Contains(body.AABB))
//        return false;

//      this.RemoveLeaf(nodeId);
//      this.nodes[nodeId].AABB = 
//        AABB.CreateExpanded(body.AABB, Config.AABB_PADDING);
//      this.InsertLeaf(nodeId);
//      return true;
//    }
//    #endregion

//    #region Tests
//    ///// <summary>
//    ///// Query an AABB for overlapping proxies. The callback class
//    ///// is called for each proxy that overlaps the supplied AABB.
//    ///// </summary>
//    ///// <param name="callback">The callback.</param>
//    ///// <param name="aabb">The aabb.</param>
//    //public void Query(Func<int, bool> callback, ref AABB aabb)
//    //{
//    //  _queryStack.Clear();
//    //  _queryStack.Push(this.root);

//    //  while (_queryStack.Count > 0)
//    //  {
//    //    int nodeId = _queryStack.Pop();
//    //    if (nodeId == DynamicTree<T>.NULL_NODE)
//    //    {
//    //      continue;
//    //    }

//    //    TreeNode<T> node = this.nodes[nodeId];

//    //    if (AABB.TestOverlap(ref node.AABB, ref aabb))
//    //    {
//    //      if (node.IsLeaf())
//    //      {
//    //        bool proceed = callback(nodeId);
//    //        if (proceed == false)
//    //        {
//    //          return;
//    //        }
//    //      }
//    //      else
//    //      {
//    //        _queryStack.Push(node.left);
//    //        _queryStack.Push(node.right);
//    //      }
//    //    }
//    //  }
//    //}

//    ///// <summary>
//    ///// Ray-cast against the proxies in the tree. This relies on the callback
//    ///// to perform a exact ray-cast in the case were the proxy contains a Shape.
//    ///// The callback also performs the any collision filtering. This has performance
//    ///// roughly equal to k * log(n), where k is the number of collisions and n is the
//    ///// number of proxies in the tree.
//    ///// </summary>
//    ///// <param name="callback">A callback class that is called for each proxy that is hit by the ray.</param>
//    ///// <param name="input">The ray-cast input data. The ray extends from p1 to p1 + maxFraction * (p2 - p1).</param>
//    //public void RayCast(Func<RayCastInput, int, float> callback, ref RayCastInput input)
//    //{
//    //  Vector2 p1 = input.Point1;
//    //  Vector2 p2 = input.Point2;
//    //  Vector2 r = p2 - p1;
//    //  Debug.Assert(r.LengthSquared() > 0.0f);
//    //  r.Normalize();

//    //  // v is perpendicular to the segment.
//    //  Vector2 absV = MathUtils.Abs(new Vector2(-r.Y, r.X)); //FPE: Inlined the 'v' variable

//    //  // Separating axis for segment (Gino, p80).
//    //  // |dot(v, p1 - c)| > dot(|v|, h)

//    //  float maxFraction = input.MaxFraction;

//    //  // Build a bounding box for the segment.
//    //  AABB segmentAABB = new AABB();
//    //  {
//    //    Vector2 t = p1 + maxFraction * (p2 - p1);
//    //    Vector2.Min(ref p1, ref t, out segmentAABB.LowerBound);
//    //    Vector2.Max(ref p1, ref t, out segmentAABB.UpperBound);
//    //  }

//    //  _raycastStack.Clear();
//    //  _raycastStack.Push(this.root);

//    //  while (_raycastStack.Count > 0)
//    //  {
//    //    int nodeId = _raycastStack.Pop();
//    //    if (nodeId == DynamicTree<T>.NULL_NODE)
//    //    {
//    //      continue;
//    //    }

//    //    TreeNode<T> node = this.nodes[nodeId];

//    //    if (AABB.TestOverlap(ref node.AABB, ref segmentAABB) == false)
//    //    {
//    //      continue;
//    //    }

//    //    // Separating axis for segment (Gino, p80).
//    //    // |dot(v, p1 - c)| > dot(|v|, h)
//    //    Vector2 c = node.AABB.Center;
//    //    Vector2 h = node.AABB.Extents;
//    //    float separation = Math.Abs(Vector2.Dot(new Vector2(-r.Y, r.X), p1 - c)) - Vector2.Dot(absV, h);
//    //    if (separation > 0.0f)
//    //    {
//    //      continue;
//    //    }

//    //    if (node.IsLeaf())
//    //    {
//    //      RayCastInput subInput;
//    //      subInput.Point1 = input.Point1;
//    //      subInput.Point2 = input.Point2;
//    //      subInput.MaxFraction = maxFraction;

//    //      float value = callback(subInput, nodeId);

//    //      if (value == 0.0f)
//    //      {
//    //        // the client has terminated the raycast.
//    //        return;
//    //      }

//    //      if (value > 0.0f)
//    //      {
//    //        // Update segment bounding box.
//    //        maxFraction = value;
//    //        Vector2 t = p1 + maxFraction * (p2 - p1);
//    //        segmentAABB.LowerBound = Vector2.Min(p1, t);
//    //        segmentAABB.UpperBound = Vector2.Max(p1, t);
//    //      }
//    //    }
//    //    else
//    //    {
//    //      _raycastStack.Push(node.left);
//    //      _raycastStack.Push(node.right);
//    //    }
//    //  }
//    //}
//    #endregion

//    #region Internals
//    private int AllocateNode()
//    {
//      // Expand the node pool as needed
//      if (this.freeList == AABBTree.NULL_NODE)
//      {
//        Debug.Assert(this.nodeCount == this.nodeCapacity);

//        // The free list is empty, rebuild a bigger pool
//        TreeNode[] oldNodes = this.nodes;
//        this.nodeCapacity *= 2;
//        this.nodes = new TreeNode[this.nodeCapacity];
//        Array.Copy(oldNodes, this.nodes, this.nodeCount);

//        // Build a linked list for the free list
//        // The parent pointer becomes the "next" pointer
//        for (int i = this.nodeCount; i < this.nodeCapacity - 1; ++i)
//        {
//          this.nodes[i] = new TreeNode();
//          this.nodes[i].parentOrNextId = i + 1;
//          this.nodes[i].height = -1;
//        }

//        this.nodes[this.nodeCapacity - 1] = new TreeNode();
//        this.nodes[this.nodeCapacity - 1].parentOrNextId = AABBTree.NULL_NODE;
//        this.nodes[this.nodeCapacity - 1].height = -1;
//        this.freeList = this.nodeCount;
//      }

//      // Peel a node off the free list
//      int nodeId = this.freeList;
//      this.freeList = this.nodes[nodeId].parentOrNextId;
//      this.nodes[nodeId].Initialize();
//      this.nodeCount++;
//      return nodeId;
//    }

//    private void FreeNode(int nodeId)
//    {
//      Debug.Assert((0 <= nodeId) && (nodeId < this.nodeCapacity));
//      Debug.Assert(0 < this.nodeCount);

//      this.nodes[nodeId].parentOrNextId = this.freeList;
//      this.nodes[nodeId].height = -1;
//      this.freeList = nodeId;
//      this.nodeCount--;
//    }

//    private void InsertLeaf(int nodeId)
//    {
//      if (this.rootId == AABBTree.NULL_NODE)
//      {
//        this.rootId = nodeId;
//        this.nodes[this.rootId].parentOrNextId = AABBTree.NULL_NODE;
//        return;
//      }

//      // Find the best sibling for this node
//      AABB leafAABB = this.nodes[nodeId].AABB;
//      int index = this.rootId;

//      while (this.nodes[index].IsLeaf == false)
//      {
//        int left = this.nodes[index].leftId;
//        int right = this.nodes[index].rightId;

//        float area = this.nodes[index].AABB.Area;

//        AABB combinedAABB = 
//          AABB.CreateMerged(this.nodes[index].AABB, leafAABB);
//        float combinedArea = combinedAABB.Area;

//        // Cost of creating a new parent for this node and the new leaf
//        float cost = 2.0f * combinedArea;

//        // Minimum cost of pushing the leaf further down the tree
//        float inheritanceCost = 2.0f * (combinedArea - area);

//        // Cost of descending into left
//        float costLeft = ComputeNodeCost(left, leafAABB) + inheritanceCost;
//        float costRight = ComputeNodeCost(right, leafAABB) + inheritanceCost;

//        // Descend according to the minimum cost
//        if (cost < costLeft && costLeft < costRight)
//          break;

//        // Descend
//        if (costLeft < costRight)
//          index = left;
//        else
//          index = right;
//      }

//      int sibling = index;

//      // Create a new parent
//      int oldParent = this.nodes[sibling].parentOrNextId;
//      int newParent = AllocateNode();
//      this.nodes[newParent].parentOrNextId = oldParent;
//      this.nodes[newParent].body = null;
//      this.nodes[newParent].AABB = 
//        AABB.CreateMerged(leafAABB, this.nodes[sibling].AABB);
//      this.nodes[newParent].height = this.nodes[sibling].height + 1;

//      // Update the parent, if any
//      if (oldParent != AABBTree.NULL_NODE)
//      {
//        // The sibling was not the root
//        if (this.nodes[oldParent].leftId == sibling)
//          this.nodes[oldParent].leftId = newParent;
//        else
//          this.nodes[oldParent].rightId = newParent;
//      }
//      else
//      {
//        // The sibling was the root
//        this.rootId = newParent;
//      }

//      // Insert the new parent node
//      this.nodes[newParent].leftId = sibling;
//      this.nodes[newParent].rightId = nodeId;
//      this.nodes[sibling].parentOrNextId = newParent;
//      this.nodes[nodeId].parentOrNextId = newParent;

//      // Walk back up the tree fixing heights and AABBs
//      index = this.nodes[nodeId].parentOrNextId;
//      while (index != AABBTree.NULL_NODE)
//      {
//        index = this.Balance(index);

//        int left = this.nodes[index].leftId;
//        int right = this.nodes[index].rightId;

//        Debug.Assert(left != AABBTree.NULL_NODE);
//        Debug.Assert(right != AABBTree.NULL_NODE);

//        this.nodes[index].height = 
//          Math.Max(this.nodes[left].height, this.nodes[right].height) + 1;
//        this.nodes[index].AABB = 
//          AABB.CreateMerged(this.nodes[left].AABB, this.nodes[right].AABB);

//        index = this.nodes[index].parentOrNextId;
//      }

//      //Validate();
//    }

//    private void RemoveLeaf(int nodeId)
//    {
//      if (nodeId == this.rootId)
//      {
//        this.rootId = AABBTree.NULL_NODE;
//        return;
//      }

//      int parent = this.nodes[nodeId].parentOrNextId;
//      int grandParent = this.nodes[parent].parentOrNextId;

//      int sibling;
//      if (this.nodes[parent].leftId == nodeId)
//        sibling = this.nodes[parent].rightId;
//      else
//        sibling = this.nodes[parent].leftId;

//      if (grandParent != AABBTree.NULL_NODE)
//      {
//        // Destroy parent and connect sibling to grandParent
//        if (this.nodes[grandParent].leftId == parent)
//          this.nodes[grandParent].leftId = sibling;
//        else
//          this.nodes[grandParent].rightId = sibling;
//        this.nodes[sibling].parentOrNextId = grandParent;
//        this.FreeNode(parent);

//        // Adjust ancestor bounds
//        int index = grandParent;
//        while (index != AABBTree.NULL_NODE)
//        {
//          index = this.Balance(index);

//          int left = this.nodes[index].leftId;
//          int right = this.nodes[index].rightId;

//          this.nodes[index].AABB = 
//            AABB.CreateMerged(this.nodes[left].AABB, this.nodes[right].AABB);
//          this.nodes[index].height =
//            Math.Max(this.nodes[left].height, this.nodes[right].height) + 1;

//          index = this.nodes[index].parentOrNextId;
//        }
//      }
//      else
//      {
//        this.rootId = sibling;
//        this.nodes[sibling].parentOrNextId = AABBTree.NULL_NODE;
//        this.FreeNode(parent);
//      }

//      //Validate();
//    }

//    private float ComputeNodeCost(int index, AABB leafAABB)
//    {
//      AABB aabb = AABB.CreateMerged(leafAABB, this.nodes[index].AABB);
//      if (this.nodes[index].IsLeaf)
//        return aabb.Area;
//      return aabb.Area - this.nodes[index].AABB.Area;
//    }

//    /// <summary>
//    /// Perform a left or right rotation if node A is imbalanced.
//    /// </summary>
//    /// <param name="iA"></param>
//    /// <returns>the new root index.</returns>
//    private int Balance(int iA)
//    {
//      Debug.Assert(iA != AABBTree.NULL_NODE);

//      TreeNode A = this.nodes[iA];
//      if (A.IsLeaf || A.height < 2)
//        return iA;

//      int iB = A.leftId;
//      int iC = A.rightId;
//      Debug.Assert(0 <= iB && iB < this.nodeCapacity);
//      Debug.Assert(0 <= iC && iC < this.nodeCapacity);

//      TreeNode B = this.nodes[iB];
//      TreeNode C = this.nodes[iC];

//      int balance = C.height - B.height;

//      // Rotate C up
//      if (balance > 1)
//      {
//        int iF = C.leftId;
//        int iG = C.rightId;
//        TreeNode F = this.nodes[iF];
//        TreeNode G = this.nodes[iG];
//        Debug.Assert(0 <= iF && iF < this.nodeCapacity);
//        Debug.Assert(0 <= iG && iG < this.nodeCapacity);

//        // Swap A and C
//        C.leftId = iA;
//        C.parentOrNextId = A.parentOrNextId;
//        A.parentOrNextId = iC;

//        // A's old parent should point to C
//        if (C.parentOrNextId != AABBTree.NULL_NODE)
//        {
//          if (this.nodes[C.parentOrNextId].leftId == iA)
//          {
//            this.nodes[C.parentOrNextId].leftId = iC;
//          }
//          else
//          {
//            Debug.Assert(this.nodes[C.parentOrNextId].rightId == iA);
//            this.nodes[C.parentOrNextId].rightId = iC;
//          }
//        }
//        else
//        {
//          this.rootId = iC;
//        }

//        // Rotate
//        if (F.height > G.height)
//        {
//          C.rightId = iF;
//          A.rightId = iG;
//          G.parentOrNextId = iA;
//          A.AABB = AABB.CreateMerged(B.AABB, G.AABB);
//          C.AABB = AABB.CreateMerged(A.AABB, F.AABB);

//          A.height = Math.Max(B.height, G.height) + 1;
//          C.height = Math.Max(A.height, F.height) + 1;
//        }
//        else
//        {
//          C.rightId = iG;
//          A.rightId = iF;
//          F.parentOrNextId = iA;
//          A.AABB = AABB.CreateMerged(B.AABB, F.AABB);
//          C.AABB = AABB.CreateMerged(A.AABB, G.AABB);

//          A.height = Math.Max(B.height, F.height) + 1;
//          C.height = Math.Max(A.height, G.height) + 1;
//        }

//        return iC;
//      }

//      // Rotate B up
//      if (balance < -1)
//      {
//        int iD = B.leftId;
//        int iE = B.rightId;
//        TreeNode D = this.nodes[iD];
//        TreeNode E = this.nodes[iE];
//        Debug.Assert(0 <= iD && iD < this.nodeCapacity);
//        Debug.Assert(0 <= iE && iE < this.nodeCapacity);

//        // Swap A and B
//        B.leftId = iA;
//        B.parentOrNextId = A.parentOrNextId;
//        A.parentOrNextId = iB;

//        // A's old parent should point to B
//        if (B.parentOrNextId != AABBTree.NULL_NODE)
//        {
//          if (this.nodes[B.parentOrNextId].leftId == iA)
//          {
//            this.nodes[B.parentOrNextId].leftId = iB;
//          }
//          else
//          {
//            Debug.Assert(this.nodes[B.parentOrNextId].rightId == iA);
//            this.nodes[B.parentOrNextId].rightId = iB;
//          }
//        }
//        else
//        {
//          this.rootId = iB;
//        }

//        // Rotate
//        if (D.height > E.height)
//        {
//          B.rightId = iD;
//          A.leftId = iE;
//          E.parentOrNextId = iA;
//          A.AABB = AABB.CreateMerged(C.AABB, E.AABB);
//          B.AABB = AABB.CreateMerged(A.AABB, D.AABB);

//          A.height = Math.Max(C.height, E.height) + 1;
//          B.height = Math.Max(A.height, D.height) + 1;
//        }
//        else
//        {
//          B.rightId = iE;
//          A.leftId = iD;
//          D.parentOrNextId = iA;
//          A.AABB = AABB.CreateMerged(C.AABB, D.AABB);
//          B.AABB = AABB.CreateMerged(A.AABB, E.AABB);

//          A.height = Math.Max(C.height, D.height) + 1;
//          B.height = Math.Max(A.height, E.height) + 1;
//        }

//        return iB;
//      }

//      return iA;
//    }
//    #endregion

//    #region Debug
//    /// <summary>
//    /// Get the ratio of the sum of the node areas to the root area.
//    /// </summary>
//    public float ComputeAreaRatio()
//    {
//      if (this.rootId == AABBTree.NULL_NODE)
//        return 0.0f;

//      TreeNode root = this.nodes[this.rootId];
//      float rootArea = root.AABB.Area;

//      float totalArea = 0.0f;
//      for (int i = 0; i < this.nodeCapacity; i++)
//      {
//        if (this.nodes[i].height < 0)
//          continue;
//        totalArea += this.nodes[i].AABB.Area;
//      }

//      return totalArea / rootArea;
//    }

//    /// <summary>
//    /// Get the maximum balance of an node in the tree. The balance is the difference
//    /// in height of the two children of a node.
//    /// </summary>
//    public int ComputeMaxBalance()
//    {
//      int maxBalance = 0;
//      for (int i = 0; i < this.nodeCapacity; ++i)
//      {
//        TreeNode node = this.nodes[i];
//        if (node.height <= 1)
//          continue;

//        Debug.Assert(node.IsLeaf == false);

//        int balance =
//          Math.Abs(
//            this.nodes[node.rightId].height -
//            this.nodes[node.leftId].height);
//        maxBalance = Math.Max(maxBalance, balance);
//      }

//      return maxBalance;
//    }

//    /// <summary>
//    /// Validate this tree. For testing.
//    /// </summary>
//    public void Validate()
//    {
//      ValidateStructure(this.rootId);
//      ValidateMetrics(this.rootId);

//      int freeCount = 0;
//      int freeIndex = this.freeList;
//      while (freeIndex != AABBTree.NULL_NODE)
//      {
//        Debug.Assert(0 <= freeIndex && freeIndex < this.nodeCapacity);
//        freeIndex = this.nodes[freeIndex].parentOrNextId;
//        freeCount++;
//      }

//      int expectedheight = 0;
//      if (this.rootId != AABBTree.NULL_NODE)
//        expectedheight = this.nodes[this.rootId].height;
//      Debug.Assert(expectedheight == this.ComputeHeight());

//      Debug.Assert(this.nodeCount + freeCount == this.nodeCapacity);
//    }

//    /// <summary>
//    /// Compute the height of a sub-tree.
//    /// </summary>
//    public int ComputeHeight(int nodeId)
//    {
//      Debug.Assert((0 <= nodeId) && (nodeId < this.nodeCapacity));

//      TreeNode node = this.nodes[nodeId];
//      if (node.IsLeaf)
//        return 0;

//      int height1 = ComputeHeight(node.leftId);
//      int height2 = ComputeHeight(node.rightId);
//      return Mathf.Max(height1, height2) + 1;
//    }

//    /// <summary>
//    /// Compute the height of the entire tree.
//    /// </summary>
//    public int ComputeHeight()
//    {
//      return this.ComputeHeight(this.rootId);
//    }

//    public void ValidateStructure(int index)
//    {
//      if (index == AABBTree.NULL_NODE)
//        return;

//      if (index == this.rootId)
//        Debug.Assert(this.nodes[index].parentOrNextId == AABBTree.NULL_NODE);

//      TreeNode node = this.nodes[index];
//      int left = node.leftId;
//      int right = node.rightId;

//      if (node.IsLeaf)
//      {
//        Debug.Assert(left == AABBTree.NULL_NODE);
//        Debug.Assert(right == AABBTree.NULL_NODE);
//        Debug.Assert(node.height == 0);
//        return;
//      }

//      Debug.Assert(0 <= left && left < this.nodeCapacity);
//      Debug.Assert(0 <= right && right < this.nodeCapacity);

//      Debug.Assert(this.nodes[left].parentOrNextId == index);
//      Debug.Assert(this.nodes[right].parentOrNextId == index);

//      this.ValidateStructure(left);
//      this.ValidateStructure(right);
//    }

//    public void ValidateMetrics(int index)
//    {
//      if (index == AABBTree.NULL_NODE)
//        return;

//      TreeNode node = this.nodes[index];

//      int left = node.leftId;
//      int right = node.rightId;
//      if (node.IsLeaf)
//      {
//        Debug.Assert(left == AABBTree.NULL_NODE);
//        Debug.Assert(right == AABBTree.NULL_NODE);
//        Debug.Assert(node.height == 0);
//        return;
//      }

//      Debug.Assert(0 <= left && left < this.nodeCapacity);
//      Debug.Assert(0 <= right && right < this.nodeCapacity);

//      int heightLeft = this.nodes[left].height;
//      int heightRight = this.nodes[right].height;
//      int height = Math.Max(heightLeft, heightRight) + 1;
//      Debug.Assert(node.height == height);

//      AABB testAABB = 
//        AABB.CreateMerged(this.nodes[left].AABB, this.nodes[right].AABB);
//      Debug.Assert(testAABB.BottomLeft == node.AABB.BottomLeft);
//      Debug.Assert(testAABB.TopRight == node.AABB.TopRight);

//      this.ValidateMetrics(left);
//      this.ValidateMetrics(right);
//    }

//    public void GizmoDraw(Color aabbColor)
//    {
//      this.DoGizmoDraw(aabbColor, this.rootId);
//    }

//    private void DoGizmoDraw(Color color, int nodeId)
//    {
//      if (nodeId == AABBTree.NULL_NODE)
//        return;

//      TreeNode node = this.nodes[nodeId];
//      node.AABB.GizmoDraw(color);

//      this.DoGizmoDraw(color, node.leftId);
//      this.DoGizmoDraw(color, node.rightId);
//    }

//    ///// <summary>
//    ///// Build an optimal tree. Very expensive. For testing.
//    ///// </summary>
//    //public void RebuildBottomUp()
//    //{
//    //  int[] nodes = new int[_nodeCount];
//    //  int count = 0;

//    //  // Build array of leaves. Free the rest.
//    //  for (int i = 0; i < this.nodeCapacity; ++i)
//    //  {
//    //    if (this.nodes[i].height < 0)
//    //    {
//    //      // free node in pool
//    //      continue;
//    //    }

//    //    if (this.nodes[i].IsLeaf())
//    //    {
//    //      this.nodes[i].parentOrNext = DynamicTree<T>.NULL_NODE;
//    //      nodes[count] = i;
//    //      ++count;
//    //    }
//    //    else
//    //    {
//    //      FreeNode(i);
//    //    }
//    //  }

//    //  while (count > 1)
//    //  {
//    //    float minCost = Settings.MaxFloat;
//    //    int iMin = -1, jMin = -1;
//    //    for (int i = 0; i < count; ++i)
//    //    {
//    //      AABB AABBi = this.nodes[nodes[i]].AABB;

//    //      for (int j = i + 1; j < count; ++j)
//    //      {
//    //        AABB AABBj = this.nodes[nodes[j]].AABB;
//    //        AABB b = new AABB();
//    //        b.Combine(ref AABBi, ref AABBj);
//    //        float cost = b.Perimeter;
//    //        if (cost < minCost)
//    //        {
//    //          iMin = i;
//    //          jMin = j;
//    //          minCost = cost;
//    //        }
//    //      }
//    //    }

//    //    int index1 = nodes[iMin];
//    //    int index2 = nodes[jMin];
//    //    TreeNode<T> left = this.nodes[index1];
//    //    TreeNode<T> right = this.nodes[index2];

//    //    int parentIndex = AllocateNode();
//    //    TreeNode<T> parent = this.nodes[parentIndex];
//    //    parent.left = index1;
//    //    parent.right = index2;
//    //    parent.height = 1 + Math.Max(left.height, right.height);
//    //    parent.AABB.Combine(ref left.AABB, ref right.AABB);
//    //    parent.parentOrNext = DynamicTree<T>.NULL_NODE;

//    //    left.parentOrNext = parentIndex;
//    //    right.parentOrNext = parentIndex;

//    //    nodes[jMin] = nodes[count - 1];
//    //    nodes[iMin] = parentIndex;
//    //    --count;
//    //  }

//    //  this.root = nodes[0];

//    //  Validate();
//    //}
//    #endregion
//  }
//}