**VolatilePhysics: A 2D Physics Library for Networked Games**

Alexander Shoulson - http://ashoulson.com

![Example 1](https://raw.githubusercontent.com/ashoulson/VolatilePhysics/master/Images/example1.gif) &nbsp; ![Example 2](https://raw.githubusercontent.com/ashoulson/VolatilePhysics/master/Images/example2.gif)

---

Based primarily on glaze-csharp: https://code.google.com/p/glaze-csharp/

With influences from: 
- Box2D: http://box2d.org/
- Chipmunk: https://chipmunk-physics.net/
- ImpulseEngine: https://github.com/RandyGaul/ImpulseEngine

---

Supported Physics Tasks:
- Convex polygon and circle shapes
- Multiple shapes per body
- Raycasts and circle sweep tests
- Point and AABB queries
- Historical ray/circle tests on past world state
- Discrete collision detection and resolution between static and dynamic bodies
- Force/torque application and integration on dynamic bodies

In Progress:
- Broadphase spatial decomposition

Wishlist:
- Continuous collision detection

Not Supported:
- Joints and constraints
- Collision resolution between two dynamic bodies (TBD)

Primary Design Features of Volatile:
- **Safe Repositioning.** Bodies and individual shapes can be moved to arbitrary positions by an external process without compromising the integrity of the physics simulation. Volatile is largely stateless -- very little trajectory data is preserved between frames aside from the position, orientation, and angular/linear velocity of each body.
- **Individual Object Ticking.** Objects can be ticked individually without forcing a tick on the entire physics world. This is useful for client-side prediction in networked games.
- **History Tracking.** Volatile can store historical state for dynamic objects and perform tests on an object's past world position. This is useful for lag compensation with raycast weapons in networked shooters.
- **Simplicity.** Volatile is designed to be simple to read and debug. Making a networked game is hard enough without worrying about how to diagnose physics issues.

Caveats:
- Volatile is designed for *dynamic-static* object collisions. *Dynamic-dynamic* object collisions are difficult to synchronize over any network simulation. Volatile could easily be modified to support them, but doing so would require a broadphase data structure compatible with Volatile's historical rollback system (currently being evaluated).
- Volatile has low frame coherence compared to most physics engines due to potential network rollbacks and corrections. Because of this, efficient spatial decomposition techniques are non-trivial, and the engine would benefit less from caching techniques like warm starting. In complex systems, Volatile's convergence will be outperformed by more sophisticated physics solvers, but Volatile should still be sufficient for many game applications.