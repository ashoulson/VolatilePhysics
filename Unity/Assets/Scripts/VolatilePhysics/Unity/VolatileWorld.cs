using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class VolatileWorld : MonoBehaviour 
{
  public static VolatileWorld Instance { get { return instance; } }
  protected static VolatileWorld instance = null;

  public World world;
  public bool doFixedUpdate;

  void Awake()
  {
    VolatileWorld.instance = this;
    this.world = new World(new Vector2(0.0f, -9.81f));
  }

  void Start()
  {
  }

  void FixedUpdate()
  {
    if (this.doFixedUpdate == true)
      this.world.Update();
  }

  public void WorldUpdate()
  {
    this.world.Update();
  }

  public void AddBody(Body body)
  {
    this.world.AddBody(body);
  }
}
