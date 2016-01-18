using System;
using System.Collections.Generic;

using UnityEngine;

public class CastHistory : MonoBehaviour 
{
  [SerializeField]
  SimpleCircleCast circleCast;

  [SerializeField]
  SimpleRayCast rayCast;

  void Start () 
  {
  }
  
  void Update () 
  {
    if (Input.GetKeyDown(KeyCode.Alpha6) == true)
    {
      this.circleCast.frame = -1;
      this.rayCast.frame = -1;
    }

    if (Input.GetKeyDown(KeyCode.Alpha7) == true)
    {
      this.circleCast.frame = 0;
      this.rayCast.frame = 0;
    }

    if (Input.GetKeyDown(KeyCode.Alpha8) == true)
    {
      this.circleCast.frame = 1;
      this.rayCast.frame = 1;
    }

    if (Input.GetKeyDown(KeyCode.Alpha9) == true)
    {
      this.circleCast.frame = 2;
      this.rayCast.frame = 2;
    }

    if (Input.GetKeyDown(KeyCode.Alpha0) == true)
    {
      this.circleCast.frame = 3;
      this.rayCast.frame = 3;
    }
  }
}
