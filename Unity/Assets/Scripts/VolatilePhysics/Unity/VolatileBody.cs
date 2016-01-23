﻿using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Volatile;

public class VolatileBody : MonoBehaviour 
{
  [SerializeField]
  private VolatileShape[] shapes;

  [SerializeField]
  private bool isStatic = false;

  public Body Body { get { return this.body; } }
  private Body body;

  private Vector2 lastPosition;
  private Vector2 nextPosition;

  private float lastAngle;
  private float nextAngle;

  void Awake()
  {
    IEnumerable<Shape> shapes = this.shapes.Select((s) => s.Shape);
    Vector2 position = transform.position;
    float radians = Mathf.Deg2Rad * transform.eulerAngles.z;

    if (this.isStatic == true)
      this.body = Body.CreateStatic(position, radians, shapes);
    else
      this.body = Body.CreateDynamic(position, radians, shapes);

    this.lastPosition = this.nextPosition = transform.position;
    this.lastAngle = this.nextAngle = transform.eulerAngles.z;
  }

  void Start()
  {
    VolatileWorld.Instance.AddBody(this.body);
  }

  void Update()
  {
    if (Selection.activeGameObject != this.gameObject)
    {
      float t = (Time.time - Time.fixedTime) / Time.deltaTime;

      transform.position = Vector2.Lerp(this.lastPosition, this.nextPosition, t);
      float angle = Mathf.LerpAngle(this.lastAngle, this.nextAngle, t);
      transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Rad2Deg * angle);
    }
    else
    {
      this.body.Set(
        this.transform.position, 
        Mathf.Deg2Rad * this.transform.rotation.eulerAngles.z);
    }
  }

  void FixedUpdate()
  {
    this.lastPosition = this.nextPosition;
    this.lastAngle = this.nextAngle;
    this.nextPosition = this.body.Position;
    this.nextAngle = this.body.Angle;
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
          shape.DrawShapeInEditor();

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

  public void Set(Vector2 position, float radians)
  {
    this.body.Set(position, radians);
  }
}
