using UnityEngine;

public class ExitGame : MonoBehaviour
{
    public GameObject confirmPanel; // 確認パネルを割り当て

    void Start() {
        if (confirmPanel != null) {
            confirmPanel.SetActive(false);
        }
    }

    // Exitボタンを押したときに呼ぶ
    public void ShowConfirmPanel() {
        if (confirmPanel != null) {
            confirmPanel.SetActive(true);
        }
    }

    // Yesボタン
    public void QuitGame() {
        Debug.Log("ゲーム終了");
        Application.Quit();
    }

    // Noボタン
    public void CancelExit() {
        if (confirmPanel != null) {
            confirmPanel.SetActive(false);
        }
    }
}

