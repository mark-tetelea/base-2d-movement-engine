using System;
using UnityEngine;

namespace TopDownKit
{
    // The names of the animations the kit plays by itself.
    public static class Anims
    {
        public const string Idle = "Idle", Walk = "Walk", Dash = "Dash", Attack = "Attack", Hurt = "Hurt", Death = "Death";
    }

    // One facing's frames of one animation.
    [Serializable]
    public class SpriteFrames
    {
        public Sprite[] frames = new Sprite[0];
        public bool Empty => frames == null || frames.Length == 0;
    }

    // One animation: a name (an action), its frame rate, whether it loops, and its frames
    // for each facing - any number of frames, any rate.
    [Serializable]
    public class SpriteAnim
    {
        [Tooltip("The action it plays for. The kit plays Idle, Walk, Dash, Attack, Hurt and Death; a game may add its own and call DirectionalSprites.Play(name).")]
        public string name = "Idle";
        [Tooltip("Frames per second.")]
        [Min(0.1f)] public float fps = 8f;
        [Tooltip("On: repeats while the action lasts (idle, walk). Off: plays once (attack, hurt; death stays on its last frame).")]
        public bool loop = true;
        [Tooltip("Frames per facing, by index: 0 right, 1 up-right, 2 up, 3 up-left, 4 left, 5 down-left, 6 down, 7 down-right. " +
                 "ONE entry = the same frames whatever the facing (a crate, a slime). Empty facings borrow the nearest drawn one; " +
                 "with Mirror Left on, 3-5 use 1, 0, 7 flipped.")]
        public SpriteFrames[] facings = new SpriteFrames[8];

        // Seconds one pass takes.
        public float Length(int facing) { SpriteFrames f = Get(facing); return f == null || fps <= 0f ? 0f : f.frames.Length / fps; }

        // The frames for a facing: its own, else the nearest facing that has some.
        public SpriteFrames Get(int facing)
        {
            if (facings == null || facings.Length == 0) return null;
            if (facings.Length == 1) return facings[0] != null && !facings[0].Empty ? facings[0] : null;
            for (int d = 0; d <= 4; d++)
                foreach (int k in d == 0 ? new[] { facing } : new[] { facing + d, facing - d })
                {
                    int i = ((k % 8) + 8) % 8;
                    if (i < facings.Length && facings[i] != null && !facings[i].Empty) return facings[i];
                }
            return null;
        }
    }

    // A character's (or enemy's, or anything's) animations as one asset: drop in your own
    // sprites, any number of frames per action, any frame rate, per facing - no code.
    // Several objects can share one set. Played by DirectionalSprites.
    [CreateAssetMenu(menuName = "TopDownKit/Sprite Animations", fileName = "Animations")]
    public class SpriteAnimSet : ScriptableObject
    {
        [Tooltip("On: left facings (3, 4, 5) show the right-side drawings flipped, so 5 drawings cover 8. Off: they use their own (true 8).")]
        public bool mirrorLeft = true;
        public SpriteAnim[] anims = new SpriteAnim[0];

        public SpriteAnim Find(string name)
        {
            if (anims != null && !string.IsNullOrEmpty(name))
                foreach (SpriteAnim a in anims)
                    if (a != null && a.name == name) return a;
            return null;
        }
    }
}
