using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    [Header("--- LOADOUT ---")]
    public List<GunData> availableGuns = new List<GunData>();
    public int currentGunIndex = 0;

    public GunData CurrentWeapon
    {
        get
        {
            if (availableGuns == null || availableGuns.Count == 0) return null;
            return availableGuns[currentGunIndex];
        }
    }

    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= availableGuns.Count) return;
        currentGunIndex = index;
        Debug.Log($"<color=green>[INVENTORY]</color> Đã trang bị súng: {CurrentWeapon.gunName}");
    }
}