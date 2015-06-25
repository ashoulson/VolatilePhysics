using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class SimpleRaycast : MonoBehaviour 
{
  public VolatileBody ignoreBody;

  private bool Filter(Body body)
  {
    return body != this.ignoreBody.body;
  }

  void OnDrawGizmos()
  {
    VolatileWorld world = VolatileWorld.Instance;
    if (Application.isPlaying == true)
    {
      RayResult result;
      world.world.Raycast(
        new RayCast(
          transform.position,
          transform.position + (transform.up * 100.0f)),
        out result,
        bodyFilter: this.Filter);

      if (result.IsValid == true)
      {
        Gizmos.DrawLine(transform.position, transform.position + (transform.up * 100.0f));
        Gizmos.DrawSphere(transform.position + (transform.up * result.Distance), 0.2f);
      }
    }
  }
}
