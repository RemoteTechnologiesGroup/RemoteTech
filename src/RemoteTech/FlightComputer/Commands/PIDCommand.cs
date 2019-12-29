using System;

namespace RemoteTech.FlightComputer.Commands
{
    public class PIDCommand : AbstractCommand
    {
        [Persistent] public double kp;
        [Persistent] public double ki;
        [Persistent] public double kd;

        public override string ShortName
        {
            get
            {
                return "Flight PID Controller parameters";
            }
        }

        public override string Description
        {
            get
            {
                return ShortName +":" + Environment.NewLine +
                    "Term P of " + kp + Environment.NewLine +
                    "Term I of " + ki + Environment.NewLine +
                    "Term D of " + kd + Environment.NewLine +
                    base.Description;
            }
        }

        public static PIDCommand WithNewChanges(double new_kp, double new_ki, double new_kd)
        {
            return new PIDCommand()
            {
                kp = new_kp,
                ki = new_ki,
                kd = new_kd,
                TimeStamp = RTUtil.GameTime,
            };
        }

        public override bool Pop(FlightComputer f)
        {
            if (f != null)
            {
                FlightComputer.PIDKp = kp;
                FlightComputer.PIDKi = ki;
                FlightComputer.PIDKd = kd;

                f.PIDController.setPIDParameters(kp, ki, kd);
            }
            return false;
        }
    }
}
