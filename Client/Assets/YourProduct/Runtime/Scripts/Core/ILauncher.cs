using Cysharp.Threading.Tasks;

namespace YourProduct.Core
{
    public interface ILauncher
    {
        UniTask Launch();
        void Reboot();
    }
}