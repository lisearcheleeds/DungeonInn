# Milestone 6 アセット生成プロンプト集

各セクションのプロンプト欄のテキストをそのまま ChatGPT にコピー&ペーストして画像を生成してください。

## 注意事項

- プロンプトは英語で記載しています（英語の方が生成精度が高いため）
- 生成後は画像編集ソフト（GIMP / Photoshop / Aseprite など）でリサイズしてください
- スプライトの背景は ChatGPT では完全透過にならない場合があります。`magenta background` と指定すると除去しやすくなります
- スタイルの統一感を保つため、全アセットを同じ会話セッションで続けて生成することを推奨します

---

## マップタイルテクスチャ（7種）

**Unity 取り込み設定（全タイル共通）**

| 設定項目 | 値 |
|---|---|
| リサイズ後サイズ | 256×256 px |
| Texture Type | Default |
| Wrap Mode | Repeat |
| Filter Mode | Point（ピクセルアート）|

---

### 1. 地上床（GroundWalkable）

**どんな場所:** 冒険者・モンスターが歩く地上マップの屋外床。ダンジョン近くの中世ファンタジー風の街並みの地面。石畳や砂利の道。

```
Create a seamless tileable pixel art texture, 512x512 pixels. Top-down view of an outdoor cobblestone street or packed dirt path for a medieval fantasy RPG town located near a dungeon entrance. Warm gray stone blocks with sandy-colored grout lines, slightly worn and uneven. Muted earthy tones: medium warm gray with hints of beige. Subtle texture variation to avoid repetition. No characters, no objects, only the ground surface. Clean pixel art style.
```

---

### 2. 地上壁（GroundBlocked）

**どんな場所:** 地上マップで歩けない障害物・壁の表面。上面（壁の上端）と側面（壁の表面）の両方に貼られるため、どちらの向きから見ても壁らしく見える必要があります。

```
Create a seamless tileable pixel art texture, 512x512 pixels. Rough-cut stone wall surface for a medieval fantasy RPG. This texture will be applied to both the top face and the side faces of a 3D cube-shaped wall block, so it must look natural from both a horizontal and vertical viewing angle. Irregular gray stone blocks with dark mortar gaps. Cool gray and dark charcoal tones. Sturdy, solid stone feel. No characters, no windows, just raw stone masonry. Clean pixel art style.
```

---

### 3. 施設予定地（Facility）

**どんな場所:** 宿屋・酒場・雑貨屋・装備屋などの建物が建つ区画の床。地上床（石畳）と区別できるよう、室内的で温かみのある素材。

```
Create a seamless tileable pixel art texture, 512x512 pixels. Top-down view of an indoor wooden floor for a medieval fantasy building such as an inn, tavern, or shop. Warm brown wooden planks laid horizontally with visible plank seams and subtle wood grain. Soft warm brown and amber tones, clean and well-maintained compared to the outdoor stone floor. Cozy and welcoming atmosphere. No furniture, no characters, only the floor surface. Clean pixel art style.
```

---

### 4. ダンジョン床（DungeonWalkable）

**どんな場所:** ダンジョン内部の歩ける床面。洞窟や古い地下遺跡の石の床。地上床より暗く、湿った雰囲気。

```
Create a seamless tileable pixel art texture, 512x512 pixels. Top-down view of a dark dungeon floor for a medieval fantasy RPG. Ancient cracked stone tiles, weathered and damp. Very dark gray and dark brown-gray tones, gloomy and oppressive. Subtle hints of moisture, small cracks, and moss in the crevices between stones. Much darker and more foreboding than the outdoor cobblestone. No characters, only the dungeon floor surface. Clean pixel art style.
```

---

### 5. ダンジョン壁（DungeonBlocked）

**どんな場所:** ダンジョン内部の壁。上面と側面の両方に貼られます。地上壁より暗く重厚な石積みの壁。

```
Create a seamless tileable pixel art texture, 512x512 pixels. Ancient dungeon stone wall surface for a medieval fantasy RPG. This texture will be applied to both the top face and side faces of a 3D cube-shaped dungeon wall block. Very dark gray, almost black stone blocks with deep shadow gaps between them. Rough, ancient stonework, slightly damp with dark patches. Significantly darker and more oppressive than the outdoor stone wall. No characters, no torches, only raw ancient stone. Clean pixel art style.
```

---

### 6. 上り階段（StairUp）

**どんな場所:** ダンジョンの上の階へ続く階段。傾斜面（ランプジオメトリ）に貼られます。斜め上から見た時に段が認識できるデザインが必要です。

```
Create a pixel art texture, 512x512 pixels. Stone staircase texture going upward, designed to be applied to a ramp or slope surface in a 3D dungeon RPG. The texture should show clear horizontal step lines evenly spaced across the image to represent stone steps when viewed at an angle. Each step edge should be slightly lighter on top and darker on the vertical face to give depth. Medium gray stone color, cleaner than the dungeon floor. The step lines should run horizontally across the full width of the texture. Clean pixel art style.
```

---

### 7. 下り階段（StairDown）

**どんな場所:** ダンジョンの下の階へ続く階段。傾斜面に貼られます。上り階段より暗く、深みへ続く雰囲気。

```
Create a pixel art texture, 512x512 pixels. Stone staircase texture going downward into darkness, designed to be applied to a ramp or slope surface in a 3D dungeon RPG. The texture should show clear horizontal step lines evenly spaced across the image to represent stone steps when viewed at an angle. Darker than the upward staircase, with shadowy tones suggesting depth below. Dark gray stone with very deep shadows at the lower portion. The step lines should run horizontally across the full width. A slightly foreboding atmosphere. Clean pixel art style.
```

---

## キャラクタースプライト（2種）

**Unity 取り込み設定（全スプライト共通）**

| 設定項目 | 値 |
|---|---|
| リサイズ後サイズ | 32×48 px |
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 16 |
| Filter Mode | Point |
| Alpha Is Transparency | ON |

生成後は画像編集ソフトで 32×48 px にリサイズし、背景を透過してから保存してください。

**スプライトの向きについて:** ゲームは左右反転で対応するため、**左向き**（または右向き）の片方向のみ用意してください。

---

### 8. 冒険者（Adventurer）

**どんなキャラクター:** プレイヤーのギルドを訪れる人間の冒険者。宿屋に泊まりながらダンジョンを探索します。軽装鎧を着た剣士・探検家のイメージ。

```
Pixel art character sprite for a 2.5D fantasy RPG, side-view facing left. A human adventurer, full body from head to feet visible. Light armor or traveler's outfit, carrying a sword or small weapon, with a belt pouch or satchel. Neutral standing idle pose, weight evenly balanced. Solid magenta background (#FF00FF) for easy removal. Classic 16-bit JRPG style, very clean silhouette readable at tiny size. Character height should fill most of the vertical space. No background scenery.
```

---

### 9. モンスター（Monster）

**どんなキャラクター:** ダンジョン内に生息する敵キャラクター。冒険者と戦闘します。ゴブリン・スライム・スケルトンなどクラシックなダンジョンモンスターのイメージ。冒険者と同じアートスタイルで統一してください。

```
Pixel art character sprite for a 2.5D fantasy RPG, side-view facing left. A classic dungeon monster: a goblin, skeleton, or slime-type creature. Full body from head to feet visible. Menacing but simple design, clearly readable as an enemy. Neutral standing pose. Solid magenta background (#FF00FF) for easy removal. Classic 16-bit JRPG style matching the adventurer sprite's art style, very clean silhouette readable at tiny size. Character height should fill most of the vertical space. No background scenery.
```

---

## Phase 3 用スプライトシート（アニメーション仕様確定後に発注）

アニメーションのフレーム数と FPS が確定したら、以下のプロンプトの `[フレーム数]` を埋めて使用してください。

---

### 10. 冒険者 Idle アニメーション

```
Pixel art sprite sheet for a 2.5D fantasy RPG. Human adventurer idle animation, side-view facing left. [フレーム数] frames arranged in a single horizontal row, each frame 32x48 pixels (total width: 32×[フレーム数] pixels, height: 48 pixels). Subtle breathing or slight swaying movement. Consistent with the previously generated adventurer design. Solid magenta background (#FF00FF). Clean 16-bit JRPG pixel art style.
```

---

### 11. 冒険者 Walk アニメーション

```
Pixel art sprite sheet for a 2.5D fantasy RPG. Human adventurer walk cycle animation, side-view facing left. [フレーム数] frames arranged in a single horizontal row, each frame 32x48 pixels (total width: 32×[フレーム数] pixels, height: 48 pixels). Smooth, natural walk cycle with arm swing. Consistent with the previously generated adventurer design. Solid magenta background (#FF00FF). Clean 16-bit JRPG pixel art style.
```

---

### 12. モンスター Idle アニメーション

```
Pixel art sprite sheet for a 2.5D fantasy RPG. Dungeon monster idle animation, side-view facing left. [フレーム数] frames arranged in a single horizontal row, each frame 32x48 pixels (total width: 32×[フレーム数] pixels, height: 48 pixels). Menacing idle animation, slight breathing or threatening movement. Consistent with the previously generated monster design. Solid magenta background (#FF00FF). Clean 16-bit JRPG pixel art style.
```

---

### 13. モンスター Walk アニメーション

```
Pixel art sprite sheet for a 2.5D fantasy RPG. Dungeon monster walk animation, side-view facing left. [フレーム数] frames arranged in a single horizontal row, each frame 32x48 pixels (total width: 32×[フレーム数] pixels, height: 48 pixels). Threatening, lumbering or scurrying movement depending on monster type. Consistent with the previously generated monster design. Solid magenta background (#FF00FF). Clean 16-bit JRPG pixel art style.
```

---

## 環境プロップ（3D モデル）について

柱・松明・木などの 3D プロップは ChatGPT では生成できません（2D 画像のみ）。以下のいずれかの方法で用意してください。

| 方法 | 難易度 | 備考 |
|---|---|---|
| Unity Asset Store から無料アセットを入手 | 低 | "Low Poly Dungeon" "Low Poly Nature" などで検索 |
| Blender で簡単なプリミティブを組み合わせて作成 | 中 | 柱＝円柱、木＝円錐＋球など |
| Meshy / Tripo などの AI 3D 生成ツールを使用 | 低〜中 | プロンプトで 3D モデルを生成できる |

Phase 4 の実装開始前に 1〜2 種類だけ用意すれば十分です。
