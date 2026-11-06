using System.IO;
using UnityEditor;
using UnityEngine;
using App.UI;

[InitializeOnLoad]
public static class RoomUiAssetBuilder
{
    static RoomUiAssetBuilder(){EditorApplication.delayCall+=Build;}
    [MenuItem("FPSshogi/Prepare room UI assets")]
    public static void Build()
    {
        const string path="Assets/Resources/UI/RoomUiAssets.asset";
        if(AssetDatabase.LoadAssetAtPath<RoomUiAssets>(path)!=null)return;
        var asset=ScriptableObject.CreateInstance<RoomUiAssets>();
        GameObject Load(string name)=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/"+name+".prefab");
        asset.pawn=Load("fuhyo");asset.lance=Load("kyosya");asset.knight=Load("keima");asset.silver=Load("gin");asset.gold=Load("kin");asset.bishop=Load("kakugyo");asset.rook=Load("hisya");asset.king=Load("ou");
        if(asset.pawn==null||asset.king==null)throw new System.InvalidOperationException("Piece prefabs are unavailable");
        AssetDatabase.CreateAsset(asset,path);AssetDatabase.SaveAssets();
        Directory.CreateDirectory(".utmp/room-ui");File.WriteAllText(".utmp/room-ui/assets-ready.txt","Room UI prefab references saved.");
    }
}
