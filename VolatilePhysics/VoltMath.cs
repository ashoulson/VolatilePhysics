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

#if UNITY
using UnityEngine;
#endif

namespace Volatile
{
  public static class VoltMath
  {
    #region Transformations
    public static Vector2 WorldToBodyPoint(
      Vector2 bodyPosition,
      Vector2 bodyFacing,
      Vector2 vector)
    {
      return (vector - bodyPosition).InvRotate(bodyFacing);
    }

    public static Vector2 WorldToBodyDirection(
      Vector2 bodyFacing,
      Vector2 vector)
    {
      return vector.InvRotate(bodyFacing);
    }
    #endregion

    #region Body-Space to World-Space Transformations
    public static Vector2 BodyToWorldPoint(
      Vector2 bodyPosition,
      Vector2 bodyFacing,
      Vector2 vector)
    {
      return vector.Rotate(bodyFacing) + bodyPosition;
    }

    public static Vector2 BodyToWorldDirection(
      Vector2 bodyFacing,
      Vector2 vector)
    {
      return vector.Rotate(bodyFacing);
    }
    #endregion

    public static Vector2 Right(this Vector2 v)
    {
      return new Vector2(v.y, -v.x);
    }

    public static Vector2 Left(this Vector2 v)
    {
      return new Vector2(-v.y, v.x);
    }

    public static Vector2 Rotate(this Vector2 v, Vector2 b)
    {
      return new Vector2(v.x * b.x - v.y * b.y, v.y * b.x + v.x * b.y);
    }

    public static Vector2 InvRotate(this Vector2 v, Vector2 b)
    {
      return new Vector2(v.x * b.x + v.y * b.y, v.y * b.x - v.x * b.y);
    }

    public static float Angle(this Vector2 v)
    {
      return Mathf.Atan2(v.y, v.x);
    }

    public static Vector2 Polar(float radians)
    {
      return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    public static float Cross(Vector2 a, Vector2 b)
    {
      return a.x * b.y - a.y * b.x;
    }

    public static float Square(float a)
    {
      return a * a;
    }
  }
}
