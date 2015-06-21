using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
using Volatile.History;

public class VolatileQuadTree : MonoBehaviour 
{
  private QuadtreeBuffer buffer;
  public VolatileShape shape;

  private bool updated;

  void Awake()
  {
    this.buffer = new QuadtreeBuffer(0, 1, 10, 5, 0, 25.0f);
    this.buffer.AddShape(this.shape.Shape);

    this.updated = false;
  }

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.A))
    {
      this.buffer.Update(0);
      this.updated = true;
    }
  }

  void OnDrawGizmos()
  {
    if (this.updated == true)
    {
      Quadtree tree = this.buffer.GetQuadTree(0);
      tree.GizmoDraw(0, true);
    }
  }
}
