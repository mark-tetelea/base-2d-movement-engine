using UnityEngine;

namespace TopDownKit
{
    // What an NPC says: a speaker name and lines shown one at a time in the TextBox.
    [CreateAssetMenu(menuName = "TopDownKit/Dialogue Lines", fileName = "Lines")]
    public class DialogueLines : ScriptableObject
    {
        public string speaker = "Someone";
        [TextArea(2, 4)] public string[] lines = { "Hello." };
    }
}
