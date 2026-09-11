using System.Collections.Generic;
using UnityEngine;

/// <summary>Layers approved gun poses over the live player's locomotion. Damage stays in PlayerGun.</summary>
[DefaultExecutionOrder(100)]
public sealed class PlayerFirearmVisual : MonoBehaviour
{
    PlayerFirearmSet set;
    WeaponData weaponData;
    Transform grip, stow, weapon, muzzle;
    GameObject holster;
    Transform[] upper, lower;
    Vector3[] lowerP, aimP, shotP;
    Quaternion[] lowerQ, aimQ, shotQ;
    Vector3[] lowerS;
    float draw, clock, sinceShot=100;
    bool moving, sprinting;
    float cadence=1;
    public bool Equipped => set != null && weaponData != null;
    public Transform Muzzle => Equipped ? muzzle : null;
    public float DrawAmount => draw;

    public bool Equip(WeaponData data)
    {
        if(data==weaponData && set!=null)return true;
        Clear();
        if(data==null||!data.isRanged)return false;
        var loaded=Resources.Load<PlayerFirearmSet>("Characters/Firearms/"+(data.firearmStance==WeaponData.FirearmStance.AssaultRifle?"Rifle":"Pistol"));
        if(loaded==null)return false;
        var bones=GetComponentsInChildren<Transform>(true);
        Transform Find(string name){foreach(var t in bones)if(t.name==name)return t;return null;}
        var hand=Find("HandSocket.R");var carrier=Find(loaded.stowBone);var spine=Find("Spine");
        if(hand==null||carrier==null||spine==null)return false;
        set=loaded;weaponData=data;
        grip=new GameObject("PlayerFirearmGrip").transform;grip.SetParent(hand,false);grip.localRotation=set.gripRotation;grip.localScale=Vector3.one/(hand.lossyScale.x/transform.lossyScale.x);
        stow=new GameObject("PlayerFirearmStow").transform;stow.SetParent(carrier,false);stow.localPosition=set.stowPosition;stow.localRotation=set.stowRotation;stow.localScale=Vector3.one/(carrier.lossyScale.x/transform.lossyScale.x);
        weapon=Instantiate(set.weaponPrefab,stow,false).transform;weapon.name="PlayerFirearm";weapon.localPosition=new Vector3(0,.035f,0);weapon.localRotation=Quaternion.identity;weapon.localScale=Vector3.one;
        foreach(var t in weapon.GetComponentsInChildren<Transform>())if(t.name=="Muzzle")muzzle=t;
        holster=Instantiate(set.holsterPrefab,stow,false);
        foreach(var t in stow.GetComponentsInChildren<Transform>(true))t.gameObject.layer=gameObject.layer;
        grip.gameObject.layer=gameObject.layer;
        var up=new List<Transform>();var lo=new List<Transform>();
        foreach(var t in bones){if(t==spine||t.IsChildOf(spine))up.Add(t);else lo.Add(t);}
        upper=up.ToArray();lower=lo.ToArray();lowerP=new Vector3[lower.Length];lowerQ=new Quaternion[lower.Length];lowerS=new Vector3[lower.Length];
        aimP=new Vector3[upper.Length];aimQ=new Quaternion[upper.Length];shotP=new Vector3[upper.Length];shotQ=new Quaternion[upper.Length];
        draw=sprinting?0:1;ApplyPose();return true;
    }
    public void SetMotion(bool isMoving,bool isSprinting,float playbackCadence){moving=isMoving;sprinting=isSprinting;cadence=Mathf.Max(.1f,playbackCadence);}
    public void PrepareShot()
    {
        if(!Equipped)return;
        // Keep the existing immediate-fire input semantics: never fire from a holstered muzzle.
        draw=1;sprinting=false;ApplyPose();
    }
    public void Shot(){sinceShot=0;}
    void LateUpdate(){Tick(Time.deltaTime);}
    public void Tick(float dt)
    {
        if(!Equipped)return;
        clock+=dt*cadence;sinceShot+=dt;
        draw=Mathf.MoveTowards(draw,sprinting?0:1,Mathf.Max(0,dt)/1.2f);ApplyPose();
    }
    void Attach(bool held)
    {
        var parent=held?grip:stow;if(weapon.parent!=parent)weapon.SetParent(parent,false);
        weapon.localPosition=new Vector3(0,.035f,0);weapon.localRotation=Quaternion.identity;weapon.localScale=Vector3.one;
    }
    void Capture(Vector3[] p,Quaternion[] q){for(int i=0;i<upper.Length;i++){p[i]=upper[i].localPosition;q[i]=upper[i].localRotation;}}
    void ApplyPose()
    {
        if(draw<=0){Attach(false);return;}
        for(int i=0;i<lower.Length;i++){lowerP[i]=lower[i].localPosition;lowerQ[i]=lower[i].localRotation;lowerS[i]=lower[i].localScale;}
        try{
            if(draw<1)set.draw.SampleAnimation(gameObject,draw*set.draw.length);
            else{
                var clip=moving?set.walk:set.aim;clip.SampleAnimation(gameObject,clock%clip.length);
                if(sinceShot<set.shoot.length){
                    Capture(aimP,aimQ);set.shoot.SampleAnimation(gameObject,sinceShot);Capture(shotP,shotQ);
                    float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(set.shoot.length*.7f,set.shoot.length,sinceShot));
                    for(int i=0;i<upper.Length;i++){upper[i].localPosition=Vector3.Lerp(aimP[i],shotP[i],weight);upper[i].localRotation=Quaternion.Slerp(aimQ[i],shotQ[i],weight);}
                }
            }
        }finally{
            // Preserve the player's root, hips, legs, sprint/dodge and world facing.
            for(int i=0;i<lower.Length;i++){lower[i].localPosition=lowerP[i];lower[i].localRotation=lowerQ[i];lower[i].localScale=lowerS[i];}
        }
        Attach(draw>=.35f);
    }
    public void Clear()
    {
        // Disable immediately; deferred destruction must not leave the old gun visible for a frame.
        if(weapon!=null){weapon.gameObject.SetActive(false);Dispose(weapon.gameObject);}
        if(holster!=null){holster.SetActive(false);Dispose(holster);}
        if(grip!=null)Dispose(grip.gameObject);if(stow!=null)Dispose(stow.gameObject);
        set=null;weaponData=null;weapon=grip=stow=muzzle=null;holster=null;draw=0;sinceShot=100;
    }
    static void Dispose(Object o){if(o is GameObject go)go.transform.SetParent(null,false);if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    void OnDestroy(){Clear();}
}
