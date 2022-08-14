using System;
using Barotrauma.MoreLevelContent.Shared.Utils;
using MoreLevelContent.Shared.Utils;
using Barotrauma;
using System.Linq;

namespace MoreLevelContent.Shared.XML
{
    public class XMLManager : Singleton<XMLManager>
    {
        public override void Setup()
        {
            foreach (var package in ContentPackageManager.AllPackages)
            {
                var a = package.Files.Where(f => f.GetType() == typeof(OtherFile));
                if (a.Count() > 0)
                {
                    Log.Debug("We found an other file boys!");
                }
            }
        }
    }
}