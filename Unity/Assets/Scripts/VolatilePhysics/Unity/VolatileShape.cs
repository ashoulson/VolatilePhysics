using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public abstract class VolatileShape : MonoBehaviour
{
  [SerializeField]
  protected float density = 1.0f;

  public abstract Shape Shape { get; }

  public abstract Shape PrepareShape(VolatileBody body);

  public abstract void DrawShapeInGame();
  public abstract void DrawShapeInEditor();
}
