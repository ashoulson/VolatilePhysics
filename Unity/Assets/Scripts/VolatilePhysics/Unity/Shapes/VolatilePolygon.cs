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

  public override void PrepareShape()
  {
    Vector2[] vertices = new Vector2[this.points.Length];
    for (int i = 0; i < this.points.Length; i++)
      vertices[i] = this.points[i].position;
    this.shape =
      Polygon.FromWorldVertices(
        vertices,
        this.density);
  }

  public override void DrawShapeInEditor()
  {
    Color current = Gizmos.color;

    for (int i = 0; i < this.points.Length; i++)
    {
      Gizmos.color = Color.white;
      Vector2 u = this.points[i].position;
      Vector2 v = this.points[(i + 1) % this.points.Length].position;
      Gizmos.DrawLine(u, v);
    }

    Gizmos.color = current;
  }

  public override Vector2 ComputeTrueCenterOfMass()
  {
    float length = (float)this.points.Length;
    Vector2 sum = Vector2.zero;
    foreach (Transform point in this.points)
      sum += (Vector2)point.position;
    return new Vector2(sum.x / length, sum.y / length);
  }
}
