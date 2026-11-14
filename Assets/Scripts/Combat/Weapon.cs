using UnityEngine;

namespace TopDownKit
{
    // One weapon's settings, an asset so several shooters can share it (pistol, shotgun,
    // rapid). Numbers in tiles and seconds (Decision 3). The roadmap names it a weapon
    // asset; like MoveFeel it holds settings only, no behaviour.
    [CreateAssetMenu(menuName = "TopDownKit/Weapon")]
    public class Weapon : ScriptableObject
    {
        [Tooltip("Shots per second while the trigger allows.")]
        [Min(0.1f)] public float fireRate = 4f;
        [Tooltip("On: holding the trigger keeps firing. Off: one shot per press.")]
        public bool automatic;
        [Tooltip("Bullets per shot (a shotgun fires several).")]
        [Min(1)] public int bulletsPerShot = 1;
        [Tooltip("Degrees, the whole cone. Several bullets fan evenly across it; one bullet goes at a random angle inside it.")]
        [Range(0f, 180f)] public float spread;
        [Tooltip("Tiles per second.")]
        [Min(0.1f)] public float bulletSpeed = 16f;
        [Tooltip("Tiles a bullet flies before it vanishes.")]
        [Min(0.1f)] public float range = 12f;
        [Min(0)] public int damage = 1;
        [Tooltip("Push on whatever a bullet hits, tiles/s.")]
        [Min(0f)] public float knockback = 6f;
        [Tooltip("Push on the shooter, backwards, per shot, tiles/s (recoil).")]
        [Min(0f)] public float recoil;
        [Tooltip("Shots per magazine. 0 = infinite, no reloading.")]
        [Min(0)] public int magazine;
        [Tooltip("Seconds to reload a magazine.")]
        [Min(0f)] public float reloadTime = 1f;
        [Tooltip("Bullet collision radius, tiles.")]
        [Min(0.01f)] public float bulletRadius = 0.15f;
        [Tooltip("Screen shake per shot, tiles (0 = none).")]
        [Min(0f)] public float shakeOnShot;
        public Sprite bulletSprite;
        public Color bulletColor = Color.white;
    }
}
