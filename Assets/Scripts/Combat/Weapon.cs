using UnityEngine;

namespace TopDownKit
{
    public enum WeaponKind { Gun, Melee }

    // Melee settings (kind Melee). The swing sweeps the arc along the aim direction; damage
    // and knockback are the weapon's own, so a melee hit goes through the same Health.Hit.
    [System.Serializable]
    public struct MeleeSettings
    {
        [Tooltip("Degrees, the whole arc swept, centred on the aim.")]
        [Range(1f, 360f)] public float arc;
        [Tooltip("Tiles from the wielder's centre the blade reaches.")]
        [Min(0.1f)] public float reach;
        [Tooltip("Seconds the swing takes to sweep the arc (0 = the whole arc at once).")]
        [Min(0f)] public float swingTime;
        [Tooltip("Seconds after a swing ends before the next can start.")]
        [Min(0f)] public float cooldown;
        [Tooltip("The blade's art (stretched to the reach). Empty = a plain bar.")]
        public Sprite sprite;
        [Tooltip("Blade width, tiles (looks only).")]
        [Min(0.01f)] public float width;
        public Color color;
    }

    // One weapon's settings, an asset so several shooters can share it (pistol, shotgun,
    // rapid, sword). Numbers in tiles and seconds (Decision 3). The roadmap names it a
    // weapon asset; like MoveFeel it holds settings only, no behaviour.
    [CreateAssetMenu(menuName = "TopDownKit/Weapon")]
    public class Weapon : ScriptableObject
    {
        [Tooltip("Gun fires bullets; Melee swings (the melee settings below). Both use damage and knockback.")]
        public WeaponKind kind = WeaponKind.Gun;
        [Tooltip("Gun: shots per second while the trigger allows. Melee ignores it (swing time + cooldown).")]
        [Min(0.1f)] public float fireRate = 4f;
        [Tooltip("On: holding the trigger keeps firing. Off: one shot per press. Ignored while charging.")]
        public bool automatic;
        [Tooltip("Bullets per shot (a shotgun fires several).")]
        [Min(1)] public int bulletsPerShot = 1;
        [Tooltip("Degrees, the whole cone. Several bullets fan evenly across it; one bullet goes at a random angle inside it.")]
        [Range(0f, 180f)] public float spread;
        [Tooltip("Tiles per second.")]
        [Min(0.1f)] public float bulletSpeed = 16f;
        [Tooltip("Tiles a bullet flies before it vanishes (bounces included).")]
        [Min(0.1f)] public float range = 12f;
        [Min(0)] public int damage = 1;
        [Tooltip("Push on whatever a bullet or swing hits, tiles/s.")]
        [Min(0f)] public float knockback = 6f;
        [Tooltip("Push on the shooter, backwards, per shot, tiles/s (recoil).")]
        [Min(0f)] public float recoil;
        [Tooltip("Shots per magazine. 0 = infinite, no reloading.")]
        [Min(0)] public int magazine;
        [Tooltip("Seconds to reload a magazine.")]
        [Min(0f)] public float reloadTime = 1f;
        [Tooltip("Bullet collision radius, tiles.")]
        [Min(0.01f)] public float bulletRadius = 0.15f;
        [Tooltip("Times a bullet bounces off walls before it stops (0 = none). Low barriers are still flown over.")]
        [Min(0)] public int bounces;
        [Tooltip("Screen shake per shot, tiles (0 = none).")]
        [Min(0f)] public float shakeOnShot;
        public Sprite bulletSprite;
        public Color bulletColor = Color.white;

        [Header("Charge (hold to charge, release to fire)")]
        [Tooltip("Seconds to full charge. 0 = off: the trigger fires as usual.")]
        [Min(0f)] public float chargeTime;
        [Tooltip("Damage x this at full charge (1 = no change). Rounded to whole points.")]
        [Min(0f)] public float chargeDamage = 1f;
        [Tooltip("Bullet radius and art x this at full charge (melee: reach). 1 = no change.")]
        [Min(0.01f)] public float chargeSize = 1f;
        [Tooltip("Bullet speed x this at full charge. 1 = no change.")]
        [Min(0.01f)] public float chargeSpeed = 1f;

        [Header("Melee (kind Melee)")]
        public MeleeSettings melee = new MeleeSettings
        {
            arc = 120f, reach = 1.5f, swingTime = 0.15f, cooldown = 0.25f, width = 0.15f, color = Color.white
        };
    }
}
