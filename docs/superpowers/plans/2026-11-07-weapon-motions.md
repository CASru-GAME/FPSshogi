# Weapon Motions Implementation Plan

> Use superpowers:executing-plans to implement this plan in the current session.

**Goal:** 全8駒の通常攻撃・副操作・スキルを実際の内容と一致する動きへ修正する。
**Architecture:** WeaponPresentationの共通pulseを武器別の段階的な軌道へ変更。各スキルの発動地点から表示へ通知し、攻撃・防御・スキルを別の時計で合成。既存の判定・効果・クールダウンの所有関係を維持する。
**Tech Stack:** Unity 6000.2.12f1 / C# / uGUI / Input System。
**Spec:** docs/superpowers/specs/2026-11-07-weapon-motions-design.md

## Constraints and review focus

- 既存のUI修正840d4bdを土台とする。同じUnityプロジェクトを使い、ブランチfix/weapon-motionsで変更を保存。
- 背景・UI・ルール・ダメージ・再使用間隔へ無関係な変更を混ぜない。
- 攻撃中のスキル、防御中の攻撃、終了間際の再入力、停止と対局終了、成りと両プレイヤーを確認。
- 物理コントローラー2台は未検証。自動入力と実行ファイル上の確認を区別する。

## Tasks

- [x] 1. Assets/Tests/Editor/WeaponMotionChecks.csを作り、VisualRepairChecksのMotionモードから全8駒の軌道・発動を記録。槍の直進、弓の出現点、スキル姿勢と強化時間の失敗を確認。
- [x] 2. Assets/App/Scripts/Main/Player/WeaponPresentation.csで握り軸と振り出し・攻撃・復帰を実装。槍・刀・薙刀・短刀・銃・弓・軍配を実画面で確認。必要な設定は表示プレハブとVisualRepairBuilderへ同じ値を反映。
- [x] 3. _Component/Skills/*.cs、SecondaryZoom.cs、CombatVisuals.csから受理済みの発動・終了を表示へ通知。BowPrimary.csで番えた矢の位置と発射位置を一致。KingTurret.csの発射表示を照合。
- [x] 4. Motion検証を通し、連続画像を確認。既存Gameplay/Visual/RoomUI回帰検証、通常Windowsビルドを実行。独立レビューと指摘修正。
- [x] 5. 記録・比較画像・確認用動画をまとめ、ローカルの実行ファイルを更新。今回の範囲だけコミット。

ユーザーは全キャラの調査・修正を明示的に依頼。既存仕様から必要な動作が定まるため、追加のデザイン承認待ちは挟まない。

## Progress

2026-11-07: Motion-Beforeで全8駒の入力・姿勢を確認、65 PASS / 17 FAIL。撮影負荷で短い攻撃のサンプル時刻がずれるため、軌道画像はゲーム時計を一時停止して実際の表示コードを指定時刻へ進める方式へ変更。入力・スキル・停止復帰は通常の時計で検証する。Motion-Red-Stableでも65 PASS / 17 FAIL、実行時エラー0を確認。途中のMotion-RedはPlay中のスクリプト再読込で中断した検証のため採用しない。

武器別の握り軸、突き・斬撃・反動・弦復帰、ズーム姿勢、スキル通知、強化時間の表示を実装。Motion-Firstで82 PASS / 0 FAIL。防御と攻撃の切り替え、砲台の上下照準・反動の追加テストで6件の失敗を再現して修正。成りを含めMotion-Finalで94 PASS / 0 FAIL。

実画面で長柄武器の防御姿勢が画面外へ出ることを確認し、画面内へ構えるよう修正。独立レビューの桂馬の防御→突進切り替えもMotion-Review-Redで再現し、姿勢を補間。弓は番えた矢から発射し、視点と弓の間の障害物との接触を発射時に解決する。低い壁への矢の初期めり込みをMotion-Cover-Focusedで再現（17 PASS / 1 FAIL）。最終Motion-Completeと全回帰、録画を実行中。

Motion-Complete 108、Gameplay 199、Visual 132、RoomUI 113、Windows仮想入力23項目が通過。独立レビューの指摘と連戦時の破棄済み参照を修正。録画を直接エンコードして実画面の色を維持。連続フレームで刀・薙刀の刃が下へ沈む点をさらに確認し、Motion-Slash-Redで3件を再現。斬撃と突進の握り位置を上げて修正し、Motion-Verifiedと命中判定、該当3種の録画を再検証中。

最終結果: Motion-Verified 111、Gameplay/Motion-Verified 199、Weapon-Verified 132項目が通過。変更した3種を再録画して24本の確認動画を完成。通常Windows版と検証用Windows版を再ビルド（エラー0）、Windows入力検証23項目も通過。ローカルブランチfix/weapon-motionsに変更・検証記録・比較資料を保存。
