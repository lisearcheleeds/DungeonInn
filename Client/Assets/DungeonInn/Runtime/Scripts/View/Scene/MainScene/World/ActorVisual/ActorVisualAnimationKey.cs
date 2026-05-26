namespace DungeonInn.View.Scene.MainScene.World
{
    public readonly struct ActorVisualAnimationKey : System.IEquatable<ActorVisualAnimationKey>
    {
        public ActorVisualAnimationKey(ActorAnimationKey animationKey, ActorAnimationDirection direction)
        {
            AnimationKey = animationKey;
            Direction = direction;
        }

        public ActorAnimationKey AnimationKey { get; }
        public ActorAnimationDirection Direction { get; }

        public bool Equals(ActorVisualAnimationKey other)
        {
            return AnimationKey == other.AnimationKey &&
                Direction == other.Direction;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorVisualAnimationKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)AnimationKey * 397) ^ (int)Direction;
        }
    }
}
