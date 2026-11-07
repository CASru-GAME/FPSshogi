#if FPS_GAMEPLAY_SMOKE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using App.Main.ViewManager;
using App.PlayData;
using App.UI;
using UnityEngine;

public sealed partial class GameplayPlayerSmoke
{
    readonly bool openingRun = Environment.GetCommandLineArgs().Contains("-opening-checks");

    static GameObject BoardModel(ViewPiece view, Vector2Int square)
    {
        var field = typeof(ViewPiece).GetField("pieceObjects", BindingFlags.Instance | BindingFlags.NonPublic);
        return ((GameObject[,])field.GetValue(view))[square.x, square.y];
    }

    static bool AtSquare(Player fighter, Vector3 square)
    {
        Bounds body = fighter.GetComponent<Collider>().bounds;
        Vector3 feet = new Vector3(body.center.x, body.min.y, body.center.z);
        return Vector2.Distance(new Vector2(feet.x, feet.z), new Vector2(square.x, square.z)) < .08f &&
            Mathf.Abs(feet.y - square.y) < .14f;
    }

    static bool LookingAt(Player fighter, Player opponent)
    {
        var view = fighter.GetComponentInChildren<Camera>().transform;
        return Vector3.Dot(view.forward, (opponent.transform.position - fighter.transform.position).normalized) > .999f;
    }

    IEnumerator CheckOpening(ShogiBoard board, ViewPiece view, IPiece attacker, Vector2Int from, Vector2Int to, Vector2Int bystander)
    {
        var attackingPlayer = attacker.Player == PlayerType.PlayerOne ? One() : Two();
        var defendingPlayer = attacker.Player == PlayerType.PlayerOne ? Two() : One();
        var home = view.boardCellPositions[from.x, from.y];
        var away = view.boardCellPositions[to.x, to.y];
        string label = attacker.Player + " " + attacker.Type + " " + from + " to " + to;
        Check(board.MovePiece(from.x, from.y, to.x, to.y, attacker.Player) == ShogiBoard.MoveResult.DuelMove, label + " starts a legal capture");
        Physics.SyncTransforms();
        Check(GameSessionUI.BlocksGameplay && AtSquare(attackingPlayer, home) && AtSquare(defendingPlayer, away), label + " countdown places each fighter on its original square");
        yield return new WaitForSecondsRealtime(3.6f);
        float distance = Vector3.Distance(attackingPlayer.transform.position, defendingPlayer.transform.position);
        Check(!GameSessionUI.BlocksGameplay && AtSquare(attackingPlayer, home) && AtSquare(defendingPlayer, away) &&
            Mathf.Abs(distance - Vector3.Distance(home, away)) < .08f, $"{label} grounded at GO, distance {distance:F2} m, feet {attackingPlayer.GetComponent<Collider>().bounds.min.y:F2}/{home.y:F2}");
        Check(LookingAt(attackingPlayer, defendingPlayer) && LookingAt(defendingPlayer, attackingPlayer), label + " both cameras face the other square");
        Check(BoardModel(view, from)?.activeInHierarchy == false && BoardModel(view, to)?.activeInHierarchy == false &&
            BoardModel(view, bystander)?.activeInHierarchy == true, label + " only the two duelling board models leave the field");
    }

    IEnumerator CheckDuelOpenings()
    {
        var board = Find<ShogiBoard>();
        var grid = board.GetBoard();
        var view = Find<ViewPiece>();
        var firstSeat = PlayerType.PlayerOne;
        var secondSeat = PlayerType.PlayerTwo;

        board.MovePiece(0,2,0,3,firstSeat);
        board.MovePiece(0,6,0,5,secondSeat);
        board.MovePiece(0,3,0,4,firstSeat);
        var from = new Vector2Int(0,5);
        var to = new Vector2Int(0,4);
        var other = new Vector2Int(1,2);
        yield return CheckOpening(board, view, grid[from.x,from.y], from, to, other);
        Shot("adjacent-pawns");
        One().playerStatus.TakeDamage(10000);
        Two().playerStatus.TakeDamage(10000);
        yield return new WaitForSecondsRealtime(2.6f);
        Check(BoardModel(view, from)?.activeInHierarchy == true && BoardModel(view, to)?.activeInHierarchy == true,
            "Draw restores both pawns on the board");

        other = new Vector2Int(1,0);
        var captures = new (IPiece piece, Vector2Int from, Vector2Int to)[] {
            (new Hisya(firstSeat), new Vector2Int(0,1), new Vector2Int(8,1)),
            (new Kakugyo(secondSeat), new Vector2Int(8,8), new Vector2Int(0,0)),
            (new Keima(firstSeat), new Vector2Int(4,3), new Vector2Int(5,5))
        };
        foreach (var capture in captures)
        {
            var attacker = capture.piece;
            from = capture.from;
            to = capture.to;
            Array.Clear(grid, 0, grid.Length);
            grid[other.x,other.y] = new King(firstSeat);
            grid[7,8] = new King(secondSeat);
            grid[from.x,from.y] = attacker;
            grid[to.x,to.y] = new Kyosya(attacker.Player == firstSeat ? secondSeat : firstSeat);
            yield return CheckOpening(board, view, attacker, from, to, other);
            if (attacker.Type == PieceType.Kakugyo)
            {
                Shot("corner-bishop");
                int bishopHp = Two().playerStatus.Hp.Current, lanceHp = One().playerStatus.Hp.Current;
                yield return FireOnce(One());
                yield return FireOnce(Two());
                Check(Two().playerStatus.Hp.Current == bishopHp && One().playerStatus.Hp.Current < lanceHp,
                    "From opposite board corners only the bishop can hit with its opening shot");
            }
            Two().playerStatus.TakeDamage(10000);
            yield return new WaitForSecondsRealtime(2.6f);
            Check(BoardModel(view, from) == null && BoardModel(view, to)?.activeInHierarchy == true && grid[to.x,to.y].Player == firstSeat,
                "Resolved " + attacker.Type + " capture shows only the survivor at the destination");
        }
        string oldId = Guid.NewGuid().ToString("N");
        File.WriteAllText(Path.Combine(PlayDataFiles.RecordsDirectory, oldId + ".json"),
            "{\"schema\":1,\"id\":\"" + oldId + "\",\"duels\":[{\"seats\":[{},{}]}]}");
        var export = PlayDataFiles.Export(PlayDataFiles.Root);
        var csv = File.ReadAllLines(Path.Combine(export.directory, "duels.csv"));
        Check(csv.Count(row => row.EndsWith(",\"9四\",\"9五\",\"5.222\"")) == 2 &&
            csv.Count(row => row.Contains(",\"1一\",\"9九\",\"59.")) == 2,
            "Opening squares and distance are recorded for both seats in the local export");
        Check(export.skipped == 0 && csv.Count(row => row.StartsWith("\"" + oldId + "\",") && row.EndsWith(",\"\",\"\",\"\"")) == 2,
            "An old record exports with empty opening fields");
    }
}
#endif
