namespace DungeonInn.Domain.Common
{
    public static partial class GameConstants
    {
        // ダンジョン生成に使う初期シード。
        public const int InitialDungeonSeed = 12345;

        // ゲーム開始時に冒険者ギルドが持つ初期所持金。
        public const int InitialGuildGold = 10000;

        // 新米冒険者へ配布する初期武器アイテムID。
        public const int InitialRookieSwordItemId = 3001;

        // ゲーム開始時にギルドが持つ新米用武器の在庫数。
        public const int InitialRookieSwordCount = 20;

        // 新米冒険者へ配布する初期防具アイテムID。
        public const int InitialRookieArmorItemId = 3003;

        // ゲーム開始時にギルドが持つ新米用防具の在庫数。
        public const int InitialRookieArmorCount = 20;

        // 初期宿屋施設の基準利用価格。
        public const int InitialInnBasePrice = 10;

        // 初期宿屋施設の収容人数。
        public const int InitialInnCapacity = 8;
        public const int InitialGeneralStoreBasePrice = 10;
        public const int InitialEquipmentShopBasePrice = 10;
        public const int InitialShopCapacity = 1;
    }
}
