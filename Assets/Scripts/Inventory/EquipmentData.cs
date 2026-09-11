using System;
using UnityEngine;

namespace ZeldaOoT.Inventory
{
    public enum EquipmentCategory
    {
        Weapon,
        Bow,
        Shield,
        Armor,
        Material,
        Item
    }

    [Serializable]
    public class EquipmentData
    {
        public string id;
        public string displayName;
        public EquipmentCategory category;
        public string subType;
        public int powerValue; // Attack or Defense
        public Color iconColor = Color.white;
        public string iconSymbol = "⚔️";
        public string description;

        public EquipmentData(string id, string displayName, EquipmentCategory category, string subType, int powerValue, Color iconColor, string iconSymbol, string description)
        {
            this.id = id;
            this.displayName = displayName;
            this.category = category;
            this.subType = subType;
            this.powerValue = powerValue;
            this.iconColor = iconColor;
            this.iconSymbol = iconSymbol;
            this.description = description;
        }

        // Backward compatibility constructor
        public EquipmentData(string id, string displayName, EquipmentCategory category, int powerValue, Color iconColor, string description)
            : this(id, displayName, category, category.ToString(), powerValue, iconColor, "⚔️", description)
        {
        }
    }
}
