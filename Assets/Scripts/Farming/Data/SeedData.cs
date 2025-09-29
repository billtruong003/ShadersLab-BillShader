using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Seed Data", menuName = "Farming/Seed Data")]
public class SeedData : ItemData
{
    [Header("Seed Information")]
    public int daysToGrow;
    public CropData cropToYield;

    [Tooltip("Các model/prefab tương ứng với từng giai đoạn phát triển của cây.")]
    public List<GameObject> growthStages;

    public override void Use(GameObject user)
    {
        Debug.Log("This is a seed. Try planting it on tilled soil.");
    }
}