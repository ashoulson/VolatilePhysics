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

  private Vector2 lastOrigin;
  private float showDelay;

	void Update() 
	{
	  if (Input.GetKeyDown(KeyCode.E))
    {
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
  }

  void OnDrawGizmos()
  {
    if (Application.isPlaying && (Time.time < showDelay))
    {
      float increment = 1.0f / this.rayCount;
      float angleIncrement = (Mathf.PI * 2.0f) * increment;

      for (int i = 0; i < this.rayCount; i++)
      {
        Vector2 normal = VoltMath.Polar(angleIncrement * i);
        Gizmos.DrawLine(this.lastOrigin, this.lastOrigin + (normal * this.radius));
      }
    }
  }
}
