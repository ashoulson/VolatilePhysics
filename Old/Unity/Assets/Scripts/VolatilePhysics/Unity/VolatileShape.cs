using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public abstract class VolatileShape : MonoBehaviour
{
  [SerializeField]
  protected float density = 1.0f;

  public abstract VoltShape PrepareShape(VoltWorld world);

  public abstract void DrawShapeInEditor();
  public abstract Vector2 ComputeTrueCenterOfMass();
}
