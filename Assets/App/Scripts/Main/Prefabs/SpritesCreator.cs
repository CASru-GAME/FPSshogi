using UnityEngine;
using App.Common.Initialize;
using App.Main.GameMaster;

namespace App.Main.Prefabs
{

    public class SpritesCreator : MonoBehaviour, IInitializable
    {
        public GameObject fuhyo;
        public GameObject kyosya;
        public GameObject keima;
        public GameObject gin; 
        public GameObject kin;
        public GameObject kakugyo;
        public GameObject hisha;
        public GameObject ou;
        public GameObject gyoku;

        public int InitializationPriority => 80; // 優先度（低いほど先に初期化される）
        public System.Type[] Dependencies => new System.Type[] { typeof(ShogiBoard) }; // 依存関係
        public void Initialize()
        {
            // 初期化処理
        }

        


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}