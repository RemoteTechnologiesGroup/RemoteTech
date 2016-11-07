using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class RTPartInfoReloader : MonoBehaviour
    {
        public void Start()
        {
            StartCoroutine(ReloadInfos());
        }

        private IEnumerator ReloadInfos()
        {
            yield return null;

            var loadedParts = PartLoader.LoadedPartsList;

            // find all RT parts
            var rtPartList = loadedParts.Where(
                part => part.partPrefab.Modules != null && 
                (part.partPrefab.Modules.Contains<Modules.ModuleRTAntennaPassive>() ||
                part.partPrefab.Modules.Contains<Modules.ModuleRTAntenna>() ||
                part.partPrefab.Modules.Contains<Modules.ModuleSPU>() ||
                part.partPrefab.Modules.Contains<Modules.ModuleSPUPassive>())
                );

            foreach (var rtPart in rtPartList)
            {
                var partHasTechPerk = rtPart.partPrefab.Modules.Contains<Modules.ModuleRTAntennaPassive>();
                var techPerkRefreshed = false;

                for (var i = rtPart.moduleInfos.Count - 1; i >= 0; --i)
                {
                    var info = rtPart.moduleInfos[i];

                    var moduleName = info.moduleName;
                    if (moduleName == "Technology Perk" || moduleName == "Antenna" || moduleName == "Signal Processor" )
                    {
                        var moduleClassName = string.Empty;
                        if (moduleName == "Technology Perk")  moduleClassName = "ModuleRTAntennaPassive";
                        if (moduleName == "Antenna")          moduleClassName = "ModuleRTAntenna";
                        if (moduleName == "Signal Processor") moduleClassName = "ModuleSPU";

                        // no moduleClassName found, skip to the next part
                        if (moduleClassName == string.Empty) continue;

                        // otherwise refresh the infos
                        var newInfos = RefreshPartInfo(rtPart.partPrefab, moduleClassName);
                        if (newInfos != string.Empty)
                        {
                            if (moduleName == "Technology Perk")
                            {
                                techPerkRefreshed = true;
                            }
                            info.info = newInfos;
                        }
                        else
                        {
                            // Remove this info block, should only be the Technology Perk
                            rtPart.moduleInfos.RemoveAt(i);
                        }
                    }
                }

                // add a new info block for TechPerk
                if (techPerkRefreshed == false && partHasTechPerk && rtPart.partPrefab.Modules.GetModule<Modules.ModuleRTAntennaPassive>().Unlocked)
                {
                    rtPart.moduleInfos.Add(CreateTechPerkInfoforPart(rtPart.partPrefab));
                }
            }
        }

        /// <summary>
        /// This method creates a new Info-block for the <paramref name="part"/> and
        /// the <paramref name="moduleClassName"/> and returns a string. If no info
        /// is available the result is an empty string. 
        /// </summary>
        /// <param name="part">Part to get the infos</param>
        /// <param name="moduleClassName">module class name for the <see cref="PartModule.GetInfo"/> call</param>
        /// <returns>new info block</returns>
        private static string RefreshPartInfo(Part part, string moduleClassName)
        {
            var newInfo = string.Empty;
                
            foreach (var pm in part.Modules)
            {
                if (pm.moduleName == moduleClassName)
                {
                    newInfo = pm.GetInfo();
                }
            }

            return newInfo;
        }

        /// <summary>
        /// This method creates a new AvailablePart.ModuleInfo object with
        /// the Technology Perk info block.
        /// </summary>
        /// <param name="part">Part to get the infos</param>
        /// <returns></returns>
        private AvailablePart.ModuleInfo CreateTechPerkInfoforPart(Part part)
        {
            // Get the infos for the TechPerk info block
            var perkInfos = RefreshPartInfo(part, "ModuleRTAntennaPassive");

            // creates a new info block
            var newPerkInfo = new AvailablePart.ModuleInfo
            {
                info = perkInfos,
                moduleName = "Technology Perk"
            };

            return newPerkInfo;
        }
    }
}
