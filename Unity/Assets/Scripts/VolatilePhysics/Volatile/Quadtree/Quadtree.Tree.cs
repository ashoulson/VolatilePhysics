using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Volatile.History
{
  internal partial class Quadtree
  {
    #region Insert/Remove/Update
    private void TreeInsert(ref Node node, BodyHandle handle)
    {
      // The node will never reject the link, we add it here no matter what
      node.TotalContained++;

      // If we have children, try each of them or just take the link ourselves
      if (node.HasChildren == true)
      {
        for (int i = 0; i < 4; i++)
        {
          int key = this.HashFind(node.GetChildKey(i));
          if (this.TreeTryInsert(ref this.nodes[key], handle))
            return;
        }

        node.ListAdd(handle);
        handle.CellKey = node.Key;
      }
      else if (
        node.ListCount < this.maxBodiesPerCell || 
        node.Depth >= this.maxDepth ||
        node.CouldFit(handle, 0.5f, 0.5f) == false)
      {
        node.ListAdd(handle);
        handle.CellKey = node.Key;
      }
      else // We need to split
      {
        // Add the new handle to the end of the chain, then rip out the
        // chain and re-add everything that was on it
        node.ListAdd(handle);
        BodyHandle chain = node.ListFirst;
        node.ListClear();
        node.TotalContained = 0;
        node.Split(this);

        // Re-insert the bodies we just removed
        BodyHandle next;
        while (chain != null)
        {
          next = chain.Next; // Make sure to fetch this before the insert
          int latestHash = this.HashFind(node.Key); // Old key may be invalid
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
      BodyHandle handle)
    {
      if (node.IsInBounds(handle) == true)
      {
        this.TreeInsert(ref node, handle);
        return true;
      }

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
      #region DEBUG
      // DEBUG
      bool found = false;
      for (var iter = node.ListFirst; iter != null; iter = iter.Next)
        if (iter == handle)
          found = true;
      Debug.Assert(found == true);
      // END DEBUG
      #endregion

      if (node.IsInBounds(handle) == true)
      {
        // In bounds, so see if we should re-insert at this node
        bool shouldReinsert =
          node.HasChildren || 
          (node.ListCount > maxBodiesPerCell && node.Depth < maxDepth);

        if (shouldReinsert == true)
        {
          node.ListRemove(handle);
          node.TotalContained--;
          this.TreeInsert(ref node, handle);
        }
      }
      else 
      {
        // Out of bounds, so re-insert from the root
        node.ListRemove(handle);
        TreePropagateChildRemoval(ref node);
        this.TreeInsert(ref this.nodes[this.HashFind(ROOT_KEY)], handle);
      }

      // We may have resized
      this.TreeMerge(ref this.nodes[this.HashFind(node.Key)]);
    }
    #endregion

    #region Split/Merge
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
  }
}