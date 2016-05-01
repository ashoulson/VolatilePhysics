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
  public static class ConsoleLogger
  {
    public static void Initialize()
    {
      UtilLogger.Message += ConsoleLogger.OnMessage;
      UtilLogger.Warning += ConsoleLogger.OnWarning;
      UtilLogger.Error += ConsoleLogger.OnError;
    }

    private static void OnMessage(string message)
    {
      Console.WriteLine("LOG: " + message);
    }

    private static void OnWarning(string warning)
    {
      Console.WriteLine(
        "WARNING: " +
        warning +
        "\n" +
        new System.Diagnostics.StackTrace());
    }

    private static void OnError(string error)
    {
      Console.WriteLine(
        "ERROR: " +
        error +
        "\n" +
        new System.Diagnostics.StackTrace());
    }
  }
}
