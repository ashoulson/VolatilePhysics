using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class SimpleCast : MonoBehaviour
{
  [SerializeField]
  VolatileBody ignoreBody;

  [SerializeField]
  int frameOffset = 0;

  [SerializeField]
  float radius;

  [SerializeField]
  Color color;

  private bool Filter(VoltBody body)
  {
    if (this.ignoreBody != null)
      return body != this.ignoreBody.Body;
    return true;
  }

  void OnDrawGizmos()
  {
    VolatileWorld world = VolatileWorld.Instance;
    if ((world != null) && (Application.isPlaying == true))
    {
      VoltRayResult result = new VoltRayResult();
      VoltRayCast cast =
        new VoltRayCast(
          transform.position.ToVolt(),
          transform.position.ToVolt() + (transform.up.ToVolt() * 100.0f));

      if (this.radius > 0.0f)
        world.World.CircleCast(ref cast, this.radius, ref result, this.Filter, -this.frameOffset);
      else
        world.World.RayCast(ref cast, ref result, this.Filter, -this.frameOffset);

      float drawRadius = (this.radius == 0.0f) ? 0.2f : this.radius;
      Gizmos.color = this.color;
      if (result.IsValid == true)
      {
        Vector2 point = transform.position + (transform.up * result.Distance);
        Gizmos.DrawLine(transform.position, point);
        Gizmos.DrawWireSphere(point,drawRadius);
      }
      else
      {
        Gizmos.DrawLine(transform.position, transform.position + (transform.up * 100.0f));
      }
    }
  }
}
