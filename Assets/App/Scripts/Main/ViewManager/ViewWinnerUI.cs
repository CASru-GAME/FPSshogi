using UnityEngine;
using UnityEngine.UI;

namespace App.Main.ViewManager
{
    public class ViewWinnerUI : MonoBehaviour
    {
        [SerializeField] private GameObject winnerUIPrefab;
        [SerializeField] private Sprite playerOneWinnerImage;
        [SerializeField] private Sprite playerTwoWinnerImage;

        public void Show(bool isPlayerOneWinner)
        {
            Debug.Log("Show Winner UI");
            if (winnerUIPrefab != null)
            {
                Debug.Log("Instantiating Winner UI");
                GameObject uiInstance = Instantiate(winnerUIPrefab, transform);
                CanvasGroup canvasGroup = uiInstance.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    Debug.Log("Enabling Winner UI CanvasGroup");
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;

                    // 勝者に応じてUIの表示内容を変更する処理をここに追加
                    if (isPlayerOneWinner)
                    {
                        Debug.Log("Player One is the winner");
                        // Player Oneが勝者の場合の処理
                        uiInstance.GetComponentInChildren<Image>().sprite = playerOneWinnerImage;
                    }
                    else
                    {
                        Debug.Log("Player Two is the winner");
                        // Player Twoが勝者の場合の処理
                        uiInstance.GetComponentInChildren<Image>().sprite = playerTwoWinnerImage;
                    }
                }
            }
        }

        public void Hide()
        {
            if (winnerUIPrefab != null)
            {
                Debug.Log("Hide Winner UI");
                CanvasGroup canvasGroup = winnerUIPrefab.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            }
        }
    }
}
