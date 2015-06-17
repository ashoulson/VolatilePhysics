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

    internal void Insert(BodyHandle handle, AABB aabb)
    {
      int key = this.HashFind(ROOT_KEY);
      this.TreeInsert(ref this.nodes[key], handle, aabb);
    }

    internal void Update(BodyHandle handle, AABB aabb)
    {
      int key = this.HashFind(handle.CellKey);
      this.TreeUpdate(ref this.nodes[key], handle, aabb);
    }

    internal void Remove(BodyHandle handle)
    {
      int key = this.HashFind(handle.CellKey);
      this.TreeRemove(ref this.nodes[key], handle);
    }

    internal void StoreOnto(Quadtree other)
    {
      other.maxDepth = this.maxDepth;
      other.maxBodiesPerCell = this.maxBodiesPerCell;
      this.HashBlit(other);
    }

    #region Debug
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

    private void GizmoDraw(
      int time,
      ref Node node,
      bool drawGrid)
    {
      this.GizmoDraw(time, ref node, drawGrid, new Color(0f, 1f, 0f, 0.3f));
    }

    private void GizmoDraw(
      int time,
      ref Node node,
      bool drawGrid,
      Color boxColor)
    {
      if (drawGrid == true)
      {
        node.GizmoDraw(time, boxColor);
      }

      if (node.HasChildren == true)
      {
        for (int i = 0; i < 4; i++)
        {
          int key = this.HashFind(node.GetChildKey(i));
          this.GizmoDraw(time, ref this.nodes[key], drawGrid, boxColor);
        }
      }

      //for (var iter = node.ListFirst; iter != null; iter = iter.HistNext(time))
      //  iter.GizmoDraw(time);
    }
    #endregion
  }
}