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
  public sealed class Contact
  {
    #region Static Methods
    private static float BiasDist(float dist)
    {
      return Config.RESOLVE_RATE * Mathf.Min(0, dist + Config.RESOLVE_SLOP);
    }
    #endregion

    public Vector2 position;
    public float penetration;
    public float cachedNormalImpulse;
    public float cachedTangentImpulse;

    public Vector2 normal;
    private Vector2 toA;
    private Vector2 toB;

    private float nMass;
    private float tMass;
    private float restitution;
    private float bias;
    private float jBias;

    internal uint id;
    internal bool updated;

    public Contact()
    {
    }

    internal void Prestep(Manifold manifold)
    {
      Body bodyA = manifold.shapeA.Body;
      Body bodyB = manifold.shapeB.Body;

      this.toA = this.position - bodyA.Position;
      this.toB = this.position - bodyB.Position;

      this.nMass = 1.0f / this.KScalar(bodyA, bodyB, this.normal);
      this.tMass = 1.0f / this.KScalar(bodyA, bodyB, this.normal.Left());

      this.bias = Contact.BiasDist(penetration);
      this.jBias = 0;
      this.restitution =
        manifold.restitution *
        Vector2.Dot(
          this.normal,
          this.RelativeVelocity(bodyA, bodyB));
    }

    internal void SolveCached(Manifold manifold)
    {
      this.ApplyContactImpulse(
        manifold.shapeA.Body,
        manifold.shapeB.Body,
        this.cachedNormalImpulse,
        this.cachedTangentImpulse);
    }

    internal void Solve(Manifold manifold)
    {
      Body bodyA = manifold.shapeA.Body;
      Body bodyB = manifold.shapeB.Body;

      // Calculate relative bias velocity
      Vector2 vb1 =
        bodyA.BiasVelocity + (bodyA.BiasRotation * this.toA.Left());
      Vector2 vb2 =
        bodyB.BiasVelocity + (bodyB.BiasRotation * this.toB.Left());
      float vbn = Vector2.Dot((vb1 - vb2), this.normal);

      // Calculate and clamp the bias impulse
      float jbn = this.nMass * (vbn - this.bias);
      jbn = Mathf.Max(-this.jBias, jbn);
      this.jBias += jbn;

      // Apply the bias impulse
      this.ApplyNormalBiasImpulse(bodyA, bodyB, jbn);

      // Calculate relative velocity
      Vector2 vr = this.RelativeVelocity(bodyA, bodyB);
      float vrn = Vector2.Dot(vr, this.normal);

      // Calculate and clamp the normal impulse
      float jn = nMass * (vrn + (this.restitution * World.elasticity));
      jn = Mathf.Max(-this.cachedNormalImpulse, jn);
      this.cachedNormalImpulse += jn;

      // Calculate the relative tangent velocity
      float vrt = Vector2.Dot(vr, this.normal.Left());

      // Calculate and clamp the friction impulse
      float jtMax = manifold.friction * this.cachedNormalImpulse;
      float jt = vrt * tMass;
      float result =
        Mathf.Clamp(this.cachedTangentImpulse + jt, -jtMax, jtMax);
      jt = result - this.cachedTangentImpulse;
      this.cachedTangentImpulse = result;

      // Apply the normal and tangent impulse
      this.ApplyContactImpulse(bodyA, bodyB, jn, jt);
    }

    #region Internals
    private float KScalar(
      Body bodyA,
      Body bodyB,
      Vector2 normal)
    {
      float massSum = bodyA.InvMass + bodyB.InvMass;
      float r1cnSqr = Util.Square(Util.Cross(this.toA, normal));
      float r2cnSqr = Util.Square(Util.Cross(this.toB, normal));
      return massSum + bodyA.InvInertia * r1cnSqr + bodyB.InvInertia * r2cnSqr;
    }

    private Vector2 RelativeVelocity(Body bodyA, Body bodyB)
    {
      return
        (bodyA.AngularVelocity * this.toA.Left() + bodyA.LinearVelocity) -
        (bodyB.AngularVelocity * this.toB.Left() + bodyB.LinearVelocity);
    }

    private void ApplyNormalBiasImpulse(
      Body bodyA,
      Body bodyB,
      float normalBiasImpulse)
    {
      Vector2 impulse = normalBiasImpulse * this.normal;
      bodyA.ApplyBias(-impulse, this.toA);
      bodyB.ApplyBias(impulse, this.toB);
    }

    private void ApplyContactImpulse(
      Body bodyA,
      Body bodyB,
      float normalImpulseMagnitude,
      float tangentImpulseMagnitude)
    {
      Vector2 impulseWorld =
        new Vector2(normalImpulseMagnitude, tangentImpulseMagnitude);
      Vector2 impulse = impulseWorld.Rotate(this.normal);

      bodyA.ApplyImpulse(-impulse, this.toA);
      bodyB.ApplyImpulse(impulse, this.toB);
    }
    #endregion
  }
}
