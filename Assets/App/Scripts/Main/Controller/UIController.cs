using App.Common.Initialize;
using App.Main.GameMaster;
using UnityEngine;
using App.Main.ViewManager;
using UnityEngine.SceneManagement;

namespace App.Main.Controller
{
    public class UIController : MonoBehaviour, IInitializable
    {
        int IInitializable.InitializationPriority => 100;

        public System.Type[] Dependencies => new System.Type[] { typeof(GameStateHolder) };

        GameStateHolder gameStateHolder;

        [SerializeField] private GameObject winnerUI;

        public void Initialize(ReferenceHolder referenceHolder)
        {
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();
            gameStateHolder.SubscribeToChangeToPlayerOneWin(OnEndGame);
            gameStateHolder.SubscribeToChangeToPlayerTwoWin(OnEndGame);
        }

        private void OnEndGame()
        {
            // GameSessionUI owns the persistent result panel and explicit rematch/title actions.
            StopReturnToTitle();
        }

        // 指定秒待ってタイトルシーンへ移動する (シーン名は "Title" を想定)
        public void StartReturnToTitle(float delaySeconds)
        {
            Debug.Log($"Returning to Title in {delaySeconds} seconds...");
            StopReturnToTitle(); // 既存のコルーチンがあれば止める
            StartCoroutine(ReturnToTitleCoroutine(delaySeconds));
        }

        public void StopReturnToTitle()
        {
            StopAllCoroutines(); // 必要に応じて特定コルーチンだけ停止する実装に変えてください
        }

        private System.Collections.IEnumerator ReturnToTitleCoroutine(float delaySeconds)
        {
            if (delaySeconds > 0f)
                yield return new WaitForSeconds(delaySeconds);

            // 追加のクリーンアップがあればここで行う

            // シーン名 "Title" に遷移（プロジェクトのタイトルシーン名に合わせて変更してください）
            SceneManager.LoadScene("Title");
        }
    }
}
