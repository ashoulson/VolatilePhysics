using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class VolatileBody : MonoBehaviour 
{
  [SerializeField]
  private VolatileShape[] shapes;

  private Body body;

  void Awake()
  {
    this.body = new Body(transform.position);
    foreach (VolatileShape shape in this.shapes)
      this.body.AddShape(shape.PrepareShape(this));

    World w = new World(new Vector2(0.0f, 0.0f));
    w.AddBody(this.body);
    this.body.Set(transform.position, Mathf.Deg2Rad * transform.rotation.eulerAngles.z);
  }

  void Update()
  {
    this.body.Set(transform.position, Mathf.Deg2Rad * transform.rotation.eulerAngles.z);
  }

  void OnDrawGizmos()
  {
    if (this.shapes != null)
    {
      if (Application.isPlaying)
      {
        foreach (VolatileShape shape in this.shapes)
          shape.DrawShapeInGame();
      }
      else
      {
        foreach (VolatileShape shape in this.shapes)
          shape.DrawShapeInEditor();
      }
    }
  }
}
