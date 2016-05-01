using System;
using System.Collections.Generic;

using UnityEngine;
using Volatile;
using Volatile.Extensions;

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

      VolatileWorld.Instance.World.ResolveExplosion(
        this.lastOrigin,
        this.radius,
        this.ExplosionCallback,
        (body) => body != this.body.Body);
    }
	}

  private void ExplosionCallback(
    VoltShape shape, 
    Vector2 normal, 
    float increment, 
    float normalizedDistance)
  {
    float force = this.forceMax * (1.0f - normalizedDistance) * increment;
    shape.Body.AddForce(normal * force);

    // Gizmo info
    this.hits.Add(normal * normalizedDistance * this.radius);
  }

  void OnDrawGizmos()
  {
    if (Application.isPlaying && (Time.time < showDelay))
      foreach (Vector2 hit in this.hits)
        Gizmos.DrawLine(this.lastOrigin, this.lastOrigin + hit);
  }
}
