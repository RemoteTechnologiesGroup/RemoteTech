using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public interface IVesselAntenna : IAntenna
    {
        IVessel Vessel { get; }

        event Action<IVesselAntenna> Destroyed;
    }

    public static class VesselAntenna
    {
        public const String AntennaIdentifier      = "IsRTAntenna";
        public const String SingleTargetIdentifier = "RTAntennaTarget";
        public const String TargetsIdentifier      = "RTAntennaTargets";
        public const String RadiansIdentifier      = "RTDishRadians";
        public const String OmniRangeIdentifier    = "RTOmniRange";
        public const String DishRangeIdentifier    = "RTDishRange";
        public const String IsPoweredIdentifier    = "IsRTPowered";
        public const String IsActiveIdentifier     = "IsRTActive";
        public const String DishAngleIdentifier    = "DishAngle";

        public static EventListWrapper<Target> ParseAntennaSingleTarget(ConfigNode node)
        {
            var guid = RTUtil.TryParseGuidNullable(node.GetValue(SingleTargetIdentifier) ?? String.Empty) ?? Guid.Empty;
            return new EventListWrapper<Target>(new List<Target>() { Target.SingleUnsafeCompatibility(guid) });
        }

        public static EventListWrapper<Target> ParseAntennaTargets(ConfigNode node)
        {
            if (!node.HasNode(TargetsIdentifier)) 
                return new EventListWrapper<Target>(new List<Target>() { Target.Empty });
            return ConfigNode.CreateObjectFromConfig<EventListWrapper<Target>>(node.GetNode(TargetsIdentifier));
        }

        public static void SaveAntennaTargets(ConfigNode node, EventListWrapper<Target> targets)
        {
            var targetsNode = ConfigNode.CreateConfigFromObject(targets, 0, null);
            targetsNode.name = TargetsIdentifier;

            if (node.HasNode(TargetsIdentifier)) 
                node.SetNode(TargetsIdentifier, targetsNode);
            else 
                node.AddNode(targetsNode);
        }

        public static double ParseAntennaRadians(ConfigNode node)
        {
            return RTUtil.TryParseDoubleNullable(node.GetValue(RadiansIdentifier)) ?? 0.0f;
        }

        public static float ParseAntennaOmniRange(ConfigNode node)
        {
            return RTUtil.TryParseSingleNullable(node.GetValue(OmniRangeIdentifier)) ?? 0.0f;
        }

        public static float ParseAntennaDishRange(ConfigNode node)
        {
            return RTUtil.TryParseSingleNullable(node.GetValue(DishRangeIdentifier)) ?? 0.0f;
        }

        public static bool ParseAntennaIsPowered(ConfigNode node)
        {
            return RTUtil.TryParseBooleanNullable(node.GetValue(IsPoweredIdentifier)) ?? false;
        }

        public static bool ParseAntennaIsActivated(ConfigNode node)
        {
            return RTUtil.TryParseBooleanNullable(node.GetValue(IsActiveIdentifier)) ?? false;
        }

        public static bool IsAntenna(this ProtoPartModuleSnapshot protoPartModuleSnapshot)
        {
            return protoPartModuleSnapshot.GetBool(AntennaIdentifier) &&
                   protoPartModuleSnapshot.GetBool(IsPoweredIdentifier) &&
                   protoPartModuleSnapshot.GetBool(IsActiveIdentifier);
        }

        public static bool IsAntenna(this PartModule partModule)
        {
            return partModule.Fields.GetValue<bool>(AntennaIdentifier) &&
                   partModule.Fields.GetValue<bool>(IsPoweredIdentifier) &&
                   partModule.Fields.GetValue<bool>(IsActiveIdentifier);
        }
    }
}
