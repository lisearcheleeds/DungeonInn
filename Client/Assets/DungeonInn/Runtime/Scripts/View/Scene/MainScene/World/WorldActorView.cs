using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorView
    {
        public WorldActorView(GameObject actorObject, SpriteRenderer spriteRenderer)
        {
            ActorObject = actorObject;
            SpriteRenderer = spriteRenderer;
            Facing = Vector2.down;
        }

        public GameObject ActorObject { get; }
        public SpriteRenderer SpriteRenderer { get; }
        public Vector2 Facing { get; private set; }
        public LayerPosition LastPosition { get; private set; }
        public bool HasLastPosition { get; private set; }

        public bool UpdateFacing(LayerPosition position)
        {
            var previousFacing = Facing;
            if (HasLastPosition && LastPosition.LayerId.Equals(position.LayerId))
            {
                var movement = new Vector2(
                    position.X - LastPosition.X,
                    position.Z - LastPosition.Z);
                if (0.0001f < movement.sqrMagnitude)
                {
                    Facing = movement.normalized;
                }
            }

            LastPosition = position;
            HasLastPosition = true;
            return !previousFacing.Equals(Facing);
        }
    }
}
