namespace DungeonInn.Application.Navigation
{
    public sealed class NavigationPathResult
    {
        public bool Success { get; }
        public NavigationPath Path { get; }

        public NavigationPathResult(bool success, NavigationPath path)
        {
            Success = success;
            Path = path;
        }
    }
}
