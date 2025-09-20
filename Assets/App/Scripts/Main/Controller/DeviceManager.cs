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

        private string[] deviceIds;
        public void SetDeviceId(int playerIndex, string deviceId)
        {
            if (deviceIds == null)
            {
                deviceIds = new string[2] { "", "" };
            }
            if (playerIndex < 0 || playerIndex >= deviceIds.Length)
            {
                Debug.LogError($"Invalid playerIndex: {playerIndex}");
                return;
            }
            deviceIds[playerIndex] = deviceId;
            Debug.Log($"✅ Player {playerIndex + 1} の deviceId を設定: {deviceId}");
        }
        public int GetDeviceIdCount()
        {
            if (deviceIds == null)
            {
                return 0;
            }
            int count = 0;
            foreach (var id in deviceIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    count++;
                }
            }
            return count;
        }
        public bool IsDeviceIdContains(string deviceId)
        {
            bool answer = false;
            if (deviceIds == null)
            {
                return false;
            }
            foreach (var id in deviceIds)
            {
                if (id == deviceId)
                {
                    answer = true;
                    break;
                }
            }
            return answer;
        }
        public bool IsDevicesRaedy()
        {
            if (deviceIds == null)
            {
                return false;
            }
            foreach (var id in deviceIds)
            {
                if (string.IsNullOrEmpty(id))
                {
                    return false;
                }
            }
            return true;
        }
        public string GetDeviceIdPlayerOne()
        {
            if (deviceIds == null || deviceIds.Length < 1)
            {
                return "";
            }
            return deviceIds[0];
        }
        public string GetDeviceIdPlayerTwo()
        {
            if (deviceIds == null || deviceIds.Length < 2)
            {
                return "";
            }
            return deviceIds[1];
        }
    }
}
