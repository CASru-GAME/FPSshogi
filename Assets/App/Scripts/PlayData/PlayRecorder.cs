using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using App.UI;
using UnityEngine;
using Fighter = App.Main.Player.Player;
using State = App.Main.GameMaster.GameStateHolder.GameState;

namespace App.PlayData
{
    [Serializable]
    internal sealed class MatchRecord
    {
        public int schema = 1;
        public string id, startedUtc, endedUtc, gameVersion, buildId, device1, device2;
        public string result = "unfinished";
        public double seconds;
        public int moves, drops;
        public List<DuelRecord> duels = new();
    }

    [Serializable]
    internal sealed class DuelRecord
    {
        public int number, move, attackingSeat;
        public string result = "unfinished";
        public double seconds;
        public FighterRecord[] seats;
    }

    [Serializable]
    internal sealed class FighterRecord
    {
        public string piece;
        public bool promoted;
        public int hpMax, hpLeft, attack;
        public int attacks, hits, primaryDamage, skillUses, skillHits, skillDamage, turretShots, turretHits, turretDamage;
        public int damageTaken, hitsBlocked;
        public double attackDistanceSum, hitDistanceSum;
    }

    public enum HitKind { Primary, Skill, Turret }

    // One recorder belongs to one played Main scene. Only the two current duel
    // statuses can contribute, even if a projectile from a previous duel survives.
    internal sealed class PlayRecorder
    {
        internal static PlayRecorder Current { get; private set; }
        readonly ShogiBoard board;
        readonly MatchRecord match;
        readonly string directory;
        readonly Fighter[] fighters = new Fighter[2];
        readonly PlayerStatus[] statuses = new PlayerStatus[2];
        readonly double started = Time.timeAsDouble;
        DuelRecord duel;
        double duelStarted, nextSave;
        int lastMover;
        bool closed, writeFailed;

        static string UtcNow() => DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        internal PlayRecorder(ShogiBoard board, LocalMatchDevices devices)
        {
            this.board = board;
            directory = PlayDataFiles.RecordsDirectory;
            match = new MatchRecord {
                id = Guid.NewGuid().ToString("N"), startedUtc = UtcNow(), gameVersion = Application.version,
                buildId = Application.buildGUID, device1 = devices.IsGamepad(0) ? "gamepad" : "keyboard_mouse",
                device2 = devices.IsGamepad(1) ? "gamepad" : "keyboard_mouse"
            };
            Current = this;
            Save();
        }

        internal void BoardMove(ShogiBoard source, PlayerType player, bool drop)
        {
            if (closed || source != board) return;
            match.moves++;
            if (drop) match.drops++;
            lastMover = player == PlayerType.PlayerOne ? 1 : 2;
            Save();
        }

        internal void BeginDuel(ShogiBoard source, Fighter one, Fighter two)
        {
            if (closed || board != source || duel != null) return;
            var pieces = board.GetDuelPiece();
            fighters[0] = one; fighters[1] = two;
            duel = new DuelRecord {
                number = match.duels.Count + 1, move = match.moves, attackingSeat = lastMover,
                seats = new FighterRecord[2]
            };
            for (int i = 0; i < 2; i++)
            {
                var piece = pieces[i == 0 ? PlayerType.PlayerOne : PlayerType.PlayerTwo];
                var status = statuses[i] = fighters[i].playerStatus;
                duel.seats[i] = new FighterRecord {
                    piece = PiecePresentation.Name(piece.Type), promoted = piece.IsPromoted,
                    hpMax = status.Hp.Max, hpLeft = status.Hp.Current, attack = status.AttackPoint.Current
                };
            }
            duelStarted = Time.timeAsDouble;
            match.duels.Add(duel);
            Save();
        }

        int Seat(PlayerStatus status)
        {
            if (duel == null || status == null) return -1;
            return status == statuses[0] ? 0 : status == statuses[1] ? 1 : -1;
        }

        internal void Attack(PlayerStatus status)
        {
            int seat = Seat(status);
            if (seat < 0) return;
            var row = duel.seats[seat];
            row.attacks++;
            row.attackDistanceSum += Vector3.Distance(fighters[seat].transform.position, fighters[1-seat].transform.position);
        }

        internal void Skill(PlayerStatus status)
        {
            int seat = Seat(status);
            if (seat >= 0) duel.seats[seat].skillUses++;
        }

        internal void TurretShot(PlayerStatus status)
        {
            int seat = Seat(status);
            if (seat >= 0) duel.seats[seat].turretShots++;
        }

        internal void Hit(PlayerStatus target, PlayerStatus attacker, HitKind kind, int removed, Vector3? origin)
        {
            int victim = Seat(target);
            if (victim < 0) return;
            var receiver = duel.seats[victim];
            receiver.hpLeft = target.Hp.Current;
            receiver.damageTaken += removed;
            int seat = Seat(attacker);
            if (seat != 1-victim) return;
            if (removed == 0) receiver.hitsBlocked++;
            var row = duel.seats[seat];
            switch (kind)
            {
                case HitKind.Primary:
                    row.hits++; row.primaryDamage += removed;
                    row.hitDistanceSum += Vector3.Distance(origin ?? fighters[seat].transform.position, fighters[victim].transform.position);
                    break;
                case HitKind.Skill: row.skillHits++; row.skillDamage += removed; break;
                case HitKind.Turret: row.turretHits++; row.turretDamage += removed; break;
            }
        }

        internal void StateChanged(State state)
        {
            switch (state)
            {
                case State.DuelPlayerOneWin: EndDuel("1P"); break;
                case State.DuelPlayerTwoWin: EndDuel("2P"); break;
                case State.DuelDraw: EndDuel("draw"); break;
                case State.PlayerOneWin: Close("1P"); break;
                case State.PlayerTwoWin: Close("2P"); break;
            }
        }

        void EndDuel(string result)
        {
            if (duel == null) return;
            duel.result = result;
            duel.seconds = Time.timeAsDouble - duelStarted;
            for (int i = 0; i < 2; i++) duel.seats[i].hpLeft = statuses[i].Hp.Current;
            duel = null;
            Array.Clear(fighters, 0, 2);
            Array.Clear(statuses, 0, 2);
            Save();
        }

        internal void Tick()
        {
            if ((!closed || writeFailed) && Time.realtimeSinceStartupAsDouble >= nextSave) Save();
        }

        internal bool Save()
        {
            if (closed && !writeFailed) return true;
            if (!closed)
            {
                match.seconds = Time.timeAsDouble - started;
                if (duel != null) duel.seconds = Time.timeAsDouble - duelStarted;
            }
            nextSave = Time.realtimeSinceStartupAsDouble + 5;
            try
            {
                PlayDataFiles.Save(directory, match);
                writeFailed = false;
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                if (!writeFailed) Debug.LogWarning("プレイ記録を保存できませんでした。 " + ex.Message);
                writeFailed = true;
                return false;
            }
        }

        internal void Close(string result = "interrupted")
        {
            if (!closed)
            {
                EndDuel("interrupted");
                match.result = result;
                match.endedUtc = UtcNow();
                Save();
                closed = true;
            }
            else if (writeFailed) Save();
            if (Current == this && !writeFailed) Current = null;
        }
    }
}
