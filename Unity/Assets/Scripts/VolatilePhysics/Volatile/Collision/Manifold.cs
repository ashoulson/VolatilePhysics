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
  // TODO: Manifold/Contact pooling
  public sealed class Manifold
  {
    #region Static Methods
    internal static Manifold FindMatch(
      IEnumerable<Manifold> manifolds,
      Shape shapeA,
      Shape shapeB)
    {
      foreach (Manifold manifold in manifolds)
      {
        bool match =
          (manifold.shapeA == shapeA && manifold.shapeB == shapeB) ||
          (manifold.shapeA == shapeB && manifold.shapeB == shapeA);
        if (match == true)
          return manifold;
      }
      return null;
    }
    #endregion

    public Shape shapeA, shapeB;
    public float restitution, friction;

    internal int used = 0;
    internal Contact[] contacts;

    internal Manifold(uint capacity)
    {
      this.contacts = new Contact[capacity];
    }

    internal bool UpdateContact(
      Vector2 position,
      Vector2 normal,
      float penetration,
      uint id)
    {
      Contact c;
      for (int i = 0; i < this.used; i++)
      {
        c = this.contacts[i];
        if (c.id == id)
          goto found;
      }

      if (this.used == contacts.Length)
        return false;
      if (this.contacts[this.used] == null)
        this.contacts[this.used] = new Contact();
      c = this.contacts[this.used];

      this.used++;

      c.id = id;
      c.cachedNormalImpulse = 0;
      c.cachedTangentImpulse = 0;

    found:
      c.position = position;
      c.normal = normal;
      c.penetration = penetration;
      c.updated = true;

      return true;
    }

    internal void Prestep()
    {
      this.restitution = Mathf.Sqrt(shapeA.restitution * shapeB.restitution);
      this.friction = Mathf.Sqrt(shapeA.friction * shapeB.friction);

      for (int i = used - 1; i >= 0; i--)
      {
        Contact c = contacts[i];
        if (!c.updated)
        {
          if (i < --used)
          {
            contacts[i] = contacts[used];
            contacts[used] = c;
          }
        }
        else
        {
          c.updated = false;
          c.Prestep(this);
        }
      }
    }

    internal void PerformCached()
    {
      for (int i = 0; i < used; i++)
        contacts[i].SolveCached(this);
    }

    internal void Perform()
    {
      for (int i = 0; i < used; i++)
        contacts[i].Solve(this);
    }
  }
}
