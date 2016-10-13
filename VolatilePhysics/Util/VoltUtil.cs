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

namespace Volatile
{
  public static class VoltUtil
  {
    public static void Swap<T>(ref T a, ref T b)
    {
      T temp = b;
      b = a;
      a = temp;
    }

    public static int ExpandArray<T>(ref T[] oldArray, int minSize = 1)
    {
      // TODO: Revisit this using next-largest primes like built-in lists do
      int newCapacity = Math.Max(oldArray.Length * 2, minSize);
      T[] newArray = new T[newCapacity];
      Array.Copy(oldArray, newArray, oldArray.Length);
      oldArray = newArray;
      return newCapacity;
    }

    public static bool Filter_StaticOnly(VoltBody body)
    {
      return body.IsStatic;
    }

    public static bool Filter_DynamicOnly(VoltBody body)
    {
      return (body.IsStatic == false);
    }

    public static bool Filter_All(VoltBody body)
    {
      return true;
    }
  }
}
