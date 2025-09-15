using UnityEngine;
using App.Common.Initialize;

namespace App.Title.ExitButton
{
    public class ExitGame : MonoBehaviour, IInitializable
    {
        public GameObject confirmPanel; // 確認パネルを割り当て
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { };
        public void Initialize()
        {
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }
        }

        // Exitボタンを押したときに呼ぶ
        public void ShowConfirmPanel()
        {
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(true);
            }
        }

        // Yesボタン
        public void QuitGame()
        {
            Debug.Log("ゲーム終了");
            Application.Quit();
        }

        // Noボタン
        public void CancelExit()
        {
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }
        }
    }
}