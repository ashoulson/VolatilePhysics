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
  internal sealed class MutableQuadtree : Quadtree
  {
    private int maxDepth;
    private int maxBodiesPerCell;

    internal int Time 
    {
      get { return this.time; }
      set { this.time = value; }
    }

    internal MutableQuadtree(
      int startingTime,
      int initialCapacity,
      int maxDepth,
      int maxBodiesPerCell,
      float extent)
    {
      this.time = startingTime;
      this.HashInit(initialCapacity);
      this.maxDepth = maxDepth;
      this.maxBodiesPerCell = maxBodiesPerCell;
      this.HashAdd(
        ROOT_KEY, 0, new AABB(Vector2.zero, new Vector2(extent, extent)));
    }

    internal void Insert(ShapeHandle entry)
    {
      int key = this.HashFind(ROOT_KEY);
      this.TreeInsert(ref this.nodes[key], entry);
    }

    internal void Update(ShapeHandle entry)
    {
      int key = this.HashFind(entry.cellKey);
      this.TreeUpdate(ref this.nodes[key], entry);
    }

    internal void Remove(ShapeHandle entry)
    {
      int key = this.HashFind(entry.cellKey);
      this.TreeRemove(ref this.nodes[key], entry);
    }

    internal void BlitOnto(Quadtree other)
    {
      other.ReceiveBlit(this);
    }

    #region Tree Functionality
    private void TreeInsert(ref Node node, ShapeHandle entry)
    {
      // The node will never reject the link, we add it here no matter what
      node.totalContained++;

      // If we have children, try each of them or just take the link ourselves
      if (node.hasChildren == true)
      {
        for (int i = 0; i < 4; i++)
        {
          int key = this.HashFind(node.ChildKey(i));
          if (this.TreeTryInsert(ref this.nodes[key], entry))
            return;
        }

        this.NodeListAdd(ref node, entry);
        entry.cellKey = node.key;
      }
      else if (
        node.listCount < this.maxBodiesPerCell ||
        node.depth >= this.maxDepth ||
        // We only ever consider the current AABB when inserting/updating
        node.AABBCouldFit(entry.CurrentAABB, 0.5f, 0.5f) == false)
      {
        this.NodeListAdd(ref node, entry);
        entry.cellKey = node.key;
      }
      else // We need to split
      {
        // Add the new entry to the end of the chain, then rip out the
        // chain and re-add everything that was on it
        this.NodeListAdd(ref node, entry);
        ShapeHandle chain = node.listFirst;
        this.NodeListClear(ref node);
        node.totalContained = 0;
        this.NodeSplit(ref node);

        // Re-insert the bodies we just removed
        ShapeHandle next;
        while (chain != null)
        {
          next = chain.next; // Make sure to fetch this before the insert
          int latestHash = this.HashFind(node.key); // Old key may be invalid
          this.TreeInsert(ref this.nodes[latestHash], chain);
          chain = next;
        }
      }
    }

    /// <summary>
    /// Returns true iff we successfully inserted the link
    /// </summary>
    private bool TreeTryInsert(
      ref Node node,
      ShapeHandle entry)
    {
      // We only ever consider the current AABB when inserting/updating
      if (node.AABBContains(entry.CurrentAABB) == true)
      {
        this.TreeInsert(ref node, entry);
        return true;
      }

      return false;
    }

    private void TreeRemove(ref Node node, ShapeHandle entry)
    {
      this.NodeListRemove(ref node, entry);
      this.TreePropagateChildRemoval(ref node);
    }

    private void TreePropagateChildRemoval(ref Node node)
    {
      node.totalContained--;
      if (node.key != ROOT_KEY)
        this.TreePropagateChildRemoval(
          ref this.nodes[this.HashFind(node.ParentKey)]);
    }

    private void TreeUpdate(ref Node node, ShapeHandle entry)
    {
      Debug.Assert(node.ListContains(entry));

      // We only ever consider the current AABB when inserting/updating
      if (node.AABBContains(entry.CurrentAABB) == true)
      {
        // AABB can fit, so see if we should re-insert at this node
        bool shouldReinsert =
          node.hasChildren ||
          (node.listCount > maxBodiesPerCell && node.depth < maxDepth);

        if (shouldReinsert == true)
        {
          this.NodeListRemove(ref node, entry);
          node.totalContained--;
          this.TreeInsert(ref node, entry);
        }
      }
      else
      {
        // Out of bounds, so re-insert from the root
        this.NodeListRemove(ref node, entry);
        TreePropagateChildRemoval(ref node);
        this.TreeInsert(ref this.nodes[this.HashFind(ROOT_KEY)], entry);
      }

      // We may have resized, so get a new reference from the key
      this.TreeMerge(ref this.nodes[this.HashFind(node.key)]);
    }

    private void TreeMerge(ref Node node)
    {
      if (node.ShouldMerge == false)
        return;
      this.TreeMergeDown(ref node);
      this.TreeMergeUp(ref this.nodes[this.HashFind(node.ParentKey)]);
    }

    private void TreeMergeUp(ref Node node)
    {
      if (node.ShouldMerge == false)
        return;

      if (node.hasChildren == true)
      {
        for (int i = 0; i < 4; i++)
        {
          int childKey = node.ChildKey(i);
          this.HashRemove(childKey);
        }
      }

      node.hasChildren = false;
      this.TreeMergeUp(ref this.nodes[this.HashFind(node.ParentKey)]);
    }

    private void TreeMergeDown(ref Node node)
    {
      if (node.hasChildren == true)
      {
        for (int i = 0; i < 4; i++)
        {
          int childKey = node.ChildKey(i);
          int childKeyHashed = this.HashFind(childKey);
          this.TreeMergeDown(ref this.nodes[childKeyHashed]);
          this.HashRemove(childKey);
        }
      }

      node.hasChildren = false;
    }
    #endregion

    #region Hash Functionality
    /// <summary>
    /// Initialize the hash table.
    /// </summary>
    private void HashInit(int capacity)
    {
      Debug.Assert(capacity > 0);

      int size = HashHelpers.GetPrime(capacity);
      this.buckets = new int[size];
      for (int i = 0; i < this.buckets.Length; i++)
        this.buckets[i] = -1;
      this.nodes = new Node[size];
      this.freeList = -1;
    }

    /// <summary>
    /// Takes in a non-hashed key.
    /// </summary>
    private void HashAdd(
      int key,
      byte depth,
      AABB aabb)
    {
      Debug.Assert(this.nodes != null);
      Debug.Assert(this.HashFind(key) == -1);

      int index;
      if (this.freeCount > 0)
      {
        index = this.freeList;
        freeList = this.nodes[index].hashNext;
        this.freeCount--;
      }
      else
      {
        if (this.count == this.nodes.Length)
          this.HashResize();
        index = this.count;
        this.count++;
      }

      int bucket = this.GetBucket(key);
      this.NodeInit(ref this.nodes[index], depth, aabb);
      this.nodes[index].hashNext = buckets[bucket];
      this.nodes[index].key = key;

      this.buckets[bucket] = index;
    }

    /// <summary>
    /// Takes in a non-hashed key.
    /// </summary>
    private void HashRemove(int key)
    {
      Debug.Assert(this.nodes != null);
      Debug.Assert(this.HashFind(key) != -1);

      int bucket = this.GetBucket(key);
      int last = -1;
      for (
        int i = this.buckets[bucket]; 
        i >= 0; 
        last = i, i = this.nodes[i].hashNext)
      {
        if (this.nodes[i].key == key)
        {
          if (last < 0)
          {
            this.buckets[bucket] = this.nodes[i].hashNext;
          }
          else
          {
            this.nodes[last].hashNext = this.nodes[i].hashNext;
          }

          this.NodeFree(ref nodes[i]);
          this.nodes[i].hashNext = this.freeList;
          this.nodes[i].key = INVALID_KEY;

          this.freeList = i;
          this.freeCount++;
        }
      }
    }

    /// <summary>
    /// Expands the hash table to the lowest prime > 2 * count.
    /// </summary>
    private void HashResize()
    {
      this.HashResize(HashHelpers.ExpandPrime(this.count));
    }

    /// <summary>
    /// Expands the hash table to the given size (should be prime).
    /// </summary>
    private void HashResize(int newSize)
    {
      Debug.Assert(newSize >= this.nodes.Length);

      Node[] newNodes = new Node[newSize];
      int[] newBuckets = new int[newSize];

      Array.Copy(this.nodes, 0, newNodes, 0, this.count);
      for (int i = 0; i < newBuckets.Length; i++)
        newBuckets[i] = -1;

      for (int i = 0; i < this.count; i++)
      {
        if (newNodes[i].IsValid == true)
        {
          int bucket = this.GetBucket(newNodes[i].key, newSize);
          newNodes[i].hashNext = newBuckets[bucket];
          newBuckets[bucket] = i;
        }
      }

      this.buckets = newBuckets;
      this.nodes = newNodes;
    }
    #endregion

    #region Node Functionality
    private bool NodeShouldSplit(ref Node node)
    {
      return 
        node.listCount > this.maxBodiesPerCell && 
        node.depth < this.maxDepth;
    }

    private void NodeInit(
        ref Node node, 
        byte depth,
        AABB aabb)
    {
      node.depth = depth;
      node.aabb = aabb;

      node.hasChildren = false;
      node.totalContained = 0;
      this.NodeListClear(ref node);
    }

    private void NodeFree(ref Node node)
    {
      this.NodeListClear(ref node);
    }

    private void NodeListAdd(ref Node node, ShapeHandle entry)
    {
      entry.next = node.listFirst;
      if (node.listFirst != null)
        node.listFirst.prev = entry;
      node.listFirst = entry;
      if (node.listLast == null)
        node.listLast = entry;
      entry.prev = null;
      node.listCount++;
    }

    private void NodeListRemove(ref Node node, ShapeHandle entry)
    {
      ShapeHandle nextEntry = entry.next;
      ShapeHandle prevEntry = entry.prev;

      if (node.listFirst == entry)
        node.listFirst = nextEntry;
      if (nextEntry != null)
        nextEntry.prev = prevEntry;
      if (prevEntry != null)
        prevEntry.next = nextEntry;
      node.listCount--;
    }

    private void NodeListClear(ref Node node)
    {
      node.listFirst = null;
      node.listLast = null;
      node.listCount = 0;
    }

    private void NodeSplit(ref Node node)
    {
      // Set the hasChildren first because the array might copy during the
      // process of adding children, and the node reference could be invalidated
      node.hasChildren = true;

      byte newDepth = (byte)(node.depth + 1);
      Vector2 center = node.aabb.Center;

      AABB topLeft = node.aabb.ComputeTopLeft(center);
      AABB topRight = node.aabb.ComputeTopRight(center);
      AABB bottomLeft = node.aabb.ComputeBottomLeft(center);
      AABB bottomRight = node.aabb.ComputeBottomRight(center);

      this.HashAdd(node.ChildKey(0), newDepth, topLeft);
      this.HashAdd(node.ChildKey(1), newDepth, topRight);
      this.HashAdd(node.ChildKey(2), newDepth, bottomLeft);
      this.HashAdd(node.ChildKey(3), newDepth, bottomRight);
    }
    #endregion
  }
}