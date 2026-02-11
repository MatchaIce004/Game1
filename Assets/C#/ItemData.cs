using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

[CreateAssetMenu(fileName = "Item", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    public string id;
    public string itemName;
    public Sprite icon;
    public string description;
    public ItemRarity rarity = ItemRarity.Common;

#if UNITY_EDITOR
    private void OnValidate()
    {
       // まだIDが空なら自動で新規IDを振る
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();   // ← ここを修正（GUID ではなく Guid）

            // ついでに変更を保存してくれると安全
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}

