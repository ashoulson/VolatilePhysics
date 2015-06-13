using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class VolatilePolygon : VolatileShape
{
  [SerializeField]
  private Transform[] points;

  public override Shape Shape { get { return this.shape; } }
  private Polygon shape;

  public override Shape PrepareShape(VolatileBody body)
  {
    Vector2[] vertices = new Vector2[this.points.Length];
    for (int i = 0; i < this.points.Length; i++)
      vertices[i] = this.GetBodyLocalPoint(this.points[i].localPosition, body);
    this.shape = new Polygon(vertices, this.density);
    return this.shape;
  }

  public override void DrawShapeInGame()
  {
    if (this.shape != null)
      VolatileDebug.DrawShape(this.shape);
  }

  public override void DrawShapeInEditor()
  {
    Color current = Gizmos.color;
    Gizmos.color = Color.white;

    for (int i = 0; i < this.points.Length; i++)
    {
      Vector2 u = this.points[i].position;
      Vector2 v = this.points[(i + 1) % this.points.Length].position;
      Gizmos.DrawLine(u, v);
    }

    Gizmos.color = current;
  }

  private Vector2 GetBodyLocalPoint(Vector2 point, VolatileBody body)
  {
    return 
      body.transform.InverseTransformPoint(
        this.transform.TransformPoint(point));
  }
}
