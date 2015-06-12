**VolatilePhysics: A 2D Physics Library for Networked Games**

---

Based primarily on glaze-csharp: https://code.google.com/p/glaze-csharp/

With influences from: 
- Box2D: http://box2d.org/
- Chipmunk: https://chipmunk-physics.net/
- ImpulseEngine: https://github.com/RandyGaul/ImpulseEngine

---

Supported Physics Tasks:
- Polygon and Circle Shapes
- Multiple Shapes per Body
- Collision Detection and Resolution
- Force/Torque Application and Integration

To be Supported:
- Raycasts
- History Buffer with BroadPhase Support (blitting quadtree buffer)

Not Supported:
- Joints and Constraints
- Continuous Collision Detection

Overarching Design Goals of Volatile:
- **Safe Repositioning.** Bodies should be able to be moved to arbitrary positions by an external process in a safe way with as much of a deterministic response as possible. Volatile is largely stateless -- very little is preserved between frames aside from the position and orientation of each body. *(Status: Mostly complete.)*
- **History Tracking.** Volatile should provide an efficient history buffer with a historical spatial decomposition (quadtree) structure for tasks like historical raycasts. This is useful for lag compensation, with built-in roll-back and roll-forward. *(Status: Implemented separately, to be integrated into Volatile.)*
- **Individual Object Ticking.** Objects should be able to be rolled back and ticked individually without forcing a tick on the entire physics world. *(Status: Not started.)*

Caveats:
- Volatile is designed primarily for *dynamic-static* object collisions. *Dynamic-dynamic* object collisions are difficult to synchronize over any network simulation, and while Volatile may support them, odd behavior may occur.
- Because Volatile is mostly stateless, it can not benefit from physics techniques like warm starting. In complex systems, Volatile's convergence will be outperformed by more advanced physics solvers, but Volatile should still be sufficient for many game applications.