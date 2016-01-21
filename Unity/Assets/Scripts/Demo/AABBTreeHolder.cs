using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
//using Volatile.History;

public class AABBTreeHolder : MonoBehaviour 
{
  //private FarseerPhysics.Collision.DynamicTreeNew aabbTree = null;

  //[SerializeField]
  //VolatileBody[] bodies;

  //private int[] indices;

  //void Awake()
  //{
  //  this.aabbTree = new FarseerPhysics.Collision.DynamicTreeNew();
  //  this.indices = new int[this.bodies.Length];
  //}

  //void Start()
  //{
  //  for (int i = 0; i < this.bodies.Length; i++)
  //    this.indices[i] = this.aabbTree.AddProxy(this.bodies[i].body.AABB, this.bodies[i].body);
  //}

  //void FixedUpdate()
  //{
  //  for (int i = 0; i < this.bodies.Length; i++)
  //    this.aabbTree.MoveProxy(this.indices[i], this.bodies[i].body.AABB);
  //}

  //void OnDrawGizmos()
  //{
  //  if (this.aabbTree != null)
  //  {
  //    this.aabbTree.GizmoDraw(Color.blue);
  //  }
  //}

  private AABBTree aabbTree = null;

  [SerializeField]
  VolatileBody[] bodies;

  void Awake()
  {
    this.aabbTree = new AABBTree();
  }

  void Start()
  {
    for (int i = 0; i < this.bodies.Length; i++)
      this.aabbTree.AddBody(this.bodies[i].Body);
  }

  void FixedUpdate()
  {
    for (int i = 0; i < this.bodies.Length; i++)
      this.aabbTree.UpdateBody(this.bodies[i].Body);
  }

  void OnDrawGizmos()
  {
    if (this.aabbTree != null)
    {
      this.aabbTree.GizmoDraw(Color.blue);
    }
  }
}
