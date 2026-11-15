using System;
using UnityEngine;

namespace TopDownKit
{
    [Serializable]
    public struct CollectSettings
    {
        [Tooltip("Magnet pickups closer than this (tiles) fly towards the collector, unless their item sets its own. 0 = no magnet.")]
        [Min(0f)] public float magnetReach;
        [Tooltip("How fast they fly, tiles/s (unless their item sets its own).")]
        [Min(0f)] public float magnetSpeed;
        [Tooltip("Walk and magnet pickups closer than this (tiles) are collected.")]
        [Min(0f)] public float collectReach;

        public CollectSettings(float magnetReach, float magnetSpeed, float collectReach)
        {
            this.magnetReach = magnetReach;
            this.magnetSpeed = magnetSpeed;
            this.collectReach = collectReach;
        }
    }

    // Picks up walk and magnet pickups (coins, ammo, health) on the fixed step: pulls magnet
    // pickups in their magnet reach (the item's, else these settings) towards itself and
    // collects any in collect reach. Only pickups that would give it something are pulled,
    // so a full bag leaves coins lying where they are.
    public class Collector : MonoBehaviour
    {
        public CollectSettings settings = new CollectSettings(2.5f, 10f, 0.6f);
        [Tooltip("Where pickups fly to, from this object's pivot (tiles).")]
        public Vector2 centre = Vector2.zero;

        public int Collected { get; private set; }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 me = (Vector2)transform.position + centre;
            // Backwards: collecting destroys (at the end of the frame, but stay safe).
            for (int i = Pickup.All.Count - 1; i >= 0; i--)
            {
                Pickup p = Pickup.All[i];
                if (p == null) continue;
                PickupMode mode = p.Mode;
                if (mode == PickupMode.Interact) continue;
                float magnet = mode == PickupMode.Magnet ? (p.MagnetReach > 0f ? p.MagnetReach : settings.magnetReach) : 0f;
                float speed = p.MagnetSpeed > 0f ? p.MagnetSpeed : settings.magnetSpeed;
                float reach = Mathf.Max(settings.collectReach, magnet);
                Vector2 at = p.transform.position;
                float d = (at - me).magnitude;
                if (!p.CollectorMay(gameObject, d, reach) || d > reach || !p.Wants(gameObject)) continue;
                if (d > settings.collectReach)
                {
                    at = Vector2.MoveTowards(at, me, speed * dt);
                    p.transform.position = at;
                    d = (at - me).magnitude;
                }
                if (d <= settings.collectReach && p.Collect(gameObject)) Collected++;
            }
        }
    }
}
