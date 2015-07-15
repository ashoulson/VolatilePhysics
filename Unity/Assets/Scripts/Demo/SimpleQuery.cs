using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class SimpleQuery : MonoBehaviour
{
  void OnDrawGizmos()
  {
    VolatileWorld world = VolatileWorld.Instance;
    if (Application.isPlaying == true)
    {
      float radius = float.PositiveInfinity;
      var pairs = world.world.QueryDistance(transform.position, 100.0f);
      foreach (var pair in pairs)
        if (pair.Value < radius)
          radius = pair.Value;

      Gizmos.color = Color.cyan;
      if (radius < float.PositiveInfinity)
        Gizmos.DrawWireSphere(transform.position, radius);
    }
  }
}
