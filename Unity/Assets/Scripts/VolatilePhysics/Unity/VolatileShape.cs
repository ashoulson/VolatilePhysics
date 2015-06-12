using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public abstract class VolatileShape : MonoBehaviour
{
  public abstract Shape Shape { get; }

  public abstract Shape PrepareShape(VolatileBody body);
  public abstract void DrawShapeInGame();
  public abstract void DrawShapeInEditor();
}
