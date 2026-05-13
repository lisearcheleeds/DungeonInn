using System.IO;
using System.Linq;
using UnityEngine;
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

        [Test]
        public void NonFrameSimulationSystemsRunOnlyFromScheduleTickBlock()
        {
            var sourcePath = Path.Combine(
                UnityEngine.Application.dataPath,
                "DungeonInn/Runtime/Scripts/Application/GameLoop/WorldSimulationOrchestrator.cs");
            var source = File.ReadAllText(sourcePath);
            var frameMethodStart = source.IndexOf("public async UniTask AdvanceFrameAsync", System.StringComparison.Ordinal);
            var scheduleMethodStart = source.IndexOf("async UniTask AdvanceScheduleSystemsAsync", System.StringComparison.Ordinal);

            Assert.That(frameMethodStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(scheduleMethodStart, Is.GreaterThan(frameMethodStart));

            var frameMethodBody = source.Substring(frameMethodStart, scheduleMethodStart - frameMethodStart);
            var scheduleMethodBody = source.Substring(scheduleMethodStart);
            var scheduleOnlyCalls = new[]
            {
                "updateEquipmentUseCase.Execute",
                "sellItemsUseCase.Execute",
                "useRecoveryItemUseCase.ExecuteAsync",
                "decideAdventurerReturnUseCase.ExecuteAsync"
            };

            foreach (var call in scheduleOnlyCalls)
            {
                Assert.That(frameMethodBody, Does.Not.Contain(call));
                Assert.That(scheduleMethodBody, Does.Contain(call));
            }
        }
    }
}
