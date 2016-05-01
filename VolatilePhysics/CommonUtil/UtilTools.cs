/*
 *  Common Utilities for Working with C# and Unity
 *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
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
using System.Collections;
using System.Collections.Generic;

namespace CommonUtil
{
  public static class UtilTools
  {
    public static IEnumerable<T> Interleave<T>(
      IEnumerable<T> first,
      IEnumerable<T> second)
    {
      using (IEnumerator<T> firstEnumerator = first.GetEnumerator(),
                            secondEnumerator = second.GetEnumerator())
      {
        bool hasFirstItem = true;
        bool hasSecondItem = true;

        while (hasFirstItem || hasSecondItem)
        {
          if (hasFirstItem)
          {
            hasFirstItem = firstEnumerator.MoveNext();

            if (hasFirstItem)
            {
              yield return firstEnumerator.Current;
            }
          }

          if (hasSecondItem)
          {
            hasSecondItem = secondEnumerator.MoveNext();
            if (hasSecondItem)
            {
              yield return secondEnumerator.Current;
            }
          }
        }
      }
    }

    public static void Swap<T>(ref T a, ref T b)
    {
      T temp = b;
      b = a;
      a = temp;
    }

    public static int ExpandArray<T>(ref T[] oldArray)
    {
      // TODO: Revisit this using next-largest primes like built-in lists do
      int newCapacity = oldArray.Length * 2;
      T[] newArray = new T[newCapacity];
      Array.Copy(oldArray, newArray, oldArray.Length);
      oldArray = newArray;
      return newCapacity;
    }

    public static bool GetFlag(byte field, byte flag)
    {
      return ((field & flag) > 0);
    }

    public static byte SetFlag(byte field, byte flag, bool value)
    {
      if (value)
        return (byte)(field | flag);
      return (byte)(field & ~flag);
    }

    public static byte ToSimpleAscii(char character)
    {
      byte value = 0;

      try
      {
        value = Convert.ToByte(character);
      }
      catch (OverflowException)
      {
        UtilDebug.LogMessage("Cannot convert to simple ASCII: " + character);
        return 0;
      }

      if (value > 127)
      {
        UtilDebug.LogMessage("Cannot convert to simple ASCII: " + character);
        return 0;
      }

      return value;
    }
  }
}
