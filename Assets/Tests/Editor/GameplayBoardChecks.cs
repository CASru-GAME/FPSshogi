using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using App.Common.Initialize;
using App.Main.GameMaster;
using App.Main.ShogiThings;
using UnityEngine;
using State=App.Main.GameMaster.GameStateHolder.GameState;

public static class GameplayBoardChecks
{
    sealed class Fixture:IDisposable
    {
        public GameObject Root=new("BoardRuleFixture");
        public ShogiBoard Board;
        public GameStateHolder State;
        public Fixture()
        {
            State=Root.AddComponent<GameStateHolder>();Board=Root.AddComponent<ShogiBoard>();
            var refs=new ReferenceHolder();refs.RegisterInitializable(State);refs.RegisterInitializable(Board);State.Initialize(refs);Board.Initialize(refs);State.ChangeIntoPlayerOneTurn();
        }
        public void Clear(){Array.Clear(Board.GetBoard(),0,81);Hand(PlayerType.PlayerOne).Clear();Hand(PlayerType.PlayerTwo).Clear();}
        public List<PieceType> Hand(PlayerType player)=>( (Dictionary<PlayerType,List<PieceType>>)Field(Board,"capturedPieces"))[player];
        public void Dispose()=>UnityEngine.Object.DestroyImmediate(Root);
    }
    static object Field(object obj,string name)=>obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);
    static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    static IPiece Piece(PieceType type,PlayerType player)=>type switch {PieceType.King=>new King(player),PieceType.Hisya=>new Hisya(player),PieceType.Kakugyo=>new Kakugyo(player),PieceType.Kin=>new Kin(player),PieceType.Gin=>new Gin(player),PieceType.Keima=>new Keima(player),PieceType.Kyosya=>new Kyosya(player),_=>new Fuhyo(player)};
    // Independent movement oracle: hand-specified geometry, never reads Movement arrays.
    static bool Expected(PieceType type,bool promoted,int dx,int forward)
    {
        int x=Math.Abs(dx),y=Math.Abs(forward);if(x==0&&y==0)return false;
        bool gold=(forward==1&&x<=1)||(forward==0&&x==1)||(forward==-1&&x==0);
        if(promoted&&(type==PieceType.Fuhyo||type==PieceType.Kyosya||type==PieceType.Keima||type==PieceType.Gin))return gold;
        return type switch
        {
            PieceType.King=>x<=1&&y<=1,
            PieceType.Hisya=>(dx==0||forward==0)||(promoted&&x==1&&y==1),
            PieceType.Kakugyo=>x==y||(promoted&&x+y==1),
            PieceType.Kin=>gold,PieceType.Gin=>(x==1&&y==1)||(dx==0&&forward==1),
            PieceType.Keima=>x==1&&forward==2,PieceType.Kyosya=>dx==0&&forward>0,_=>dx==0&&forward==1
        };
    }
    public static IEnumerator Run(Action<string,bool> check)
    {
        using(var f=new Fixture())
        {
            check("Initial board contains 20 pieces per player",f.Board.GetBoard().Cast<IPiece>().Count(p=>p?.Player==PlayerType.PlayerOne)==20&&f.Board.GetBoard().Cast<IPiece>().Count(p=>p?.Player==PlayerType.PlayerTwo)==20);
            foreach(var player in new[]{PlayerType.PlayerOne,PlayerType.PlayerTwo})foreach(PieceType type in Enum.GetValues(typeof(PieceType)))
            {
                if(type==PieceType.None)continue;
                for(int promotion=0;promotion<(Piece(type,player).IsPromotable?2:1);promotion++)
                {
                    f.Clear();var piece=Piece(type,player);if(promotion==1)piece.Promote();f.Board.GetBoard()[4,4]=piece;
                    bool correct=true;string mismatch="";
                    for(int x=0;x<9;x++)for(int y=0;y<9;y++)
                    {
                        bool expected=Expected(type,promotion==1,x-4,player==PlayerType.PlayerOne?y-4:4-y);
                        if(f.Board.CanMove(4,4,x,y,player)!=expected){correct=false;mismatch=$" at {x},{y}";}
                    }
                    check($"Movement {player} {type} promoted={promotion}"+mismatch,correct);
                }
            }
            foreach(var player in new[]{PlayerType.PlayerOne,PlayerType.PlayerTwo})
            {
                var other=player==PlayerType.PlayerOne?PlayerType.PlayerTwo:PlayerType.PlayerOne;
                foreach(var type in new[]{PieceType.Hisya,PieceType.Kakugyo,PieceType.Kyosya})
                {
                    int dy=player==PlayerType.PlayerOne?1:-1,dx=type==PieceType.Kakugyo?1:0;
                    f.Clear();f.Board.GetBoard()[4,4]=Piece(type,player);f.Board.GetBoard()[4+dx,4+dy]=new Fuhyo(other);
                    check($"{player} {type} captures blocker but cannot cross it",f.Board.CanMove(4,4,4+dx,4+dy,player)&&!f.Board.CanMove(4,4,4+dx*2,4+dy*2,player));
                }
                f.Clear();int front=player==PlayerType.PlayerOne?1:-1;f.Board.GetBoard()[4,4]=new Keima(player);f.Board.GetBoard()[4,4+front]=new Fuhyo(other);
                check(player+" knight jumps intervening piece",f.Board.CanMove(4,4,5,4+2*front,player));
                f.Clear();f.Hand(player).Add(PieceType.Fuhyo);var pawn=new Fuhyo(player);pawn.Promote();f.Board.GetBoard()[4,4]=pawn;
                check(player+" may drop pawn on file containing only own promoted pawn",f.Board.CanDrop(4,3,PieceType.Fuhyo,player));
                f.Board.GetBoard()[4,4]=new Fuhyo(player);check(player+" cannot drop second unpromoted pawn",!f.Board.CanDrop(4,3,PieceType.Fuhyo,player));
                f.Board.GetBoard()[4,4]=new Fuhyo(other);check(player+" enemy pawn does not count as nifu",f.Board.CanDrop(4,3,PieceType.Fuhyo,player));
                f.Clear();int last=player==PlayerType.PlayerOne?8:0,penultimate=player==PlayerType.PlayerOne?7:1,third=player==PlayerType.PlayerOne?6:2;
                foreach(var type in new[]{PieceType.Fuhyo,PieceType.Kyosya,PieceType.Keima})
                {
                    f.Hand(player).Add(type);
                    check($"{player} {type} drop avoids dead rank",!f.Board.CanDrop(4,last,type,player)&&f.Board.CanDrop(4,third,type,player));
                    if(type==PieceType.Keima)check(player+" knight cannot drop on penultimate rank",!f.Board.CanDrop(4,penultimate,type,player));
                }
                f.Clear();f.Hand(player).Add(PieceType.King);check(player+" cannot drop a captured king",!f.Board.CanDrop(4,4,PieceType.King,player));
            }
            f.Clear();f.Board.GetBoard()[4,4]=new King(PlayerType.PlayerOne);f.Board.GetBoard()[4,5]=new Fuhyo(PlayerType.PlayerOne);
            check("Friendly target and off-board coordinates rejected",!f.Board.CanMove(4,4,4,5,PlayerType.PlayerOne)&&!f.Board.CanMove(-1,0,0,0,PlayerType.PlayerOne)&&!f.Board.CanMove(4,4,9,4,PlayerType.PlayerOne));
        }
        foreach(var player in new[]{PlayerType.PlayerOne,PlayerType.PlayerTwo})
        {
            using(var f=new Fixture())
            {
                f.Clear();if(player==PlayerType.PlayerTwo)f.State.ChangeIntoPlayerTwoTurn();int from=player==PlayerType.PlayerOne?5:3,to=player==PlayerType.PlayerOne?6:2;
                f.Board.GetBoard()[4,from]=new Fuhyo(player);
                check(player+" auto-promotes entering enemy camp",f.Board.MovePiece(4,from,4,to,player)==ShogiBoard.MoveResult.NormalMove&&f.Board.GetPieceAt(4,to).IsPromoted);
            }
            using(var f=new Fixture())
            {
                f.Clear();if(player==PlayerType.PlayerTwo)f.State.ChangeIntoPlayerTwoTurn();f.Hand(player).Add(PieceType.Gin);
                check(player+" drop consumes one piece and remains unpromoted",f.Board.SetPiece(4,player==PlayerType.PlayerOne?6:2,PieceType.Gin,player)==ShogiBoard.MoveResult.NormalMove&&f.Hand(player).Count==0&&!f.Board.GetPieceAt(4,player==PlayerType.PlayerOne?6:2).IsPromoted);
            }
            foreach(bool attackerWins in new[]{true,false})foreach(bool king in new[]{false,true})
            {
                using(var f=new Fixture())
                {
                    f.Clear();if(player==PlayerType.PlayerTwo)f.State.ChangeIntoPlayerTwoTurn();var other=player==PlayerType.PlayerOne?PlayerType.PlayerTwo:PlayerType.PlayerOne;
                    var attacker=new Hisya(player);var defender=Piece(king?PieceType.King:PieceType.Gin,other);if(!king)defender.Promote();
                    f.Board.GetBoard()[4,4]=attacker;f.Board.GetBoard()[4,5]=defender;
                    check($"{player} capture starts duel before changing board",f.Board.MovePiece(4,4,4,5,player)==ShogiBoard.MoveResult.DuelMove&&f.Board.GetPieceAt(4,4)==attacker&&f.Board.GetPieceAt(4,5)==defender);
                    var winner=attackerWins?player:other;
                    f.State.ChangeState(winner==PlayerType.PlayerOne?State.DuelPlayerOneWin:State.DuelPlayerTwoWin);
                    bool boardOk=f.Board.GetPieceAt(4,4)==null&&f.Board.GetPieceAt(4,5)==(attackerWins?attacker:defender);
                    bool handOk=f.Hand(winner).SequenceEqual(new[]{attackerWins?defender.Type:attacker.Type});
                    State expected=king&&attackerWins?(winner==PlayerType.PlayerOne?State.PlayerOneWin:State.PlayerTwoWin):(player==PlayerType.PlayerOne?State.PlayerTwoTurn:State.PlayerOneTurn);
                    check($"{player} attackerWins={attackerWins} kingTarget={king} resolves correct board hand and next state",boardOk&&handOk&&f.State.CurrentState==expected);
                }
            }
        }
        using(var f=new Fixture())
        {
            f.Clear();f.Board.GetBoard()[4,4]=new Fuhyo(PlayerType.PlayerTwo);
            check("Wrong-turn move is rejected without mutation",f.Board.MovePiece(4,4,4,3,PlayerType.PlayerTwo)==ShogiBoard.MoveResult.InvalidMove&&f.Board.GetPieceAt(4,4)!=null&&f.Board.GetPieceAt(4,3)==null);
        }
        using(var f=new Fixture())
        {
            f.Clear();f.Hand(PlayerType.PlayerTwo).Add(PieceType.Gin);
            check("Wrong-turn drop is rejected without spending hand",f.Board.SetPiece(4,4,PieceType.Gin,PlayerType.PlayerTwo)==ShogiBoard.MoveResult.InvalidMove&&f.Hand(PlayerType.PlayerTwo).Count==1&&f.Board.GetPieceAt(4,4)==null);
        }
        using(var f=new Fixture())
        {
            f.Clear();f.Board.GetBoard()[4,4]=new Hisya(PlayerType.PlayerOne);f.Board.GetBoard()[4,5]=new Gin(PlayerType.PlayerTwo);f.Board.MovePiece(4,4,4,5,PlayerType.PlayerOne);
            f.Board.SetPiece(8,8,PieceType.Gin,PlayerType.PlayerTwo);
            check("Invalid drop during duel does not change saved attacker",(PlayerType)Field(f.Board,"currentPlayer")==PlayerType.PlayerOne);
            check("Board cannot move during duel",f.Board.MovePiece(4,4,5,4,PlayerType.PlayerOne)==ShogiBoard.MoveResult.InvalidMove&&f.Board.GetPieceAt(4,4)!=null);
        }
        using(var f=new Fixture())
        {
            f.Clear();f.Board.GetBoard()[4,4]=new Fuhyo(PlayerType.PlayerOne);f.State.ChangeState(State.Paused);
            check("Paused board rejects mutation",f.Board.MovePiece(4,4,4,5,PlayerType.PlayerOne)==ShogiBoard.MoveResult.InvalidMove&&f.Board.GetPieceAt(4,4)!=null);
        }
        yield return .1d;
    }
}
