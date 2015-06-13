/*
 *  VolatilePhysics - A 2D Physics Library for Networked Games
 *  Copyright (c) 2015 - Alexander Shoulson - http://ashoulson.com
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
  internal sealed class Manifold
  {
    internal Shape ShapeA { get; private set; }
    internal Shape ShapeB { get; private set; }
    internal float Restitution { get; private set; }
    internal float Friction { get; private set; }

    private int used = 0;
    private Contact[] contacts;

    internal Manifold(Shape shapeA, Shape shapeB)
    {
      this.contacts = new Contact[Config.MAX_CONTACTS];

      this.ShapeA = shapeA;
      this.ShapeB = shapeB;

      this.Restitution = Mathf.Sqrt(shapeA.restitution * shapeB.restitution);
      this.Friction = Mathf.Sqrt(shapeA.friction * shapeB.friction);
    }

    internal bool AddContact(
      Vector2 position,
      Vector2 normal,
      float penetration)
    {
      if (this.used >= contacts.Length)
        return false;

      // TODO: POOLING
      this.contacts[this.used++] = new Contact(position, normal, penetration);
      return true;
    }

    internal void Prestep()
    {
      for (int i = 0; i < this.used; i++)
        contacts[i].Prestep(this);
    }

    internal void Solve()
    {
      for (int i = 0; i < this.used; i++)
        contacts[i].Solve(this);
    }

    internal void SolveCached()
    {
      for (int i = 0; i < this.used; i++)
        contacts[i].SolveCached(this);
    }
  }
}
