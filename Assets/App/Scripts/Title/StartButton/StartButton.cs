using UnityEngine;

namespace App.Title.StartButton
{
    public class StartButton : MonoBehaviour
    {
        // UI のボタンなどから OnClick に割り当てて使ってください
        public void OnClick()
        {
            // タイトル画面からメインシーンへ遷移
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        }
    }
}