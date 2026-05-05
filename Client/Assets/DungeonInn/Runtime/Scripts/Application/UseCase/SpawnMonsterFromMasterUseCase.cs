using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Factory;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// モンスター種族マスタから Actor を生成するユースケース。
    /// </summary>
    public sealed class SpawnMonsterFromMasterUseCase
    {
        readonly IActorFactory actorFactory;

        public SpawnMonsterFromMasterUseCase(IActorFactory actorFactory)
        {
            this.actorFactory = actorFactory ?? throw new ArgumentNullException(nameof(actorFactory));
        }

        /// <summary>
        /// 種族マスタに基づいてモンスター Actor を生成する。
        /// </summary>
        public UniTask<Actor> ExecuteAsync(MonsterCreateRequest request)
        {
            return UniTask.FromResult(actorFactory.CreateMonster(request));
        }
    }
}
