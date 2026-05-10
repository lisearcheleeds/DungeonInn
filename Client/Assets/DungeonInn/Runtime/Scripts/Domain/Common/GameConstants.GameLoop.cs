namespace DungeonInn.Domain.Common
{
    public static partial class GameConstants
    {
        // 1ゲーム内日付を構成するスケジュールTick数。1Tickは現在1ゲーム秒として扱う。
        public const int GameScheduleTicksPerDay = 1200;

        // ゲームループ中の汎用乱数に使う初期シード。
        public const int InitialGameRandomSeed = 42195;

        // 冒険者スポーン抽選を行う間隔。単位はスケジュールTick。
        public const int AdventurerSpawnIntervalTicks = 5;

        // 冒険者が探索中に目的地Roomへ到達した回数がこの値に達すると帰還を開始する。
        public const int AdventurerExplorationRoomArrivalTarget = 8;

        // 初期ゲームループで同時に存在できる冒険者の上限。
        public const int InitialMaxAdventurerCount = 8;

        // 初期ゲームループで同時に存在できるモンスターの上限。
        public const int InitialMaxMonsterCount = 20;

        // モンスタースポーン抽選を行う間隔。単位はスケジュールTick。
        public const int MonsterSpawnIntervalTicks = 10;

        // アクターの基本移動速度（メートル/秒）。
        public const float ActorMoveSpeedMetersPerSecond = 5.0f;

        // 探索中の冒険者がワールド上のアイテムを自動拾得できる距離（メートル）。
        public const float AdventurerItemPickupRadiusMeters = 1.5f;

        // アクターのインベントリに入るアイテム種別スロット数。
        public const int DefaultInventorySlotCapacity = 10;

        // 初期ギルドストレージのアイテムスロット数。
        public const int InitialGuildInventorySlotCapacity = 100;

        // 探索階層選択で、モンスター平均戦闘力に掛ける難易度係数。
        public const float DungeonFloorDifficultyCoefficient = 3.0f;

        // Return decision starts when the accumulated return score reaches this value.
        public const int AdventurerReturnDecisionThresholdScore = 100;

        public const int AdventurerReturnGoalCompletedScore = 100;
        public const int AdventurerReturnCriticalHpScore = 100;
        public const int AdventurerReturnLowHpWithoutRecoveryItemScore = 100;

        public const float AdventurerReturnLowHpRatio = 0.6f;
        public const float AdventurerReturnCriticalHpRatio = 0.3f;
    }
}
