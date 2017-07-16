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
      this.body.AddForce(this.body.Body.Facing.ToUnity() * thrust * 0.1f);
      this.body.AddTorque(-turn * 0.03f);

      // Stabilize
      float angVel = this.body.Body.AngularVelocity;
      float inertia = this.body.Body.Inertia;
      float correction =
        (angVel * inertia) / Time.fixedDeltaTime;

      float quotient = Mathf.Approximately(turn, 0.0f) ? 0.6f : 0.2f;
      this.body.AddTorque(correction * quotient);
    }
  }
}
