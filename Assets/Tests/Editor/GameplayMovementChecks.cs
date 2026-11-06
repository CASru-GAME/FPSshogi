using System;
using System.Collections;
using System.Reflection;
using App.Main.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class GameplayMovementChecks
{
    static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    static object Get(object obj,string name)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
    static void Invoke(Player player,string method)=>typeof(Player).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);
    public static IEnumerator Run(Action<string,bool> check)
    {
        var root=new GameObject("MovementFixture");root.transform.position=new Vector3(3000,200,2000);root.transform.localScale=Vector3.one*2;
        var body=root.AddComponent<CapsuleCollider>();body.height=2;body.radius=.5f;
        var camera=new GameObject("MovementCamera");camera.transform.SetParent(root.transform,false);camera.AddComponent<Camera>().enabled=false;
        var player=root.AddComponent<Player>();player.enabled=false;Set(player,"cameraTransform",camera.transform);player.Initialize();
        var rb=root.GetComponent<Rigidbody>();rb.useGravity=false;
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="ClimbablePieceFixture";
        obstacle.transform.position=root.transform.position+new Vector3(0,-1.25f,1.3f);obstacle.transform.localScale=new Vector3(3,1.5f,.4f);
        var mouse=InputSystem.AddDevice<Mouse>("Gameplay relative mouse");var look=new InputAction("Look",InputActionType.Value,"<Mouse>/delta");
        var callback=typeof(Player).GetMethod("OnActionTriggered",BindingFlags.Instance|BindingFlags.NonPublic);
        look.performed+=context=>callback.Invoke(player,new object[]{context});look.canceled+=context=>callback.Invoke(player,new object[]{context});look.Enable();
        int originalCapture=Time.captureFramerate;
        try
        {
            Physics.SyncTransforms();Set(player,"moveInput",Vector2.up);Set(player,"climbInput",true);Invoke(player,"FixedUpdate");
            check("Climb input raises player beside a solid board piece",rb.linearVelocity.y>0);
            Set(player,"climbInput",false);rb.linearVelocity=Vector3.zero;Invoke(player,"FixedUpdate");
            check("Releasing climb stops climbing impulse",Mathf.Abs(rb.linearVelocity.y)<.01f);
            obstacle.SetActive(false);Physics.SyncTransforms();Set(player,"climbInput",true);rb.linearVelocity=Vector3.zero;Invoke(player,"FixedUpdate");
            check("Climb cannot fly without a nearby obstacle",Mathf.Abs(rb.linearVelocity.y)<.01f);
            Set(player,"climbInput",false);Set(player,"moveInput",Vector2.zero);
            float[] deltas=new float[2];int index=0;
            foreach(int rate in new[]{30,120})
            {
                Time.captureFramerate=rate;yield return .15d;
                Set(player,"yaw",0f);Set(player,"lookInput",Vector2.zero);
                InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(20,0)});InputSystem.Update();Invoke(player,"Update");
                deltas[index++]=(float)Get(player,"yaw");
                float after=(float)Get(player,"yaw");Invoke(player,"Update");
                check($"Mouse displacement is consumed once at {rate} fps",Mathf.Abs((float)Get(player,"yaw")-after)<.0001f);
            }
            check($"Equal mouse travel turns equally at 30 and 120 fps ({deltas[0]:F3}/{deltas[1]:F3})",deltas[0]>.01f&&Mathf.Abs(deltas[0]-deltas[1])<.01f);
            var flash=new FlashBangEffect();player.playerStatus.EffectList.AddEffect(flash);
            var overlay=(GameObject)Get(flash,"overlayRoot");
            new GinSkill().UseSkill(player,player.playerStatus);
            player.ResetDuelPresentation();
            check("Duel reset clears active effect speed changes",Mathf.Approximately(player.playerStatus.MoveSpeed.Current,player.playerStatus.MoveSpeed.Default));
            check("Duel reset restores defense after discarding active skill",player.playerStatus.DefensePoint.Current==0);
            yield return .1d;
            check("Duel reset removes temporary flash overlay",overlay==null);
        }
        finally
        {
            Time.captureFramerate=originalCapture;look.Dispose();if(mouse.added)InputSystem.RemoveDevice(mouse);
            UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(obstacle);
        }
        yield return .1d;
    }
}
