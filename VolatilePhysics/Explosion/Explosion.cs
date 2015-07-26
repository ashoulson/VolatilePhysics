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
  public class Explosion
  {
    // We push the arc borders in to avoid edge-cases on the borders
    // of the shape we're casting on
    private const float BORDER_EPSILON = 0.005f;

    private World world;
    private Vector2 origin;
    private float radius;
    private float radiusSqr;
    private BodyFilter filter;

    public Explosion(
      World world,
      Vector2 origin,
      float radius,
      BodyFilter filter = null)
    {
      this.world = world;
      this.origin = origin;
      this.radius = radius;
      this.radiusSqr = radius * radius;
      this.filter = filter;
    }

    public void PerformRaycasts(
      Body body, 
      int resolution, 
      IList<Tuple<Vector2, float>> rayHits, 
      out float interval)
    {
      float minAngle;
      this.ComputeAngleInformation(
        body, 
        resolution,
        out minAngle, 
        out interval);

      IEnumerable<Vector2> directions = 
        this.GetRayDirections(
          resolution, 
          minAngle, 
          interval);

      RayCast ray;
      RayResult result = new RayResult();;
      BodyFilter filter;
      foreach (Vector2 direction in directions)
      {
        Vector2 scaled = direction * this.radius;
        ray = new RayCast(this.origin, this.origin + scaled);
        filter = this.CreateFilter(body);
        result.Reset();

        if (this.world.RayCast(ref ray, ref result, filter) == true)
        {
          if (result.Shape.Body == body)
          {
            Tuple<Vector2, float> hit =
              new Tuple<Vector2, float>(
                result.ComputePoint(ref ray),
                result.Distance);
            rayHits.Add(hit);
          }
        }
      }
    }

    private BodyFilter CreateFilter(Body targetBody)
    {
      return (b) => ((b.IsStatic == true) || (b == targetBody));
    }

    private void ComputeAngleInformation(
      Body body,
      int resolution, 
      out float minAngle,
      out float interval)
    {
      Vector2 minVertex, maxVertex;
      this.GetMinMaxVertices(
        this.DecomposeBody(body),
        body.Position,
        out minVertex,
        out maxVertex);

      minAngle = (minVertex - this.origin).Angle() + BORDER_EPSILON;
      float maxAngle = (maxVertex - this.origin).Angle() - BORDER_EPSILON;
      interval = CCWDiffAngle(minAngle, maxAngle) / (float)resolution;
    }

    private IEnumerable<Vector2> GetRayDirections(
      int resolution,
      float minAngle,
      float interval)
    {
      for (int i = 0; i <= resolution; i++)
        yield return VolatileUtil.Polar(minAngle + ((float)i * interval));
    }

    /// <summary>
    /// Decomposes a body into a point cloud of its vertices
    /// </summary>
    private IEnumerable<Vector2> DecomposeBody(Body body)
    {
      for (int i = 0; i < body.shapes.Count; i++)
      {
        Circle circle = body.shapes[i] as Circle;
        if (circle != null)
        {
          // For circles, find the tangent points
          Vector2 delta = circle.Position - this.origin;
          float dd = Mathf.Sqrt(Vector2.Dot(delta, delta));
          float a = Mathf.Asin(circle.Radius / dd);
          float b = Mathf.Atan2(delta.y, delta.x);

          float t1 = b - a;
          float t2 = b + a;

          // Add the tangent points
          yield return
            circle.Position + 
            new Vector2(
              circle.Radius * Mathf.Sin(t1),
              circle.Radius * -Mathf.Cos(t1));
          yield return
            circle.Position + 
            new Vector2(
              circle.Radius * -Mathf.Sin(t2),
              circle.Radius * Mathf.Cos(t2));
          continue;
        }

        Polygon polygon = body.shapes[i] as Polygon;
        if (polygon != null)
        {
          // For polygons, just return all of the world vertices
          for (int j = 0; j < polygon.worldVertices.Length; j++)
            yield return polygon.worldVertices[j];
          continue;
        }
      }
    }

    /// <summary>
    /// Returns the counter-clockwise difference between a min and max angle.
    /// </summary>
    private float CCWDiffAngle(float min, float max)
    {
      if (max >= 0.0f && min >= 0.0f)
        return (min >= 0.0f) ? max - min : max + Mathf.Abs(min);
      else
        return (min >= 0.0f) ? (max + (2.0f * Mathf.PI)) - min : max - min;
    }

    #region Pseudoangles
    /// <summary>
    /// Gets the extreme points describing the circle's coverage arc for
    /// a cloud of vertices.
    /// </summary>
    private void GetMinMaxVertices(
      IEnumerable<Vector2> vertices,
      Vector2 referencePoint,
      out Vector2 minVertex,
      out Vector2 maxVertex)
    {
      minVertex = Vector2.zero;
      maxVertex = Vector2.zero;

      float referenceAngle = this.PseudoAngle(referencePoint - this.origin);
      float minDiff = 0.0f;
      float maxDiff = 0.0f;
      float maxAngle = 0.0f;
      float minAngle = 0.0f;

      float vertexAngle = 0.0f;
      float diff = 0.0f;

      foreach (Vector2 vertex in vertices)
      {
        vertexAngle = this.PseudoAngle(vertex - this.origin);
        diff = this.PseudoDistance(vertexAngle, referenceAngle);
        if (diff < minDiff)
        {
          minDiff = diff;
          minAngle = vertexAngle;
          minVertex = vertex;
        }
        if (diff > maxDiff)
        {
          maxDiff = diff;
          maxAngle = vertexAngle;
          maxVertex = vertex;
        }
      }
    }

    /// <summary>
    /// http://stackoverflow.com/questions/16542042/
    /// Given a vector from circle origin to a point (length doesn't matter),
    /// returns a value in [0, 4) representing the angle of that vector relative
    /// to the positive X axis. This value is monotonic with true angle.
    /// </summary>
    private float PseudoAngle(Vector2 originToPoint)
    {
      float denominator =
        Mathf.Abs(originToPoint.x) + Mathf.Abs(originToPoint.y);
      if (denominator == 0.0f)
        return 0.0f;
      if (originToPoint.y < 0.0f)
        return 3.0f + (originToPoint.x / denominator);
      return 1.0f - (originToPoint.x / denominator);
    }

    /// <summary>
    /// Compares two pseudoangles to each other using a third reference
    /// </summary>
    private int CompareToMin(
      float pseudoA,
      float pseudoB,
      float pseudoMin)
    {
      float angleDiff =
        ShortestPseudoDistance(pseudoA, pseudoMin) -
        ShortestPseudoDistance(pseudoB, pseudoMin);

      if (angleDiff > 0.0f)
        return 1;
      else if (angleDiff < 0.0f)
        return -1;
      return 0;
    }

    /// <summary>
    /// Returns the shortest angle difference between two pseudoangles
    /// (i.e. going CW or CCW)
    /// </summary>
    private float ShortestPseudoDistance(float pseudoA, float pseudoB)
    {
      return 2.0f - Mathf.Abs(Mathf.Abs(pseudoA - pseudoB) - 2.0f);
    }

    /// <summary>
    /// Returns the distance between the two angles in the range [-2, 2]
    /// </summary>
    private float PseudoDistance(float pseudoA, float pseudoB)
    {
      float diff = pseudoA - pseudoB;
      if (diff < -2.0f)
        diff += 4.0f;
      if (diff > 2.0f)
        diff -= 4.0f;
      return diff;
    }

    /// <summary>
    /// Returns true iff the given angle falls between the min and max angles
    /// </summary>
    private bool IsInRange(float minAngle, float maxAngle, float angle)
    {
      if (maxAngle > minAngle)
        return ((minAngle < angle) && (angle < maxAngle));
      return ((minAngle < angle) || (angle < maxAngle));
    }
    #endregion
  }
}
