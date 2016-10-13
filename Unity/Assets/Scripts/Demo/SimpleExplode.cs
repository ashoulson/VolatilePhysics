using System;
using System.Collections.Generic;

using UnityEngine;
using Volatile;

public class SimpleExplode : MonoBehaviour
{
  [SerializeField]
  private float radius;

  [SerializeField]
  private float forceMax;

  [SerializeField]
  private int rayCount;

  [SerializeField]
  private VolatileBody body;

  private List<Vector2> hits;
  private Vector2 lastOrigin;
  private float showDelay;

  void Awake()
  {
    this.hits = new List<Vector2>();
  }

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.E))
    {
      this.hits.Clear();
      this.lastOrigin = this.transform.position;
      this.showDelay = Time.time + 0.2f;

      VolatileWorld.Instance.World.PerformExplosion(
        this.lastOrigin,
        this.radius,
        this.ExplosionCallback,
        (body) => (body.IsStatic == false) && (body != this.body.Body),
        VoltWorld.FilterExcept(this.body.Body));
    }
  }

  private void ExplosionCallback(
    VoltRayCast rayCast,
    VoltRayResult rayResult,
    float rayWeight)
  {
    Vector2 point = rayResult.ComputePoint(ref rayCast);
    this.hits.Add(point);
  }

  void OnDrawGizmos()
  {
    if (Application.isPlaying && (Time.time < showDelay))
      foreach (Vector2 hit in this.hits)
        Gizmos.DrawLine(this.lastOrigin, hit);
  }
}
