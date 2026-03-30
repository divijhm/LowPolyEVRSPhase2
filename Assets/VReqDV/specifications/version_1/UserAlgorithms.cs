using UnityEngine;
using VReqDV;

namespace Version_1
{
    /// <summary>
    /// Space Debris Collector — Version 1
    ///
    /// Navigation model:
    ///   XR Ray Select (via OnXRInteraction) → Spaceship state = flying
    ///   Every frame while flying:
    ///     SteerTowardNearestDebris() called as precondition → steers ship AND returns
    ///     true when within CollectRadius → CollectNearestDebris() runs → ship stays flying
    ///   When no more floating debris: IsAllDebrisCollected() fires → state = returning
    ///   Every frame while returning:
    ///     SteerToHomeBase() called as precondition → steers ship AND returns true on arrival
    ///     → DockAndReset() runs → state = idle, all debris reset to floating
    /// </summary>
    public static class UserAlgorithms
    {
        // ─── Constants ───────────────────────────────────────────────────────────

        private const float CollectRadius = 1.5f;   // Distance to snap debris to ship
        private const float FlySpeed      = 5f;     // Spaceship move speed (units/sec)
        private const float DockThreshold = 0.6f;   // Distance to trigger dock

        private static readonly Vector3 HomePos = new Vector3(-8f, 1f, 0f);

        private static readonly string[] DebrisNames = { "Debris_1", "Debris_2", "Debris_3" };

        private static readonly Vector3[] DebrisOrigins = {
            new Vector3( 2f,  0f,  -3f),
            new Vector3( 5f,  1f,   2f),
            new Vector3( 8f, -0.5f, 0f)
        };

        private static readonly Quaternion[] DebrisRotations = {
            Quaternion.Euler(15f, 30f, 10f),
            Quaternion.Euler(45f, 10f, 25f),
            Quaternion.Euler(20f, 60f,  5f)
        };

        /// <summary>
        /// Starts the Spaceship moving toward the nearest debris field.
        /// Physics note: gravity is off (space), rb is set non-kinematic so velocity applies.
        /// </summary>
        public static void LaunchSpaceship()
        {
            GameObject ship = GameObject.Find("Spaceship");
            if (ship == null) return;

            Rigidbody rb = ship.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity  = false;
                rb.velocity    = Vector3.zero;
                // Give a gentle initial nudge toward the debris field (positive X)
                rb.AddForce(Vector3.right * FlySpeed * 0.5f, ForceMode.VelocityChange);
            }

            StateAccessor.SetState("Spaceship", "Flying", ship, "Version_1");
            Debug.Log("[V4] Spaceship launched.");
        }

        // ─── Continuous Steering — toward nearest debris ──────────────────────────

        /// <summary>
        /// CALLED EVERY FRAME as the SpaceshipFlyToDebris precondition.
        /// Steers the Spaceship toward the nearest floating debris every frame.
        /// Returns true (and triggers CollectNearestDebris action) when within
        /// CollectRadius. Returns false otherwise — keeping the ship in motion.
        /// </summary>
        public static bool SteerTowardNearestDebris(GameObject obj)
        {
            // If all debris collected, stop flying toward debris
            if (CountFloatingDebris() == 0) return false;

            GameObject nearest = FindNearestFloatingDebris(obj.transform.position);
            if (nearest == null) return false;

            // Redirect velocity toward nearest debris every frame
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = (nearest.transform.position - obj.transform.position).normalized;
                rb.velocity = dir * FlySpeed;    // Overwrite velocity → continuous steering
            }

            float dist = Vector3.Distance(obj.transform.position, nearest.transform.position);
            return dist <= CollectRadius;   // Fires collect action when close enough
        }

        // ─── Collect Nearest Debris ───────────────────────────────────────────────

        /// <summary>
        /// Attaches the nearest floating debris to the Spaceship hull.
        /// Ship stays in "flying" state so it immediately steers to the next debris.
        /// </summary>
        public static void CollectNearestDebris(GameObject obj)
        {
            GameObject nearest = FindNearestFloatingDebris(obj.transform.position);
            if (nearest == null)
            {
                Debug.Log("[V4] CollectNearestDebris: no floating debris nearby.");
                return;
            }

            Debug.Log($"[V4] Collecting: {nearest.name}");

            // Freeze debris physics
            Rigidbody debrisRb = nearest.GetComponent<Rigidbody>();
            if (debrisRb != null)
            {
                debrisRb.isKinematic = true;
                debrisRb.velocity    = Vector3.zero;
            }

            // Parent debris to ship — stacks behind the hull visually
            int slot = DebrisNames.Length - CountFloatingDebris(); // 0,1,2
            nearest.transform.SetParent(obj.transform);
            nearest.transform.localPosition = new Vector3(-1.2f - slot * 0.8f, 0f, 0f);
            nearest.transform.localRotation  = Quaternion.identity;

            // Mark this debris collected
            StateAccessor.SetState(nearest.name, "Collected", nearest, "Version_1");

            // Ship stays "flying" — it will immediately start steering toward next debris
            StateAccessor.SetState("Spaceship", "Flying", obj, "Version_1");
            Debug.Log($"[V1] Collected {nearest.name}. Floating remaining: {CountFloatingDebris()}");
        }

        // ─── All-Collected Check ──────────────────────────────────────────────────

        /// <summary>
        /// Returns true when all three Debris objects have been collected.
        /// This precondition on SpaceshipReturn is checked every frame while flying,
        /// and fires before SteerTowardNearestDebris when no debris remain.
        /// </summary>
        public static bool IsAllDebrisCollected()
        {
            bool done = CountFloatingDebris() == 0;
            if (done) Debug.Log("[V4] All debris collected! Returning to HomeBase.");
            return done;
        }

        // ─── Continuous Steering — toward HomeBase ────────────────────────────────

        /// <summary>
        /// CALLED EVERY FRAME as the SpaceshipDock precondition.
        /// Steers the Spaceship back toward HomeBase every frame.
        /// Returns true when within DockThreshold — triggering DockAndReset.
        /// </summary>
        public static bool SteerToHomeBase(GameObject obj)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = (HomePos - obj.transform.position).normalized;
                rb.velocity = dir * FlySpeed;   // Continuous steering toward home
            }

            float dist = Vector3.Distance(obj.transform.position, HomePos);
            return dist <= DockThreshold;   // Near enough → dock
        }

        /// <summary>
        /// Sets initial heading toward HomeBase when transitioning from flying → returning.
        /// SteerToHomeBase() then takes over per-frame.
        /// </summary>
        public static void ReturnToBase(GameObject obj)
        {
            Debug.Log("[V4] Heading home...");

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = (HomePos - obj.transform.position).normalized;
                rb.velocity = dir * FlySpeed;
            }

            StateAccessor.SetState("Spaceship", "Returning", obj, "Version_1");
        }

        // ─── Dock & Reset ─────────────────────────────────────────────────────────

        /// <summary>
        /// Docks the Spaceship, detaches debris, and resets the whole scene.
        /// </summary>
        public static void DockAndReset()
        {
            Debug.Log("[V4] Docked! Resetting scene...");

            // Reset Spaceship
            GameObject ship = GameObject.Find("Spaceship");
            if (ship != null)
            {
                Rigidbody rb = ship.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity        = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic     = true;
                }

                ship.transform.DetachChildren();       // Release held debris
                ship.transform.position = HomePos;
                ship.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

                StateAccessor.SetState("Spaceship", "Idle", ship, "Version_1");
            }

            // Reset Debris
            for (int i = 0; i < DebrisNames.Length; i++)
            {
                GameObject d = GameObject.Find(DebrisNames[i]);
                if (d == null) continue;

                d.transform.SetParent(null);
                d.transform.position = DebrisOrigins[i];
                d.transform.rotation  = DebrisRotations[i];

                Rigidbody rb = d.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic     = false;
                    rb.velocity        = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.useGravity      = false;
                }

                StateAccessor.SetState(DebrisNames[i], "Floating", d, "Version_1");
            }
        }

        // ─── Debris-side behaviors (DebrisGetCollected) ───────────────────────────

        /// <summary>
        /// Debris precondition: returns true when the Spaceship enters CollectRadius.
        /// This runs in parallel with the Spaceship's own steering behavior.
        /// </summary>
        public static bool IsDebrisCaptured(GameObject obj)
        {
            GameObject ship = GameObject.Find("Spaceship");
            if (ship == null) return false;
            return Vector3.Distance(obj.transform.position, ship.transform.position) <= CollectRadius;
        }

        /// <summary>
        /// Debris action: snap this debris to the Spaceship.
        /// Runs on the Debris actor (parallel path to CollectNearestDebris on Ship).
        /// </summary>
        public static void SnapDebrisToShip(GameObject obj)
        {
            GameObject ship = GameObject.Find("Spaceship");
            if (ship == null) return;

            Debug.Log($"[V4] {obj.name} snapped to ship.");

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity    = Vector3.zero;
            }

            int slot = DebrisNames.Length - CountFloatingDebris();
            obj.transform.SetParent(ship.transform);
            obj.transform.localPosition = new Vector3(-1.2f - slot * 0.8f, 0f, 0f);
            obj.transform.localRotation  = Quaternion.identity;

            StateAccessor.SetState(obj.name, "Collected", obj, "Version_1");
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Finds the closest debris that is still unparented (= floating, not yet collected).
        /// </summary>
        private static GameObject FindNearestFloatingDebris(Vector3 from)
        {
            GameObject nearest = null;
            float      minDist = float.MaxValue;

            foreach (string name in DebrisNames)
            {
                GameObject d = GameObject.Find(name);
                if (d == null || d.transform.parent != null) continue; // skip collected

                float dist = Vector3.Distance(from, d.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = d;
                }
            }

            return nearest;
        }

        /// <summary>Returns how many debris objects are still floating (unparented).</summary>
        private static int CountFloatingDebris()
        {
            int count = 0;
            foreach (string name in DebrisNames)
            {
                GameObject d = GameObject.Find(name);
                if (d != null && d.transform.parent == null) count++;
            }
            return count;
        }
    }
}
