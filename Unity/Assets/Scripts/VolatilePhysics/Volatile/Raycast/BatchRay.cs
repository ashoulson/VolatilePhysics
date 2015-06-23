using UnityEngine;
using System.Collections;

/// <summary>
/// A semi-precomputed ray optimized for many tests
/// </summary>
public struct BatchRay
{
  public Vector2 origin;
  public Vector2 direction;
  public Vector2 invDirection;
  public bool signX;
  public bool signY;

  public BatchRay(Vector2 origin, Vector2 direction)
  {
    this.origin = origin;
    this.direction = direction;
    this.invDirection =
      new Vector2(
        1.0f / direction.x,
        1.0f / direction.y);
    this.signX = invDirection.x < 0.0f;
    this.signY = invDirection.y < 0.0f;
  }
}