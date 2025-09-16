namespace MonsterLegion.Data
{
    public enum Rarity { Common, Rare, Epic, Legendary }
    public enum Element { Fire, Water, Earth, Wind, Light, Dark, Neutral }
    public enum MonsterClass { Tank, Attacker, Support, Skirmisher }
    public enum TargetingPriority { Closest, Farthest, LowestHP, HighestThreat }
    public enum Personality { Neutral, Brave, Reckless, Cautious, Timid }
    public enum ProjectileMovementType { Linear, Homing, Lobbed }
    public enum ProjectileImpactEffectType { SingleTarget, AreaOfEffect, Pierce, Chain }
}