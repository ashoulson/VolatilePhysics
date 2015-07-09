using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class SimpleCircleCast : MonoBehaviour
{
  public VolatileBody ignoreBody;
  public float radius = 1.0f;

  private bool Filter(Body body)
  {
    if (this.ignoreBody != null)
      return body != this.ignoreBody.body;
    return true;
  }

  void OnDrawGizmos()
  {
    VolatileWorld world = VolatileWorld.Instance;
    if (Application.isPlaying == true)
    {
      RayResult result;
      world.world.CircleCast(
        new RayCast(
          transform.position,
          transform.position + (transform.up * 100.0f)),
        this.radius,
        out result,
        bodyFilter: this.Filter);

      Gizmos.color = Color.red;
      Gizmos.DrawLine(
        transform.position, 
        transform.position + (transform.up * 100.0f));

      if (result.IsValid == true)
        Gizmos.DrawWireSphere(
          transform.position + (transform.up * result.Distance), 
          this.radius);
    }
  }
}
