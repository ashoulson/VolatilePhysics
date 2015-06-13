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
  internal sealed class Manifold : IPoolable<Manifold>
  {
    #region Pool Class
    internal sealed class Pool : ObjectPool<Manifold>
    {
      private Contact.Pool contactPool;

      public Pool(Contact.Pool contactPool)
      {
        this.contactPool = contactPool;
      }

      protected override Manifold Create()
      {
        return new Manifold(this.contactPool);
      }
    }
    #endregion

    #region IPoolable Members
    Manifold IPoolable<Manifold>.Next { get; set; }

    bool IPoolable<Manifold>.IsValid 
    { 
      get { return this.isValid; } 
      set { this.isValid = value; } 
    }

    private bool isValid = false;
    #endregion

    internal Shape ShapeA { get; private set; }
    internal Shape ShapeB { get; private set; }
    internal float Restitution { get; private set; }
    internal float Friction { get; private set; }

    private int used = 0;
    private Contact[] contacts;
    private ObjectPool<Contact> contactPool;

    public Manifold(ObjectPool<Contact> contactPool)
    {
      this.contactPool = contactPool;

      this.ShapeA = null;
      this.ShapeB = null;
      this.Restitution = 0.0f;
      this.Friction = 0.0f;
      this.contacts = new Contact[Config.MAX_CONTACTS];
      this.used = 0;

      this.isValid = false;     
    }

    internal Manifold Assign(
      Shape shapeA, 
      Shape shapeB)
    {
      this.ShapeA = shapeA;
      this.ShapeB = shapeB;
      this.Restitution = Mathf.Sqrt(shapeA.restitution * shapeB.restitution);
      this.Friction = Mathf.Sqrt(shapeA.friction * shapeB.friction);
      this.used = 0;

      this.isValid = true;
      return this;
    }

    internal bool AddContact(
      Vector2 position,
      Vector2 normal,
      float penetration)
    {
      if (this.used >= contacts.Length)
        return false;
      this.contacts[this.used++] = 
        this.contactPool.Acquire().Assign(position, normal, penetration);
      return true;
    }

    internal void Prestep()
    {
      for (int i = 0; i < this.used; i++)
        this.contacts[i].Prestep(this);
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

    internal void ReleaseContacts()
    {
      for (int i = 0; i < this.used; i++)
        this.contactPool.Release(this.contacts[i]);
    }
  }
}
