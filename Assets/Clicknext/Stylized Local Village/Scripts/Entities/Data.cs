using System;
namespace Clicknext.StylizedLocalVillage.Entities 
{
    [Serializable]
    public struct ProcessData
    {
        public Product product;
        public RawData[] rawData;
    }

    [Serializable]
    public struct RawData
    {
        public Product product;
        public int amount;
    }
}