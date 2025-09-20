using UnityEngine;
using App.Common.Initialize;

namespace App.Main.GameMaster
{
    public class DuelManager : MonoBehaviour, IInitializable
    {
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { };
        [SerializeField] public GameObject PlayerOne;
        [SerializeField] public GameObject PlayerTwo;
        public void Initialize(ReferenceHolder referenceHolder)
        {
            Debug.Log("DuelManager initialized.");
        }
    }
}
