using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    [KSPModule("Technology Perk")]
    public class ModuleRTAntennaPassive : PartModule
    {
        
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class ModuleRTAntennaPassive_ReloadPartInfo : MonoBehaviour
    {
        public void Start()
        {
            StartCoroutine(RefreshPartInfo());
        }

        private IEnumerator RefreshPartInfo()
        {
            yield return null;
            foreach (var ap in PartLoader.LoadedPartsList.Where(ap => ap.partPrefab.Modules != null && ap.partPrefab.Modules.Contains("ModuleRTAntennaPassive")))
            {
                var new_info = new StringBuilder();
                foreach (PartModule pm in ap.partPrefab.Modules)
                {
                    var info = pm.GetInfo();
                    new_info.Append(info);
                    if (info != String.Empty) new_info.AppendLine();
                }
                ap.moduleInfo = new_info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
            }
        }
    }
}