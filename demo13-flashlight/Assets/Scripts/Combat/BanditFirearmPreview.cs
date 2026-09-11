using System;
using System.Linq;
using UnityEngine;

/// <summary>Isolated art-review actor. No raid spawning, damage or balance changes.</summary>
public sealed class BanditFirearmPreview : MonoBehaviour
{
    public enum FirearmKind { Pistol, AssaultRifle }
    public FirearmKind kind;
    public AnimationClip idle, walk, aim, armedWalk, shoot;
    public bool autoPreview = true;
    [Tooltip("Optional review target. With autoPreview off, approaching this target draws the gun.")]
    public Transform target;
    public float previewDetectionRange = 6;
    public string CurrentPhase { get; private set; }
    public float DrawAmount { get; private set; }
    public bool IsAiming => DrawAmount > .995f;
    public Transform Muzzle { get; private set; }
    public int PreviewShots { get; private set; }
    public event Action Fired;

    Transform[] bones, upper;
    Transform weapon, grip, stow, hand, socket, arm, forearm;
    Vector3[] idlePositions, reachPositions, clearPositions, aimPositions;
    Quaternion[] idleRotations, reachRotations, clearRotations, aimRotations;
    float clock, shotAge = 10, nextShot;
    bool initialized;
    Vector3 origin;
    LineRenderer tracer;
    Material tracerMaterial;

    void Awake() { Initialize(); origin = transform.position; }
    public void Initialize()
    {
        if (initialized) return;
        bones = GetComponentsInChildren<Transform>();
        Transform Find(string name) => bones.Single(t => t.name == name);
        weapon=Find("Firearm");grip=Find("FirearmGrip");stow=Find("FirearmStow");
        hand=Find("Hand.R");socket=Find("HandSocket.R");arm=Find("UpperArm.R");forearm=Find("Forearm.R");Muzzle=Find("Muzzle");
        upper=Find("Spine").GetComponentsInChildren<Transform>().Where(t=>t!=weapon&&!t.IsChildOf(weapon)&&t!=stow&&!t.IsChildOf(stow)&&t!=grip).ToArray();
        initialized=true;
    }
    void Capture(out Vector3[] positions,out Quaternion[] rotations)
    { positions=upper.Select(t=>t.localPosition).ToArray();rotations=upper.Select(t=>t.localRotation).ToArray(); }
    void Blend(Vector3[] p0,Quaternion[] q0,Vector3[] p1,Quaternion[] q1,float t)
    { t=Mathf.SmoothStep(0,1,t);for(int i=0;i<upper.Length;i++){upper[i].localPosition=Vector3.Lerp(p0[i],p1[i],t);upper[i].localRotation=Quaternion.Slerp(q0[i],q1[i],t);} }
    void Reach(Vector3 palm,Quaternion rotation)
    {
        var offset=hand.InverseTransformPoint(socket.position);var wrist=palm-rotation*Vector3.Scale(offset,hand.lossyScale);
        float a=Vector3.Distance(arm.position,forearm.position),b=Vector3.Distance(forearm.position,hand.position);
        var shoulder=arm.position;var delta=wrist-shoulder;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,(a+b)*.98f);var direction=delta.normalized;
        float along=(a*a-b*b+d*d)/(2*d);var pole=transform.TransformDirection(new Vector3(1,-.4f,-.2f));pole-=direction*Vector3.Dot(pole,direction);
        var elbow=shoulder+direction*along+pole.normalized*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        arm.rotation=Quaternion.FromToRotation(forearm.position-arm.position,elbow-shoulder)*arm.rotation;
        forearm.rotation=Quaternion.FromToRotation(hand.position-forearm.position,shoulder+direction*d-forearm.position)*forearm.rotation;hand.rotation=rotation;
    }
    void Attach(Transform parent)
    { if(weapon.parent!=parent)weapon.SetParent(parent,false);weapon.localPosition=new Vector3(0,.035f,0);weapon.localRotation=Quaternion.identity;weapon.localScale=Vector3.one; }

    /// <summary>Deterministic art sample; also used by the capture/validation tools.</summary>
    public void Sample(float time,float draw,bool moving,float sinceShot=10)
    {
        // Imported clips can contain root curves. Sampling must not teleport the review actor.
        var p=transform.localPosition;var q=transform.localRotation;var s=transform.localScale;
        try { SamplePose(time,draw,moving,sinceShot); }
        finally { transform.localPosition=p;transform.localRotation=q;transform.localScale=s; }
    }
    void SamplePose(float time,float draw,bool moving,float sinceShot)
    {
        Initialize();DrawAmount=Mathf.Clamp01(draw);var baseClip=moving?walk:idle;baseClip.SampleAnimation(gameObject,time%baseClip.length);
        if(DrawAmount<=0){Attach(stow);CurrentPhase="Stowed";return;}
        if(DrawAmount>=1)
        {
            (moving?armedWalk:aim).SampleAnimation(gameObject,time%(moving?armedWalk:aim).length);
            if(sinceShot<shoot.length)
            {
                Capture(out var lowerBaseP,out var lowerBaseQ);
                shoot.SampleAnimation(gameObject,Mathf.Max(0,sinceShot));Capture(out var fireP,out var fireQ);
                (moving?armedWalk:aim).SampleAnimation(gameObject,time%(moving?armedWalk:aim).length);
                // Blend the authored shot out into aim, retaining the walking lower body.
                float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(shoot.length*.7f,shoot.length,sinceShot));
                Blend(lowerBaseP,lowerBaseQ,fireP,fireQ,weight);
            }
            Attach(grip);CurrentPhase=sinceShot<shoot.length?"Firing":moving?"AimWalk":"Aim";return;
        }
        Capture(out idlePositions,out idleRotations);
        // Reach the actual stored grip, then bring the weapon around the body's right side.
        var storedRotation=stow.rotation*Quaternion.Inverse(grip.localRotation)*Quaternion.Inverse(socket.localRotation);
        Reach(stow.position,storedRotation);Capture(out reachPositions,out reachRotations);
        baseClip.SampleAnimation(gameObject,time%baseClip.length);
        var clearRotation=Quaternion.LookRotation(transform.TransformDirection(new Vector3(.25f,-.65f,1)),transform.up)*Quaternion.Inverse(grip.localRotation)*Quaternion.Inverse(socket.localRotation);
        Reach(transform.TransformPoint(new Vector3(.40f,.90f,.07f)),clearRotation);Capture(out clearPositions,out clearRotations);
        aim.SampleAnimation(gameObject,time%aim.length);Capture(out aimPositions,out aimRotations);
        baseClip.SampleAnimation(gameObject,time%baseClip.length);
        if(draw<.35f)Blend(idlePositions,idleRotations,reachPositions,reachRotations,draw/.35f);
        else if(draw<.65f)Blend(reachPositions,reachRotations,clearPositions,clearRotations,(draw-.35f)/.3f);
        else Blend(clearPositions,clearRotations,aimPositions,aimRotations,(draw-.65f)/.35f);
        Attach(draw<.35f?stow:grip);CurrentPhase="Draw / Stow";
    }
    public void SampleTimeline(float time)
    {
        float t=Mathf.Repeat(time,14);float draw=t<2?0:t<3.2f?(t-2)/1.2f:t<10?1:t<11.2f?1-(t-10)/1.2f:0;
        bool moving=t<2||(t>=5&&t<8)||t>=11.2f;
        float interval=kind==FirearmKind.Pistol?.85f:.8f;
        float age=t>=8&&t<10?(t-8>=interval?t-8-interval:t-8):10;
        Sample(time,draw,moving,age);
    }
    void Update()
    {
        clock+=Time.deltaTime;shotAge+=Time.deltaTime;
        if(autoPreview)
        {
            SampleTimeline(clock);float t=Mathf.Repeat(clock,14);
            bool moving=t<2||(t>=5&&t<8)||t>=11.2f;
            transform.position=origin+transform.right*(moving?Mathf.Sin(clock*2)*.12f:0);
            if(t>=8&&t<9.5f&&Time.time>=nextShot)FirePreview();
        }
        else
        {
            bool detected=target!=null&&Vector3.Distance(transform.position,target.position)<previewDetectionRange;
            if(detected){var direction=target.position-transform.position;direction.y=0;if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(direction);}
            Sample(clock,Mathf.MoveTowards(DrawAmount,detected?1:0,Time.deltaTime/1.2f),false,shotAge);
            if(detected&&IsAiming&&Time.time>=nextShot)FirePreview();
        }
        if(tracer!=null){tracer.enabled=shotAge<.08f;if(tracer.enabled){tracer.SetPosition(0,Muzzle.position);tracer.SetPosition(1,Muzzle.position+Muzzle.forward*2.5f);}}
    }
    void FirePreview()
    {
        // Art-only tracer. Production projectile/damage integration follows visual approval.
        shotAge=0;nextShot=Time.time+(kind==FirearmKind.Pistol?.85f:.16f);PreviewShots++;Fired?.Invoke();
        if(tracer==null)
        {
            var go=new GameObject("Preview muzzle tracer");go.transform.SetParent(transform,false);tracer=go.AddComponent<LineRenderer>();
            tracerMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));tracerMaterial.SetColor("_BaseColor",new Color(1,.65f,.15f));tracer.sharedMaterial=tracerMaterial;
            tracer.positionCount=2;tracer.startWidth=.035f;tracer.endWidth=.009f;tracer.useWorldSpace=true;
        }
    }
    void OnDestroy(){if(tracerMaterial!=null){if(Application.isPlaying)Destroy(tracerMaterial);else DestroyImmediate(tracerMaterial);}}
}
