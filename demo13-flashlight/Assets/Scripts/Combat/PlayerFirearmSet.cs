using UnityEngine;

/// <summary>Approved firearm presentation on the shared survivor rig. No combat stats.</summary>
public sealed class PlayerFirearmSet : ScriptableObject
{
    public AnimationClip aim, walk, shoot, draw;
    public GameObject weaponPrefab, holsterPrefab;
    public string stowBone;
    public Vector3 stowPosition;
    public Quaternion stowRotation = Quaternion.identity;
    public Quaternion gripRotation = Quaternion.identity;
}
