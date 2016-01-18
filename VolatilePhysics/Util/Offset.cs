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
  public struct Offset
  {
    public Vector2 PositionOffset { get { return this.positionOffset; } }
    public Vector2 FacingOffset { get { return this.facingOffset; } }

    private Vector2 positionOffset;
    private Vector2 facingOffset;

    public Offset(
      Vector2 parentPosition,
      Vector2 parentFacing,
      Vector2 childPosition,
      Vector2 childFacing)
    {
      Vector2 rawPosOffset = childPosition - parentPosition;
      this.positionOffset = rawPosOffset.InvRotate(parentFacing);
      this.facingOffset = childFacing.InvRotate(parentFacing);
    }

    public Offset(Body parent, Shape child)
      : this(parent.Position, parent.Facing, child.Position, child.Facing) { }

    public void Compute(
      Vector2 parentPosition,
      Vector2 parentFacing,
      out Vector2 childPosition,
      out Vector2 childFacing)
    {
      childPosition =
        parentPosition + this.positionOffset.Rotate(parentFacing);
      childFacing = parentFacing.Rotate(this.facingOffset);
    }
  }
}