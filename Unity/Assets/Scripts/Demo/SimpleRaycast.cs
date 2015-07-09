using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class SimpleRaycast : MonoBehaviour 
{
  public VolatileBody ignoreBody;

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
      world.world.RayCast(
        new RayCast(
          transform.position,
          transform.position + (transform.up * 100.0f)),
        out result,
        bodyFilter: this.Filter);

      Gizmos.color = Color.green;
      Gizmos.DrawLine(transform.position, transform.position + (transform.up * 100.0f));
      if (result.IsValid == true)
        Gizmos.DrawWireSphere(transform.position + (transform.up * result.Distance), 0.2f);
    }
  }
}
