# -*- coding: utf-8 -*-
"""Create ItemData SO from tools/items.csv. Run: python tools/generate_all_items.py"""
import csv
import os
import re
import uuid

SCRIPT_GUID = "fe39bfb873cc9304c8d8106a9584fe35"
DIR = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Items"))
CSV_PATH = os.path.join(os.path.dirname(__file__), "items.csv")
MED = {
    "bandage": "3df682d6038789446a7b822d46d2ab48",
    "splint": "b20cc94e77ff26546bd1b0fec9a92408",
    "painkiller": "bd6c2245073e02341ad2983b36c373b1",
    "kit": "41259a63b5b1c624c9e7ccde98743af2",
}


def asset_name(item_id: str) -> str:
    return "".join(w.capitalize() for w in item_id.split("_"))


def existing_ids():
    ids = set()
    for fn in os.listdir(DIR):
        if not fn.endswith(".asset"):
            continue
        with open(os.path.join(DIR, fn), encoding="utf-8") as f:
            m = re.search(r"itemId:\s*(\S+)", f.read())
            if m:
                ids.add(m.group(1))
    return ids


def write_asset(row: dict):
    p = row
    name = asset_name(p["id"])
    guid = uuid.uuid4().hex
    med_key = p.get("med") or ""
    med_line = (
        f"  medicalData: {{fileID: 11400000, guid: {MED[med_key]}, type: 2}}"
        if med_key in MED
        else "  medicalData: {fileID: 0}"
    )
    dur = int(p.get("dur") or 0)
    stack = 1 if dur else int(p["stack"])
    dur_block = ""
    if dur:
        dur_block = (
            f"  hasDurability: 1\n"
            f"  maxDurability: {int(p['maxDur'])}\n"
            f"  durabilityCostPerUse: {int(p['costDur'])}\n"
        )
    yaml = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: Game.Scripts::ItemData
  itemId: {p['id']}
  displayName: "{p['disp']}"
  description: "{p['desc']}"
  icon: {{fileID: 0}}
  gridWidth: {int(p['gw'])}
  gridHeight: {int(p['gh'])}
  category: {int(p['cat'])}
  rarity: {int(p['rar'])}
  maxStack: {stack}
  weight: {float(p['wt'])}
  isUsable: {int(p['use'])}
  useEffect: {int(p['fx'])}
  effectValue: {float(p['val'])}
{dur_block}{med_line}
  worldDropPrefab: {{fileID: 0}}
"""
    with open(os.path.join(DIR, f"{name}.asset"), "w", encoding="utf-8", newline="\n") as f:
        f.write(yaml)
    meta = f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(os.path.join(DIR, f"{name}.asset.meta"), "w", encoding="ascii", newline="\n") as f:
        f.write(meta)


def main():
    exist = existing_ids()
    created = skipped = 0
    with open(CSV_PATH, encoding="utf-8-sig") as f:   # utf-8-sig: BOM 있든 없든 처리(Excel 호환 위해 items.csv에 BOM 부착됨)
        for row in csv.DictReader(f):
            if row["id"] in exist:
                skipped += 1
                continue
            write_asset(row)
            created += 1
    print(f"Existing: {len(exist)} | Created: {created} | Skipped: {skipped} | Total: {len(exist) + created}")


if __name__ == "__main__":
    main()
