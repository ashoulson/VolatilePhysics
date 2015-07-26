using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class SimpleExplosion : MonoBehaviour 
{
  [SerializeField]
  private float radius;

  [SerializeField]
  private int resolution;

  [SerializeField]
  private VolatileBody body;

  private Vector2 origin { get { return this.transform.position; } }

  void OnDrawGizmos()
  {
    if (Application.isPlaying == true)
    {
      Explosion explosion =
        new Explosion(
          VolatileWorld.Instance.world,
          transform.position,
          this.radius);

      if (this.body != null)
      {
        Gizmos.DrawWireSphere(this.origin, this.radius);

        List<Tuple<Vector2, float>> rayHits = new List<Tuple<Vector2,float>>();
        float interval;
        explosion.PerformRaycasts(
          this.body.body, 
          this.resolution, 
          rayHits, 
          out interval);

        if (rayHits.Count > 0)
        {
          for (int i = 0; i < rayHits.Count; i++)
          {
            Tuple<Vector2, float> hit = rayHits[i];
            Gizmos.DrawLine(this.origin, hit.Item1);
          }
          Debug.Log(interval);
        }
      }
    }
  }


}
