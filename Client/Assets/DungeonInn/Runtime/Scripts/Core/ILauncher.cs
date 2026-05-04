using Cysharp.Threading.Tasks;

namespace DungeonInn.Core
{
    public interface ILauncher
    {
        UniTask Launch();
        void Reboot();
    }
}