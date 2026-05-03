# task_003 完了報告

## 実施内容

- 5 種の ConfigRepository を `IAssetManager.CreateScope()` ベースに変更し、ロード結果をキャッシュする実装に修正。
- `ProductLifetimeScope` に `IAssetManager` と各 ConfigRepository の DI 登録を追加。
- `Launcher` に全 5 リポジトリを注入し、起動時に `UniTask.WhenAll` でコンフィグを eager load するよう修正。
- `Assets/DungeonInn/Runtime/StaticResources/Config/` に ScriptableObject アセット 5 個を作成。
- Addressables 設定を作成し、Default Local Group に以下 5 エントリを登録。
  - `Config/WorldConfig`
  - `Config/InnConfig`
  - `Config/AdventurerConfig`
  - `Config/MonsterConfig`
  - `Config/DungeonConfig`

## 検証

- `uloop.cmd compile --project-path Client`
  - ErrorCount: 0
  - WarningCount: 0
- SO アセット 5 個の存在を確認済み。
- Addressables Default Local Group に 5/5 エントリ登録済み。

## 備考

- Unity Editor 側で Addressables 設定が未作成だったため、`AddressableAssetSettingsDefaultObject.GetSettings(true)` で設定と Default Local Group を作成してから登録した。
