using Cysharp.Threading.Tasks;

namespace DungeonInn.Runtime.Scripts.Core
{
    public interface ILauncher
    {
        UniTask Launch();
        void Reboot();
    }
}