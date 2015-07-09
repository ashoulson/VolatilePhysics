using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class SimpleCircleCast : MonoBehaviour
{
  public VolatileBody ignoreBody;
  public VolatileShape shape;
  public float radius;

  public Vector2 point = Vector2.zero;
  public bool isValid = false;

  private bool Filter(Body body)
  {
    if (this.ignoreBody != null)
      return body != this.ignoreBody.body;
    return true;
  }

  void FixedUpdate()
  {
    RayResult result = new RayResult();
    RayCast ray =
      new RayCast(
        transform.position,
        transform.position + (transform.up * 100.0f));
    this.shape.Shape.CircleCast(ref ray, ref result, this.radius);
    this.point = result.GetPoint(ref ray);
    this.isValid = result.IsValid;
  }

  void OnDrawGizmos()
  {
    if (Application.isPlaying == true)
    {
      Gizmos.color = Color.green;
      Gizmos.DrawLine(transform.position, transform.position + (transform.up * 100.0f));
      if (this.isValid == true)
        Gizmos.DrawWireSphere(this.point, this.radius);
    }
  }
}
