using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
using Volatile.History;

public class SimpleRayCast : MonoBehaviour 
{
  public VolatileBody ignoreBody;

  public int frame = -1;

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
      RayCast cast =
        new RayCast(
          transform.position,
          transform.position + (transform.up * 100.0f));

      if (this.frame >= 0)
        world.world.RayCast(this.frame, cast, out result, this.Filter);
      else
        world.world.RayCast(cast, out result, this.Filter);

      Gizmos.color = Color.green;
      Gizmos.DrawLine(transform.position, transform.position + (transform.up * 100.0f));
      if (result.IsValid == true)
        Gizmos.DrawWireSphere(transform.position + (transform.up * result.Distance), 0.2f);
    }
  }
}
