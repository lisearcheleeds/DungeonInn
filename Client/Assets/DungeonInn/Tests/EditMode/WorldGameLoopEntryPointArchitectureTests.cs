using System.Linq;
using DungeonInn.View.Scene.MainScene.World;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class WorldGameLoopEntryPointArchitectureTests
    {
        [Test]
        public void EntryPointDoesNotDirectlyDependOnApplicationUseCasesOrOrchestrators()
        {
            var entryPointType = typeof(WorldGameLoopEntryPoint);
            var forbiddenDependencies = entryPointType
                .GetFields(System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public)
                .Select(field => field.FieldType)
                .Concat(entryPointType
                    .GetMethods(System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public)
                    .Where(method => method.Name == "Construct")
                    .SelectMany(method => method.GetParameters())
                    .Select(parameter => parameter.ParameterType))
                .Where(type => type.Namespace == "DungeonInn.Application.UseCase" ||
                    type.Namespace == "DungeonInn.Application.Orchestration")
                .Select(type => type.FullName)
                .ToArray();

            Assert.That(forbiddenDependencies, Is.Empty);
        }
    }
}
