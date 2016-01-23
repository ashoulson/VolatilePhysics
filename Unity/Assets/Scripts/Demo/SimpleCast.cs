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

  private bool Filter(Body body)
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
      RayResult result = new RayResult();
      RayCast cast =
        new RayCast(
          transform.position,
          transform.position + (transform.up * 100.0f));

      int frame = Volatile.History.CURRENT_FRAME;
      if (this.frameOffset < 0)
      {
        frame = world.CurrentFrame + this.frameOffset;
        if (frame < 0)
          frame = Volatile.History.CURRENT_FRAME; ;
      }
      else
      {
        frame = Volatile.History.CURRENT_FRAME; ;
      }

      if (this.radius > 0.0f)
        world.world.CircleCast(ref cast, this.radius, ref result, this.Filter, frame);
      else
        world.world.RayCast(ref cast, ref result, this.Filter, frame);

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
