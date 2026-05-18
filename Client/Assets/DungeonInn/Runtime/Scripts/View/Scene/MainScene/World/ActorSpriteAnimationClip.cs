using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [CreateAssetMenu(menuName = "DungeonInn/Visual/ActorSpriteAnimationClip")]
    public sealed class ActorSpriteAnimationClip : ScriptableObject
    {
        [SerializeField] int[] frameIndices = { 0 };
        [SerializeField] float fps = 4f;
        [SerializeField] bool loop = true;

        public float Fps => fps;
        public int FrameCount => frameIndices?.Length ?? 0;
        public bool Loop => loop;

        public int GetFrameIndex(int clipFrameIndex)
        {
            if (frameIndices == null || frameIndices.Length == 0)
            {
                return 0;
            }

            if (clipFrameIndex < 0)
            {
                return frameIndices[0];
            }

            if (clipFrameIndex < frameIndices.Length)
            {
                return frameIndices[clipFrameIndex];
            }

            return frameIndices[frameIndices.Length - 1];
        }
    }
}
