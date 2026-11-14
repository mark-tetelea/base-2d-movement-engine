using System;
using UnityEngine;

namespace TopDownKit
{
    [Serializable]
    public struct CollectSettings
    {
        [Tooltip("Auto pickups closer than this (tiles) fly towards the collector. 0 = no magnet.")]
        [Min(0f)] public float magnetReach;
        [Tooltip("How fast they fly, tiles/s.")]
        [Min(0f)] public float magnetSpeed;
        [Tooltip("Auto pickups closer than this (tiles) are collected.")]
        [Min(0f)] public float collectReach;

        public CollectSettings(float magnetReach, float magnetSpeed, float collectReach)
        {
            this.magnetReach = magnetReach;
            this.magnetSpeed = magnetSpeed;
            this.collectReach = collectReach;
        }
    }

    // Picks up auto pickups (coins, ammo, health) on the fixed step: pulls those in magnet
    // reach towards itself and collects those in collect reach. Only pickups that would
    // give it something are pulled, so a full bag leaves coins lying where they are.
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
            float collect2 = settings.collectReach * settings.collectReach;
            float magnet2 = settings.magnetReach * settings.magnetReach;
            // Backwards: collecting destroys (at the end of the frame, but stay safe).
            for (int i = Pickup.All.Count - 1; i >= 0; i--)
            {
                Pickup p = Pickup.All[i];
                if (p == null || !p.auto || !p.Ready) continue;
                Vector2 at = p.transform.position;
                float d2 = (at - me).sqrMagnitude;
                if (d2 > Mathf.Max(collect2, magnet2) || !p.Wants(gameObject)) continue;
                if (d2 > collect2)
                {
                    at = Vector2.MoveTowards(at, me, settings.magnetSpeed * dt);
                    p.transform.position = at;
                    d2 = (at - me).sqrMagnitude;
                }
                if (d2 <= collect2 && p.Collect(gameObject)) Collected++;
            }
        }
    }
}
