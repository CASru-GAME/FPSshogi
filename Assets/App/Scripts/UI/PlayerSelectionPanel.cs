using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace App.UI
{
    public sealed class PlayerSelectionPanel
    {
        readonly GameSessionUI session;
        readonly Text[] device = new Text[2], state = new Text[2], controls = new Text[2];
        readonly Image[] stripe = new Image[2];
        readonly Text status, keyboardCaption;
        readonly Button keyboardButton;
        public PlayerSelectionPanel(RectTransform panel, GameSessionUI session)
        {
            this.session = session;
            panel.name = "Player selection panel";
            UiTheme.Label(panel, "Selection instructions", "使う機器で参加：コントローラー A ／ キーマウ Enter\n参加した順に1P・2Pになります。機器と担当画面を確認してください。", 44, 130, 992, 84, 26);
            for (int i = 0; i < 2; i++)
            {
                var color = i == 0 ? UiTheme.Gold : UiTheme.Blue;
                var card = UiTheme.Box(panel, $"P{i+1} selection card", 44 + i * 512, 232, 480, 330, UiTheme.Panel).rectTransform;
                stripe[i] = UiTheme.Box(card, "Player stripe", 0, 0, 480, 5, color);
                UiTheme.Label(card, "Player heading", $"{i+1}P  /  " + (i == 0 ? "先手・左画面" : "後手・右画面"), 24, 20, 432, 58, 34, color);
                device[i] = UiTheme.Label(card, $"P{i+1} selection device", "", 24, 92, 432, 84, 27);
                device[i].resizeTextForBestFit = true; device[i].resizeTextMinSize = 19; device[i].resizeTextMaxSize = 27;
                state[i] = UiTheme.Label(card, $"P{i+1} selection state", "", 24, 186, 432, 48, 30, color);
                controls[i] = UiTheme.Label(card, $"P{i+1} selection controls", "", 24, 246, 432, 68, 23, UiTheme.Muted);
            }
            status = UiTheme.Label(panel, "Selection status", "", 44, 581, 992, 80, 30, UiTheme.Gold, TextAnchor.MiddleCenter);
            keyboardButton = UiTheme.Button(panel, "Keyboard selection button", "キーマウで参加", 44, 680, 620, 64, session.KeyboardSelectionAction);
            keyboardCaption = keyboardButton.GetComponentInChildren<Text>();
            UiTheme.Button(panel, "Selection back button", "タイトルへ戻る", 696, 680, 340, 64, session.CancelSelection);
            UiTheme.Label(panel, "Selection footer", "A / Enter：参加・準備完了　　B / Esc：準備取消 → 参加解除", 44, 762, 992, 48, 23, UiTheme.Muted, TextAnchor.MiddleCenter);
            Tick();
        }
        public void Tick()
        {
            var devices = session.Devices;
            for (int i = 0; i < 2; i++)
            {
                bool connected = devices.Connected(i), ready = devices.IsPrepared(i);
                device[i].text = devices.Description(i);
                state[i].text = ready ? "準備完了" : connected ? "参加済み・準備待ち" : "使う機器で参加";
                string confirm = devices.IsGamepad(i) ? "A" : "Enter", cancel = devices.IsGamepad(i) ? "B" : "Esc";
                controls[i].text = connected ? ready ? cancel + "：準備を取り消す" : confirm + "：準備完了\n" + cancel + "：参加を取り消す" : "コントローラーは A\nキーマウは Enter";
                stripe[i].color = ready ? Color.Lerp(i == 0 ? UiTheme.Gold : UiTheme.Blue, Color.white, .3f) : i == 0 ? UiTheme.Gold : UiTheme.Blue;
            }
            status.text = devices.AllPrepared ? "両者準備完了 — 対局を始めます" : "2人とも機器を確認して、準備完了にしてください";
            int slot = devices.PlayerFor(Keyboard.current);
            keyboardButton.interactable = Keyboard.current != null && Keyboard.current.enabled && Mouse.current != null && Mouse.current.enabled && (slot >= 0 || !devices.Ready);
            keyboardCaption.text = slot < 0 ? "キーマウで参加" : devices.IsPrepared(slot) ? $"{slot+1}P キーマウ：準備を取り消す" : $"{slot+1}P キーマウ：準備完了";
        }
    }
}
