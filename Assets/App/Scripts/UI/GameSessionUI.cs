using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using App.Main.GameMaster;
using App.Main.Player;
using State=App.Main.GameMaster.GameStateHolder.GameState;

namespace App.UI
{
    public sealed class GameSessionUI : MonoBehaviour
    {
        public static GameSessionUI Instance { get; private set; }
        static LocalMatchDevices matchDevices;
        public LocalMatchDevices Devices => matchDevices;
        public bool IsTitle { get; private set; }
        public bool ConnectionStartsMatch => IsTitle || pendingNewMatch;
        public static bool BlocksGameplay => Instance!=null && Instance.Blocked;
        enum Page { None, Title, Pause, Help, Settings, Connection, Result, Confirm }
        Page page;
        MenuPanels menus;
        BoardHud boardHud;
        DuelHud duelHud;
        PlayerManager players;
        GameStateHolder state;
        RectTransform transition;
        Text transitionTitle, transitionDetail;
        float transitionRemaining;
        bool countdown;
        bool releaseGate;
        float releaseAfter, menuNavigationAfter;
        bool previousReady;
        bool pendingNewMatch;
        string transitionCaption;
        Button defaultFocus;
        bool Blocked => !IsTitle && (page!=Page.None || transitionRemaining>0 || !Devices.Ready || releaseGate);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance=null; matchDevices=null; }
        void Awake()
        {
            Instance=this;
            IsTitle=SceneManager.GetActiveScene().name=="Title";
            if(IsTitle || matchDevices==null) matchDevices=new LocalMatchDevices();
            Devices.Refresh(IsTitle); previousReady=Devices.Ready;
            foreach(var old in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)) old.gameObject.SetActive(false);
            var canvas=gameObject.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=300;
            var scaler=gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();
            var layout=UiTheme.Rect(transform,"Layout",0,0,1920,1080);
            layout.anchorMin=layout.anchorMax=new Vector2(.5f,.5f); layout.pivot=new Vector2(.5f,.5f); layout.anchoredPosition=Vector2.zero;
            EnsureEventSystem();
            if(!IsTitle)
            {
                players=FindFirstObjectByType<PlayerManager>(); state=FindFirstObjectByType<GameStateHolder>();
                PairPlayers();
                boardHud=new BoardHud(layout,this); duelHud=new DuelHud(layout,players,Devices);
                state.SubscribeToStateChange(StateChanged);
                GateRelease();
            }
            transition=UiTheme.Box(layout,"Transition",490,380,940,280,UiTheme.Ink).rectTransform;
            transitionTitle=UiTheme.Label(transition,"Transition title","",30,26,880,100,52,UiTheme.Gold,TextAnchor.MiddleCenter);
            transitionDetail=UiTheme.Label(transition,"Transition detail","",30,145,880,102,32,UiTheme.Paper,TextAnchor.MiddleCenter);
            transition.gameObject.SetActive(false);
            menus=new MenuPanels(layout,this);
            if(IsTitle) { page=Page.Title; menus.Title(); }
            else if(!Devices.Ready) { page=Page.Connection; menus.Connection(); }
            else { page=Page.None; menus.Hide(); }
            ApplyBlock();
        }
        void EnsureEventSystem()
        {
            var system=EventSystem.current;
            if(system==null) system=new GameObject("UI EventSystem",typeof(EventSystem)).GetComponent<EventSystem>();
            var module=system.GetComponent<InputSystemUIInputModule>();
            if(module==null) module=system.gameObject.AddComponent<InputSystemUIInputModule>();
            module.enabled=false; module.AssignDefaultActions(); module.enabled=true;
            system.sendNavigationEvents=true;
        }
        public void Focus(Button button)
        {
            defaultFocus=button;
            menuNavigationAfter=Time.unscaledTime+.15f;
            EventSystem.current.firstSelectedGameObject=button.gameObject;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
        void PairPlayers()
        {
            if(players==null) return;
            Devices.Pair(players.PlayerOne.GetComponent<PlayerInput>(),0);
            Devices.Pair(players.PlayerTwo.GetComponent<PlayerInput>(),1);
            players.PlayerOne.GetComponent<Player>().LookSensitivity=Sensitivity(0);
            players.PlayerTwo.GetComponent<Player>().LookSensitivity=Sensitivity(1);
        }
        public float Sensitivity(int player) => PlayerPrefs.GetFloat("FPSshogi.Sensitivity."+player,2f);
        public void SetSensitivity(int player,float value)
        {
            PlayerPrefs.SetFloat("FPSshogi.Sensitivity."+player,Mathf.Clamp(value,.5f,5));
            PairPlayers();
        }
        static bool ControlsHeld() => (Keyboard.current!=null && (Keyboard.current.enterKey.isPressed || Keyboard.current.escapeKey.isPressed || Keyboard.current.fKey.isPressed)) ||
            (Mouse.current!=null && Mouse.current.leftButton.isPressed) || Gamepad.all.Any(p=>p.buttonSouth.isPressed||p.buttonEast.isPressed||p.startButton.isPressed);
        static bool MenuPressed() => (Keyboard.current?.escapeKey.wasPressedThisFrame??false) || Gamepad.all.Any(p=>p.startButton.wasPressedThisFrame);
        void GateRelease() { releaseGate=true; releaseAfter=Time.unscaledTime+.15f; }
        void Update()
        {
            bool changed=Devices.Refresh(IsTitle);
            if(changed) PairPlayers();
            menus.UpdateDevices();
            if(!IsTitle && !Devices.Ready && previousReady && page!=Page.Result)
            { page=Page.Connection; menus.Connection(); }
            previousReady=Devices.Ready;
            if(releaseGate && Time.unscaledTime>=releaseAfter && !ControlsHeld()) releaseGate=false;
            if(transitionRemaining>0 && page==Page.None && Devices.Ready)
            {
                transitionRemaining=Mathf.Max(0,transitionRemaining-Time.unscaledDeltaTime);
                if(transitionRemaining==0) GateRelease();
            }
            transition.gameObject.SetActive(transitionRemaining>0);
            if(transitionRemaining>0)
            {
                transitionTitle.text=transitionCaption;
                transitionDetail.text=countdown?$"{Mathf.CeilToInt(transitionRemaining)}\n左右の画面で対決します":"盤面を確認して、次の一手へ";
            }
            bool cancelMenu=menus.Visible && Gamepad.all.Any(p=>p.buttonEast.wasPressedThisFrame);
            if((MenuPressed() || cancelMenu) && Time.unscaledTime>menuNavigationAfter)
            {
                if(IsTitle) { if(page!=Page.Title) Back(); }
                else if(page==Page.None) OpenPause();
                else if(page==Page.Pause) Resume();
                else if(page!=Page.Result && page!=Page.Connection) Back();
            }
            if((Keyboard.current?.f1Key.wasPressedThisFrame??false) && page!=Page.Result) OpenHelp();
            if(EventSystem.current!=null)
            {
                EventSystem.current.sendNavigationEvents=menus.Visible && Time.unscaledTime>menuNavigationAfter;
                if(menus.Visible && EventSystem.current.currentSelectedGameObject==null && defaultFocus!=null && defaultFocus.interactable)
                    EventSystem.current.SetSelectedGameObject(defaultFocus.gameObject);
            }
            ApplyBlock();
            if(state!=null)
            {
                bool duel=state.CurrentState==State.Duel;
                boardHud.Tick(!duel); duelHud.Tick(duel);
            }
        }
        void ApplyBlock()
        {
            bool blocked=Blocked;
            if(players!=null) players.SetUiBlocked(blocked);
            Time.timeScale=blocked?0:1;
            bool lockCursor=!IsTitle && state!=null && state.CurrentState==State.Duel && !blocked;
            Cursor.lockState=lockCursor?CursorLockMode.Locked:CursorLockMode.None;
            Cursor.visible=!lockCursor;
        }
        void StateChanged(State from,State to)
        {
            if(to==State.Duel)
            {
                var pieces=FindFirstObjectByType<ShogiBoard>().GetDuelPiece();
                transitionCaption=$"1P {PiecePresentation.Name(pieces[App.Main.ShogiThings.PlayerType.PlayerOne].Type)}  対  2P {PiecePresentation.Name(pieces[App.Main.ShogiThings.PlayerType.PlayerTwo].Type)}";
                transitionRemaining=3; countdown=true;
            }
            if(to==State.DuelPlayerOneWin || to==State.DuelPlayerTwoWin)
            {
                transitionCaption=$"{(to==State.DuelPlayerOneWin?1:2)}P 決闘勝利";
                transitionRemaining=2; countdown=false;
            }
            if(to==State.DuelDraw)
            {
                transitionCaption="相打ち — 両方の駒が残ります";
                transitionRemaining=2;countdown=false;
            }
            if(to==State.PlayerOneWin || to==State.PlayerTwoWin)
            {
                transitionRemaining=0; page=Page.Result;
                menus.Result(to==State.PlayerOneWin?1:2);
            }
            ApplyBlock();
        }
        public void OpenPause() { if(IsTitle || page==Page.Result) return; page=Page.Pause; menus.Pause(); ApplyBlock(); }
        public void OpenHelp() { page=Page.Help; menus.Help(); ApplyBlock(); }
        public void OpenSettings() { page=Page.Settings; menus.Settings(); ApplyBlock(); }
        public void ConfirmReturn() { page=Page.Confirm; menus.ConfirmReturn(); ApplyBlock(); }
        public void Back() { PlayerPrefs.Save(); if(IsTitle) {page=Page.Title;menus.Title();} else OpenPause(); }
        public void Resume()
        {
            if(IsTitle || page==Page.Result) return;
            if(pendingNewMatch) { StartMatch(); return; }
            if(!Devices.Ready) { page=Page.Connection; menus.Connection(); ApplyBlock(); return; }
            page=Page.None; menus.Hide(); EventSystem.current.SetSelectedGameObject(null); GateRelease(); ApplyBlock();
        }
        public void StartMatch()
        {
            Devices.Refresh();
            if(!Devices.Ready) { pendingNewMatch=true; page=Page.Connection; menus.Connection(); ApplyBlock(); return; }
            PlayerPrefs.Save(); Time.timeScale=1; SceneManager.LoadScene("Main");
        }
        public void ReturnTitle() { PlayerPrefs.Save(); Time.timeScale=1; SceneManager.LoadScene("Title"); }
        public void Quit()
        {
            PlayerPrefs.Save(); Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #endif
        }
        void OnDestroy()
        {
            if(state!=null) state.UnsubscribeFromStateChange(StateChanged);
            if(Instance==this) { Instance=null; Time.timeScale=1; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        }
    }
}
