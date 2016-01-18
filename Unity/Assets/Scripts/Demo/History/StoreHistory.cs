using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;
using Volatile.History;

public class StoreHistory : MonoBehaviour
{
  private VolatileBody body;

  void Awake()
  {
    this.body = this.GetComponent<VolatileBody>();
  }

  void Start()
  {
    if (this.body.body.IsStatic == false)
      this.body.body.BeginLogging(10);
  }

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.Alpha1) == true)
      this.body.body.Store(0);
    if (Input.GetKeyDown(KeyCode.Alpha2) == true)
      this.body.body.Store(1);
    if (Input.GetKeyDown(KeyCode.Alpha3) == true)
      this.body.body.Store(2);
    if (Input.GetKeyDown(KeyCode.Alpha4) == true)
      this.body.body.Store(3);
    if (Input.GetKeyDown(KeyCode.Alpha5) == true)
      this.body.body.Store(4);
  }

  void OnDrawGizmos()
  {
    if (Application.isPlaying == true)
      if (this.body.body.IsStatic == false)
        this.body.body.GizmoDrawHistory(
          new Color(0.0f, 0.5f, 0.0f),
          new Color(0.0f, 0.9f, 0.0f));
  }
}
