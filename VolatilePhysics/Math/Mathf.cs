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

#if !UNITY
using System;

namespace Volatile
{
  public static class Mathf
  {
    public const float PI = 3.14159274f;
    public const float Infinity = float.PositiveInfinity;
    public const float NegativeInfinity = float.NegativeInfinity;
    public const float Deg2Rad = 0.0174532924f;
    public const float Rad2Deg = 57.29578f;

    public static readonly float Epsilon = (!MathfInternal.IsFlushToZeroEnabled) ? MathfInternal.FloatMinDenormal : MathfInternal.FloatMinNormal;

    public static float Sin(float f)
    {
      return (float)Math.Sin((double)f);
    }

    public static float Cos(float f)
    {
      return (float)Math.Cos((double)f);
    }

    public static float Tan(float f)
    {
      return (float)Math.Tan((double)f);
    }

    public static float Asin(float f)
    {
      return (float)Math.Asin((double)f);
    }

    public static float Acos(float f)
    {
      return (float)Math.Acos((double)f);
    }

    public static float Atan(float f)
    {
      return (float)Math.Atan((double)f);
    }

    public static float Atan2(float y, float x)
    {
      return (float)Math.Atan2((double)y, (double)x);
    }

    public static float Sqrt(float f)
    {
      return (float)Math.Sqrt((double)f);
    }

    public static float Abs(float f)
    {
      return Math.Abs(f);
    }

    public static int Abs(int value)
    {
      return Math.Abs(value);
    }

    public static float Min(float a, float b)
    {
      return (a >= b) ? b : a;
    }

    public static float Min(params float[] values)
    {
      int num = values.Length;
      if (num == 0)
      {
        return 0f;
      }
      float num2 = values[0];
      for (int i = 1; i < num; i++)
      {
        if (values[i] < num2)
        {
          num2 = values[i];
        }
      }
      return num2;
    }

    public static int Min(int a, int b)
    {
      return (a >= b) ? b : a;
    }

    public static int Min(params int[] values)
    {
      int num = values.Length;
      if (num == 0)
      {
        return 0;
      }
      int num2 = values[0];
      for (int i = 1; i < num; i++)
      {
        if (values[i] < num2)
        {
          num2 = values[i];
        }
      }
      return num2;
    }

    public static float Max(float a, float b)
    {
      return (a <= b) ? b : a;
    }

    public static float Max(params float[] values)
    {
      int num = values.Length;
      if (num == 0)
      {
        return 0f;
      }
      float num2 = values[0];
      for (int i = 1; i < num; i++)
      {
        if (values[i] > num2)
        {
          num2 = values[i];
        }
      }
      return num2;
    }

    public static int Max(int a, int b)
    {
      return (a <= b) ? b : a;
    }

    public static int Max(params int[] values)
    {
      int num = values.Length;
      if (num == 0)
      {
        return 0;
      }
      int num2 = values[0];
      for (int i = 1; i < num; i++)
      {
        if (values[i] > num2)
        {
          num2 = values[i];
        }
      }
      return num2;
    }

    public static float Pow(float f, float p)
    {
      return (float)Math.Pow((double)f, (double)p);
    }

    public static float Exp(float power)
    {
      return (float)Math.Exp((double)power);
    }

    public static float Log(float f, float p)
    {
      return (float)Math.Log((double)f, (double)p);
    }

    public static float Log(float f)
    {
      return (float)Math.Log((double)f);
    }

    public static float Log10(float f)
    {
      return (float)Math.Log10((double)f);
    }

    public static float Ceil(float f)
    {
      return (float)Math.Ceiling((double)f);
    }

    public static float Floor(float f)
    {
      return (float)Math.Floor((double)f);
    }

    public static float Round(float f)
    {
      return (float)Math.Round((double)f);
    }

    public static int CeilToInt(float f)
    {
      return (int)Math.Ceiling((double)f);
    }

    public static int FloorToInt(float f)
    {
      return (int)Math.Floor((double)f);
    }

    public static int RoundToInt(float f)
    {
      return (int)Math.Round((double)f);
    }

    public static float Sign(float f)
    {
      return (f < 0f) ? -1f : 1f;
    }

    public static float Clamp(float value, float min, float max)
    {
      if (value < min)
      {
        value = min;
      }
      else
      {
        if (value > max)
        {
          value = max;
        }
      }
      return value;
    }

    public static int Clamp(int value, int min, int max)
    {
      if (value < min)
      {
        value = min;
      }
      else
      {
        if (value > max)
        {
          value = max;
        }
      }
      return value;
    }

    public static float Clamp01(float value)
    {
      if (value < 0f)
      {
        return 0f;
      }
      if (value > 1f)
      {
        return 1f;
      }
      return value;
    }

    public static float Lerp(float from, float to, float t)
    {
      return from + (to - from) * Mathf.Clamp01(t);
    }

    public static float LerpAngle(float a, float b, float t)
    {
      float num = Mathf.Repeat(b - a, 360f);
      if (num > 180f)
      {
        num -= 360f;
      }
      return a + num * Mathf.Clamp01(t);
    }

    public static float MoveTowards(float current, float target, float maxDelta)
    {
      if (Mathf.Abs(target - current) <= maxDelta)
      {
        return target;
      }
      return current + Mathf.Sign(target - current) * maxDelta;
    }

    public static float MoveTowardsAngle(float current, float target, float maxDelta)
    {
      target = current + Mathf.DeltaAngle(current, target);
      return Mathf.MoveTowards(current, target, maxDelta);
    }

    public static float SmoothStep(float from, float to, float t)
    {
      t = Mathf.Clamp01(t);
      t = -2f * t * t * t + 3f * t * t;
      return to * t + from * (1f - t);
    }

    public static float Gamma(float value, float absmax, float gamma)
    {
      bool flag = false;
      if (value < 0f)
      {
        flag = true;
      }
      float num = Mathf.Abs(value);
      if (num > absmax)
      {
        return (!flag) ? num : (-num);
      }
      float num2 = Mathf.Pow(num / absmax, gamma) * absmax;
      return (!flag) ? num2 : (-num2);
    }

    public static bool Approximately(float a, float b)
    {
      return Mathf.Abs(b - a) < Mathf.Max(1E-06f * Mathf.Max(Mathf.Abs(a), Mathf.Abs(b)), Mathf.Epsilon * 8f);
    }

    public static float Repeat(float t, float length)
    {
      return t - Mathf.Floor(t / length) * length;
    }

    public static float PingPong(float t, float length)
    {
      t = Mathf.Repeat(t, length * 2f);
      return length - Mathf.Abs(t - length);
    }

    public static float InverseLerp(float from, float to, float value)
    {
      if (from < to)
      {
        if (value < from)
        {
          return 0f;
        }
        if (value > to)
        {
          return 1f;
        }
        value -= from;
        value /= to - from;
        return value;
      }
      else
      {
        if (from <= to)
        {
          return 0f;
        }
        if (value < to)
        {
          return 1f;
        }
        if (value > from)
        {
          return 0f;
        }
        return 1f - (value - to) / (from - to);
      }
    }

    public static float DeltaAngle(float current, float target)
    {
      float num = Mathf.Repeat(target - current, 360f);
      if (num > 180f)
      {
        num -= 360f;
      }
      return num;
    }
  }
}
#endif