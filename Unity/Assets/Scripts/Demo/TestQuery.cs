using System;
using System.Collections.Generic;

using UnityEngine;

public class TestQuery : MonoBehaviour 
{
  [SerializeField]
  VolatileShape shape;

  [SerializeField]
  VolatileBody body;

	void Start () 
	{
	}
	
	void Update () 
	{
    bool world = false;// ((Volatile.Polygon)(this.shape.Shape)).ContainsPointWorld(transform.position);
    bool query = this.body.Body.Query(this.transform.position);

    Debug.Log((world ? "Yes" : "No") + " --- " + (query ? "Yes" : "No"));
	}
}
