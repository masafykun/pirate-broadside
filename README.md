# 🏴‍☠️ Pirate Broadside

> HMS Resolute を操り、海賊三隻を一斉射撃で沈める高速ネイバルコンバット

Unity 6 で開発した、スタイライズドな海戦アクションゲームです。旗艦 **HMS Resolute** を操舵し、クリーンな射角を取りながら、左舷・右舷の一斉射撃（ブロードサイド）をタイミングよく撃ち込んで海賊船三隻の艦隊を撃沈します。WebGL ビルドを GitHub Pages 上でプレイできます。

![Unity](https://img.shields.io/badge/Unity-6000.0.77f1-000000?style=flat-square&logo=unity)
![WebGL](https://img.shields.io/badge/WebGL-build-990000?style=flat-square&logo=webgl)
![Blender](https://img.shields.io/badge/Blender-5.1.2-F5792A?style=flat-square&logo=blender)
![Git LFS](https://img.shields.io/badge/Git%20LFS-assets-F64935?style=flat-square&logo=git)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)

🔗 **[Live Demo](https://masafykun.github.io/pirate-broadside/)**

---

## 📸 スクリーンショット
![Pirate Broadside title screen](Documentation/Screenshots/title.png)

![Pirate Broadside naval combat](Documentation/Screenshots/battle.png)

---

## 🎮 操作方法
| 操作 | 動作 |
|---|---|
| W / ↑ | 増速（スロットルを上げる） |
| S / ↓ | 後進・減速 |
| A / D または ← / → | 操舵（ステアリング） |
| Q または 左クリック | 左舷ブロードサイド発射 |
| E または 右クリック | 右舷ブロードサイド発射 |
| Esc | メニューに戻る |

---

## ✨ 特徴
- **Rigidbody ベースの操船** — 喫水線スタビライズによる物理的な船体ハンドリング
- **独立した左右の砲列** — 左舷・右舷それぞれの一斉射撃を撃ち分け
- **海賊船 AI** — ブロードサイドの位置取りを行う自律的な敵船三隻
- **完結したゲームフロー** — ダメージ・沈没・勝利・敗北・即時リプレイ
- **リッチな演出** — カスタムのアニメーション海洋シェーダー、島、航跡、煙、着弾、プロシージャル音響
- **オリジナル 3D 資産** — Blender 製のガレオン船（曲面の船体、帆、砲甲板、艤装、ランタン）
- **レスポンシブなフルスクリーン WebGL 表示**

---

## 🛠️ 技術スタック
| カテゴリ | 技術 |
|---|---|
| ゲームエンジン | Unity (6000.0.77f1) |
| ビルド | WebGL |
| 3D アセット | Blender 5.1.2 |
| アセット管理 | Git LFS（ソースアートと 3D アセット） |
| 配信 | GitHub Pages |

---

## 🚀 セットアップ
```bash
# ブラウザでそのままプレイする場合は Live Demo を開く

# ローカルでビルドする場合:
# 1. Unity (6000.0.77f1) でプロジェクトを開く
# 2. メニューの Pirate Broadside > Build WebGL を実行
# 3. ビルド成果物は Build/WebGL に出力される
```

---

## ライセンス
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

ソースコードは **MIT ライセンス** のもとで公開しています。オリジナルのアートワークおよび生成されたゲームアセットは、本プロジェクトでの利用を前提として同梱されています。

© 2026 masafykun (https://github.com/masafykun)
