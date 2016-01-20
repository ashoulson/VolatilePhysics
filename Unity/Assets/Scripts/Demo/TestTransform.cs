using System;
using System.Collections.Generic;

using UnityEngine;

public class TestTransform : MonoBehaviour 
{
  [SerializeField]
  VolatileBody body;

  [SerializeField]
  GameObject query;

  [SerializeField]
  Vector2 facing;

	void Start () 
	{
	}
	
	void Update () 
	{
    //Vector2 queryWorldPos = this.query.transform.position;
    //Vector2 queryLocalPos = this.query.transform.localPosition;

    //Vector2 derivedWorldPos = this.body.body.TransformBodyToWorld(queryLocalPos);
    //Vector2 derivedLocalPos = this.body.body.TransformWorldToBody(queryWorldPos);

    //float deltaWorld = (queryWorldPos - derivedWorldPos).magnitude;
    //if (deltaWorld < 0.005f)
    //  deltaWorld = 0;

    //float deltaLocal = (queryLocalPos - derivedLocalPos).magnitude;
    //if (deltaLocal < 0.005f)
    //  deltaLocal = 0;

    //Debug.Log("World: " + queryWorldPos + " " + derivedWorldPos + " " + deltaWorld);
    //Debug.Log("Local: " + queryLocalPos + " " + derivedLocalPos + " " + deltaLocal);

    Debug.Log(body.transform.worldToLocalMatrix.MultiplyVector(facing));
	}
}
