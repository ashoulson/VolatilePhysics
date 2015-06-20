using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
using Volatile.History;

public class VolatileQuadTree : MonoBehaviour 
{
  private MutableQuadtree sqt;

  private BodyHandle handle1;
  private BodyHandle handle2;

  public Transform root1;
  public Transform root2;

  void Awake()
  {
    this.sqt = new MutableQuadtree(10, 5, 0, 25.0f);

    this.handle1 = new BodyHandle(0, new Vector2(1.0f, 1.0f), 0.0f);
    this.handle2 = new BodyHandle(0, new Vector2(1.0f, 1.0f), 0.0f);
    this.sqt.Insert(handle1, new AABB(root1.transform.position, new Vector2(0.5f, 0.5f)));
    this.sqt.Insert(handle2, new AABB(root2.transform.position, new Vector2(0.5f, 0.5f)));
  }

  void Update()
  {
    this.sqt.Update(this.handle1, new AABB(root1.transform.position, new Vector2(0.5f, 0.5f)));
    this.sqt.Update(this.handle2, new AABB(root2.transform.position, new Vector2(0.5f, 0.5f)));
  }

  void OnDrawGizmos()
  {
    if (this.sqt != null)
      this.sqt.GizmoDraw(0, true);
    if (this.root1 != null)
      VolatileDebug.DrawAABB(new AABB(root1.transform.position, new Vector2(0.5f, 0.5f)), Color.red);
    if (this.root2 != null)
      VolatileDebug.DrawAABB(new AABB(root2.transform.position, new Vector2(0.5f, 0.5f)), Color.red);
  }
}
