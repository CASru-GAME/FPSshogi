using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace App.Title.ExitButton
{
    // UI のボタンなどから OnClick に割り当てて使ってください
    public class ExitButton : MonoBehaviour
    {
        public void OnClick()
        {
            // 実行ビルドではアプリケーションを終了
            Application.Quit();

#if UNITY_EDITOR
            // エディタでは再生を停止
            EditorApplication.isPlaying = false;
#endif
        }
    }
}