using System;
using System.Collections.Generic;

using UnityEngine;

using Volatile;

public class ShapeTester : MonoBehaviour 
{
  public Body body;
  public Polygon shape;

  public Transform bodyOrigin;
  public Transform origin;
  public Transform[] points;

  private Vector2 offset;
  private Vector2 facingOffset;

  void Start () 
  {
    Vector2[] vertices = new Vector2[this.points.Length];
    for (int i = 0; i < vertices.Length; i++)
      vertices[i] = this.points[i].position;

    this.shape = Polygon.FromWorldVertices(this.origin.position, this.origin.right, vertices);
    this.body = new Body(this.bodyOrigin.position);
  }


  //Vector2 a = new Vector2(1.8f, 3.5f).normalized;
  //Vector2 b = new Vector2(9.3f, 6.7f).normalized;

  //Debug.Log(a.x + " " + a.y);
  //Debug.Log(b.x + " " + b.y);

  ////Vector2 c = a.Rotate(b);

  ////Debug.Log(c.x + " " + c.y);

  ////Vector2 d = new Vector2(a.x * b.x - a.y * b.y, a.y * b.x + a.x * b.y);
  //Vector2 c = a.Rotate(b);

  //Debug.Log(c.x + " " + c.y);

  ////Vector2 p = new Vector2(b.x * d.x + b.y * d.y, -b.y * d.x + b.x * d.y);
  //Vector2 p = c.InvRotate(b);

  //Debug.Log(p.x + " " + p.y);
  
  //void Update () 
  //{
  //  if (Input.GetKeyDown(KeyCode.A))
  //  {
  //    this.body = true;
  //    this.facingOffset = ((Vector2)this.bodyOrigin.right).InvRotate(this.origin.right);

  //    Vector2 rawOffset = this.origin.position - this.bodyOrigin.position;
  //    this.offset = ((Vector2)this.bodyOrigin.right).InvRotate(rawOffset);
  //  }
  //  if (Input.GetKeyDown(KeyCode.S))
  //  {
  //    this.body = false;
  //  }

  //  if (this.body == true)
  //  {
  //    this.origin.position =
  //      (Vector2)this.bodyOrigin.position +
  //      this.offset.Rotate(this.bodyOrigin.right);
  //    Vector2 facing =
  //      ((Vector2)bodyOrigin.right).Rotate(this.facingOffset);
  //    SetTransformRight(this.origin, facing);
  //  }

  //  this.shape.SetWorld(origin.position, origin.right);
  //}

  private Fixture fixture;

  void Update()
  {
    this.body.SetWorld(this.bodyOrigin.position, Mathf.Deg2Rad * this.bodyOrigin.rotation.eulerAngles.z);
    if (this.fixture != null)
    {
      this.fixture.Apply(this.body.Position, this.body.Facing);
    }
    else
    {
      this.shape.SetWorld(this.origin.position, this.origin.right);
    }

    if (Input.GetKeyDown(KeyCode.A))
    {
      this.fixture = Fixture.FromWorldSpace(this.body, this.shape);
    }
    if (Input.GetKeyDown(KeyCode.S))
    {
      this.fixture = null;
    }
  }

  private void SetTransformRight(Transform t, Vector2 facing)
  {
    float angle = Mathf.Rad2Deg * Mathf.Atan2(facing.x, -facing.y) - 90.0f;
    t.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
  }

  void OnDrawGizmos()
  {
    if (this.shape != null)
    {
      VolatileDebug.DrawShape(this.shape, Color.yellow, Color.red, Color.black);



      if (this.fixture != null)
      {
        Vector2 bodyFacing = this.bodyOrigin.right;
        Vector2 fixtureOffset = this.fixture.positionOffset;

        Vector2[] vertices = this.shape.LocalVertices;

        for (int i = 0; i < vertices.Length; i++)
        {
          Vector2 originOffset = bodyFacing.Rotate(fixtureOffset);
          Vector2 vertexOffset = originOffset + this.shape.Facing.Rotate(vertices[i]);

          Vector2 origin = this.bodyOrigin.position;
          Vector2 destination = origin + vertexOffset;

          Gizmos.color = Color.white;
          Gizmos.DrawLine(origin, destination);
        }
      }
    }
  }
}
