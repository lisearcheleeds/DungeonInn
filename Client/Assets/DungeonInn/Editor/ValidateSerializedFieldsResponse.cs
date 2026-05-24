using io.github.hatayama.uLoopMCP;

namespace DungeonInn.Editor
{
    public class ValidateSerializedFieldsResponse : BaseToolResponse
    {
        public bool Success { get; set; }
        public int ErrorCount { get; set; }
        public string[] Errors { get; set; }
    }
}
