using System;
using Barotrauma.MoreLevelContent.Shared.Utils;
using MoreLevelContent.Shared.Utils;
using Barotrauma;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

namespace MoreLevelContent.Shared.XML
{
    public class XMLManager : Singleton<XMLManager>
    {
        public override void Setup()
        {
            return;
            List<ContentFile> otherFiles = new List<ContentFile>();
            foreach (var package in ContentPackageManager.AllPackages)
            {
                var a = package.Files.Where(f => f.GetType() == typeof(OtherFile));
                otherFiles.AddRange(a);
            }
            Log.Debug($"Collected {otherFiles.Count} other files to check");

            foreach (ContentFile file in otherFiles)
            {
                XDocument doc = XMLExtensions.TryLoadXml(file.Path);
                if (doc == null) { continue; }

                var rootElement = doc.Root.FromPackage(file.ContentPackage);
                var tags = rootElement.GetAttributeStringArray("tags", Array.Empty<string>(), convertToLowerInvariant: true);
                if (!tags.Contains("MLC")) continue;
                Log.Debug("");
            }
        }
    }
}