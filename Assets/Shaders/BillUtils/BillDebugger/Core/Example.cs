using BillDebugger;
using Sirenix.OdinInspector;
using UnityEngine;

public class AdvancedGameplaySystem : MonoBehaviour
{

    [Button]
    public void ExecuteComplexLogic(int value)
    {
        // Log một thông điệp đơn giản
        BillDebug.Log(DebugUser.DEV1, $"Executing complex logic with value: {value}");

        if (value < 0)
        {
            // Log kèm stacktrace để truy vết lỗi
            BillDebug.LogClickableTrace(DebugUser.QA1, "Negative value detected, this might be an issue.");
        }
    }
}
