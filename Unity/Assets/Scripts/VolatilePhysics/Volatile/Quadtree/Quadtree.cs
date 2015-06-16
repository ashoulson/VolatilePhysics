using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Volatile.History
{
  /// <summary>
  /// A quadtree stored entirely in-place in a single contiguous array. Can
  /// dynamically resize to accommodate more cells if needed. This array-based
  /// quadtree is designed to be stored and blitted across a rolling buffer
  /// for performing in-the-past raycasts on objects.
  /// </summary>
  internal partial class Quadtree
  {
    internal uint Time { get; set; }

    private int maxDepth;
    private int maxBodiesPerCell;

    internal Quadtree(
      int capacity,
      int maxDepth,
      int maxBodiesPerCell,
      float extent)
    {
      this.Time = Config.INVALID_TIME;
      this.HashInit(capacity);
      this.maxDepth = maxDepth;
      this.maxBodiesPerCell = maxBodiesPerCell;
      this.HashAdd(
        ROOT_KEY, 0, new AABB(Vector2.zero, new Vector2(extent, extent)));
    }

    internal void Insert(BodyHandle handle)
    {
      int key = this.HashFind(ROOT_KEY);
      this.TreeInsert(ref this.nodes[key], handle);
    }

    internal void Update(BodyHandle handle)
    {
      int key = this.HashFind(handle.CellKey);
      this.TreeUpdate(ref this.nodes[key], handle);
    }

    ///// <summary>
    ///// Returns all found bodies contained in the hit cells. Note that this
    ///// does not check the actual bounding polygons of the contained bodies.
    ///// </summary>
    //internal void Raycast(
    //  int time,
    //  Vector2 origin, 
    //  Vector2 direction,
    //  out RayHits hits,
    //  float distance,
    //  int layerMask)
    //{
    //  BatchRay batchRay = new BatchRay(origin, direction);
    //  PooledList<SnapshotLink> internalHits = 
    //    PooledList<SnapshotLink>.Acquire();

    //  this.Raycast(
    //    time,
    //    ref this.nodes[this.HashFind(ROOT_KEY)],
    //    ref batchRay,
    //    distance,
    //    internalHits);

    //  hits = RayHits.Acquire();
    //  for (int i = 0; i < internalHits.Count; i++)
    //    internalHits[i].Raycast(
    //      time, 
    //      ref batchRay, 
    //      hits,
    //      distance,
    //      layerMask);
    //}

    ///// <summary>
    ///// Returns all found bodies contained in the hit cells. Note that this
    ///// does not check the actual bounding polygons of the contained bodies.
    ///// </summary>
    //internal void Spherecast(
    //  int time,
    //  Vector2 origin,
    //  Vector2 direction,
    //  float radius,
    //  out RayHits hits,
    //  float distance,
    //  int layerMask)
    //{
    //  BatchRay batchRay = new BatchRay(origin, direction);
    //  PooledList<SnapshotLink> internalHits =
    //    PooledList<SnapshotLink>.Acquire();

    //  this.Spherecast(
    //    time,
    //    ref this.nodes[this.HashFind(ROOT_KEY)],
    //    ref batchRay,
    //    radius,
    //    distance,
    //    internalHits);

    //  hits = RayHits.Acquire();
    //  for (int i = 0; i < internalHits.Count; i++)
    //    internalHits[i].Spherecast(
    //      time,
    //      ref batchRay,
    //      radius,
    //      hits,
    //      distance,
    //      layerMask);
    //}

    //internal void Remove(History history)
    //{
    //  int key = this.HashFind(history.Cell);
    //  this.TreeRemove(ref this.nodes[key], history);
    //}

    //internal void StoreOnto(SnapshotQuadTree other)
    //{
    //  other.maxDepth = this.maxDepth;
    //  other.maxBodiesPerCell = this.maxBodiesPerCell;
    //  this.HashBlit(other);
    //}

    internal void GizmoDraw(int time, bool drawGrid)
    {
      int key = this.HashFind(ROOT_KEY);
      this.GizmoDraw(time, ref this.nodes[key], drawGrid);
    }

    internal void GizmoDraw(int time, bool drawGrid, Color boxColor)
    {
      int key = this.HashFind(ROOT_KEY);
      this.GizmoDraw(time, ref this.nodes[key], drawGrid, boxColor);
    }
  }
}