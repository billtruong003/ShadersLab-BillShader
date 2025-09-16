using System.Collections.Generic;
using MonsterLegion.Data;

namespace MonsterLegion.Combat
{
    public static class ElementalAdvantageSystem
    {
        private static readonly Dictionary<(Element, Element), float> AdvantageMatrix = new Dictionary<(Element, Element), float>
        {
            // Cặp (Attacker, Defender) -> Multiplier
            { (Element.Fire, Element.Earth), 1.3f },
            { (Element.Earth, Element.Wind), 1.3f },
            { (Element.Wind, Element.Water), 1.3f },
            { (Element.Water, Element.Fire), 1.3f },
            { (Element.Light, Element.Dark), 1.3f },
            { (Element.Dark, Element.Light), 1.3f },

            { (Element.Fire, Element.Water), 0.7f },
            { (Element.Earth, Element.Fire), 0.7f },
            { (Element.Wind, Element.Earth), 0.7f },
            { (Element.Water, Element.Wind), 0.7f },
        };

        public static float GetDamageMultiplier(Element attackerElement, Element targetElement)
        {
            if (AdvantageMatrix.TryGetValue((attackerElement, targetElement), out float multiplier))
            {
                return multiplier;
            }
            return 1.0f; // Sát thương chuẩn nếu không có trong ma trận
        }
    }
}