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

            List<AvailablePart> loadedParts = PartLoader.LoadedPartsList;
            // find all RT parts
            IEnumerable<AvailablePart> rtPartList = loadedParts.Where(part => part.partPrefab.Modules != null && (part.partPrefab.Modules.Contains<Modules.ModuleRTAntennaPassive>() ||
                                                                                                                  part.partPrefab.Modules.Contains<Modules.ModuleRTAntenna>() ||
                                                                                                                  part.partPrefab.Modules.Contains<Modules.ModuleSPU>() ||
                                                                                                                  part.partPrefab.Modules.Contains<Modules.ModuleSPUPassive>()));



            foreach (var rtPart in rtPartList)
            {
                bool partHasTechPerk = rtPart.partPrefab.Modules.Contains<Modules.ModuleRTAntennaPassive>();
                bool techPerkRefreshed = false;

                for (int i = rtPart.moduleInfos.Count - 1; i >= 0; --i)
                {
                    var info = rtPart.moduleInfos[i];

                    String moduleName = info.moduleName;
                    if (moduleName == "Technology Perk" || moduleName == "Antenna" || moduleName == "Signal Processor" )
                    {
                        String moduleClassName = String.Empty;
                        if (moduleName == "Technology Perk")  moduleClassName = "ModuleRTAntennaPassive";
                        if (moduleName == "Antenna")          moduleClassName = "ModuleRTAntenna";
                        if (moduleName == "Signal Processor") moduleClassName = "ModuleSPU";

                        // no moduleClassName found, skip to the next part
                        if (moduleClassName == String.Empty) continue;

                        // otherwise refresh the infos
                        string new_infos = this.RefreshPartInfo(rtPart.partPrefab, moduleClassName);
                        if (new_infos != String.Empty)
                        {
                            if (moduleName == "Technology Perk")
                            {
                                techPerkRefreshed = true;
                            }
                            info.info = new_infos;
                        }
                        else
                        {
                            // Remove this info block, should only be the Technology Perk
                            rtPart.moduleInfos.RemoveAt(i);
                        }
                    }
                }

                // add a new info block for TechPerk
                if (techPerkRefreshed == false && partHasTechPerk == true && rtPart.partPrefab.Modules.GetModule<Modules.ModuleRTAntennaPassive>().Unlocked)
                {
                    rtPart.moduleInfos.Add(this.CreateTechPerkInfoforPart(rtPart.partPrefab));
                }
            }
        }

        /// <summary>
        /// This method creates a new Info-block for the <paramref name="part"/> and
        /// the <paramref name="moduleClassName"/> and returns a string. If no info
        /// is available the result is an empty string.
        /// 
        /// </summary>
        /// <param name="part">Part to get the infos</param>
        /// <param name="moduleClassName">module class name for the getInfo() call</param>
        /// <returns>new info block</returns>
        private String RefreshPartInfo(Part part, String moduleClassName)
        {
            String new_info = String.Empty;
                
            foreach (PartModule pm in part.Modules)
            {
                if (pm.moduleName == moduleClassName)
                {
                    new_info = pm.GetInfo();
                }
            }

            return new_info;
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
            string perk_infos = this.RefreshPartInfo(part, "ModuleRTAntennaPassive");

            // creates a new info block
            AvailablePart.ModuleInfo NewPerkInfo = new AvailablePart.ModuleInfo();
            NewPerkInfo.info = perk_infos;
            NewPerkInfo.moduleName = "Technology Perk";

            return NewPerkInfo;
        }
    }
}
