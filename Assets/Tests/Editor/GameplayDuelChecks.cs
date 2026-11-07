using System;
using System.Collections;
using System.Linq;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using State=App.Main.GameMaster.GameStateHolder.GameState;

public static class GameplayDuelChecks
{
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static Player One()=>Find<PlayerManager>().PlayerOne.GetComponent<Player>();
    static Player Two()=>Find<PlayerManager>().PlayerTwo.GetComponent<Player>();
    static IEnumerator ToggleZoom(Gamepad pad)
    {
        InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=1});yield return .14d;
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return .18d;
    }
    static IEnumerator CheckZoomTurn(Player actor, Player other, int slot, float expectedRatio, Action<string,bool> check)
    {
        var pad = actor.GetComponent<PlayerInput>().devices.OfType<Gamepad>().Single();
        var camera = actor.GetComponentInChildren<Camera>();
        var otherCamera = other.GetComponentInChildren<Camera>();
        var otherRotation = otherCamera.transform.rotation;
        float originalFov = camera.fieldOfView;
        var input = new GamepadState { rightStick = new Vector2(.38f, .1f) };
        var rates = new Vector2[3];
        for (int phase = 0; phase < rates.Length; phase++)
        {
            input.leftTrigger = phase == 0 ? 0 : 1;
            InputSystem.QueueStateEvent(pad, input);
            yield return .12d;
            input.leftTrigger = 0;
            InputSystem.QueueStateEvent(pad, input);
            yield return .08d;
            float yaw = actor.transform.eulerAngles.y;
            float pitch = camera.transform.localEulerAngles.x;
            float started = Time.time;
            yield return .24d;
            rates[phase] = new Vector2(Mathf.DeltaAngle(yaw, actor.transform.eulerAngles.y),
                Mathf.DeltaAngle(pitch, camera.transform.localEulerAngles.x)) / (Time.time - started);
            float expectedFov = phase == 1 ? originalFov / 2 : originalFov;
            check($"{slot+1}P LT selects the correct FOV at phase {phase}", Mathf.Approximately(camera.fieldOfView, expectedFov));
        }
        InputSystem.QueueStateEvent(pad, new GamepadState());
        var ratio = new Vector2(rates[1].x / rates[0].x, rates[1].y / rates[0].y);
        check($"{slot+1}P held stick zoom yaw/pitch ratio {ratio:F3} matches {expectedRatio:F3}",
            Mathf.Abs(ratio.x - expectedRatio) < .05f && Mathf.Abs(ratio.y - expectedRatio) < .05f);
        check($"{slot+1}P unzoom restores the same stick speed on both axes", (rates[2] - rates[0]).magnitude < rates[0].magnitude * .08f);
        check($"{slot+1}P zoom affects only its own camera", Quaternion.Angle(otherCamera.transform.rotation, otherRotation) < .05f);
        yield return .12d;
    }
    static void Start(PlayerType attacker)
    {
        var b=Find<ShogiBoard>();var grid=b.GetBoard();Array.Clear(grid,0,81);
        grid[4,4]=new Hisya(attacker);grid[4,5]=new Gin(attacker==PlayerType.PlayerOne?PlayerType.PlayerTwo:PlayerType.PlayerOne);
        if(attacker==PlayerType.PlayerTwo)Find<GameStateHolder>().ChangeIntoPlayerTwoTurn();
        if(b.MovePiece(4,4,4,5,attacker)!=ShogiBoard.MoveResult.DuelMove)throw new InvalidOperationException("Duel fixture failed to start");
    }
    public static IEnumerator Run(Action<string,bool> check)
    {
        SceneManager.LoadScene("Main");yield return .8d;Start(PlayerType.PlayerOne);yield return 3.5d;
        const string fovKey = "FPSshogi.FieldOfView.0";
        bool hadFov = PlayerPrefs.HasKey(fovKey); float savedFov = PlayerPrefs.GetFloat(fovKey);
        try
        {
            var session = App.UI.GameSessionUI.Instance;
            var camera = One().GetComponentInChildren<Camera>();
            var otherCamera = Two().GetComponentInChildren<Camera>();
            float otherFov = otherCamera.fieldOfView;
            var pad = One().GetComponent<PlayerInput>().devices.OfType<Gamepad>().Single();
            var toggle = ToggleZoom(pad);while(toggle.MoveNext())yield return toggle.Current;
            session.OpenPause();session.OpenSettings();yield return .2d;
            var fov = GameObject.Find("Field of view slider 0")?.GetComponent<Slider>();
            const int wideFov = 140;
            if (fov != null) fov.value = wideFov;
            check("Changing normal FOV during zoom immediately updates only that player's zoomed camera",
                camera.fieldOfView == wideFov / 2f && otherCamera.fieldOfView == otherFov);
            session.Back();session.Resume();yield return .4d;
            toggle=ToggleZoom(pad);while(toggle.MoveNext())yield return toggle.Current;
            check("Releasing zoom restores the FOV selected during zoom", camera.fieldOfView == wideFov);
            toggle=ToggleZoom(pad);while(toggle.MoveNext())yield return toggle.Current;
            check("Entering zoom again uses the new FOV", camera.fieldOfView == wideFov / 2f);
            session.OpenPause();Two().playerStatus.TakeDamage(10000);yield return .2d;
            check("Paused duel defers death resolution",Find<GameStateHolder>().CurrentState==State.Duel);
            GameplayRegressionRunner.Shot("paused-duel");
            session.Resume();yield return 2.7d;
            check("Resuming resolves pending death and returns to next turn",Find<GameStateHolder>().CurrentState==State.PlayerTwoTurn);
            check("Ending a zoomed duel restores the latest normal FOV", camera.fieldOfView == wideFov && !camera.enabled);
        }
        finally
        {
            if (hadFov) PlayerPrefs.SetFloat(fovKey,savedFov); else PlayerPrefs.DeleteKey(fovKey);
            PlayerPrefs.Save();
        }
        foreach(var attacker in new[]{PlayerType.PlayerOne,PlayerType.PlayerTwo})
        {
            SceneManager.LoadScene("Main");yield return .8d;Start(attacker);yield return 3.5d;
            bool firstAttacks = attacker == PlayerType.PlayerOne;
            var actor = firstAttacks ? One() : Two();
            var session = App.UI.GameSessionUI.Instance;
            int slot = firstAttacks ? 0 : 1;
            float storedZoom = session.ZoomSensitivity(slot);
            string zoomKey = "FPSshogi.GamepadZoomSensitivity." + slot;
            bool hadZoom = PlayerPrefs.HasKey(zoomKey);
            float requestedZoom = firstAttacks ? .8f : .5f;
            try
            {
                session.SetZoomSensitivity(slot, requestedZoom);
                var zoom = CheckZoomTurn(actor, firstAttacks ? Two() : One(), slot, requestedZoom / session.Sensitivity(slot), check);
                while (zoom.MoveNext()) yield return zoom.Current;
            }
            finally
            {
                session.SetZoomSensitivity(slot, storedZoom);
                if (!hadZoom) PlayerPrefs.DeleteKey(zoomKey);
            }
            var board=Find<ShogiBoard>();var a=board.GetPieceAt(4,4);var d=board.GetPieceAt(4,5);
            One().playerStatus.TakeDamage(10000);Two().playerStatus.TakeDamage(10000);yield return .2d;
            check(attacker+" simultaneous deaths preserve both pieces",board.GetPieceAt(4,4)==a&&board.GetPieceAt(4,5)==d);
            check(attacker+" simultaneous deaths award no captured piece",board.GetCapturedPieces(PlayerType.PlayerOne).Count==0&&board.GetCapturedPieces(PlayerType.PlayerTwo).Count==0);
            check(attacker+" simultaneous deaths end attacker turn",Find<GameStateHolder>().CurrentState==(attacker==PlayerType.PlayerOne?State.PlayerTwoTurn:State.PlayerOneTurn));
            GameplayRegressionRunner.Shot("draw-"+attacker);yield return 2.5d;
            check(attacker+" draw returns to usable board",!App.UI.GameSessionUI.BlocksGameplay&&!One().GetComponentInChildren<Camera>().enabled&&One().GetComponentInChildren<PlayerWeapon>()==null);
        }
        SceneManager.LoadScene("Main");yield return .8d;Start(PlayerType.PlayerOne);yield return 3.5d;
        var orderedBoard=Find<ShogiBoard>();var orderedAttacker=orderedBoard.GetPieceAt(4,4);var orderedDefender=orderedBoard.GetPieceAt(4,5);
        var sources=new GameObject("Same-frame damage sources");
        sources.AddComponent<GameplayDamageBeforeDuel>().target=One();
        sources.AddComponent<GameplayDamageAfterDuel>().target=Two();
        yield return .2d;
        check("Lethal hits from different Update orders in one frame resolve as a draw",orderedBoard.GetPieceAt(4,4)==orderedAttacker&&orderedBoard.GetPieceAt(4,5)==orderedDefender&&orderedBoard.GetCapturedPieces(PlayerType.PlayerTwo).Count==0);
        UnityEngine.Object.Destroy(sources);yield return 2.5d;
    }
}
