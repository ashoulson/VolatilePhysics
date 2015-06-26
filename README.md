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
- Convex Polygon and Circle Shapes
- Multiple Shapes per Body
- Raycasts
- Collision Detection and Resolution
- Force/Torque Application and Integration

Wishlist:
- Broadphase Spatial Decomposition
- Continuous Collision Detection

Not Supported:
- Joints and Constraints

Primary Design Features of Volatile:
- **Safe Repositioning.** Bodies and individual shapes can be moved to arbitrary positions by an external process without compromising the integrity of the physics simulation. Volatile is largely stateless -- very little data is preserved between frames aside from the position, orientation, and angular/linear velocity of each body.
- **Individual Object Ticking.** Objects can be ticked individually without forcing a tick on the entire physics world. This is useful for client-side prediction in networked games.
- **Simplicity.** Volatile is designed to be simple to read and debug. Making a networked game is hard enough without worrying about how to diagnose physics issues.

Caveats:
- Volatile is designed primarily for *dynamic-static* object collisions. *Dynamic-dynamic* object collisions are difficult to synchronize over any network simulation, and while Volatile inherently supports them, no explicit support is given for synchronizing these collisions between peers. Individual object ticking does not support dynamic-dynamic collisions (two dynamic objects will simply pass through one another).
- Volatile has low frame coherence compared to most physics engines due to potential network rollbacks and corrections. Because of this, efficient spatial decomposition techniques are non-trivial, and the engine would benefit less from caching techniques like warm starting. In complex systems, Volatile's convergence will be outperformed by more sophisticated physics solvers, but Volatile should still be sufficient for many game applications.