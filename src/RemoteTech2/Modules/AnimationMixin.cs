using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class AnimationMixin
    {
        private static ILogger Logger = RTLogger.CreateLogger(typeof(AnimationMixin));

        private const String DeployIndicator = "DeployFxModules";
        private const String ProgressIndicator = "ProgressFxModules";

        public AnimationMixin(Func<int, PartModule> getPartModule)
        {
            this.getPartModule = getPartModule;
        }

        public bool Animating { get { return deployFxModules.Any(fx => fx.GetScalar > 0.1f && fx.GetScalar < 0.9f); } }

        public float Progress
        {
            set
            {
                if (progressCoroutine != null)
                {
                    progressCoroutine.Abort = true;
                    progressCoroutine = null;
                    Logger.Debug("Aborting existing Progress coroutine.");
                }
                progressCoroutine = new Coroutine(SetFXModules_Coroutine(progressFxModules, value));
                RTCore.Instance.StartCoroutine(progressCoroutine.GetEnumerator());
                Logger.Debug("Setting Progress to {0}.", value);
            }
        }
        
        public bool Deployed
        {
            set
            {
                if (deployedCoroutine != null) 
                {
                    deployedCoroutine.Abort = true;
                    deployedCoroutine = null;
                    Logger.Debug("Aborting existing Deployed coroutine.");
                }
                deployedCoroutine = new Coroutine(SetFXModules_Coroutine(deployFxModules, value ? 1.0f : 0.0f));
                RTCore.Instance.StartCoroutine(deployedCoroutine.GetEnumerator());
                Logger.Debug("Setting Deployed to {0}.", value);
            }
        }

        private int[] deployFxModuleIndices;
        private int[] progressFxModuleIndices;
        private Coroutine progressCoroutine;
        private Coroutine deployedCoroutine;
        private List<IScalarModule> deployFxModules = new List<IScalarModule>();
        private List<IScalarModule> progressFxModules = new List<IScalarModule>();
        private readonly Func<int, PartModule> getPartModule;

        public void Load(ConfigNode node)
        {
            if (node.HasValue(DeployIndicator))
            {
                Logger.Debug("Found Deploy Indices.");
                deployFxModuleIndices = KSPUtil.ParseArray<Int32>(node.GetValue(DeployIndicator), new ParserMethod<Int32>(Int32.Parse));
            }
            if (node.HasValue(ProgressIndicator))
            {
                Logger.Debug("Found Progress Indices.");
                progressFxModuleIndices = KSPUtil.ParseArray<Int32>(node.GetValue(ProgressIndicator), new ParserMethod<Int32>(Int32.Parse));
            }
        }

        public void Start()
        {
            deployFxModules = FindFxModules(this.deployFxModuleIndices);
            progressFxModules = FindFxModules(this.progressFxModuleIndices);
        }

        private List<IScalarModule> FindFxModules(int[] indices)
        {
            var modules = new List<IScalarModule>();
            if (indices == null) return modules;
            foreach (int i in indices)
            {
                var item = getPartModule(i) as IScalarModule;
                if (item != null)
                {
                    item.SetUIWrite(false);
                    item.SetUIRead(false);
                    modules.Add(item);
                }
                else
                {
                    Logger.Error("PartModule {0} doesn't implement IScalarModule", getPartModule(i).name);
                }
            }
            return modules;
        }


        private IEnumerator SetFXModules_Coroutine(List<IScalarModule> modules, float target)
        {
            bool busy = true;
            while (busy)
            {
                busy = false;
                foreach (var module in modules)
                {
                    if (Mathf.Abs(module.GetScalar - target) > 0.01f)
                    {
                        module.SetScalar(target);
                        busy = true;
                    }
                }
                yield return true;
            }
        }
    }
}
