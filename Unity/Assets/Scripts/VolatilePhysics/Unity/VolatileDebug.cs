using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public static class VolatileDebug 
{
  public static void DrawShape(Polygon polygon)
  {
    Color current = Gizmos.color;

    Vector2[] vertices = polygon.WorldVertices;
    Vector2[] normals = polygon.WorldNormals;

    for (int i = 0; i < vertices.Length; i++)
    {
      Vector2 u = vertices[i];
      Vector2 v = vertices[(i + 1) % vertices.Length];
      Vector2 n = normals[i];

      Vector2 delta = v - u;
      Vector2 midPoint = u + (delta * 0.5f);

      // Draw edge
      Gizmos.color = Color.yellow;
      Gizmos.DrawLine(u, v);

      // Draw normal
      Gizmos.color = new Color(1.0f, 0.0f, 1.0f);
      Gizmos.DrawLine(midPoint, midPoint + (n * 0.25f));
    }

    Gizmos.color = current;
  }

  public static void DrawShape(Circle circle)
  {
    Color current = Gizmos.color;

    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(circle.WorldCenter, circle.Radius);

    Gizmos.color = current;
  }
}
