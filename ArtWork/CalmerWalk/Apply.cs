using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
public static class CalmerWalkApply
{
    public static string Run()
    {
        var db=AssetDatabase.LoadAssetAtPath<StatDB>("Assets/Resources/Data/StatDB.asset");
        db.playerStat.motions.Single(m=>m.anim=="walk").animSpeed=.5f;
        EditorUtility.SetDirty(db);AssetDatabase.SaveAssetIfDirty(db);
        return JsonConvert.SerializeObject(new {walkSpeed=db.playerStat.moveSpeed,walkMotion=db.playerStat.motions.Single(m=>m.anim=="walk").animSpeed,runMotion=db.playerStat.motions.Single(m=>m.anim=="run").animSpeed});
    }
}
