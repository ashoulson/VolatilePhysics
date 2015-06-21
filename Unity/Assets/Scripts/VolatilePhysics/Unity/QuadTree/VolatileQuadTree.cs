using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
using Volatile.History;

public class VolatileQuadTree : MonoBehaviour 
{
  private MutableQuadtree active;
  private Quadtree stored;

  public VolatileShape shape;
  private ShapeHandle handle;

  void Awake()
  {
    this.active = new MutableQuadtree(10, 5, 0, 25.0f);
    this.stored = new Quadtree();
    this.handle = new ShapeHandle(this.shape.Shape);
    this.active.Insert(handle);
  }

  void Update()
  {
    this.active.Update(this.handle);
    if (Input.GetKeyDown(KeyCode.B))
    {
      this.active.BlitOnto(this.stored);
      this.handle.RecordState(0);
    }
  }

  void OnDrawGizmos()
  {
    if (this.stored != null)
    {
      this.stored.GizmoDraw(0, true);
      
      this.handle.Rollback(0);
      this.shape.DrawShapeInGame();
    }
  }
}
