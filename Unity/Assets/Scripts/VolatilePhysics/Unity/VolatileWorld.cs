using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class VolatileWorld : MonoBehaviour 
{
  public static VolatileWorld Instance { get { return instance; } }
  private static VolatileWorld instance = null;

  private World world;

  void Awake()
  {
    this.world = new World(new Vector2(0.0f, -9.81f));
    instance = this;
  }

  void FixedUpdate()
  {
    this.world.RunPhysics(Time.fixedDeltaTime, 20);
  }

  public void AddBody(Body body)
  {
    this.world.AddBody(body);
  }
}
