using System;
using System.Collections;
using System.Linq;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using UnityEngine;
using UnityEngine.SceneManagement;
using State=App.Main.GameMaster.GameStateHolder.GameState;

public static class GameplayDuelChecks
{
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static Player One()=>Find<PlayerManager>().PlayerOne.GetComponent<Player>();
    static Player Two()=>Find<PlayerManager>().PlayerTwo.GetComponent<Player>();
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
        App.UI.GameSessionUI.Instance.OpenPause();Two().playerStatus.TakeDamage(10000);yield return .2d;
        check("Paused duel defers death resolution",Find<GameStateHolder>().CurrentState==State.Duel);
        GameplayRegressionRunner.Shot("paused-duel");
        App.UI.GameSessionUI.Instance.Resume();yield return 2.7d;
        check("Resuming resolves pending death and returns to next turn",Find<GameStateHolder>().CurrentState==State.PlayerTwoTurn);
        foreach(var attacker in new[]{PlayerType.PlayerOne,PlayerType.PlayerTwo})
        {
            SceneManager.LoadScene("Main");yield return .8d;Start(attacker);yield return 3.5d;
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
