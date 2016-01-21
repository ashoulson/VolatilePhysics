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

using UnityEngine;

namespace Volatile
{
  public static class VolatileUtil
  {
    public static void Swap<T>(ref T a, ref T b)
    {
      T temp = b;
      b = a;
      a = temp;
    }

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

    public static Vector2 Polar(float a)
    {
      return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    public static float Cross(Vector2 a, Vector2 b)
    {
      return a.x * b.y - a.y * b.x;
    }

    public static float Square(float a)
    {
      return a * a;
    }

    public static void TransformRay(
      ref RayCast cast, 
      Vector2 localOrigin,
      Vector2 localFacing,
      out Vector2 origin, 
      out Vector2 direction)
    {
      Vector2 rayOrigin = cast.origin;
      Vector2 rayDirection = cast.direction;

      float sin = localFacing.y;
      float cos = localFacing.x;

      // Our shape's transform matrix (counter-clockwise rotation)
      //  ⌈  cos Θ    -sin Θ     t_x  ⌉ ⌈ x ⌉ 
      //  |  sin Θ     cos Θ     t_y  | | y |
      //  ⌊    0         0        1   ⌋ ⌊ 1 ⌋ 
      //
      //  When inverting, we reverse the matrix order (TR -> R^{-1} T^{-1})

      // First invert the transposition
      Vector2 newOrigin = rayOrigin - localOrigin;

      // Now invert the rotations, where R^{-1} =
      //  ⌈  cos Θ     sin Θ      0   ⌉ ⌈ x ⌉ 
      //  | -sin Θ     cos Θ      0   | | y |
      //  ⌊    0         0        1   ⌋ ⌊ 1 ⌋ 

      origin = new Vector2(
        cos * newOrigin.x + sin * newOrigin.y,
        -sin * newOrigin.x + cos * newOrigin.y);

      direction = new Vector2(
        cos * rayDirection.x + sin * rayDirection.y,
        -sin * rayDirection.x + cos * rayDirection.y);
    }

    #region Debug
    public static void Draw(Body body)
    {
      body.GizmoDraw(
        new Color(1.0f, 1.0f, 0.0f, 1.0f), // Edge Color
        new Color(1.0f, 0.0f, 1.0f, 1.0f), // Normal Color
        new Color(1.0f, 0.0f, 0.0f, 1.0f), // Body Origin Color
        new Color(0.0f, 0.0f, 0.0f, 1.0f), // Shape Origin Color
        new Color(0.1f, 0.0f, 0.5f, 1.0f), // Body AABB Color
        new Color(0.7f, 0.0f, 0.3f, 0.5f), // Shape AABB Color
        0.25f);
    }

    public static void Draw(Shape shape)
    {
      shape.GizmoDraw(
        new Color(1.0f, 1.0f, 0.0f, 1.0f), // Edge Color
        new Color(1.0f, 0.0f, 1.0f, 1.0f), // Normal Color
        new Color(0.0f, 0.0f, 0.0f, 1.0f), // Origin Color
        new Color(0.7f, 0.0f, 0.3f, 1.0f), // AABB Color
        0.25f);
    }

    public static void Draw(AABB aabb)
    {
      aabb.GizmoDraw(
        new Color(1.0f, 0.0f, 0.5f, 1.0f)); // AABB Color
    }
    #endregion
  }
}
