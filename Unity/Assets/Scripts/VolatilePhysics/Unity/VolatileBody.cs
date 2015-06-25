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

  [SerializeField]
  private bool isStatic = false;

  public Body body;

  void Awake()
  {
    List<Shape> shapes = new List<Shape>();
    foreach (VolatileShape shape in this.shapes)
    {
      shapes.Add(shape.Shape);
      shape.isStandalone = false;
    }

    this.body = new Body(
      transform.position, 
      Mathf.Deg2Rad * transform.eulerAngles.z, 
      shapes);
    this.body.UseGravity = this.useGravity;
    this.body.IsStatic = this.isStatic;
  }

  void Start()
  {
    VolatileWorld.Instance.AddBody(this.body);
  }

  void Update()
  {
    if (UnityEditor.Selection.activeGameObject == this.gameObject)
    {
      this.body.SetWorld(
        transform.position, 
        Mathf.Deg2Rad * transform.rotation.eulerAngles.z);
    }
    else
    {
      transform.position = this.body.Position;
      transform.rotation = 
        Quaternion.Euler(0.0f, 0.0f, Mathf.Rad2Deg * this.body.Angle);
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
        VolatileUtil.Draw(this.body);
      }
      else
      {
        foreach (VolatileShape shape in this.shapes)
        {
          // Draw True COM
          Vector2 trueShapeCOM = shape.ComputeTrueCenterOfMass();
          trueBodyCOM += trueShapeCOM;
          Gizmos.color = Color.blue;
          Gizmos.DrawWireSphere(trueShapeCOM, 0.1f);
        }
      }
    }

    if (Application.isPlaying == false)
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
    this.body.SetWorld(position);
  }

  public void Set(float radians)
  {
    this.body.SetWorld(radians);
  }

  public void Set(Vector2 position, float radians)
  {
    this.body.SetWorld(position, radians);
  }
}
