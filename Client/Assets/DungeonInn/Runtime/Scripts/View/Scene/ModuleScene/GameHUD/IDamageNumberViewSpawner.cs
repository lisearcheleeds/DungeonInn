using UnityEngine;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public interface IDamageNumberViewSpawner
    {
        void Spawn(int damage, Vector3 worldPosition);
        void Tick(float deltaSeconds);
    }
}
