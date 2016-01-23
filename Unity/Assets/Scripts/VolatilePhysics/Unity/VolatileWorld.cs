using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class VolatileWorld : MonoBehaviour 
{
  public static VolatileWorld Instance { get { return instance; } }
  protected static VolatileWorld instance = null;

  [SerializeField]
  int historyLength = 0;

  public World world;
  public bool doFixedUpdate;

  public int CurrentFrame { get { return this.currentFrame; } }
  private int currentFrame;

  void Awake()
  {
    VolatileWorld.instance = this;
    this.currentFrame = 0;
    this.world = new World(this.historyLength);
  }

  void Start()
  {
  }

  void FixedUpdate()
  {
    if (this.doFixedUpdate == true)
      this.world.Update(this.currentFrame++);
  }

  public void AddBody(Body body)
  {
    this.world.AddBody(body);
  }
}
