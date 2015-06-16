using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Volatile.History
{
  internal partial class Quadtree
  {
    private const int HASH_MASK = 0x7FFFFFFF;
    private readonly static IEqualityComparer<int> comparer =
      EqualityComparer<int>.Default;

    private int[] buckets;
    private Node[] nodes;
    private int count;
    private int freeList;
    private int freeCount;

    internal int HashCount
    {
      get { return this.count - this.freeCount; }
    }

    internal int HashCapacity
    {
      get { return this.nodes.Length; }
    }

    private void HashInit(int capacity)
    {
      if (capacity < 1)
        throw new ArgumentOutOfRangeException();

      int size = HashHelpers.GetPrime(capacity);
      this.buckets = new int[size];
      for (int i = 0; i < this.buckets.Length; i++)
        this.buckets[i] = -1;
      this.nodes = new Node[size];
      this.freeList = -1;
    }

    /// <summary>
    /// Blits the entire hash table onto another
    /// </summary>
    private void HashBlit(Quadtree other)
    {
      if (other.buckets.Length != this.buckets.Length)
        other.buckets = new int[this.buckets.Length];
      if (other.nodes.Length != this.nodes.Length)
        other.nodes = new Node[this.nodes.Length];

      Array.Copy(this.buckets, other.buckets, this.buckets.Length);
      Array.Copy(this.nodes, other.nodes, this.nodes.Length);

      other.count = this.count;
      other.freeList = this.freeList;
      other.freeCount = this.freeCount;
    }

    /// <summary>
    /// Takes in a non-hashed key and returns the actual array index.
    /// </summary>
    private int HashFind(int key)
    {
      if (this.buckets != null)
      {
        int hashCode = comparer.GetHashCode(key) & HASH_MASK;
        int target = hashCode % this.buckets.Length;

        for (int i = this.buckets[target]; i >= 0; i = this.nodes[i].HashNext)
          if (this.nodes[i].Key == key)
            return i;
      }
      return -1;
    }

    /// <summary>
    /// Takes in a non-hashed key. Returns true iff the array was resized 
    /// during the addition.
    /// </summary>
    private bool HashAdd(
      int key,
      byte depth,
      AABB aabb)
    {
      if (this.buckets == null)
        this.HashInit(0);

      int hashCode = comparer.GetHashCode(key) & HASH_MASK;
      int target = hashCode % this.buckets.Length;

      // DEBUG: Check for existing node
      for (int i = this.buckets[target]; i >= 0; i = this.nodes[i].HashNext)
        if (this.nodes[i].Key == key)
          throw new ArgumentException("Duplicate: " + key.ToString());

      // Keep track of whether we resized or not
      bool wasResized = false;

      int index;
      if (this.freeCount > 0)
      {
        index = this.freeList;
        freeList = this.nodes[index].HashNext;
        this.freeCount--;
      }
      else
      {
        if (this.count == this.nodes.Length)
        {
          this.HashResize();
          wasResized = true;
          target = hashCode % this.buckets.Length;
        }
        index = this.count;
        this.count++;
      }

      this.nodes[index].HashNext = buckets[target];
      this.nodes[index].Key = key;

      // Write the values to the node
      this.nodes[index].Set(depth, false, aabb, 0);

      this.buckets[target] = index;

      return wasResized;
    }

    /// <summary>
    /// Takes in a non-hashed key. Returns true iff the node was found.
    /// </summary>
    private bool HashRemove(int key)
    {
      if (this.buckets != null)
      {
        int hashCode = comparer.GetHashCode(key) & HASH_MASK;
        int target = hashCode % this.buckets.Length;

        int last = -1;
        for (int i = buckets[target]; i >= 0; last = i, i = nodes[i].HashNext)
        {
          if (this.nodes[i].Key == key)
          {
            if (last < 0)
            {
              buckets[target] = nodes[i].HashNext;
            }
            else
            {
              nodes[last].HashNext = nodes[i].HashNext;
            }

            nodes[i].HashNext = this.freeList;
            nodes[i].Key = INVALID_KEY;

            // Clear out the list for garbage collection
            nodes[i].ListClear();

            this.freeList = i;
            freeCount++;
            return true;
          }
        }
      }

      Debug.Assert(false, "Node not found for removal");
      return false;
    }

    private void HashResize()
    {
      this.HashResize(HashHelpers.ExpandPrime(this.count));
    }

    private void HashResize(int newSize)
    {
      Debug.Assert(newSize >= this.nodes.Length);

      int[] newBuckets = new int[newSize];
      for (int i = 0; i < newBuckets.Length; i++)
        newBuckets[i] = -1;

      Node[] newNodes = new Node[newSize];
      Array.Copy(this.nodes, 0, newNodes, 0, this.count);

      for (int i = 0; i < count; i++)
      {
        if (newNodes[i].HashIsValid == true)
        {
          int hashCode =
            comparer.GetHashCode(newNodes[i].Key) & HASH_MASK;
          int bucket = hashCode % newSize;
          newNodes[i].HashNext = newBuckets[bucket];
          newBuckets[bucket] = i;
        }
      }

      this.buckets = newBuckets;
      this.nodes = newNodes;
    }
  }
}