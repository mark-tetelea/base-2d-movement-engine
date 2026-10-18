using UnityEngine;

namespace TopDownKit
{
    // One movement feel as an asset (Instant, Glide, ...), so several movers can share it
    // and values tuned during play mode are kept afterwards. Movers read it every step, so
    // a change shows at once.
    [CreateAssetMenu(menuName = "Top-Down Kit/Move Feel", fileName = "MoveFeel")]
    public class MoveFeel : ScriptableObject
    {
        public MoveSettings settings = new MoveSettings { speed = 6f };
    }
}
