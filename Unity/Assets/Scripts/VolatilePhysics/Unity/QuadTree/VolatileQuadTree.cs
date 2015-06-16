using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
using Volatile.History;

public class VolatileQuadTree : MonoBehaviour 
{
  private Quadtree sqt;

  void Awake()
  {
    this.sqt = new Quadtree(10, 5, 0, 25.0f);

    BodyHandle record = new BodyHandle(0, new Vector2(1.0f, 1.0f), 0.0f);
    record.AABB = new AABB(new Vector2(1.0f, 1.0f), new Vector2(0.5f, 0.5f));
    this.sqt.Insert(record);
  }

  void OnDrawGizmos()
  {
    if (this.sqt != null)
      this.sqt.GizmoDraw(0, true);
  }
}
