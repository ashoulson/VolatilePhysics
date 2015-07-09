using System;
using System.Collections.Generic;

using UnityEngine;

public class TestRay : MonoBehaviour 
{
  public Transform start;
  public Transform end;
  public Transform A;
  public Transform B;

  void OnDrawGizmos()
  {
    Vector2 vstart = start.position;
    Vector2 vend = end.position;
    Vector2 vA = A.position;
    Vector2 vB = B.position;

    Gizmos.color = Color.green;
    Gizmos.DrawLine(vstart, vend);

    Gizmos.color = Color.red;
    Gizmos.DrawLine(vA, vB);

    Vector2 o = vstart;
    Vector2 d = (vstart - vend).normalized;

    Vector2 a = vA;
    Vector2 b = vB;

    Vector2 v1 = o - a;
    Vector2 v2 = vB - a;
    Vector2 v3 = new Vector2(-d.y, d.x);

    float t1 = Volatile.VolatileUtil.Cross(v2, v1) / Vector2.Dot(v2, v3);
    float t2 = Vector2.Dot(v1, v3) / Vector2.Dot(v2, v3);

    if (t2 > 0.0f && t2 < 1.0f)
    {
      Vector2 point = o + (d * t1);

      Gizmos.color = Color.white;
      Gizmos.DrawSphere(point, 0.2f);
    }
  }
}
