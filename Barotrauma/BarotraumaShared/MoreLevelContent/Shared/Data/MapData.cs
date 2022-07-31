using Barotrauma;
using System;
using System.Runtime.CompilerServices;

namespace MoreLevelContent.Shared.Data
{
    [Serializable]
    public class LevelData_MLC
    {
        public bool HasBeaconConstruction;

        public LevelData_MLC()
        {
            HasBeaconConstruction = false;
        }
    }

    public static class LevelDataExtension
    {
        private static readonly ConditionalWeakTable<LevelData, LevelData_MLC> data = new ConditionalWeakTable<LevelData, LevelData_MLC>();

        internal static LevelData_MLC MLC(this LevelData levelData) => data.GetOrCreateValue(levelData);

        internal static void AddData(this LevelData levelData, LevelData_MLC additional)
        {
            try
            {
                data.Add(levelData, additional);
            } catch(Exception e) { Log.Error(e.ToString()); }
        }
    }
}