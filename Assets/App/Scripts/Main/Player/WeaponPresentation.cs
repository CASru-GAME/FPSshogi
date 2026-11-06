using UnityEngine;

namespace App.Main.Player
{
    /// <summary>A stable bind pose and a single attack timeline shared with melee collision.</summary>
    public sealed class WeaponPresentation : MonoBehaviour
    {
        public enum Motion { Thrust, Slash, Gun, Bow, Knife, Command }
        public Motion motion;
        public Transform visual;
        public Vector3 holdPosition = new(.55f, -.45f, 1.45f);
        public Vector3 holdEuler;
        public float attackDuration = .65f;
        public float hitStart = .12f;
        public float hitEnd = .38f;
        public Vector3 muzzleLocal = new(.5f, -.35f, 2.5f);
        public LineRenderer bowString;
        public Transform nockedArrow;
        Player owner;
        Camera view;
        float attackStarted = -100f;
        float guardUntil;
        public bool IsAttacking => Time.time - attackStarted < attackDuration;
        public Vector3 MuzzlePosition => transform.TransformPoint(muzzleLocal);
        public void Bind(Player p) { owner=p; view=p.GetComponentInChildren<Camera>(true); }
        public float Attack() { attackStarted=Time.time; return attackDuration; }
        public void Guard(float seconds) { guardUntil=Time.time+seconds; }
        void LateUpdate()
        {
            if(view!=null)transform.SetPositionAndRotation(view.transform.position,view.transform.rotation);
            if(visual==null)return;
            var pos=holdPosition;var euler=holdEuler;
            float t=Mathf.Clamp01((Time.time-attackStarted)/attackDuration);
            float pulse=Mathf.Sin(t*Mathf.PI);
            if(bowString!=null)bowString.SetPosition(1,new Vector3(-.08f,0,IsAttacking?-.3f*Mathf.Clamp01((t-.45f)/.55f):-.3f));
            if(nockedArrow!=null)nockedArrow.gameObject.SetActive(!IsAttacking||t>.6f);
            if(IsAttacking)
            {
                switch(motion)
                {
                    case Motion.Thrust:pos+=new Vector3(-.15f,.08f,.85f)*pulse;euler.x-=15*pulse;break;
                    case Motion.Slash:pos.x-=.6f*pulse;euler+=new Vector3(-25,25,-65)*pulse;break;
                    case Motion.Knife:pos+=new Vector3(-.25f,.1f,.55f)*pulse;euler.z-=35*pulse;break;
                    case Motion.Gun:pos.z-=.22f*Mathf.Exp(-t*7);euler.x-=8*Mathf.Exp(-t*7);break;
                    case Motion.Bow:pos.z-=.18f*pulse;euler.y+=10*pulse;break;
                    case Motion.Command:euler.z+=55*pulse;pos.y+=.15f*pulse;break;
                }
            }
            else if(Time.time<guardUntil){pos+=new Vector3(-.28f,.25f,-.12f);euler.z+=35;}
            else if(owner!=null)
            {
                var rb=owner.GetComponent<Rigidbody>();float speed=rb!=null?new Vector2(rb.linearVelocity.x,rb.linearVelocity.z).magnitude:0;
                pos.y+=Mathf.Sin(Time.time*(speed>.2f?9:2))*(speed>.2f?.025f:.006f);
            }
            visual.localPosition=pos;visual.localRotation=Quaternion.Euler(euler);
        }
    }
}
