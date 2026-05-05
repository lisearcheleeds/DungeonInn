using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Factory;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// モンスター種族マスタから Actor を生成するユースケース。
    /// </summary>
    public sealed class SpawnMonsterFromMasterUseCase
    {
        readonly IMonsterFactory monsterFactory;

        [Inject]
        public SpawnMonsterFromMasterUseCase(IMonsterFactory monsterFactory)
        {
            this.monsterFactory = monsterFactory ?? throw new ArgumentNullException(nameof(monsterFactory));
        }

        /// <summary>
        /// 種族マスタに基づいてモンスター Actor を生成する。
        /// </summary>
        public UniTask<Actor> ExecuteAsync(MonsterCreateRequest request)
        {
            return UniTask.FromResult(monsterFactory.Create(request));
        }
    }
}
