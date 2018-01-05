using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class VolatileWorld : MonoBehaviour 
{
  public static VolatileWorld Instance { get { return instance; } }
  protected static VolatileWorld instance = null;

  

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

  void FixedUpdate()
  {
    if (this.doUpdate)
      this.World.Update();
  }
}
