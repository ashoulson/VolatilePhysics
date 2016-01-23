using System;
using System.Collections.Generic;

using UnityEngine;

public class SimpleForce : MonoBehaviour 
{
  [SerializeField]
  private VolatileBody body;

  [SerializeField]
  private Vector2 force;

  void FixedUpdate() 
  {
    this.body.Body.AddForce(force);
  }
}
