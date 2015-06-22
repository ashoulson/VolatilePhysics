using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public static class VolatileDebug 
{
  public static void DrawBody(Body body)
  {
    VolatileDebug.DrawBody(
      body,
      new Color(0.0f, 1.0f, 1.0f, 1.0f), // Edge Color
      new Color(1.0f, 0.0f, 1.0f, 1.0f), // Normal Color
      new Color(1.0f, 0.0f, 0.0f, 1.0f), // Body Origin Color
      new Color(0.0f, 0.0f, 0.0f, 1.0f), // Shape Origin Color
      new Color(0.1f, 0.0f, 0.5f, 1.0f), // Body AABB Color
      new Color(0.7f, 0.0f, 0.3f, 0.5f), // Shape AABB Color
      0.25f);
  }

  public static void DrawShape(Shape shape)
  {
    VolatileDebug.DrawShape(
      shape,
      new Color(0.0f, 1.0f, 1.0f, 1.0f), // Edge Color
      new Color(1.0f, 0.0f, 1.0f, 1.0f), // Normal Color
      new Color(0.0f, 0.0f, 0.0f, 1.0f), // Origin Color
      new Color(0.7f, 0.0f, 0.3f, 1.0f), // AABB Color
      0.25f);
  }

  public static void DrawAABB(AABB aabb)
  {
    VolatileDebug.DrawAABB(
      aabb,
      new Color(1.0f, 0.0f, 0.5f, 1.0f)); // Edge Color
  }

  public static void DrawShape(
    Shape shape,
    Color edgeColor, 
    Color normalColor,
    Color originColor,
    Color aabbColor,
    float normalLength)
  {
    if (shape.Type == Shape.ShapeType.Circle)
    {
      VolatileDebug.DrawShape(
        (Circle)shape, 
        edgeColor, 
        aabbColor);
    }
    else if (shape.Type == Shape.ShapeType.Polygon)
    {
      VolatileDebug.DrawShape(
        (Polygon)shape,
        edgeColor, 
        normalColor,
        originColor,
        aabbColor,
        normalLength);
    }
  }

  public static void DrawShape(
    Polygon polygon, 
    Color edgeColor, 
    Color normalColor,
    Color originColor,
    Color aabbColor,
    float normalLength)
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

      // Draw normal
      Gizmos.color = normalColor;
      Gizmos.DrawLine(midPoint, midPoint + (n * normalLength));

      // Draw line to origin
      Gizmos.color = originColor;
      Gizmos.DrawLine(u, polygon.Position);
    }

    // Draw facing
    Gizmos.color = normalColor;
    Gizmos.DrawLine(
      polygon.Position,
      polygon.Position + polygon.Facing * normalLength);

    // Draw origin
    Gizmos.color = originColor;
    Gizmos.DrawWireSphere(polygon.Position, 0.05f);

    VolatileDebug.DrawAABB(polygon.AABB, aabbColor);

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

    VolatileDebug.DrawAABB(circle.AABB, aabbColor);

    Gizmos.color = current;
  }

  public static void DrawBody(
    Body body,
    Color edgeColor,
    Color normalColor,
    Color bodyOriginColor,
    Color shapeOriginColor,
    Color bodyAabbColor,
    Color shapeAabbColor,
    float normalLength)
  {
    Color current = Gizmos.color;

    // Draw origin
    Gizmos.color = bodyOriginColor;
    Gizmos.DrawWireSphere(body.Position, 0.1f);

    // Draw facing
    Gizmos.color = normalColor;
    Gizmos.DrawLine(
      body.Position,
      body.Position + body.Facing * normalLength);

    VolatileDebug.DrawAABB(body.AABB, bodyAabbColor);

    foreach (Shape shape in body.Shapes)
      VolatileDebug.DrawShape(
        shape,
        edgeColor,
        normalColor,
        shapeOriginColor,
        shapeAabbColor,
        normalLength);

    Gizmos.color = current;
  }

  public static void DrawAABB(
    AABB aabb, 
    Color aabbColor)
  {
    Color current = Gizmos.color;

    Vector2 A = new Vector2(aabb.Left, aabb.Top);
    Vector2 B = new Vector2(aabb.Right, aabb.Top);
    Vector2 C = new Vector2(aabb.Right, aabb.Bottom);
    Vector2 D = new Vector2(aabb.Left, aabb.Bottom);

    Gizmos.color = aabbColor;
    Gizmos.DrawLine(A, B);
    Gizmos.DrawLine(B, C);
    Gizmos.DrawLine(C, D);
    Gizmos.DrawLine(D, A);
    Gizmos.color = current;
  }
}
