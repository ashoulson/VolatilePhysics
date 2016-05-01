using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class VolatileWorld : MonoBehaviour 
{
  public static VolatileWorld Instance { get { return instance; } }
  protected static VolatileWorld instance = null;

  //DynamicTree testTree = null;

  [SerializeField]
  int historyLength = 0;

  [SerializeField]
  bool doUpdate = true;

  public VoltWorld World { get; private set; }

  void Awake()
  {
    VolatileWorld.instance = this;
    this.World = new VoltWorld(this.historyLength);
  }

  void Start()
  {
    //this.testTree = new DynamicTree();
    //foreach (VoltBody body in this.World.Bodies)
      //testTree.AddBody(body);
  }

  void FixedUpdate()
  {
    if (this.doUpdate)
      this.World.Update();

    //foreach (VoltBody body in this.World.Bodies)
      //this.testTree.MoveBody(body, Vector2.zero);
  }

  void OnDrawGizmos()
  {
    //if (this.testTree != null)
      //this.testTree.GizmoDraw(Color.blue);
  }
}
