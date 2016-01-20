using System;
using System.Collections.Generic;

using UnityEngine;

public class GizmoDrawer : MonoBehaviour 
{
  public static GizmoDrawer Instance;

  public Vector2 start;
  public Vector2 end;

  void Awake()
  {
    Instance = this;
  }

  void OnDrawGizmos()
  {
    Gizmos.color = Color.magenta;
    Gizmos.DrawLine(start, end);
  }
}
