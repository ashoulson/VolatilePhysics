/*
 *  Copyright (c) 2017 - Alexander Shoulson - http://ashoulson.com
 */

using System;
using System.IO;
using System.Text;

using UnityEngine;

public class LogConsole : MonoBehaviour
{
  private class DebugLogWriter : TextWriter
  {
    public override void Write(string value)
    {
      base.Write(value);

      string lower = value.ToLower();
      if (lower.StartsWith("error"))
        Debug.LogError(value);
      else if (lower.StartsWith("warning"))
        Debug.LogWarning(value);
      else
        Debug.Log(value);
    }

    public override Encoding Encoding
    {
      get { return Encoding.UTF8; }
    }
  }

  protected void Awake()
  {
    Console.SetOut(new DebugLogWriter());
    Console.SetError(new DebugLogWriter());
  }
}
