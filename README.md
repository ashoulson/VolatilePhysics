**VolatilePhysics: A 2D Physics Library for Networked Games**

Alexander Shoulson, Ph.D. - http://ashoulson.com

![Example 1](https://raw.githubusercontent.com/ashoulson/VolatilePhysics/master/Images/example1.gif) &nbsp; ![Example 2](https://raw.githubusercontent.com/ashoulson/VolatilePhysics/master/Images/example2.gif) &nbsp; ![Example 3](https://raw.githubusercontent.com/ashoulson/VolatilePhysics/master/Images/example3.gif)

---

Based primarily on glaze-csharp: https://code.google.com/p/glaze-csharp/

With influences from: 
- Box2D: http://box2d.org/
- Chipmunk: https://chipmunk-physics.net/
- ImpulseEngine: https://github.com/RandyGaul/ImpulseEngine

---

Supported Physics Tasks:
- Convex polygon and circle shapes
- Multiple shapes per body with arbitrary center of mass
- Raycasts and circle sweep tests
- Point and circle queries
- Historical ray/circle tests on past world state
- Discrete collision detection and resolution bodies
- Force/torque application and integration on dynamic bodies

Wishlist:
- Continuous collision detection
- Broadphase spatial decomposition (previous attempts have not improved performance)

Not Supported:
- Joints and constraints
- Determinism (Volatile uses floating-point values, and is not deterministic across hardware configurations)

Primary Design Features of Volatile:
- **Safe Repositioning.** Bodies and individual shapes can be moved to arbitrary positions by an external process without compromising the integrity of the physics simulation. This occurs frequently when clients correct for prediction errors and must move a player controller to match the authoritative server's position. Volatile is largely stateless -- very little trajectory data is preserved between frames aside from the position, orientation, and angular/linear velocity of each body.
- **Individual Object Ticking.** Objects can be ticked individually without forcing a tick on the entire physics world. This is useful for client-side prediction in networked games. Ticking objects individually is more efficient, but may cause duplicate collisions if more than one object is handled this way.
- **History Tracking.** Volatile can store historical state for dynamic objects and perform tests on an object's past world position. This is useful for lag compensation with raycast weapons in networked shooters.
- **Simplicity.** Volatile is designed to be simple to read and debug. This library offers as minimal a feature set as possible to keep the total source small and readable.

Caveats:
- Volatile is designed primarily for *dynamic-static* object collisions (like a character colliding with world geometry). *Dynamic-dynamic* object collisions are supported, but resolving them is difficult to synchronize over any network simulation if each body has a different client owner. Volatile provides no explicit support for synchronizing dynamic object collisions over a network.
- Volatile has low frame coherence compared to most physics engines due to potential network rollbacks and corrections. Because of this, efficient spatial decomposition techniques are non-trivial, and the engine would benefit less from caching techniques like warm starting. In complex systems, Volatile's convergence will be outperformed by more sophisticated physics solvers, but Volatile should still be sufficient for many game applications.
- Volatile currently does not use broadphase spatial decomposition (like an octree or dynamic tree). Several implementation attempts exist in the commit history, but these data structures failed to significantly improve performance in evaluation. Spatial decomposition is notably more complicated in Volatile than in other engines due to Volatile's built-in support for storing and querying historical world states.

---

By default, Volatile builds against the official UnityEngine.dll and uses Unity data structures (like Vector2). Volatile includes a separate "FakeUnity" project that emulates Unity's functionality to the extent Volatile needs. You can add this project as a reference and remove the UnityEngine.dll reference to build Volatile as a standalone library. It is safe to delete FakeUnity if you are not interested in using Volatile outside of Unity.