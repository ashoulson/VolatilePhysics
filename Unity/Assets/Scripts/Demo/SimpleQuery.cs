using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
using Volatile.History;

public class SimpleQuery : MonoBehaviour
{
  public int frame = -1;

  void OnDrawGizmos()
  {
    VolatileWorld world = VolatileWorld.Instance;
    if (Application.isPlaying == true)
    {
      float radius = float.PositiveInfinity;

      IEnumerable<KeyValuePair<Body, float>> pairs = null;
      if (this.frame >= 0)
        pairs = world.world.MinDistanceBodies(this.frame, transform.position, 100.0f);
      else
        pairs = world.world.MinDistanceBodies(transform.position, 100.0f);

      foreach (var pair in pairs)
        if (pair.Value < radius)
          radius = pair.Value;

      Gizmos.color = Color.cyan;
      if (radius < float.PositiveInfinity)
        Gizmos.DrawWireSphere(transform.position, radius);
    }
  }
}
