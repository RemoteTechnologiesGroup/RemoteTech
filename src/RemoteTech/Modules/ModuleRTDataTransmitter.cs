using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech.Modules
{
    public sealed class ModuleRTDataTransmitter : PartModule, IScienceDataTransmitter
    {
        [KSPField]
        public float
            PacketInterval = 0.5f,
            PacketSize = 1.0f,
            PacketResourceCost = 10f;

        [KSPField]
        public String
            RequiredResource = "ElectricCharge";
        [KSPField(guiName = "Comms", guiActive = true)]
        public String GUI_Status = "";

        private bool mBusy;
        private List<ScienceData> mQueue = new List<ScienceData>();
        
        private Callback CallBackFunction;

        // Compatible with ModuleDataTransmitter
        public override void OnLoad(ConfigNode node)
        {
            foreach (ConfigNode data in node.GetNodes("CommsData"))
            {
                mQueue.Add(new ScienceData(data));
            }

            var antennas = part.FindModulesImplementing<ModuleRTAntenna>();
            GUI_Status = "Idle";
        }

        // Compatible with ModuleDataTransmitter
        public override void OnSave(ConfigNode node)
        {
            mQueue.ForEach(d => d.Save(node.AddNode("CommsData")));
        }
       
        bool IScienceDataTransmitter.CanTransmit()
        {
            return true;
        }

        float IScienceDataTransmitter.DataRate { get { return PacketSize / PacketInterval; } }
        double IScienceDataTransmitter.DataResourceCost { get { return PacketResourceCost / PacketSize; } }
        bool IScienceDataTransmitter.IsBusy() { return mBusy; }

        void IScienceDataTransmitter.TransmitData(List<ScienceData> dataQueue, Callback NewCallBackFunction)
        {
            mQueue.AddRange(dataQueue);
            if (!mBusy)
            {
                StartCoroutine(Transmit());
                CallBackFunction = NewCallBackFunction;    
            }
        }

        private IEnumerator Transmit()
        {
            var msg = new ScreenMessage(String.Format("[{0}]: Starting Transmission...", part.partInfo.title), 4f, ScreenMessageStyle.UPPER_LEFT);
            var msg_status = new ScreenMessage(String.Empty, 4.0f, ScreenMessageStyle.UPPER_LEFT);
            ScreenMessages.PostScreenMessage(msg);

            mBusy = true;

            while (mQueue.Any())
            {
                RnDCommsStream commStream = null;
                var science_data = mQueue[0];
                var data_amount = science_data.dataAmount;
                mQueue.RemoveAt(0);
                var subject = ResearchAndDevelopment.GetSubjectByID(science_data.subjectID);
                int packets = Mathf.CeilToInt(science_data.dataAmount / PacketSize);
                if (ResearchAndDevelopment.Instance != null)
                {
                    // pre calculate the time interval - fix for x64 systems
                    // workaround for issue #136
                    float time1 = Time.time;
                    yield return new WaitForSeconds(PacketInterval);
                    // get the delta time
                    float x64PacketInterval = (Time.time - time1);

                    RTLog.Notify("Changing RnDCommsStream timeout from {0} to {1}", PacketInterval, x64PacketInterval);

                    commStream = new RnDCommsStream(subject, science_data.dataAmount, x64PacketInterval,
                                            science_data.transmitValue, ResearchAndDevelopment.Instance);
                }
                //StartCoroutine(SetFXModules_Coroutine(modules_progress, 0.0f));
                float power = 0;
                while (packets > 0)
                {
                    power += part.RequestResource("ElectricCharge", PacketResourceCost - power);
                    if (power >= PacketResourceCost * 0.95)
                    {
                        float frame = Math.Min(PacketSize, data_amount);
                        power -= PacketResourceCost;
                        GUI_Status = "Uploading Data...";
                        data_amount -= frame;
                        packets--;
                        float progress = (science_data.dataAmount - data_amount) / science_data.dataAmount;
                        //StartCoroutine(SetFXModules_Coroutine(modules_progress, progress));
                        msg_status.message = String.Format("[{0}]: Uploading Data... {1}", part.partInfo.title, progress.ToString("P0"));
                        RTLog.Notify("[Transmitter]: Uploading Data... ({0}) - {1} Mits/sec. Packets to go: {2} - Files to Go: {3}",
                            science_data.title, (PacketSize / PacketInterval).ToString("0.00"), packets, mQueue.Count);
                        ScreenMessages.PostScreenMessage(msg_status, true);
                        if (commStream != null)
                        {
                            commStream.StreamData(frame, this.part.vessel.protoVessel);
                        }
                    }
                    else
                    {
                        msg.message = String.Format("<b><color=orange>[{0}]: Warning! Not Enough {1}!</color></b>", part.partInfo.title, RequiredResource);
                        ScreenMessages.PostScreenMessage(msg, true);
                        GUI_Status = String.Format("{0}/{1} {2}", power, PacketResourceCost, RequiredResource);

                    }
                    yield return new WaitForSeconds(PacketInterval);
                }
                yield return new WaitForSeconds(PacketInterval * 2);
            }
            mBusy = false;
            msg.message = String.Format("[{0}]: Done!", part.partInfo.title);
            ScreenMessages.PostScreenMessage(msg, true);
            GUI_Status = "Idle";
            yield break;
        }
    }
}
