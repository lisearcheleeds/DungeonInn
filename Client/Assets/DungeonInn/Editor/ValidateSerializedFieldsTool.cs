using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using io.github.hatayama.uLoopMCP;

namespace DungeonInn.Editor
{
    [McpTool(Description = "Validate that all [SerializeField] reference fields in DungeonInn MonoBehaviours are assigned. Scans all scenes and prefabs under Assets/DungeonInn.")]
    public class ValidateSerializedFieldsTool : AbstractUnityTool<ValidateSerializedFieldsSchema, ValidateSerializedFieldsResponse>
    {
        public override string ToolName => "validate-serialized-fields";

        protected override Task<ValidateSerializedFieldsResponse> ExecuteAsync(
            ValidateSerializedFieldsSchema parameters,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            SerializedFieldNullValidator.RunValidation(errors);

            return Task.FromResult(new ValidateSerializedFieldsResponse
            {
                Success = errors.Count == 0,
                ErrorCount = errors.Count,
                Errors = errors.ToArray()
            });
        }
    }
}
