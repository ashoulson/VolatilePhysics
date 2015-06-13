using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class VolatileBody : MonoBehaviour 
{
  [SerializeField]
  private VolatileShape[] shapes;

  [SerializeField]
  private bool useGravity = false;

  public Body body;

  void Awake()
  {
    this.body = new Body(
      transform.position, 
      Mathf.Deg2Rad * transform.eulerAngles.z, 
      this.useGravity);
    foreach (VolatileShape shape in this.shapes)
      this.body.AddShape(shape.PrepareShape(this));
    this.body.Initialize();
  }

  void Start()
  {
    VolatileWorld.Instance.AddBody(this.body);
  }

  void Update()
  {
    if (UnityEditor.Selection.activeGameObject == this.gameObject)
    {
      this.body.Set(transform.position, Mathf.Deg2Rad * transform.rotation.eulerAngles.z);
    }
    else
    {
      transform.position = this.body.Position;
      transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Rad2Deg * this.body.Angle);
    }
  }

  void OnDrawGizmos()
  {
    Color current = Gizmos.color;
    Vector2 trueBodyCOM = Vector2.zero;

    if (this.shapes != null)
    {
      if (Application.isPlaying)
      {
        foreach (VolatileShape shape in this.shapes)
        {
          shape.DrawShapeInGame();
        }
      }
      else
      {
        foreach (VolatileShape shape in this.shapes)
        {
          shape.DrawShapeInEditor();

          // Draw True COM
          Vector2 trueShapeCOM = shape.ComputeTrueCenterOfMass();
          trueBodyCOM += trueShapeCOM;
          Gizmos.color = Color.blue;
          Gizmos.DrawWireSphere(trueShapeCOM, 0.1f);
        }
      }
    }

    if (!Application.isPlaying)
    {
      // Draw Body Root
      Gizmos.color = new Color(1.0f, 0.5f, 0.0f);
      Gizmos.DrawWireSphere(transform.position, 0.2f);

      // Draw Body True COM
      Gizmos.color = Color.cyan;
      float length = this.shapes.Length;
      Gizmos.DrawWireSphere(
        new Vector2(trueBodyCOM.x / length, trueBodyCOM.y / length), 0.2f);
    }

    Gizmos.color = current;
  }

  public void AddForce(Vector2 force)
  {
    this.body.AddForce(force);
  }

  public void AddTorque(float radians)
  {
    this.body.AddTorque(radians);
  }

  public void Set(Vector2 position)
  {
    this.body.Set(position);
  }

  public void Set(float radians)
  {
    this.body.Set(radians);
  }

  public void Set(Vector2 position, float radians)
  {
    this.body.Set(position, radians);
  }
}
