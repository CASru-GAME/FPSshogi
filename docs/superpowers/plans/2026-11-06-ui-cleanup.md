# UI Cleanup Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task by task.

**Goal:** 白いウィンドウと重複HUDを取り除き、和室と駒が見えるUIにする。
**Architecture:** 既存のUnity uGUI生成コードを修正。UiThemeで透明なボタン・グラデーション・文字色を共有し、画面ごとに外枠を除去。GameSessionUIでメニューとHUDの表示を分離する。
**Tech Stack:** Unity 6000.2.12f1、C#、uGUI、Input System。
**Spec:** docs/superpowers/specs/2026-11-06-ui-cleanup-design.md

## Constraints / Review Focus

- 左メニュー・左右の1P/2P・機器参加と準備確認を維持。
- メニューの下にHUDが残る問題と、戻った後にHUDが消えたままになる問題を回帰検証。
- 明るい障子・畳と暗い床のどちらでも文字とアイコンが読めるか画像で確認。
- 1280x720、1920x1080、2560x1080、1280x1024でレイアウトを確認。
- 透明化後もマウスの当たり判定とコントローラーのフォーカスを維持。
- 実機2台の検証はできないため、仮想機器の実シーン検証と区別して報告。

## Tasks

- [x] 1. RoomUiRegressionへメニュー中HUD非表示・再開復帰の検証を追加し、現状で失敗を確認。
- [x] 2. UiTheme / UiSymbol / MenuPanels / PlayerSelectionPanel / PieceGuidePanelを整理。通常の外枠を透明にし、文字の整列、区切り線、フォーカスだけで階層を示す。
- [x] 3. BoardHud / DuelHud / DuelTransitionの大きな下地を除去。GameSessionUIでメニュー・遷移中はHUDを隠す。
- [x] 4. RoomUiRegressionとPlayerSelectionRegression、既存UI回帰検証を実行。画像確認し必要な修正を行う。
- [x] 5. Windows通常版とネイティブ入力検証をビルド・実行。変更範囲をレビューして記録、ローカルの変更をコミットする。

## Progress

作業ブランチ fix/ui-cleanup。開いているUnityプロジェクトを使って画面を確認するため同じ作業フォルダーを使用。既存の.vscodeと過去のレビュー画像変更は対象外。

今回のユーザーの具体的修正依頼と既に合意した左メニュー方針の範囲で進める。追加のデザイン承認待ちは挟まない。純粋な色・余白の変更には実装を写したテストを作らず、目視と既存の文字収まり検証を使う。

HUD重複の追加検証は Cleanup-Red で3 FAILを確認。修正後 Cleanup-First 96 PASS、感度の実入力検証を含む Cleanup-Final 99 PASS。機器選択56、既存UI60、将棋/FPS199もすべてPASS。

独立レビューでは重大な問題なし。感度5.0保存時に増加検証が誤って失敗する点を、保存値を最後に復元する既存の仕組みを保って初期値2.0のfixtureへ修正。基準レイアウト外で下地が途切れる点を、内容側のグラデーションを維持して画面外側へ延長する方法で修正した。

縦長の盤面画像から操作案内の重なりも確認。原因はCanvasScaler.Expandの余白が増えてもフッターが1080基準に留まること。実際のマスとの重なり検証を追加し Cleanup-Aspect-Red で1280x1024のみ1 FAILを確認。フッターを実Canvasの下端へ追従させて修正。

最終検証（2026-11-07）: RoomUiRegression 113 PASS / 0 FAIL、機器選択56、既存UI60、将棋/FPS199。Windows通常版と入力検証版を再ビルドし、両方Succeeded / Errors 0 / Warnings 2。Windows入力検証23 PASS / 0 FAIL。計451項目、実行時エラー記録は空。警告2件は既存のKingTurretの旧APIとProjectileArrowの未使用フィールド。

通常版を起動し、マウスでタイトル→設定→感度を2.0から3.7へドラッグ→HP数値をオン→初期値へ戻す（2.0/2.0/オフ）→タイトルへ戻るまで確認。タイトルを開いた状態で保持。物理コントローラー2台の試験は未実施で、機器入力の自動検証はInput Systemの仮想デバイスを使用。

最新の画面・回帰検証・ビルド・入力検証記録は VisualReview/RoomUI/Cleanup-Complete/。作業ブランチ fix/ui-cleanup とUnityプロジェクト、Builds/FPSshogi を保持。今回の更新はローカルまでで、以前配布したOpenCloudのZIPは更新していない。
