using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public static class EquippedQuickslotProbe
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Field(object target, string name) => target.GetType().GetField(name, Hidden).GetValue(target);
    static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
    static string Root => Path.GetFullPath("../ArtWork/EquippedQuickslots");
    public static string Run()
    {
        TopDownPlayer.Instance.StartCoroutine(Check());
        return "Testing equipment selection and quickslot keys with save writes suppressed; runtime state restored in finally";
    }

    static IEnumerator Check()
    {
        var eq = TopDownPlayer.Instance.GetComponent<PlayerEquipment>();
        var inv = TopDownPlayer.Instance.GetComponent<PlayerInventory>();
        var grids = new[] { inv.Grid, inv.PocketsGrid, inv.SecureGrid }.Where(g => g != null).ToArray();
        var savedGrids = grids.Select(g => g.GetSaveData()).ToArray();
        var savedEquipment = eq.GetSaveData();
        var savedWeapons = eq.GetWeaponStates();
        var bar = QuickSlotBar.Instance;
        var savedSlots = bar.GetSlotIds();
        var wasOpen = UIManager.Instance.IsInventoryOpen;
        bool suppress = SaveManager.SuppressWrites, virtualInput = GameInput.Virtual;
        var checks = new Dictionary<string, bool>();
        CharacterPanelUI ui = null;
        SaveManager.SuppressWrites = true;
        GameInput.Virtual = true;
        try
        {
            UIManager.Instance.ShowCharacterPanelWithStash();
            yield return null;
            ui = UnityEngine.Object.FindAnyObjectByType<CharacterPanelUI>();
            var fixture = new InventoryGrid(8, 1);
            var pistol = new ItemInstance(ItemDatabase.Get("pistol9"));
            var bat = new ItemInstance(ItemDatabase.Get("bat"));
            fixture.TryAutoPlace(pistol); fixture.TryAutoPlace(bat);
            if (!eq.TryEquipWeaponFromGrid(pistol, fixture, EquipSlot.PrimaryWeapon)
                || !eq.TryEquipWeaponFromGrid(bat, fixture, EquipSlot.SecondaryWeapon))
                throw new InvalidOperationException("Cannot set up temporary weapon fixture");
            bar.LoadSlotIds(Enumerable.Repeat("", QuickSlotBar.SlotCount).ToList());

            void Click(EquipSlot slot) => Call(ui, "OnEquipSlotClicked", slot);
            IEnumerator Key(KeyCode key)
            {
                GameInput.VEndFrame();
                GameInput.VPressKey(key);
                try { Call(ui, "HandleDragAndDrop"); yield return null; }
                finally { GameInput.VEndFrame(); GameInput.VReleaseKey(key); }
            }
            Click(EquipSlot.PrimaryWeapon);
            checks["click_selects_equipped_instance"] = Field(ui, "selectedItem") == pistol;
            yield return Key(KeyCode.Alpha1);
            checks["number_registers_without_unequipping"] = bar.GetSlotIds()[0] == "pistol9"
                && eq.GetSlotInstance(EquipSlot.PrimaryWeapon) == pistol;
            var highlights = (HashSet<EquipSlot>)Field(ui, "highlightSlots");
            checks["only_selected_equipment_highlighted"] = highlights.SetEquals(new[] { EquipSlot.PrimaryWeapon });

            Click(EquipSlot.SecondaryWeapon);
            yield return Key(KeyCode.Keypad6);
            checks["secondary_numpad_six"] = bar.GetSlotIds()[5] == "bat"
                && eq.GetSlotInstance(EquipSlot.SecondaryWeapon) == bat;
            eq.TryStoreWeapon(EquipSlot.SecondaryWeapon, fixture);
            yield return Key(KeyCode.Alpha2);
            checks["stored_selection_cannot_register"] = bar.GetSlotIds()[1] == "" && Field(ui, "selectedItem") == null;

            Click(EquipSlot.PrimaryWeapon);
            var replacement = new ItemInstance(pistol.data);
            fixture.TryAutoPlace(replacement);
            eq.TryEquipWeaponFromGrid(replacement, fixture);
            yield return Key(KeyCode.Alpha3);
            checks["same_id_replacement_invalidates_selection"] = bar.GetSlotIds()[2] == "" && Field(ui, "selectedItem") == null;

            Call(ui, "ShowEquipContextMenu", EquipSlot.PrimaryWeapon, replacement.data);
            yield return null;
            var menu = (GameObject)Field(ui, "contextMenuGO");
            var button = menu.GetComponentsInChildren<Button>().Single(b => b.GetComponentsInChildren<Text>().Any(t => t.text == "퀵슬롯 등록 (1~6)"));
            button.onClick.Invoke();
            yield return Key(KeyCode.Alpha4);
            checks["context_menu_selects_for_number_key"] = bar.GetSlotIds()[3] == "pistol9" && !menu.activeSelf;

            Call(ui, "SelectItem", bat, fixture);
            yield return Key(KeyCode.Alpha5);
            checks["grid_selection_still_registers"] = bar.GetSlotIds()[4] == "bat"
                && (EquipSlot)Field(ui, "selectedEquipSlot") == EquipSlot.None;
            foreach (var grid in grids) grid.Clear();
            GameInput.VPressKey(KeyCode.LeftControl);
            try { Click(EquipSlot.PrimaryWeapon); }
            finally { GameInput.VReleaseKey(KeyCode.LeftControl); GameInput.VEndFrame(); }
            checks["ctrl_click_still_unequips"] = eq.GetSlotInstance(EquipSlot.PrimaryWeapon) == null
                && grids.Any(g => g.GetAll().Any(p => p.item == replacement));
            var source = grids.First(g => g.GetAll().Any(p => p.item == replacement));
            eq.TryEquipWeaponFromGrid(replacement, source);
            Call(ui, "RefreshAllGrids");
            Click(EquipSlot.PrimaryWeapon);
            var slotImages = (Dictionary<EquipSlot, Image>)Field(ui, "equipSlotBgs");
            GameInput.VSetMousePos(RectTransformUtility.WorldToScreenPoint(null, slotImages[EquipSlot.PrimaryWeapon].transform.position));
            Call(ui, "ShowEquipContextMenu", EquipSlot.PrimaryWeapon, replacement.data);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(Root, "EquipmentMenu.png"));
            yield return new WaitForSecondsRealtime(.2f);
            File.WriteAllText(Path.Combine(Root, "Validation.json"), JsonConvert.SerializeObject(new { pass = checks.Values.All(v => v), checks }, Formatting.Indented));
        }
        finally
        {
            if (ui != null) { Call(ui, "HideContextMenu"); Call(ui, "ClearSelection"); }
            eq.ResetForNewGame(); eq.LoadSaveData(savedEquipment); eq.RestoreWeaponStates(savedWeapons);
            for (int i = 0; i < grids.Length; i++) grids[i].LoadSaveData(savedGrids[i]);
            bar.LoadSlotIds(savedSlots);
            if (ui != null) Call(ui, "RefreshAllGrids");
            if (!wasOpen) UIManager.Instance.CloseAll();
            GameInput.Virtual = virtualInput;
            SaveManager.SuppressWrites = suppress;
        }
    }
}
