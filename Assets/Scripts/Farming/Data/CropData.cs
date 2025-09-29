using UnityEngine;

[CreateAssetMenu(fileName = "New Crop Data", menuName = "Farming/Crop Data")]
public class CropData : ItemData
{
    [Header("Crop Information")]
    public int purchasePrice;
    public int sellPrice;

    public override void Use(GameObject user)
    {
        // Có thể thêm logic ăn để hồi máu hoặc năng lượng ở đây.
        Debug.Log($"Consumed {itemName}. It was delicious.");
    }
}