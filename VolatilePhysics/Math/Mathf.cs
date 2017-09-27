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
namespace Volatile
{
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
      return (float)System.Math.Sin(a);
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
}
#endif