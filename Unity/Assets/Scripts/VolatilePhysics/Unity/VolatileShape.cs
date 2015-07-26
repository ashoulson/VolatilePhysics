using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public abstract class VolatileShape : MonoBehaviour
{
  [SerializeField]
  protected float density = 1.0f;

  public abstract Shape Shape { get; }
  public abstract void PrepareShape();

  public abstract void DrawShapeInEditor();
  public abstract Vector2 ComputeTrueCenterOfMass();

  void Awake()
  {
    this.PrepareShape();
  }
}
