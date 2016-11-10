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
        public String GUIStatus = "";

        private bool isBusy;
        private readonly List<ScienceData> scienceDataQueue = new List<ScienceData>();


        /// <summary> When a transmission almost succeed but not totally (issue #667), 
        /// e.g. the probe transmitted 9.999999 instead of 10 due to a floating point error in KSP, 
        /// we use this constant value as the maximum size that can be left to transmit. 
        /// If the remaining size is less or equal than this constant value, RT will push the remaining science by itself (like a ghost packet, to alleviate the rounding error).
        /// If it's bigger than this constant value, RT will not do anything to push the remaining size to the R & D center.
        /// </summary>
        private const float PacketRemainingSize = 0.1f;

        // Compatible with ModuleDataTransmitter
        public override void OnLoad(ConfigNode node)
        {
            RTLog.Notify("ModuleRTDataTransmitter::OnLoad");
            foreach (ConfigNode data in node.GetNodes("CommsData"))
            {
                scienceDataQueue.Add(new ScienceData(data));
            }

            var antennas = part.FindModulesImplementing<ModuleRTAntenna>();
            GUIStatus = "Idle";
        }

        // Compatible with ModuleDataTransmitter
        public override void OnSave(ConfigNode node)
        {
            RTLog.Notify("ModuleRTDataTransmitter::OnSave");
            scienceDataQueue.ForEach(d => d.Save(node.AddNode("CommsData")));
        }

        bool IScienceDataTransmitter.CanTransmit()
        {
            RTLog.Notify("ModuleRTDataTransmitter::CanTransmit");
            return true;
        }

        void IScienceDataTransmitter.TransmitData(List<ScienceData> dataQueue)
        {
            RTLog.Notify("ModuleRTDataTransmitter::TransmitData(2p)");
            scienceDataQueue.AddRange(dataQueue);
            if (!isBusy)
            {
                StartCoroutine(Transmit());
            }
        }

        float IScienceDataTransmitter.DataRate { get { return PacketSize / PacketInterval; } }
        double IScienceDataTransmitter.DataResourceCost { get { return PacketResourceCost / PacketSize; } }
        bool IScienceDataTransmitter.IsBusy() { return isBusy; }

        private IEnumerator Transmit(Callback callback = null)
        {
            RTLog.Notify("ModuleRTDataTransmitter::Transmit");
            var msg = new ScreenMessage(String.Format("[{0}]: Starting Transmission...", part.partInfo.title), 4f, ScreenMessageStyle.UPPER_LEFT);
            var msgStatus = new ScreenMessage(String.Empty, 4.0f, ScreenMessageStyle.UPPER_LEFT);
            ScreenMessages.PostScreenMessage(msg);

            isBusy = true;

            while (scienceDataQueue.Any())
            {
                var scienceData = scienceDataQueue[0];
                var dataAmount = scienceData.dataAmount;
                scienceDataQueue.RemoveAt(0);
                scienceData.triggered = true;

                var subject = ResearchAndDevelopment.GetSubjectByID(scienceData.subjectID);
                if (subject == null)
                    subject = new ScienceSubject("", "", 1, 0, 0);

                int packets = Mathf.CeilToInt(scienceData.dataAmount / PacketSize);

                RnDCommsStream commStream = null;
                if (ResearchAndDevelopment.Instance != null)
                {
                    // pre-calculate the time interval - fix for x64 systems
                    // workaround for issue #136
                    float time1 = Time.time;
                    yield return new WaitForSeconds(PacketInterval);

                    // get the delta time
                    float x64PacketInterval = (Time.time - time1);
                    RTLog.Notify("Changing RnDCommsStream timeout from {0} to {1}", PacketInterval, x64PacketInterval);

                    //TODO (porting to 1.2): check if scienceData.baseTransmitValue alone or with scienceData.transmitBonus
                    commStream = new RnDCommsStream(subject, scienceData.dataAmount, x64PacketInterval,
                                            scienceData.baseTransmitValue, false, ResearchAndDevelopment.Instance);
                }
                //StartCoroutine(SetFXModules_Coroutine(modules_progress, 0.0f));
                float power = 0;
                while (packets > 0)
                {
                    power += part.RequestResource("ElectricCharge", PacketResourceCost - power);
                    if (power >= PacketResourceCost * 0.95)
                    {
                        GUIStatus = "Uploading Data...";

                        // remove some power due to transmission
                        power -= PacketResourceCost;

                        // transmitted size
                        float frame = Math.Min(PacketSize, dataAmount);

                        // subtract current packet size from data left to transmit
                        // and clamp it to 1 digit precision to avoid large float precision error (#667)
                        dataAmount -= frame;
                        dataAmount = (float)Math.Round(dataAmount, 1);

                        packets--;

                        float progress = (scienceData.dataAmount - dataAmount) / scienceData.dataAmount;
                        msgStatus.message = String.Format("[{0}]: Uploading Data... {1:P0}", part.partInfo.title, progress);
                        ScreenMessages.PostScreenMessage(msgStatus);

                        RTLog.Notify("[Transmitter]: Uploading Data... ({0}) - {1} Mbits/sec. Packets to go: {2} - Other experiments waiting to transfer: {3}",
                            scienceData.title, (PacketSize / PacketInterval).ToString("0.00"), packets, scienceDataQueue.Count);

                        // if we've a defined callback parameter so skip to stream each packet
                        if (commStream != null && callback == null)
                        {
                            RTLog.Notify(
                                "[Transmitter]: PacketSize: {0}; Transmitted size (frame): {1}; Data left to transmit (dataAmount): {2}; Packets left (packets): {3}",
                                PacketSize, frame, dataAmount, packets);

                            // use try / catch to prevent NRE spamming in KSP code when RT is used with other mods.
                            try
                            {
                                commStream.StreamData(frame, vessel.protoVessel);
                            }
                            catch (NullReferenceException nre)
                            {
                                RTLog.Notify("A problem occurred during science transmission: {0}", RTLogLevel.LVL2, nre);
                            }

                            // TODO: remove this when fixed in stock
                            // Fix a problem in stock KSP (discovered in 1.1.3, and still here in 1.2.1)
                            // issue #667 ; floating point error in RnDCommsStream.StreamData method when adding to dataIn private field
                            // e.g scienceData.dataAmount is 10 but in the end RnDCommsStream.dataIn will be 9.999999, so the science never
                            //     gets registered to the ResearchAndDevelopment center.
                            if (packets == 0) // check that we have no packet left to send.
                            {
                                // get the private field (dataIn) in RnDCommsStream. This field is subject to floating point rounding error
                                // We handle this problem on our side.
                                var dataIn = RTUtil.GetInstanceField(typeof(RnDCommsStream), commStream, "dataIn");
                                if (dataIn != null)
                                {
                                    // check if we have a delta (e.g. 10 - 9.999999999 will give us a tiny delta)
                                    var delta = scienceData.dataAmount - (float) dataIn;
                                    RTLog.Notify("[Transmitter]: delta: {0}", delta);

                                    // the delta must be positive and less than this constant to push / transmit the remaining size.
                                    // This prevent us pushing packets with too much leftover to transmit (e.g if there was a connection loss).
                                    if ((delta > 0f) && (delta <= PacketRemainingSize))
                                    {
                                        try
                                        {
                                            // we have a delta, try to send the remaining little bit of science
                                            commStream.StreamData(delta, vessel.protoVessel);
                                        }
                                        catch (NullReferenceException nre)
                                        {
                                            RTLog.Notify("A problem occurred during science transmission (delta): {0}",
                                                RTLogLevel.LVL2, nre);
                                        }
                                    }
                                }
                                else
                                {
                                    RTLog.Notify("[Transmitter]: dataIn is null.");
                                }
                            } // end stock fix
                        }
                        else
                        {
                            RTLog.Notify("[Transmitter]: [DEBUG] commstream is null and no callback");
                        }
                    }
                    else
                    {
                        // not enough power
                        msg.message = String.Format("<b><color=orange>[{0}]: Warning! Not Enough {1}!</color></b>", part.partInfo.title, RequiredResource);
                        ScreenMessages.PostScreenMessage(msg);

                        GUIStatus = String.Format("{0}/{1} {2}", power, PacketResourceCost, RequiredResource);
                    }

                    yield return new WaitForSeconds(PacketInterval);
                }

                // effectively inform the game that science has been transmitted
                GameEvents.OnTriggeredDataTransmission.Fire(scienceData, vessel, false);
                yield return new WaitForSeconds(PacketInterval * 2);
            }

            isBusy = false;

            msg.message = String.Format("[{0}]: Done!", part.partInfo.title);
            ScreenMessages.PostScreenMessage(msg);

            if (callback != null)
                callback.Invoke();

            GUIStatus = "Idle";
        }
    }
}
