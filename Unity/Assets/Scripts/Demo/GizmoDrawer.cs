using System;
using System.Collections.Generic;

using UnityEngine;

public class GizmoDrawer : MonoBehaviour 
{
  public static GizmoDrawer Instance;

  public List<Vector2> starts = new List<Vector2>();
  public List<Vector2> ends = new List<Vector2>();
  public List<Color> colors = new List<Color>();

  public Vector2 point = Vector2.zero;

  void Awake()
  {
    Instance = this;
  }

  public void AddLine(Vector2 start, Vector2 end, Color color)
  {
    this.starts.Add(start);
    this.ends.Add(end);
    this.colors.Add(color);
  }

  public void ClearLines()
  {
    this.starts.Clear();
    this.ends.Clear();
  }

  void OnDrawGizmos()
  {
    for (int i = 0; i < this.starts.Count; i++)
    {
      Gizmos.color = this.colors[i];
      Gizmos.DrawLine(this.starts[i], this.ends[i]);
    }

    Gizmos.DrawWireSphere(this.point, 0.1f);
  }
}
