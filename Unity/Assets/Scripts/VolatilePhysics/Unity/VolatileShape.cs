using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public abstract class VolatileShape : MonoBehaviour
{
  [SerializeField]
  protected float density = 1.0f;

  // TODO: This is mostly a debug feature
  [SerializeField]
  public bool isStandalone = true;

  public abstract Shape Shape { get; }
  public abstract void PrepareShape();

  public abstract void DrawShapeInEditor();
  public abstract Vector2 ComputeTrueCenterOfMass();

  void Awake()
  {
    this.PrepareShape();
  }

  void Update()
  {
    // TODO: This is mostly a debug feature
    if (this.isStandalone == true && this.Shape != null)
    {
      this.Shape.SetWorld(transform.position, transform.right);
    }
  }

  void OnDrawGizmos()
  {
    // TODO: This is mostly a debug feature
    if (this.isStandalone == true)
    {
      Color current = Gizmos.color;

      if (this.Shape != null)
      {
        VolatileUtil.Draw(this.Shape);
      }
      else
      {
        this.DrawShapeInEditor();
      }

      Gizmos.color = current;
    }
  }
}
