using System;
using System.Collections.Generic;

using UnityEngine;

public class SimpleController : MonoBehaviour 
{
  private bool forward;
  private bool backward;
  private bool left;
  private bool right;

  private VolatileBody body;

	void Start () 
	{
    this.body = this.GetComponent<VolatileBody>();
    Debug.Log(this.body);
	}
	
	void Update () 
	{
    this.forward = Input.GetKey(KeyCode.UpArrow);
    this.backward = Input.GetKey(KeyCode.DownArrow);
    this.left = Input.GetKey(KeyCode.LeftArrow);
    this.right = Input.GetKey(KeyCode.RightArrow);
	}

  void FixedUpdate()
  {
    if (this.body != null)
    {
      float thrust = 
        (this.forward ? 1.0f : 0.0f) + (this.backward ? -1.0f : 0.0f);
      float turn =
        (this.left ? 1.0f : 0.0f) + (this.right ? -1.0f : 0.0f);
      this.body.AddForce(transform.up * thrust * 0.1f);
      this.body.AddTorque(-turn * 0.03f);

      // Stabilize
      // TEMP - Do this with force application instead
      this.body.body.AngularVelocity *= 0.79f;
    }
  }
}
