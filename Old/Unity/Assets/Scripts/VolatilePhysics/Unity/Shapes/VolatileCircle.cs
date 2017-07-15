using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class VolatileCircle : VolatileShape
{
  [SerializeField]
  private float radius;

  private VoltCircle shape;

  public override VoltShape PrepareShape(VoltWorld world)
  {
    this.shape = world.CreateCircleWorldSpace(
      this.transform.position,
      this.radius, 
      this.density);
    return this.shape;
  }

  public override void DrawShapeInEditor()
  {
    Color current = Gizmos.color;
    Gizmos.color = Color.white;

    Gizmos.DrawWireSphere(transform.position, this.radius);

    Gizmos.color = current;
  }

  protected Vector2 GetBodyLocalPoint(Vector2 point, VolatileBody body)
  {
    return
      body.transform.InverseTransformPoint(
        this.transform.TransformPoint(point));
  }

  public override Vector2 ComputeTrueCenterOfMass()
  {
    return transform.position;
  }
}
