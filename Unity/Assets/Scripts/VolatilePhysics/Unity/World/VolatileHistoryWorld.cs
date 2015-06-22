using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
using Volatile.History;

public class VolatileHistoryWorld : VolatileWorld
{
  private int timeOffset = 0;
  private Quadtree currentTree = null;

  void Awake()
  {
    VolatileWorld.instance = this;
    this.world =
      new HistoryWorld(
        0,
        300,
        13,
        5,
        0,
        25.0f,
        new Vector2(0.0f, -9.81f));
  }

  void Start()
  {
    this.world.Initialize();
  }

  private int GetOffsetTime()
  {
    HistoryWorld historyWorld = (HistoryWorld)this.world;
    return historyWorld.CurrentTime + this.timeOffset;
  }

  void Update()
  {
    HistoryWorld historyWorld = (HistoryWorld)this.world;

    if (Input.GetKey(KeyCode.U))
    {
      this.world.Update();
    }
    if (Input.GetKey(KeyCode.Minus))
    {
      if (this.timeOffset > -299 && this.GetOffsetTime() > 0)
        this.timeOffset--;
    }
    if (Input.GetKey(KeyCode.Equals))
    {
      if (this.timeOffset < 0)
        this.timeOffset++;
    }

    this.currentTree = historyWorld.GetTree(this.GetOffsetTime());
  }

  void FixedUpdate()
  {
  }

  void OnDrawGizmos()
  {
    if (this.currentTree != null)
      Util.Draw(this.currentTree);
  }
}