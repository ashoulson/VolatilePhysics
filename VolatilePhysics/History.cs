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
  public static class History
  {
    public const int CURRENT_FRAME = -1;

    /// <summary>
    /// Validates user-input frame numbers for processing internally.
    /// </summary>
    internal static bool ShouldStoreOnFrame(int frame)
    {
      if (frame == History.CURRENT_FRAME)
        return false;

      if (frame < 0)
      {
        Debug.LogError("Invalid frame value: " + frame);
        return false;
      }

      return true;
    }

    /// <summary>
    /// Validates a frame number for performing casts and queries.
    /// </summary>
    internal static int ValidateTestFrame(int frame)
    {
      if ((frame != History.CURRENT_FRAME) && (frame < 0))
      {
        Debug.LogError("Invalid frame value " + frame);
        return History.CURRENT_FRAME;
      }

      return frame;
    }
  }
}