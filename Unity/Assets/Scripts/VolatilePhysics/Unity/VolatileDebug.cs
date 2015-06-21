using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public static class VolatileDebug 
{
  public const bool DRAW_AABB = false;
  public const bool DRAW_ORIGIN = false;
  public const bool DRAW_NORMALS = false;

  public static void DrawShape(
    Polygon polygon, 
    Color edgeColor, 
    Color normalColor,
    Color originColor,
    Color aabbColor,
    float normalLength = 0.25f)
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
      Gizmos.color = edgeColor;
      Gizmos.DrawLine(u, v);

      if (VolatileDebug.DRAW_NORMALS == true)
      {
        // Draw normal
        Gizmos.color = normalColor;
        Gizmos.DrawLine(midPoint, midPoint + (n * normalLength));
      }

      if (VolatileDebug.DRAW_ORIGIN == true)
      {
        // Draw line to origin
        Gizmos.color = originColor;
        Gizmos.DrawLine(u, polygon.Position);
      }
    }

    if (VolatileDebug.DRAW_NORMALS == true)
    {
      // Draw facing
      Gizmos.color = normalColor;
      Gizmos.DrawLine(
        polygon.Position,
        polygon.Position + polygon.Facing * normalLength);
    }

    if (VolatileDebug.DRAW_ORIGIN == true)
    {
      // Draw origin
      Gizmos.color = originColor;
      Gizmos.DrawWireSphere(polygon.Position, 0.05f);
    }

    if (VolatileDebug.DRAW_AABB == true)
    {
      VolatileDebug.DrawAABB(polygon.AABB, aabbColor);
    }

    Gizmos.color = current;
  }

  public static void DrawShape(
    Circle circle, 
    Color circleColor, 
    Color aabbColor)
  {
    Color current = Gizmos.color;

    Gizmos.color = circleColor;
    Gizmos.DrawWireSphere(circle.Position, circle.Radius);

    if (VolatileDebug.DRAW_AABB == true)
    {
      VolatileDebug.DrawAABB(circle.AABB, aabbColor);
    }

    Gizmos.color = current;
  }

  public static void DrawBody(
    Body body,
    Color aabbColor)
  {
    Color current = Gizmos.color;

    if (VolatileDebug.DRAW_AABB == true)
    {
      VolatileDebug.DrawAABB(body.AABB, aabbColor);
    }

    Gizmos.color = current;
  }

  public static void DrawAABB(AABB aabb, Color color)
  {
    Color current = Gizmos.color;

    Vector2 A = new Vector2(aabb.Left, aabb.Top);
    Vector2 B = new Vector2(aabb.Right, aabb.Top);
    Vector2 C = new Vector2(aabb.Right, aabb.Bottom);
    Vector2 D = new Vector2(aabb.Left, aabb.Bottom);

    Gizmos.color = color;
    Gizmos.DrawLine(A, B);
    Gizmos.DrawLine(B, C);
    Gizmos.DrawLine(C, D);
    Gizmos.DrawLine(D, A);
    Gizmos.color = current;
  }
}
