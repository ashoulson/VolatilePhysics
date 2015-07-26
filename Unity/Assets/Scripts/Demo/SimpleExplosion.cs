using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
using Volatile.History;

public class SimpleExplosion : MonoBehaviour
{
  [SerializeField]
  private float radius;

  [SerializeField]
  private int rayBudget;

  [SerializeField]
  private int minRays;

  [SerializeField]
  private int frame = -1;

  [SerializeField]
  private Gradient colorGradient;

  private Vector2 origin { get { return this.transform.position; } }
  private List<Tuple<Vector2, float>> results = null;

  void OnDrawGizmos()
  {
    if (Application.isPlaying == true)
    {
      Explosion explosion =
        new Explosion(
          VolatileWorld.Instance.world,
          transform.position,
          this.radius);

      if (this.results == null)
        this.results = new List<Tuple<Vector2, float>>();
      this.results.Clear();

      Gizmos.DrawWireSphere(this.origin, this.radius);

      if (this.frame == -1)
        explosion.Perform(this.rayBudget, this.minRays, this.HitCallback);
      else
        explosion.Perform(
          this.frame, 
          this.rayBudget, 
          this.minRays, this.HitCallback);

      foreach (Tuple<Vector2, float> hit in this.results)
      {
        float t = hit.Item2 / this.radius;
        Gizmos.color = this.colorGradient.Evaluate(t);
        Gizmos.DrawLine(this.origin, hit.Item1);
      }
    }
  }

  private void HitCallback(
    Body body,
    bool isContained,
    Vector2 point,
    Vector2 direction,
    float distance,
    float interval)
  {
    if (isContained == true)
      this.results.Add(new Tuple<Vector2, float>(body.Position, 0.0f));
    else
      this.results.Add(new Tuple<Vector2, float>(point, distance));
  }
}
