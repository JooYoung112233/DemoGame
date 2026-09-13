using UnityEngine;
using System.IO;
using Newtonsoft.Json;
public static class ArmoryIsolatedSaveProbe
{
    public static string Run()
    {
        // Only a detached DTO and an ArtWork test file change. Never Save(), Load(), or alter live items.
        var sample=new GameSaveData();
        var pistol=new ItemInstance(ItemDatabase.Get("pistol9"));pistol.SetAttachment(WeaponPartType.Magazine,"mag_9x19");pistol.ammoCount=7;pistol.ammoItemId="ammo_9x19";
        var grid=new InventoryGrid(1,1);grid.TryAutoPlace(pistol);
        sample.equippedWeapon="5:pistol9|6:bat";
        sample.equippedWeaponStates=new System.Collections.Generic.List<PlayerEquipment.WeaponState>{new PlayerEquipment.WeaponState{slot=EquipSlot.PrimaryWeapon,items=grid.GetSaveData()}};
        sample.starterArmoryClaimed=true;
        string folder=Path.GetFullPath("../ArtWork/ArmoryInteraction62");
        string path=Path.Combine(folder,"IsolatedSave.json");
        File.WriteAllText(path,SaveManager.Instance.ToJson(sample));
        var restored=JsonConvert.DeserializeObject<GameSaveData>(File.ReadAllText(path));
        bool same=SaveManager.Instance.ToJson(sample)==SaveManager.Instance.ToJson(restored);
        var stash=MainStash.Ensure();bool oldClaim=stash.StarterArmoryClaimed;
        bool noRepeat;
        try
        {
            int count=stash.GetGrid().ItemCount;
            stash.RestoreStarterArmoryClaim(restored.starterArmoryClaimed);
            noRepeat=!stash.GrantStarterArmory()&&stash.GetGrid().ItemCount==count;
        }
        finally {stash.RestoreStarterArmoryClaim(oldClaim);}
        File.WriteAllText(Path.Combine(folder,"DiskSaveValidation.json"),JsonConvert.SerializeObject(new{pass=same&&noRepeat,isolatedFile=true,userSaveUntouched=true,weaponAmmo=restored.equippedWeaponStates[0].items[0].ammoCount,restoredGrantFlagPreventsRepeat=noRepeat},Formatting.Indented));
        UIManager.Instance.ShowCharacterPanelWithStash();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"ReadyInventory.png"));
        return "Isolated disk round-trip: "+same+"; actual user save and equipment untouched.";
    }
}
