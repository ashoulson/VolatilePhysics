using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Volatile.History
{
  internal partial class Quadtree
  {
    #region Tests
    ///// <summary>
    ///// Returns all found bodies contained in the hit cells. Note that this
    ///// does not check the actual bounding polygons of the contained bodies.
    ///// </summary>
    //private void Raycast(
    //  int time,
    //  ref Node node,
    //  ref BatchRay ray,
    //  float distance,
    //  PooledList<SnapshotLink> foundLinks)
    //{
    //  // If we have no contents or children, bail out
    //  if (node.TotalContained == 0)
    //    return;

    //  bool intersects =
    //    PhysicsMath.BatchRayIntersectAABB(
    //      ref ray,
    //      node.Min,
    //      node.Max,
    //      distance);

    //  if (intersects == true)
    //  {
    //    // Add our contained bodies if we have any
    //    var iter = node.ListFirst;
    //    for (; iter != null; iter = iter.HistNext(time))
    //      foundLinks.Add(iter);

    //    // Perform the raycast on our children if we have any
    //    if (node.HasChildren == true)
    //    {
    //      for (int i = 0; i < 4; i++)
    //      {
    //        int hashedKey = this.HashFind(node.GetChild(i));
    //        this.Raycast(
    //          time,
    //          ref this.nodes[hashedKey],
    //          ref ray,
    //          distance,
    //          foundLinks);
    //      }
    //    }
    //  }
    //}

    ///// <summary>
    ///// Returns all found bodies contained in the hit cells. Note that this
    ///// does not check the actual bounding polygons of the contained bodies.
    ///// </summary>
    //private void Spherecast(
    //  int time,
    //  ref Node node,
    //  ref BatchRay ray,
    //  float radius,
    //  float distance,
    //  PooledList<SnapshotLink> foundLinks)
    //{
    //  // If we have no contents or children, bail out
    //  if (node.TotalContained == 0)
    //    return;

    //  bool intersects =
    //    PhysicsMath.BatchMovingCircleIntersectAABB(
    //      ref ray,
    //      node.Min,
    //      node.Max,
    //      radius,
    //      distance);

    //  if (intersects == true)
    //  {
    //    // Add our contained bodies if we have any
    //    var iter = node.ListFirst;
    //    for (; iter != null; iter = iter.HistNext(time))
    //      foundLinks.Add(iter);

    //    // Perform the raycast on our children if we have any
    //    if (node.HasChildren == true)
    //    {
    //      for (int i = 0; i < 4; i++)
    //      {
    //        int hashedKey = this.HashFind(node.GetChild(i));
    //        this.Spherecast(
    //          time,
    //          ref this.nodes[hashedKey],
    //          ref ray,
    //          radius,
    //          distance,
    //          foundLinks);
    //      }
    //    }
    //  }
    //}
    #endregion

    #region Insert/Remove/Update
    /// <summary>
    /// Returns true iff we triggered an array resize
    /// </summary>
    private bool TreeInsert(ref Node node, BodyHandle handle)
    {
      // The node will never "reject" the link, so increment the number
      // of links contained no matter what
      node.TotalContained++;

      // If we have children, try each of them or just take the link ourselves
      if (node.HasChildren == true)
      {
        for (int i = 0; i < 4; i++)
        {
          int key = this.HashFind(node.GetChildKey(i));
          bool resized;
          bool success =
            this.TreeTryInsert(ref this.nodes[key], handle, out resized);
          if (success == true)
            return resized;
        }

        node.ListAdd(handle);
        handle.CellKey = node.Key;
        return false;
      }
      else // Otherwise, we need to see if we should split
      {
        if (node.ListCount < this.maxBodiesPerCell)
        {
          node.ListAdd(handle);
          handle.CellKey = node.Key;
          return false;
        }
        else
        {
          // See if the given AABB could fit in one of our quadrants
          bool couldFit = node.AABB.CouldFit(handle.AABB, 0.5f, 0.5f);

          if (node.Depth >= this.maxDepth || couldFit == false)
          {
            node.ListAdd(handle);
            handle.CellKey = node.Key;
            return false;
          }
          else // It's time to play a very dangerous game...
          {
            // Add the new link to the new list to hook it into the chain
            node.ListAdd(handle);

            // Now save the chain and cut the node's link to it. We're going
            // to clear everything out and re-insert each node plus the new one
            BodyHandle chain = node.ListFirst;

            // Note that this doesn't break the chain -between- the list nodes
            node.ListClear();

            // We can safely set this to zero because this node has no children
            node.TotalContained = 0;

            // After this split, shit completely hits the fan because the 
            // array could have been resized, which would invalidate our node
            // ref parameter. Since C# doesn't let us update pointers, we need
            // to check for an array resize and, if so, switch to using the
            // hashed array index directly.
            int? newKey = null;
            if (this.TreeSplit(ref node) == true)
              newKey = this.HashFind(node.Key);

            // Re-insert the bodies we just removed
            BodyHandle next;
            while (chain != null)
            {
              // Fetch the node's next link first, because insertion will
              // change the value after we do it
              next = chain.Next;

              if (newKey.HasValue == true)
              {
                // Update the key again if we need to
                if (this.TreeInsert(ref this.nodes[newKey.Value], chain))
                  newKey = this.HashFind(node.Key);
              }
              else
              {
                // We can just use the ref if the key hasn't changed
                if (this.TreeInsert(ref node, chain) == true)
                  newKey = this.HashFind(node.Key);
              }

              // Move to the next node on the chain
              chain = next;
            }

            return newKey.HasValue;
          }
        }
      }
    }

    /// <summary>
    /// Returns true iff we successfully inserted the link
    /// </summary>
    private bool TreeTryInsert(
      ref Node node,
      BodyHandle handle,
      out bool resized)
    {
      if (node.AABB.Contains(handle.AABB) == true)
      {
        resized = this.TreeInsert(ref node, handle);
        return true;
      }

      resized = false;
      return false;
    }

    private void TreeRemove(ref Node node, BodyHandle handle)
    {
      node.ListRemove(handle);
      TreePropagateChildRemoval(ref node);
    }

    private void TreePropagateChildRemoval(ref Node node)
    {
      node.TotalContained--;
      if (node.Key != ROOT_KEY)
        this.TreePropagateChildRemoval(
          ref this.nodes[this.HashFind(node.ParentKey)]);
    }

    private void TreeUpdate(ref Node node, BodyHandle handle)
    {
      // DEBUG
      bool found = false;
      for (var iter = node.ListFirst; iter != null; iter = iter.Next)
        if (iter == handle)
          found = true;
      Debug.Assert(found == true);
      // END DEBUG

      int? newKey = null;

      // We know we contain this collider
      if (node.AABB.Contains(handle.AABB) == true)
      {
        // No children + leaf = do nothing
        bool doNothing =
          node.HasChildren == false &&
          (node.ListCount <= this.maxBodiesPerCell ||
            node.Depth >= this.maxDepth);

        if (doNothing == false)
        {
          // Remove link. Since we're re-inserting in the same node,
          // we don't need to propagate the updated containment count.
          node.ListRemove(handle);
          node.TotalContained--;

          // But start insertion from this node
          if (this.TreeInsert(ref node, handle) == true)
            newKey = this.HashFind(node.Key);
        }
      }
      else
      {
        // Remove link
        node.ListRemove(handle);
        TreePropagateChildRemoval(ref node);

        // Insert into tree root
        int rootKey = this.HashFind(ROOT_KEY);
        if (this.TreeInsert(ref this.nodes[rootKey], handle) == true)
          newKey = this.HashFind(node.Key);
      }

      // Use the new key if we caused an array resize, or the ref otherwise
      if (newKey.HasValue == true)
        this.TreeMerge(ref this.nodes[newKey.Value]);
      else
        this.TreeMerge(ref node);
    }
    #endregion

    #region Split/Merge
    /// <summary>
    /// Returns true iff the array was resized during the split
    /// </summary>
    private bool TreeSplit(ref Node node)
    {
      bool resized = false;

      // Set the hasChildren first because the array might copy during the
      // process of adding children, and the node reference could be invalidated
      node.HasChildren = true;

      byte newDepth = (byte)(node.Depth + 1);

      AABB nodeAABB = node.AABB;
      Vector2 center = nodeAABB.Center;

      AABB topLeft = nodeAABB.ComputeTopLeft(center);
      AABB topRight = nodeAABB.ComputeTopRight(center);
      AABB bottomLeft = nodeAABB.ComputeBottomLeft(center);
      AABB bottomRight = nodeAABB.ComputeBottomRight(center);

      resized |= this.HashAdd(node.GetChildKey(0), newDepth, topLeft);
      resized |= this.HashAdd(node.GetChildKey(1), newDepth, topRight);
      resized |= this.HashAdd(node.GetChildKey(2), newDepth, bottomLeft);
      resized |= this.HashAdd(node.GetChildKey(3), newDepth, bottomRight);

      return resized;
    }

    private void TreeMerge(ref Node node)
    {
      if (node.ShouldMerge == false)
        return;

      // First recurse down and merge anything below us (we know it's empty)
      this.TreeMergeDown(ref node);

      // Next recurse upwards and start collapsing any ancestors we can
      this.TreeMergeUp(ref this.nodes[this.HashFind(node.ParentKey)]);
    }

    private void TreeMergeUp(ref Node node)
    {
      if (node.ShouldMerge == false)
        return;

      if (node.HasChildren == true)
      {
        for (int i = 0; i < 4; i++)
        {
          int childKey = node.GetChildKey(i);
          this.HashRemove(childKey);
        }
      }

      node.HasChildren = false;
      this.TreeMergeUp(ref this.nodes[this.HashFind(node.ParentKey)]);
    }

    private void TreeMergeDown(ref Node node)
    {
      if (node.HasChildren == true)
      {
        for (int i = 0; i < 4; i++)
        {
          int childKey = node.GetChildKey(i);
          int childKeyHashed = this.HashFind(childKey);
          this.TreeMergeDown(ref this.nodes[childKeyHashed]);
          this.HashRemove(childKey);
        }
      }

      node.HasChildren = false;
    }
    #endregion

    #region Debug
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
        Gizmos.color = new Color(1f, 1f, 1f, 1f);

        Vector2 topLeft = node.AABB.TopLeft;
        Vector2 topRight = node.AABB.TopRight;
        Vector2 bottomLeft = node.AABB.BottomLeft;
        Vector2 bottomRight = node.AABB.BottomRight;

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        //UnityEditor.Handles.Label(
        //  new Vector3(center.x, 0.0f, center.y), 
        //  node.TotalContained + 
        //  "\n" + 
        //  System.Convert.ToString(node.Key, 16));

        if (node.HasBodies == true)
        {
          Gizmos.color = boxColor;
          Gizmos.DrawCube(node.AABB.Center, node.AABB.Extent * 2.0f);
        }
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