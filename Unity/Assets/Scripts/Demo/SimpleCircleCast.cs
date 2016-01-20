using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
//using Volatile.History;

public class SimpleCircleCast : MonoBehaviour
{
  public VolatileBody ignoreBody;
  public float radius = 1.0f;

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
      RayResult result = new RayResult();
      RayCast cast =
        new RayCast(
          transform.position,
          transform.position + (transform.up * 100.0f));

      //if (this.frame >= 0)
      //  world.world.CircleCast(this.frame, ref cast, this.radius, ref result, this.Filter);
      //else
      world.world.CircleCast(ref cast, this.radius, ref result, this.Filter);

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
