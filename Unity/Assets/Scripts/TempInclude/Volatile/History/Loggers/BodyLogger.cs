///*
// *  VolatilePhysics - A 2D Physics Library for Networked Games
// *  Copyright (c) 2015-2016 - Alexander Shoulson - http://ashoulson.com
// *
// *  This software is provided 'as-is', without any express or implied
// *  warranty. In no event will the authors be held liable for any damages
// *  arising from the use of this software.
// *  Permission is granted to anyone to use this software for any purpose,
// *  including commercial applications, and to alter it and redistribute it
// *  freely, subject to the following restrictions:
// *  
// *  1. The origin of this software must not be misrepresented; you must not
// *     claim that you wrote the original software. If you use this software
// *     in a product, an acknowledgment in the product documentation would be
// *     appreciated but is not required.
// *  2. Altered source versions must be plainly marked as such, and must not be
// *     misrepresented as being the original software.
// *  3. This notice may not be removed or altered from any source distribution.
//*/

//using System;
//using System.Collections.Generic;

//using UnityEngine;

//namespace Volatile.History
//{
//  internal class BodyLogger
//  {
//    private struct BodyRecord
//    {
//      public int Frame { get { return this.frame; } }
//      public AABB AABB { get { return this.aabb; } }
//      public Vector2 Position { get { return this.position; } }
//      public float Angle { get { return this.radians; } }

//      public bool IsValid { get { return this.frame >= 0; } }

//      private int frame;
//      private AABB aabb;
//      private Vector2 position;
//      private float radians;

//      public void Set(int frame, Body body)
//      {
//        this.frame = frame;
//        this.aabb = body.AABB;
//        this.position = body.Position;
//        this.radians = body.Angle;
//      }

//      public void Invalidate()
//      {
//        this.frame = -1;
//      }

//      internal bool QueryAABB(AABB aabb)
//      {
//        return this.aabb.Intersect(aabb);
//      }

//      internal bool QueryAABB(Vector2 point)
//      {
//        return this.aabb.Query(point);
//      }

//      internal bool QueryAABB(Vector2 point, float radius)
//      {
//        return this.aabb.Query(point, radius);
//      }

//      public bool RayCastAABB(ref RayCast ray)
//      {
//        return this.aabb.RayCast(ref ray);
//      }

//      public bool CircleCastAABB(ref RayCast ray, float radius)
//      {
//        return this.aabb.CircleCast(ref ray, radius);
//      }
//    }

//    internal int RepresentedFrame { get { return this.representedFrame; } }

//    private Body body;
//    private BodyRecord[] records;
//    private Stack<BodyRecord> rollbackStack;
//    private int representedFrame;

//    public BodyLogger(Body body, int capacity)
//    {
//      this.body = body;
//      this.rollbackStack = new Stack<BodyRecord>();
//      this.representedFrame = History.CURRENT_FRAME;

//      this.records = new BodyRecord[capacity];
//      for (int i = 0; i < capacity; i++)
//        this.records[i].Invalidate();

//      // Initialize the shape loggers
//      IList<Shape> bodyShapes = this.body.shapes;
//      for (int i = 0; i < bodyShapes.Count; i++)
//        bodyShapes[i].shapeLogger =
//          new ShapeLogger(bodyShapes[i], capacity);
//    }

//    public void Store(int frame)
//    {
//      this.records[this.FrameToIndex(frame)].Set(frame, this.body);
//      IList<Shape> bodyShapes = this.body.shapes;
//      for (int i = 0; i < bodyShapes.Count; i++)
//        bodyShapes[i].shapeLogger.Store(frame);
//    }

//    public void Rollback(int frame)
//    {
//      BodyRecord record = this.records[this.FrameToIndex(frame)];
//      if (record.Frame == frame)
//      {
//        // Store the current state
//        BodyRecord current = new BodyRecord();
//        current.Set(this.representedFrame, this.body);
//        this.rollbackStack.Push(current);

//        // Restore the previous state
//        this.body.SetWorld(record.Position, record.Angle);
//        this.representedFrame = record.Frame;
//      }
//    }

//    public void Restore()
//    {
//      if (this.rollbackStack.Count > 0)
//      {
//        BodyRecord priorState = this.rollbackStack.Pop();
//        this.body.SetWorld(priorState.Position, priorState.Angle);
//        this.representedFrame = priorState.Frame;
//      }
//    }

//    public void ClearRestorePoints()
//    {
//      this.rollbackStack.Clear();
//    }

//    #region Tests
//    /// <summary>
//    /// Checks if a body's AABB overlaps with a given AABB.
//    /// </summary>
//    internal bool Query(
//      int frame,
//      AABB area)
//    {
//      BodyRecord record = this.records[this.FrameToIndex(frame)];
//      if (record.Frame == frame)
//        return record.QueryAABB(area);
//      return this.body.Query(area);
//    }

//    /// <summary>
//    /// Checks if a point is contained within this body
//    /// Begins with AABB checks.
//    /// </summary>
//    internal bool Query(
//      int frame,
//      Vector2 point,
//      Func<Shape, bool> filter = null)
//    {
//      BodyRecord record = this.records[this.FrameToIndex(frame)];
//      if ((record.Frame == frame) && (record.QueryAABB(point) == true))
//      {
//        for (int i = 0; i < this.body.shapes.Count; i++)
//          if (this.body.shapes[i].shapeLogger.Query(frame, point))
//            return true;
//        return false;
//      }

//      return this.body.Query(point);
//    }

//    /// <summary>
//    /// Checks if a circle overlaps with this body. 
//    /// Begins with AABB checks.
//    /// </summary>
//    internal bool Query(
//      int frame,
//      Vector2 point,
//      float radius)
//    {
//      BodyRecord record = this.records[this.FrameToIndex(frame)];
//      if ((record.Frame == frame) && (record.QueryAABB(point, radius) == true))
//      {
//        for (int i = 0; i < this.body.shapes.Count; i++)
//          if (this.body.shapes[i].shapeLogger.Query(frame, point, radius))
//            return true;
//        return false;
//      }

//      return this.body.Query(point);
//    }

//    internal bool RayCast(
//      int frame,
//      ref RayCast ray,
//      ref RayResult result)
//    {
//      BodyRecord record = this.records[this.FrameToIndex(frame)];

//      if (record.Frame == frame)
//      {
//        if (record.RayCastAABB(ref ray) == true)
//        {
//          for (int i = 0; i < this.body.shapes.Count; i++)
//          {
//            this.body.shapes[i].shapeLogger.RayCast(
//              frame,
//              ref ray,
//              ref result);

//            if (result.IsContained == true)
//              return true;
//          }
//        }
//        return result.IsValid;
//      }

//      return this.body.RayCast(ref ray, ref result);
//    }

//    internal bool CircleCast(
//      int frame,
//      ref RayCast ray,
//      float radius,
//      ref RayResult result,
//      Func<Shape, bool> filter = null)
//    {
//      BodyRecord record = this.records[this.FrameToIndex(frame)];

//      if (record.Frame == frame)
//      {
//        if (record.CircleCastAABB(ref ray, radius) == true)
//        {
//          for (int i = 0; i < this.body.shapes.Count; i++)
//          {
//            this.body.shapes[i].shapeLogger.CircleCast(
//              frame,
//              ref ray,
//              radius,
//              ref result);

//            if (result.IsContained == true)
//              return true;
//          }
//        }
//        return result.IsValid;
//      }

//      // If the record is invalid, fall back to a current-time circle cast
//      return this.body.CircleCast(ref ray, radius, ref result);
//    }
//    #endregion

//    #region Internals
//    private int FrameToIndex(int frame)
//    {
//      return frame % this.records.Length;
//    }

//    private IEnumerable<Shape> GetShapes(Func<Shape, bool> filter = null)
//    {
//      IList<Shape> bodyShapes = this.body.shapes;
//      for (int i = 0; i < bodyShapes.Count; i++)
//        if (filter == null || filter(bodyShapes[i]) == true)
//          yield return bodyShapes[i];
//    }
//    #endregion

//    #region Debug
//    public void GizmoDraw(
//      Color aabbColorBody,
//      Color aabbColorShape)
//    {
//      for (int i = 0; i < this.records.Length; i++)
//        if (this.records[i].IsValid == true)
//          this.records[i].AABB.GizmoDraw(aabbColorBody);
//      foreach (Shape shape in this.body.shapes)
//        if (shape.shapeLogger != null)
//          shape.shapeLogger.GizmoDraw(aabbColorShape);
//    }
//    #endregion
//  }
//}