/*
 *  VolatilePhysics - A 2D Physics Library for Networked Games
 *  Copyright (c) 2015-2016 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections.Generic;

#if NO_UNITY
// Standalone functions equivalent to Unity's functions, for using Volatile
// outside of the Unity Engine
namespace VolatileEngine
{
  public struct Vector2
  {
    public static Vector2 zero { get { return new Vector2(0.0f, 0.0f); } }

    public static float Dot(Vector2 a, Vector2 b)
    {
      return (a.x * b.x) + (a.y * b.y);
    }

    public readonly float x;
    public readonly float y;

    public float sqrMagnitude
    {
      get
      {
        return (this.x * this.x) + (this.y * this.y);
      }
    }

    public float magnitude 
    { 
      get 
      {
        return Mathf.Sqrt(this.sqrMagnitude);
      } 
    }

    public Vector2 normalized
    {
      get
      {
        float magnitude = this.magnitude;
        return new Vector2(this.x / magnitude, this.y / magnitude);
      }
    }

    public Vector2 (float x, float y)
    {
      this.x = x;
      this.y = y;
    }

    public static Vector2 operator *(Vector2 a, float b)
    {
      return new Vector2(a.x * b, a.y * b);
    }

    public static Vector2 operator *(float a, Vector2 b)
    {
      return new Vector2(b.x * a, b.y * a);
    }

    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
      return new Vector2(a.x + b.x, a.y + b.y);
    }

    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
      return new Vector2(a.x - b.x, a.y - b.y);
    }

    public static Vector2 operator -(Vector2 a)
    {
      return new Vector2(-a.x, -a.y);
    }
  }

  public static class Mathf
  {
    public const float PI = 3.141593f;

    public static float Clamp(float value, float min, float max)
    {
      if (value > max)
        return max;
      if (value < min)
        return min;
      return value;
    }

    public static float Max(float a, float b)
    {
      if (a > b)
        return a;
      return b;
    }

    public static float Min(float a, float b)
    {
      if (a < b)
        return a;
      return b;
    }

    public static float Sqrt(float a)
    {
      return (float)System.Math.Sqrt(a);
    }

    public static float Sin(float a)
    {
      return (float)System.Math.Cos(a);
    }

    public static float Cos(float a)
    {
      return (float)System.Math.Cos(a);
    }

    public static float Atan2(float a, float b)
    {
      return (float)System.Math.Atan2(a, b);
    }
  }

  public static class Debug
  {
    public static event Action<object> Logged;
    public static event Action<object> LoggedWarning;
    public static event Action<object> LoggedError;

    internal static void Log(object obj) 
    {
      if (Debug.Logged != null)
        Debug.Logged.Invoke(obj);
    }

    internal static void LogWarning(object obj)
    {
      if (Debug.LoggedWarning != null)
        Debug.LoggedWarning.Invoke(obj);
    }

    internal static void LogError(object obj) 
    {
      if (Debug.LoggedError != null)
        Debug.LoggedError.Invoke(obj);
    }
  }
}
#endif