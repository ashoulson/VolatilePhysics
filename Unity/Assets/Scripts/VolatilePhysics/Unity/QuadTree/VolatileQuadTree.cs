using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
using Volatile.History;

public class VolatileQuadTree : MonoBehaviour 
{
  private Quadtree sqt;
  private BodyHandle handle;

  public Transform root;

  void Awake()
  {
    this.sqt = new Quadtree(10, 5, 0, 25.0f);

    this.handle = new BodyHandle(0, new Vector2(1.0f, 1.0f), 0.0f);
    handle.AABB = new AABB(root.transform.position, new Vector2(0.5f, 0.5f));
    this.sqt.Insert(handle);
  }

  void Update()
  {
    this.handle.AABB = new AABB(root.transform.position, new Vector2(0.5f, 0.5f));
    this.sqt.Update(this.handle);
  }


  void OnDrawGizmos()
  {
    if (this.sqt != null)
      this.sqt.GizmoDraw(0, true);
    if (this.root != null)
    {
      VolatileDebug.DrawAABB(new AABB(root.transform.position, new Vector2(0.5f, 0.5f)), Color.red);
    }
  }
}
