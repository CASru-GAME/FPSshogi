using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using State=App.Main.GameMaster.GameStateHolder.GameState;

namespace App.UI
{
    public sealed class GameSessionUI : MonoBehaviour
    {
        public static GameSessionUI Instance { get; private set; }
        static LocalMatchDevices matchDevices;
        public LocalMatchDevices Devices => matchDevices;
        public RoomPresentation Presentation { get; private set; }
        public bool IsTitle { get; private set; }
        public bool ConnectionStartsMatch => IsTitle || pendingNewMatch;
        public static bool BlocksGameplay => Instance!=null && Instance.Blocked;
        public PieceType GuidePiece { get; set; }=PieceType.Hisya;
        public bool GuidePromoted { get; set; }
        public bool ShowHpNumbers { get=>PlayerPrefs.GetInt("FPSshogi.HpNumbers",0)==1;set=>PlayerPrefs.SetInt("FPSshogi.HpNumbers",value?1:0); }
        public string MenuOwnerLabel => menuOwner==null?"":Devices.PlayerFor(menuOwner) is int p && p>=0?$"{p+1}P 操作中":menuOwner is Gamepad?"コントローラー 操作中":"キーマウ 操作中";
        enum Page { None, Title, Pause, Help, Controls, Pieces, Settings, Connection, Result, Confirm, Selection, Loading }
        struct PageFrame { public Page page;public string focus; }
        readonly Stack<PageFrame> history=new();
        Page page;
        MenuPanels menus;
        BoardHud boardHud;
        DuelHud duelHud;
        DuelTransition transition;
        PlayerManager players;
        GameStateHolder state;
        InputSystemUIInputModule module;
        InputDevice menuOwner, requestedMenuDevice;
        float transitionRemaining;
        bool releaseGate, previousReady, pendingNewMatch, waitingReconnect, quitting;
        float releaseAfter, menuNavigationAfter;
        Selectable defaultFocus;
        bool selectionInputArmed;
        float selectionStartAt=-1;
        int keyboardSelectionFrame=-1, winner=1;
        bool loading;
        string loadError;
        bool Blocked => !IsTitle && (page!=Page.None || transitionRemaining>0 || !Devices.Ready || releaseGate || waitingReconnect);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance=null; matchDevices=null; }
        void Awake()
        {
            Instance=this;IsTitle=SceneManager.GetActiveScene().name=="Title";
            if(IsTitle || matchDevices==null) matchDevices=new LocalMatchDevices();
            Devices.Refresh();previousReady=Devices.Ready;
            foreach(var old in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))old.gameObject.SetActive(false);
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=300;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();gameObject.AddComponent<UiFeedback>();Presentation=gameObject.AddComponent<RoomPresentation>();
            var layout=UiTheme.Rect(transform,"Layout",0,0,1920,1080);
            layout.anchorMin=layout.anchorMax=layout.pivot=new Vector2(.5f,.5f);layout.anchoredPosition=Vector2.zero;
            EnsureEventSystem();
            if(!IsTitle){
                players=FindFirstObjectByType<PlayerManager>();state=FindFirstObjectByType<GameStateHolder>();PairPlayers();
                boardHud=new BoardHud(layout,this);duelHud=new DuelHud((RectTransform)transform,players,Devices);
                state.SubscribeToStateChange(StateChanged);GateRelease();
            }else Presentation.SetTitleRoom();
            transition=new DuelTransition(layout,Presentation,Devices);menus=new MenuPanels(layout,this);
            if(IsTitle){page=Page.Title;RenderPage();}
            else if(!Devices.Ready)StartMatch();else{page=Page.None;RenderPage();}
            ApplyBlock();
        }
        void EnsureEventSystem()
        {
            var system=EventSystem.current;if(system==null)system=new GameObject("UI EventSystem",typeof(EventSystem)).GetComponent<EventSystem>();
            module=system.GetComponent<InputSystemUIInputModule>();if(module==null)module=system.gameObject.AddComponent<InputSystemUIInputModule>();
            module.enabled=false;module.AssignDefaultActions();module.enabled=true;system.sendNavigationEvents=true;
        }
        public void Focus(Selectable selectable)
        {
            defaultFocus=selectable;menuNavigationAfter=Time.unscaledTime+.15f;
            EventSystem.current.firstSelectedGameObject=selectable.gameObject;
            EventSystem.current.SetSelectedGameObject(null);EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
        void SetMenuDevices()
        {
            if(page==Page.None || page==Page.Title || page==Page.Selection || page==Page.Connection || page==Page.Loading)menuOwner=null;
            if(module==null||module.actionsAsset==null)return;
            if(menuOwner==null){module.actionsAsset.devices=null;return;}
            int slot=Devices.PlayerFor(menuOwner);
            module.actionsAsset.devices=slot>=0?Devices.DevicesFor(slot):menuOwner is Gamepad?new[]{menuOwner}:new InputDevice[]{Keyboard.current,Mouse.current}.Where(d=>d!=null).ToArray();
        }
        void ClaimMenu()
        {
            var requested=requestedMenuDevice;requestedMenuDevice=null;
            if(menuOwner!=null)return;
            menuOwner=requested;
            if(menuOwner==null)menuOwner=Gamepad.all.FirstOrDefault(p=>p.buttonSouth.wasPressedThisFrame||p.startButton.wasPressedThisFrame||p.selectButton.wasPressedThisFrame);
            if(menuOwner==null)menuOwner=Keyboard.current;requestedMenuDevice=null;
        }
        bool AcceptMenu(InputDevice device)=>device!=null&&(menuOwner==null||menuOwner==device||Devices.PlayerFor(menuOwner)>=0&&Devices.PlayerFor(device)==Devices.PlayerFor(menuOwner));
        void PairPlayers()
        {
            if(players==null)return;
            Devices.Pair(players.PlayerOne.GetComponent<PlayerInput>(),0);Devices.Pair(players.PlayerTwo.GetComponent<PlayerInput>(),1);
            players.PlayerOne.GetComponent<Player>().LookSensitivity=Sensitivity(0);players.PlayerTwo.GetComponent<Player>().LookSensitivity=Sensitivity(1);
        }
        public float Sensitivity(int player)=>PlayerPrefs.GetFloat("FPSshogi.Sensitivity."+player,2f);
        public void SetSensitivity(int player,float value){PlayerPrefs.SetFloat("FPSshogi.Sensitivity."+player,Mathf.Clamp(value,.5f,5));PairPlayers();}
        public void ResetSettings(){SetSensitivity(0,2);SetSensitivity(1,2);ShowHpNumbers=false;PlayerPrefs.Save();}
        bool GateDevice(InputDevice device)=>device!=null&&(IsTitle||pendingNewMatch||Devices.PlayerFor(device)>=0);
        bool ControlsHeld()
        {
            var kb=Keyboard.current;
            bool keyboard=GateDevice(kb)&&(kb.enterKey.isPressed||kb.escapeKey.isPressed||kb.fKey.isPressed||kb.f1Key.isPressed||kb.wKey.isPressed||kb.aKey.isPressed||kb.sKey.isPressed||kb.dKey.isPressed||kb.eKey.isPressed||kb.cKey.isPressed||kb.spaceKey.isPressed);
            return keyboard||(GateDevice(Mouse.current)&&(Mouse.current.leftButton.isPressed||Mouse.current.rightButton.isPressed))||
                Gamepad.all.Any(p=>GateDevice(p)&&(p.buttonSouth.isPressed||p.buttonEast.isPressed||p.buttonWest.isPressed||p.startButton.isPressed||p.selectButton.isPressed||p.leftTrigger.ReadValue()>.2f||p.rightTrigger.ReadValue()>.2f||p.leftStick.ReadValue().sqrMagnitude>.04f||p.rightStick.ReadValue().sqrMagnitude>.04f||p.dpad.ReadValue().sqrMagnitude>.04f));
        }
        void GateRelease(){releaseGate=true;releaseAfter=Time.unscaledTime+.15f;}
        void ArmSelection(){selectionInputArmed=false;selectionStartAt=-1;menuNavigationAfter=Time.unscaledTime+.2f;keyboardSelectionFrame=-1;}
        void Update()
        {
            bool changed=Devices.Refresh(waitingReconnect);
            if(changed){PairPlayers();if(waitingReconnect){ArmSelection();if(!Devices.Ready)Devices.BeginSelection(true);}}
            if(!IsTitle&&!Devices.Ready&&previousReady&&!pendingNewMatch&&page!=Page.Result&&page!=Page.Loading){
                waitingReconnect=true;Devices.BeginSelection(true);history.Clear();page=Page.Connection;ArmSelection();RenderPage();
            }
            previousReady=Devices.Ready;menus.UpdateDevices();
            if(menuOwner!=null&&(!menuOwner.added||!menuOwner.enabled)){
                string focus=EventSystem.current?.currentSelectedGameObject?.name;menuOwner=null;RenderPage(focus);
            }
            if(releaseGate&&Time.unscaledTime>=releaseAfter&&!ControlsHeld())releaseGate=false;
            if(transitionRemaining>0&&page==Page.None&&Devices.Ready){transitionRemaining=Mathf.Max(0,transitionRemaining-Time.unscaledDeltaTime);if(transitionRemaining==0)GateRelease();}
            transition.Tick(transitionRemaining,page==Page.None);
            if(page==Page.Selection){if(PollSelection())return;}else if(page==Page.Connection)PollConnection();
            if(Time.unscaledTime>menuNavigationAfter&&!loading){
                InputDevice cancel=null,help=null;
                if(AcceptMenu(Keyboard.current)){
                    if(Keyboard.current.escapeKey.wasPressedThisFrame)cancel=Keyboard.current;
                    if(Keyboard.current.f1Key.wasPressedThisFrame)help=Keyboard.current;
                }
                foreach(var pad in Gamepad.all)if(AcceptMenu(pad)){
                    if(pad.startButton.wasPressedThisFrame||(page!=Page.None&&pad.buttonEast.wasPressedThisFrame))cancel=pad;
                    if(pad.selectButton.wasPressedThisFrame)help=pad;
                }
                if(cancel!=null&&page!=Page.Selection&&page!=Page.Connection&&page!=Page.Result){
                    if(page==Page.None){if(Devices.PlayerFor(cancel)>=0){requestedMenuDevice=cancel;OpenPause();}}
                    else if(page==Page.Pause)Resume();else if(page!=Page.Title)Back();
                }
                if(help!=null&&page!=Page.Result&&page!=Page.Connection&&page!=Page.Confirm&&page!=Page.Help&&page!=Page.Controls&&page!=Page.Pieces){requestedMenuDevice=help;OpenHelp();}
            }
            if(EventSystem.current!=null){
                bool navigation=menus.Visible&&page!=Page.Selection&&page!=Page.Connection&&!loading;
                EventSystem.current.sendNavigationEvents=navigation&&Time.unscaledTime>menuNavigationAfter;
                if(navigation&&EventSystem.current.currentSelectedGameObject==null&&defaultFocus!=null&&defaultFocus.interactable)EventSystem.current.SetSelectedGameObject(defaultFocus.gameObject);
            }
            ApplyBlock();if(state!=null){bool duel=state.CurrentState==State.Duel;boardHud.Tick(!duel);duelHud.Tick(duel);}
        }
        void ApplyBlock()
        {
            bool blocked=Blocked;if(players!=null)players.SetUiBlocked(blocked);Time.timeScale=blocked?0:1;
            bool locked=!IsTitle&&state!=null&&state.CurrentState==State.Duel&&!blocked;
            Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked;
        }
        void StateChanged(State from,State to)
        {
            if(to==State.Duel){var pieces=FindFirstObjectByType<ShogiBoard>().GetDuelPiece();transition.Duel(pieces[PlayerType.PlayerOne],pieces[PlayerType.PlayerTwo]);transitionRemaining=3;}
            if(to==State.DuelPlayerOneWin||to==State.DuelPlayerTwoWin){transition.Result(to==State.DuelPlayerOneWin?0:1);transitionRemaining=2;}
            if(to==State.DuelDraw){transition.Result(-1);transitionRemaining=2;}
            if(transitionRemaining>0&&(to==State.PlayerOneTurn||to==State.PlayerTwoTurn))transition.NextTurn(to==State.PlayerOneTurn?1:2);
            if(to==State.PlayerOneWin||to==State.PlayerTwoWin){transitionRemaining=0;winner=to==State.PlayerOneWin?1:2;history.Clear();page=Page.Result;RenderPage();}
            ApplyBlock();
        }
        void RenderPage(string focus=null)
        {
            SetMenuDevices();defaultFocus=null;
            switch(page){
                case Page.None:menus.Hide();break;case Page.Title:menus.Title();break;case Page.Pause:menus.Pause();break;
                case Page.Help:menus.Help();break;case Page.Controls:menus.Help(true);break;case Page.Pieces:menus.Pieces();break;
                case Page.Settings:menus.Settings();break;case Page.Selection:menus.Selection();break;case Page.Connection:menus.Connection();break;
                case Page.Result:menus.Result(winner);break;case Page.Confirm:menus.ConfirmReturn(quitting);break;case Page.Loading:menus.Loading(loadError);break;
            }
            if(page==Page.Selection||page==Page.Connection||page==Page.None||page==Page.Loading&&loading){defaultFocus=null;EventSystem.current.SetSelectedGameObject(null);}
            else menus.FocusNamed(focus);
            ApplyBlock();
        }
        void Open(Page target)
        {
            if(loading||page==target)return;ClaimMenu();
            history.Push(new PageFrame{page=page,focus=EventSystem.current?.currentSelectedGameObject?.name});page=target;RenderPage();
        }
        public void OpenPause(){if(IsTitle||page==Page.Result||waitingReconnect)return;Open(Page.Pause);}
        public void OpenHelp()=>Open(Page.Help);
        public void SetHelpTab(bool controls){page=controls?Page.Controls:Page.Help;RenderPage(controls?"Controls tab":"Basics tab");}
        public void OpenPieces()=>Open(Page.Pieces);
        public void OpenSettings()=>Open(Page.Settings);
        public void ConfirmReturn(){quitting=false;Open(Page.Confirm);}
        public void ConfirmQuit(){quitting=true;Open(Page.Confirm);}
        public void Back()
        {
            if(loading)return;if(page==Page.Loading){ReturnTitle();return;}if(page==Page.Selection){CancelSelection();return;}PlayerPrefs.Save();UiFeedback.Play(true);
            if(history.Count==0){if(IsTitle){page=Page.Title;RenderPage();}else Resume();return;}
            var previous=history.Pop();page=previous.page;
            if(page==Page.None){Resume();return;}
            if(page==Page.Selection||page==Page.Connection)ArmSelection();RenderPage(previous.focus);
        }
        public void Resume()
        {
            if(IsTitle||page==Page.Result||page==Page.Selection||loading)return;
            if(pendingNewMatch){StartMatch();return;}
            if(!Devices.Ready||waitingReconnect&&!Devices.AllPrepared){page=Page.Connection;RenderPage();return;}
            waitingReconnect=false;history.Clear();page=Page.None;RenderPage();GateRelease();ApplyBlock();
        }
        public void StartMatch()
        {
            if(page==Page.Selection||loading)return;
            Devices.BeginSelection(!IsTitle);waitingReconnect=false;history.Clear();page=Page.Selection;pendingNewMatch=true;ArmSelection();RenderPage();
        }
        public void CancelSelection()
        {
            if(page!=Page.Selection)return;
            Devices.BeginSelection(false);pendingNewMatch=false;selectionStartAt=-1;history.Clear();
            if(IsTitle){page=Page.Title;RenderPage();}else ReturnTitle();
        }
        public void KeyboardSelectionAction(){int slot=Devices.PlayerFor(Keyboard.current);SelectionAction(Keyboard.current,slot>=0&&Devices.IsPrepared(slot));}
        void SelectionAction(InputDevice device,bool cancel)
        {
            if(page!=Page.Selection||!selectionInputArmed||device==null)return;
            if(device is Keyboard){if(keyboardSelectionFrame==Time.frameCount)return;keyboardSelectionFrame=Time.frameCount;}
            if(cancel){if(!Devices.Cancel(device)&&!Devices.HasParticipants){CancelSelection();return;}}else Devices.Confirm(device);
            UiFeedback.Play(cancel);menus.UpdateDevices();
        }
        bool PollSelection()
        {
            if(!selectionInputArmed){if(Time.unscaledTime>=menuNavigationAfter&&!ControlsHeld())selectionInputArmed=true;return false;}
            foreach(var pad in Gamepad.all){if(pad.buttonEast.wasPressedThisFrame)SelectionAction(pad,true);else if(pad.buttonSouth.wasPressedThisFrame)SelectionAction(pad,false);}
            var keyboard=Keyboard.current;if(keyboard!=null){if(keyboard.escapeKey.wasPressedThisFrame)SelectionAction(keyboard,true);else if(keyboard.enterKey.wasPressedThisFrame)SelectionAction(keyboard,false);}
            if(page!=Page.Selection)return false;menus.UpdateDevices();
            if(!Devices.AllPrepared){selectionStartAt=-1;return false;}if(selectionStartAt<0)selectionStartAt=Time.unscaledTime+1;
            if(Time.unscaledTime<selectionStartAt)return false;
            Devices.Refresh();if(!Devices.AllPrepared){selectionStartAt=-1;return false;}
            pendingNewMatch=false;LoadScene("Main");return true;
        }
        public void ConfirmReconnect(InputDevice device)
        {
            if(page!=Page.Connection||!selectionInputArmed||Devices.PlayerFor(device)<0)return;
            Devices.Confirm(device);UiFeedback.Play(false);menus.UpdateDevices();if(Devices.AllPrepared)Resume();
        }
        void PollConnection()
        {
            if(!selectionInputArmed){if(Time.unscaledTime>=menuNavigationAfter&&!ControlsHeld())selectionInputArmed=true;return;}
            foreach(var pad in Gamepad.all){if(pad.buttonSouth.wasPressedThisFrame)ConfirmReconnect(pad);else if(pad.buttonEast.wasPressedThisFrame)CancelReconnect(pad);}
            var kb=Keyboard.current;if(kb!=null){if(kb.enterKey.wasPressedThisFrame)ConfirmReconnect(kb);else if(kb.escapeKey.wasPressedThisFrame)CancelReconnect(kb);}
        }
        void CancelReconnect(InputDevice device)
        {
            if(page!=Page.Connection)return;int slot=Devices.PlayerFor(device);if(slot<0)return;
            if(Devices.IsPrepared(slot))Devices.Cancel(device);else{requestedMenuDevice=device;ConfirmReturn();}
        }
        void LoadScene(string name){if(loading)return;loading=true;loadError=null;history.Clear();page=Page.Loading;RenderPage();PlayerPrefs.Save();StartCoroutine(Load(name));}
        IEnumerator Load(string name)
        {
            yield return null;AsyncOperation operation=null;string error=null;
            try{operation=SceneManager.LoadSceneAsync(name);}catch(System.Exception e){error=e.Message;}
            if(operation==null){loading=false;loadError=error??"読み込みを開始できませんでした。";RenderPage();yield break;}
            while(!operation.isDone)yield return null;
        }
        public void ReturnTitle()=>LoadScene("Title");
        public void Quit(){PlayerPrefs.Save();Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #endif
        }
        void OnDestroy()
        {
            if(state!=null)state.UnsubscribeFromStateChange(StateChanged);
            if(Instance==this){Instance=null;Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        }
    }
}
