using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace App.PlayData
{
    internal static class PlayDataFiles
    {
        static readonly CultureInfo Numbers = CultureInfo.InvariantCulture;
        static readonly UTF8Encoding ExcelUtf8 = new UTF8Encoding(true);
        static readonly UTF8Encoding JsonUtf8 = new UTF8Encoding(false);
#if FPS_GAMEPLAY_SMOKE || FPS_AUDIO_SMOKE || FPS_SELECTION_SMOKE || FPS_VISUAL_SMOKE
        static readonly string TestRun = Guid.NewGuid().ToString("N");
        internal static string Root => Path.Combine(Application.temporaryCachePath, "PlayDataTests", TestRun);
#elif UNITY_EDITOR
        internal static string Root => Path.Combine(Application.persistentDataPath, "PlayData-Editor");
#else
        internal static string Root => Path.Combine(Application.persistentDataPath, "PlayData");
#endif
        internal static string RecordsDirectory => Path.Combine(Root, "Matches");
        internal static string ExportsDirectory => Path.Combine(Root, "Exports");

        internal static void Save(string directory, MatchRecord match)
        {
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, match.id + ".json");
            string temporary = file + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(match), JsonUtf8);
            if (File.Exists(file)) File.Replace(temporary, file, null);
            else File.Move(temporary, file);
        }

        internal sealed class ExportResult
        {
            public string directory;
            public int matches, duels, skipped;
        }

        internal static Task<ExportResult> ExportAsync()
        {
            if (PlayRecorder.Current?.Save() == false) throw new IOException("現在の対局を保存できません。");
            string root = Root;
            return Task.Run(() => Export(root));
        }

        internal static ExportResult Export(string root)
        {
            var result = new ExportResult();
            string records = Path.Combine(root, "Matches");
            if (!Directory.Exists(records)) return result;
            var files = Directory.GetFiles(records, "*.json");
            if (files.Length == 0) return result;
            string exports = Path.Combine(root, "Exports");
            string id = Guid.NewGuid().ToString("N");
            string pending = Path.Combine(exports, ".pending-" + id);
            Directory.CreateDirectory(pending);
            try
            {
                using (var matches = new StreamWriter(Path.Combine(pending, "matches.csv"), false, ExcelUtf8))
                using (var duels = new StreamWriter(Path.Combine(pending, "duels.csv"), false, ExcelUtf8))
                {
                    Row(matches, "対局ID","開始UTC","終了UTC","対局結果","プレイ秒","手数","駒打ち回数","決闘数","1P機器","2P機器","ゲーム版","ビルドID");
                    Row(duels, "対局ID","決闘番号","手数","攻撃側","席","機器","駒","成り","相手の駒","相手の成り","決闘結果","この席の結果","決闘秒",
                        "最大HP","最終HP","攻撃力","通常攻撃回数","通常命中数","通常命中率(%)","通常与ダメージ","平均攻撃時距離(m)","平均命中距離(m)",
                        "スキル使用数","スキル直接命中数","スキル直接与ダメージ","砲台発射数","砲台命中数","砲台与ダメージ","被ダメージ","完全防御数");
                    foreach (string file in files)
                    {
                        MatchRecord match;
                        try
                        {
                            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
                            using var reader = new StreamReader(stream, Encoding.UTF8);
                            match = JsonUtility.FromJson<MatchRecord>(reader.ReadToEnd());
                            if (match == null || match.schema != 1 || match.id != Path.GetFileNameWithoutExtension(file) ||
                                !Guid.TryParseExact(match.id,"N",out _) || match.duels == null ||
                                match.duels.Any(d => d?.seats == null || d.seats.Length != 2 || d.seats.Any(s => s == null)))
                            { result.skipped++; continue; }
                        }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
                        { result.skipped++; continue; }
                        Row(matches, match.id, match.startedUtc, match.endedUtc, Result(match.result), match.seconds, match.moves,
                            match.drops, match.duels.Count, Device(match.device1), Device(match.device2), match.gameVersion, match.buildId);
                        result.matches++;
                        foreach (var duel in match.duels)
                        {
                            for (int i = 0; i < 2; i++) WriteSeat(duels, match, duel, i);
                            result.duels++;
                        }
                    }
                }
                if (result.matches == 0) return result;
                string folder = DateTime.Now.ToString("yyyyMMdd-HHmmss", Numbers) + "-" + id.Substring(0,8);
                result.directory = Path.Combine(exports, folder);
                Directory.Move(pending, result.directory);
                return result;
            }
            finally
            {
                if (Directory.Exists(pending)) Directory.Delete(pending, true);
            }
        }

        static void WriteSeat(StreamWriter output, MatchRecord match, DuelRecord duel, int i)
        {
            var s = duel.seats[i]; var opponent = duel.seats[1-i];
            string seat = (i+1) + "P";
            string outcome = duel.result == seat ? "勝ち" : duel.result == "1P" || duel.result == "2P" ? "負け" : Result(duel.result);
            Row(output, match.id, duel.number, duel.move, duel.attackingSeat + "P", seat, Device(i == 0 ? match.device1 : match.device2),
                s.piece, s.promoted ? 1 : 0, opponent.piece, opponent.promoted ? 1 : 0, Result(duel.result), outcome, duel.seconds,
                s.hpMax, s.hpLeft, s.attack, s.attacks, s.hits, Average(100d*s.hits,s.attacks), s.primaryDamage,
                Average(s.attackDistanceSum,s.attacks), Average(s.hitDistanceSum,s.hits), s.skillUses, s.skillHits, s.skillDamage,
                s.turretShots, s.turretHits, s.turretDamage, s.damageTaken, s.hitsBlocked);
        }

        static string Average(double sum, int count) => count == 0 ? "" : (sum/count).ToString("0.###", Numbers);
        static string Device(string device) => device == "gamepad" ? "パッド" : "キーマウ";
        static string Result(string value) => value switch {
            "1P" => "1P", "2P" => "2P", "draw" => "相打ち", "interrupted" => "中断", _ => "未完了"
        };

        static void Row(StreamWriter writer, params object[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) writer.Write(',');
                string value = fields[i] is double number ? number.ToString("0.###", Numbers) : Convert.ToString(fields[i], Numbers) ?? "";
                writer.Write('"'); writer.Write(value.Replace("\"", "\"\"")); writer.Write('"');
            }
            writer.WriteLine();
        }
    }
}
