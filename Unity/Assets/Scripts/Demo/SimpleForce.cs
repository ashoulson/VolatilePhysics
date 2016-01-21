using System;
using System.Collections.Generic;

using UnityEngine;

public class SimpleForce : MonoBehaviour 
{
  [SerializeField]
  private VolatileBody body;

  [SerializeField]
  private float intensity;

  void FixedUpdate() 
  {
    if (Input.GetKeyDown(KeyCode.F) == true)
    {
      Vector2 force = transform.up * this.intensity * Time.fixedDeltaTime;
      this.body.Body.AddForce(force, transform.position);
    }
  }
}
