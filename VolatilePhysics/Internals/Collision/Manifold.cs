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

#if UNITY
using UnityEngine;
#endif

namespace Volatile
{
  internal sealed class Manifold
    : IVoltPoolable<Manifold>
  {
    #region Interface
    IVoltPool<Manifold> IVoltPoolable<Manifold>.Pool { get; set; }
    void IVoltPoolable<Manifold>.Reset() { this.Reset(); }
    #endregion

    internal VoltShape ShapeA { get; private set; }
    internal VoltShape ShapeB { get; private set; }
    internal float Restitution { get; private set; }
    internal float Friction { get; private set; }

    private readonly Contact[] contacts;
    private int used = 0;
    private VoltWorld world;

    public Manifold()
    {
      this.contacts = new Contact[VoltConfig.MAX_CONTACTS];
      this.used = 0;
      this.Reset();
    }

    internal Manifold Assign(
      VoltWorld world,
      VoltShape shapeA,
      VoltShape shapeB)
    {
      this.world = world;
      this.ShapeA = shapeA;
      this.ShapeB = shapeB;

      this.Restitution = Mathf.Sqrt(shapeA.Restitution * shapeB.Restitution);
      this.Friction = Mathf.Sqrt(shapeA.Friction * shapeB.Friction);
      this.used = 0;

      return this;
    }

    internal bool AddContact(
      Vector2 position,
      Vector2 normal,
      float penetration)
    {
      if (this.used >= VoltConfig.MAX_CONTACTS)
        return false;

      this.contacts[this.used] =
        this.world.AllocateContact().Assign(
          position, 
          normal, 
          penetration);
      this.used++;

      return true;
    }

    internal void PreStep()
    {
      for (int i = 0; i < this.used; i++)
        this.contacts[i].PreStep(this);
    }

    internal void Solve()
    {
      for (int i = 0; i < this.used; i++)
        this.contacts[i].Solve(this);
    }

    internal void SolveCached()
    {
      for (int i = 0; i < this.used; i++)
        this.contacts[i].SolveCached(this);
    }

    private void ClearContacts()
    {
      for (int i = 0; i < this.used; i++)
        VoltPool.Free(this.contacts[i]);
      this.used = 0;
    }

    private void Reset()
    {
      this.ShapeA = null;
      this.ShapeB = null;
      this.Restitution = 0.0f;
      this.Friction = 0.0f;

      this.ClearContacts();
      this.world = null;
    }
  }
}