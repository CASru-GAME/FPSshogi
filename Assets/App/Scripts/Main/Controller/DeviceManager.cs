using App.Common.Initialize;
using UnityEngine;
using System.Collections.Generic;

namespace App.Main.Controller
{
    public class DeviceManager : MonoBehaviour, IInitializable
    {
        private Common.Controller.Controller controller;
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { };
        public void Initialize(ReferenceHolder referenceHolder)
        {
            Debug.Log("DeviceManager initialized.");
        }

        private List<string> deviceIds;
        public void SetDeviceId(int playerIndex, string deviceId)
        {
            if (deviceIds == null)
            {
                deviceIds = new List<string> { "", "" };
            }
            if (playerIndex < 0 || playerIndex >= deviceIds.Count)
            {
                Debug.LogError($"Invalid playerIndex: {playerIndex}");
                return;
            }
            deviceIds[playerIndex] = deviceId;
            Debug.Log($"✅ Player {playerIndex + 1} の deviceId を設定: {deviceId}");
        }
        public bool IsDeviceIdContains(string deviceId)
        {
            if (deviceIds == null)
            {
                return false;
            }
            return deviceIds.Contains(deviceId);
        }
        public int GetDeviceIdCount()
        {
            if (deviceIds == null)
            {
                return 0;
            }
            return deviceIds.Count;
        }
        public string GetDeviceIdPlayerOne()
        {
            if (deviceIds == null || deviceIds.Count < 1)
            {
                return "";
            }
            return deviceIds[0];
        }
        public string GetDeviceIdPlayerTwo()
        {
            if (deviceIds == null || deviceIds.Count < 2)
            {
                return "";
            }
            return deviceIds[1];
        }
    }
}
