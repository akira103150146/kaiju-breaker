# Unity 6.3 — Physics Module Reference

**Last verified:** 2026-07-01
**Knowledge Gap:** Unity 6 physics improvements, solver changes

---

## Overview

Unity 6.3 uses **PhysX 5.1** (improved from PhysX 4.x in 2022 LTS):
- Better solver stability
- Improved performance
- Enhanced collision detection

> ⚠️ **This project uses 2D physics, not 3D PhysX.** The PhysX / `Rigidbody` /
> `CharacterController` material in this file is reference only. See **"2D Physics" below**
> for the path this project actually uses (`Rigidbody2D` / `Collider2D` + trigger overlap).

---

## 2D Physics (used by this project)

This is a 2D bullet-hell. Movement and collision use Unity's **2D physics (Box2D)** —
`Rigidbody2D`, `Collider2D`, and `Physics2D` queries — **not** the 3D `Physics`/`Rigidbody`
API documented further down.

### Bullets — kinematic move + trigger overlap (no rigidbody simulation)
Per `technical-preferences.md`, bullets and most projectiles use **custom kinematic movement
+ trigger overlap**, not simulated rigidbodies:

```csharp
// Bullet: move by hand each frame, detect hits via a 2D trigger
void Update() {
    transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
}

void OnTriggerEnter2D(Collider2D other) {        // bullet collider has Is Trigger = true
    if (other.TryGetComponent<BossPart>(out var part)) part.TakeHit(_damage);
}
```

### 2D queries (non-allocating — important for dense patterns)
```csharp
var filter = new ContactFilter2D { useLayerMask = true, layerMask = _enemyMask, useTriggers = true };
var hits = new Collider2D[16];
int n = Physics2D.OverlapCircle(center, radius, filter, hits);   // results-buffer overload, no GC

var rayHits = new RaycastHit2D[8];
int m = Physics2D.Raycast(origin, direction, filter, rayHits, maxDistance);
```

### `velocity` renamed to `linearVelocity` (Unity 6.0)
`Rigidbody2D.velocity` (and 3D `Rigidbody.velocity`) are **deprecated** in Unity 6 — use
`linearVelocity`. `angularVelocity` is unchanged.

```csharp
// ❌ Deprecated in Unity 6
rb2d.velocity = new Vector2(x, 0);
// ✅ Unity 6+
rb2d.linearVelocity = new Vector2(x, 0);
```

### Box2D v3 — `LowLevelPhysics2D` (6.3, opt-in)
Unity 6.3 adds a **new low-level 2D physics API backed by Box2D v3** (`LowLevelPhysics2D`).
It is **separate and opt-in** — the classic `Rigidbody2D`/`Collider2D` component workflow is
unchanged and remains the default. Do **not** assume bullet code must migrate to it; reach for
it only if profiling shows the component API is the bottleneck.

---

## Key Changes from 2022 LTS

### Default Solver Iterations Increased
Unity 6 increased default solver iterations for better stability:

```csharp
// Default changed from 6 to 8 iterations
Physics.defaultSolverIterations = 8; // Check if relying on old behavior
```

### Enhanced Collision Detection

```csharp
// ✅ Unity 6: Improved Continuous Collision Detection (CCD)
rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
// Better handling of fast-moving objects
```

---

## Core Physics Components

### Rigidbody

```csharp
// ✅ Best practice: Use AddForce, not direct velocity writes
Rigidbody rb = GetComponent<Rigidbody>();
rb.AddForce(Vector3.forward * 10f, ForceMode.Impulse);

// ❌ Avoid: Direct velocity assignment (can cause instability)
// NOTE: `Rigidbody.velocity` is deprecated in Unity 6 → use `rb.linearVelocity`
rb.linearVelocity = new Vector3(0, 10, 0); // Only use when necessary
```

### Colliders

```csharp
// Primitive colliders: Box, Sphere, Capsule (cheapest)
// Mesh colliders: Expensive, use only for static geometry

// ✅ Compound colliders (multiple primitives) > single mesh collider
```

---

## Raycasting

### Efficient Raycasting (Avoid Allocations)

```csharp
// ✅ Non-allocating raycast
if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance)) {
    Debug.Log($"Hit: {hit.collider.name}");
}

// ✅ Multiple hits (non-allocating)
RaycastHit[] results = new RaycastHit[10];
int hitCount = Physics.RaycastNonAlloc(origin, direction, results, maxDistance);
for (int i = 0; i < hitCount; i++) {
    Debug.Log($"Hit {i}: {results[i].collider.name}");
}

// ❌ Avoid: RaycastAll (allocates array every call)
RaycastHit[] hits = Physics.RaycastAll(origin, direction); // GC allocation!
```

### LayerMask for Selective Raycasting

```csharp
// ✅ Use LayerMask to filter collisions
int layerMask = 1 << LayerMask.NameToLayer("Enemy");
Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask);
```

---

## Physics Queries

### OverlapSphere (Check for nearby objects)

```csharp
// ✅ Non-allocating version
Collider[] results = new Collider[10];
int count = Physics.OverlapSphereNonAlloc(center, radius, results);
for (int i = 0; i < count; i++) {
    // Process results[i]
}
```

### SphereCast (Thick raycast)

```csharp
// Useful for character controllers
if (Physics.SphereCast(origin, radius, direction, out RaycastHit hit, maxDistance)) {
    // Hit something with a sphere-shaped ray
}
```

---

## Collision Events

### OnCollisionEnter / Stay / Exit

```csharp
void OnCollisionEnter(Collision collision) {
    // Triggered when collision starts
    Debug.Log($"Collided with {collision.gameObject.name}");

    // Access contact points
    foreach (ContactPoint contact in collision.contacts) {
        Debug.DrawRay(contact.point, contact.normal, Color.red, 2f);
    }
}
```

### OnTriggerEnter / Stay / Exit

```csharp
void OnTriggerEnter(Collider other) {
    // Trigger collider (Is Trigger = true)
    if (other.CompareTag("Pickup")) {
        Destroy(other.gameObject);
    }
}
```

---

## Character Controllers

### CharacterController Component

```csharp
CharacterController controller = GetComponent<CharacterController>();

// ✅ Move with collision detection
Vector3 move = transform.forward * speed * Time.deltaTime;
controller.Move(move);

// Apply gravity manually
if (!controller.isGrounded) {
    velocity.y += Physics.gravity.y * Time.deltaTime;
}
controller.Move(velocity * Time.deltaTime);
```

---

## Physics Materials

### Friction & Bounciness

```csharp
// Create: Assets > Create > Physic Material
// Assign to collider: Collider > Material

// PhysicMaterial settings:
// - Dynamic Friction: 0.6 (sliding friction)
// - Static Friction: 0.6 (starting friction)
// - Bounciness: 0.0 - 1.0
// - Friction Combine: Average, Minimum, Maximum, Multiply
// - Bounce Combine: Average, Minimum, Maximum, Multiply
```

---

## Joints

### Fixed Joint (Attach two rigidbodies)

```csharp
FixedJoint joint = gameObject.AddComponent<FixedJoint>();
joint.connectedBody = otherRigidbody;
```

### Hinge Joint (Door, wheel)

```csharp
HingeJoint hinge = gameObject.AddComponent<HingeJoint>();
hinge.axis = Vector3.up; // Rotation axis
hinge.useLimits = true;
hinge.limits = new JointLimits { min = -90, max = 90 };
```

---

## Performance Optimization

### Physics Layer Collision Matrix
`Edit > Project Settings > Physics > Layer Collision Matrix`
- Disable unnecessary collision checks between layers
- Massive performance gain

### Fixed Timestep
`Edit > Project Settings > Time > Fixed Timestep`
- Default: 0.02 (50 FPS physics)
- Lower = more accurate, higher CPU cost
- Match game's target framerate if possible

### Simplified Collision Geometry
- Use primitive colliders (box, sphere, capsule) over mesh colliders
- Bake mesh colliders at build time, not runtime

---

## Common Patterns

### Ground Check (Character Controller)

```csharp
bool IsGrounded() {
    float rayLength = 0.1f;
    return Physics.Raycast(transform.position, Vector3.down, rayLength);
}
```

### Apply Explosion Force

```csharp
void ApplyExplosion(Vector3 explosionPos, float radius, float force) {
    Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);
    foreach (Collider hit in colliders) {
        Rigidbody rb = hit.GetComponent<Rigidbody>();
        if (rb != null) {
            rb.AddExplosionForce(force, explosionPos, radius);
        }
    }
}
```

---

## Debugging

### Physics Debugger (Unity 6+)
- `Window > Analysis > Physics Debugger`
- Visualize colliders, contacts, queries

### Gizmos

```csharp
void OnDrawGizmos() {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, detectionRadius);
}
```

---

## Sources
- https://docs.unity3d.com/6000.3/Documentation/Manual/Physics2DReference.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics2D.html
- https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html (Box2D v3 / LowLevelPhysics2D)
- https://docs.unity3d.com/6000.0/Documentation/Manual/PhysicsOverview.html
- https://docs.unity3d.com/ScriptReference/Physics.html
