/*
 *  Copyright (c) 2017 - Alexander Shoulson - http://ashoulson.com
 */

using System.Collections.Generic;

using UnityEngine;

using Volatile;

public static class VoltExtensions
{
  public static Vector2 ToUnity(
    this VoltVec2 vec2)
  {
    return new Vector2(vec2.x, vec2.y);
  }

  public static VoltVec2 ToVolt(
    this Vector2 vec2)
  {
    return new VoltVec2(vec2.x, vec2.y);
  }

  public static VoltVec2 ToVolt(
    this Vector3 vec2)
  {
    return new VoltVec2(vec2.x, vec2.y);
  }

  public static void GizmoDrawAll(
    this VoltBody body)
  {
    body.GizmoDraw(
      new Color(1.0f, 1.0f, 0.0f, 1.0f),  // Edge Color
      new Color(1.0f, 0.0f, 1.0f, 1.0f),  // Normal Color
      new Color(1.0f, 0.0f, 0.0f, 1.0f),  // Body Origin Color
      new Color(0.0f, 0.0f, 0.0f, 1.0f),  // Shape Origin Color
      new Color(0.1f, 0.0f, 0.5f, 1.0f),  // Body AABB Color
      new Color(0.7f, 0.0f, 0.3f, 0.5f),  // Shape AABB Color
      0.25f);

    body.GizmoDrawHistory(
      new Color(0.0f, 0.0f, 1.0f, 0.3f)); // History AABB Color
  }

  //public static void Draw(VoltShape shape)
  //{
  //  shape.GizmoDraw(
  //    new Color(1.0f, 1.0f, 0.0f, 1.0f),  // Edge Color
  //    new Color(1.0f, 0.0f, 1.0f, 1.0f),  // Normal Color
  //    new Color(0.0f, 0.0f, 0.0f, 1.0f),  // Origin Color
  //    new Color(0.7f, 0.0f, 0.3f, 1.0f),  // AABB Color
  //    0.25f);
  //}

  //public static void Draw(VoltAABB aabb)
  //{
  //  aabb.GizmoDraw(
  //    new Color(1.0f, 0.0f, 0.5f, 1.0f));  // AABB Color
  //}

  /// <summary>
  /// Gizmo-draw a body
  /// </summary>
  public static void GizmoDraw(
    this VoltBody body,
    Color edgeColor,
    Color normalColor,
    Color bodyOriginColor,
    Color shapeOriginColor,
    Color bodyAabbColor,
    Color shapeAabbColor,
    float normalLength)
  {
    Color current = Gizmos.color;

    Vector2 position = body.Position.ToUnity();
    Vector2 facing = body.Facing.ToUnity();

    // Draw origin
    Gizmos.color = bodyOriginColor;
    Gizmos.DrawWireSphere(position, 0.1f);

    // Draw facing
    Gizmos.color = normalColor;
    Gizmos.DrawLine(
      position,
      position + facing * normalLength);

    body.AABB.GizmoDraw(bodyAabbColor);

    foreach (VoltShape shape in body.GetShapes())
      shape.GizmoDraw(
        edgeColor,
        normalColor,
        shapeOriginColor,
        shapeAabbColor,
        normalLength);

    Gizmos.color = current;
  }

  /// <summary>
  /// Gizmo-draw the AABBs in a body's history
  /// </summary>
  public static void GizmoDrawHistory(
    this VoltBody body,
    Color aabbColor)
  {
    Color current = Gizmos.color;
    foreach (VoltAABB aabb in body.GetHistoryAABBs())
      aabb.GizmoDraw(aabbColor);
    Gizmos.color = current;
  }

  /// <summary>
  /// Gizmo draw a shape
  /// </summary>
  public static void GizmoDraw(
    this VoltShape shape,
    Color edgeColor,
    Color normalColor,
    Color originColor,
    Color aabbColor,
    float normalLength)
  {
    if (shape is VoltPolygon)
      VoltExtensions.GizmoDraw(
        (VoltPolygon)shape, 
        edgeColor, 
        normalColor, 
        originColor, 
        aabbColor, 
        normalLength);
    if (shape is VoltCircle)
      VoltExtensions.GizmoDraw(
        (VoltCircle)shape,
        edgeColor,
        normalColor,
        originColor,
        aabbColor,
        normalLength);

    shape.AABB.GizmoDraw(aabbColor);
  }

  /// <summary>
  /// Gizmo-draw a polygon shape
  /// </summary>
  public static void GizmoDraw(
    this VoltPolygon polygon,
    Color edgeColor,
    Color normalColor,
    Color originColor,
    Color aabbColor,
    float normalLength)
  {
    Color current = Gizmos.color;

    List<VoltVec2> vertices = new List<VoltVec2>(polygon.GetVertices());
    List<VoltVec2> normals = new List<VoltVec2>(polygon.GetNormals());

    for (int i = 0; i < vertices.Count; i++)
    {
      Vector2 u = vertices[i].ToUnity();
      Vector2 v = vertices[(i + 1) % vertices.Count].ToUnity();
      Vector2 n = normals[i].ToUnity();

      Vector2 delta = v - u;
      Vector2 midPoint = u + (delta * 0.5f);

      // Draw edge
      Gizmos.color = edgeColor;
      Gizmos.DrawLine(u, v);

      // Draw normal
      Gizmos.color = normalColor;
      Gizmos.DrawLine(midPoint, midPoint + (n * normalLength));
    }

    Gizmos.color = current;
  }

  /// <summary>
  /// Gizmo-draw a circle shape
  /// </summary>
  public static void GizmoDraw(
    this VoltCircle circle,
    Color edgeColor, 
    Color normalColor, 
    Color originColor, 
    Color aabbColor, 
    float normalLength)
  {
    Color current = Gizmos.color;

    Gizmos.color = edgeColor;
    Gizmos.DrawWireSphere(
      circle.Origin.ToUnity(), 
      circle.Radius);

    Gizmos.color = current;
  }

  /// <summary>
  /// Gizmo-draw an AABB
  /// </summary>
  public static void GizmoDraw(
    this VoltAABB aabb, 
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
