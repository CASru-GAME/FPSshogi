using App.Common.Initialize;
using UnityEngine;
using UnityEngine.InputSystem;
using App.Main.GameMaster;
using UnityEngine.iOS;
using Unity.VisualScripting.Antlr3.Runtime;

namespace App.Main.Controller
{
    public class ControlDataReceptorStarting : MonoBehaviour, IInitializable
    {
        private Common.Controller.Controller controller;
        private GameStateHolder gameStateHolder;
        private DeviceManager deviceManager;
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(Common.Controller.Controller), typeof(GameStateHolder), typeof(DeviceManager) };
        public void Initialize(ReferenceHolder referenceHolder)
        {
            controller = referenceHolder.GetInitializable<Common.Controller.Controller>();
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();
            deviceManager = referenceHolder.GetInitializable<DeviceManager>();
            controller.EnableUIInput();
            controller.SubscribeToSelectUI(OnSelectUI);
        }

        private void OnSelectUI(InputAction.CallbackContext ctx)
        {
            Debug.Log("OnSelectUI");
            if (ctx.canceled)
            {
                Debug.Log("Release SelectUIButton");
                var device = ctx.control.device;
                SetDeviceId(device.deviceId.ToString());
                if (deviceManager.IsDevicesRaedy())
                {
                    Debug.Log($"deviceManager.GetDeviceIdPlayerOne(): {deviceManager.GetDeviceIdPlayerOne()}");
                    Debug.Log($"deviceManager.GetDeviceIdPlayerTwo(): {deviceManager.GetDeviceIdPlayerTwo()}");
                    controller.DisableUIInput();
                }
            }
        }

        private void SetDeviceId(string deviceId)
        {
            if (deviceManager.IsDeviceIdContains(deviceId)) return;
            if (deviceManager.IsDevicesRaedy()) return;
            deviceManager.SetDeviceId(deviceManager.GetDeviceIdCount(), deviceId);
        }
    }
}
