//css_ref Microsoft.CSharp.dll
//css_ref Blacktrace.Common.dll
//css_ref Blacktrace.HardwareInterfaces.dll
//css_ref Blacktrace.HardwareDefinition.dll
//css_ref Blacktrace.MessageModel.dll
//css_ref Blacktrace.Scripting.dll

using System;

using Blacktrace.Common.Utils;
using Blacktrace.HardwareDefinition;

using Blacktrace.Scripting.Script;
using Blacktrace.Scripting.DataModel;
using Blacktrace.Scripting.Configuration;
using Blacktrace.Scripting.UserInterface;



public class TitanPumpSetupAndTestProtocolFactory : IScriptFactory
{
    public Version GetApiVersion()
    {
        return new Version(1, 10, 0, 0);
    }

    public void DefineDataModel(Context context)
    {
        context.DefineVersion(0, 0, 1)
            // Short description of what this script is for
            .DefineDescription("This protocol is only for use under the guidance of Blacktrace Support.")
            .DefineApparatus("titan pump", DeviceType.TitanPump)

            .DefineAccessLevel(AccessLevelEnum.Advanced);
    }

    public ScriptBase CreateScript()
    {
        return new TitanPumpSetupAndTestProtocol();
    }

    public void ExportConfiguration(Context context, ConfigFileBundle bundle)
    {
        context.ExportToBundle(bundle);
    }

    public void ImportConfiguration(Context context, ConfigFileBundle bundle)
    {
        // Extract FCC configuration file
        context.ImportFromBundle(bundle);
    }

    public class TitanPumpSetupAndTestProtocol : ScriptBase
    {
        protected override void OnLoad()
        {

        }

        protected override void OnStart()
        {
            if (!TitanPump.IsAssigned)
            {
                Abort("Need to assign a Titan Pump.");
            }
        }

        protected override void OnTimerTick()
        {
            switch (Step.Number)
            {
                case 1: EnableRemoteLock(); break;
                case 2: PumpSerialNumber(); break;
                case 3: CassetteSerialNumber(); break;
                case 4: ValveTypes(); break;
            }
        }

        private void EnableRemoteLock()
        {
            if (!Step.IsInitialized)
            {
                Output += "*** ENABLE REMOTE CONTROL ***";

                if (TitanPump.RemoteLock == RemoteLockEnum.Disabled)
                {
                    Output += "Enabling remote control...";
                    Status = "Enabling remote control";

                    TitanPump.RemoteLock = RemoteLockEnum.Enabled;
                }
                else
                {
                    Output += "Remote control already enabled";

                    Step.Next();
                }

                Step.IsInitialized = true;
            }
            else if (TitanPump.RemoteLock == RemoteLockEnum.Enabled)
            {
                Output += "Remote control enabled";
                Status = "Remote control enabled";

                Step.Next();
            }
        }

        private void PumpSerialNumber()
        {
            // display and set the serial number
            AssertRemoteLock();

            if (!Step.IsInitialized)
            {
                Output += "*** SET PUMP SERIAL NUMBER ***";
                Output += "Current pump serial number is " + TitanPump.SerialNumber;
                Status = "Setting pump serial number";

                InputDialog.Message = "Enter the correct PUMP serial number (6-9 digits) below \n (default value is the current value in pump memory)";
                InputDialog.Type = UserInput.InputType.Text;
                InputDialog.PartialMatchPattern = "^[0-9]{0,9}$";
                InputDialog.FinalMatchPattern = "^[0-9]{6,9}$";
                InputDialog.Buttons = UserInputButton.OK;
                InputDialog.TextValue = TitanPump.SerialNumber;
                InputDialog.Open();

                SkipButton.OnClick = null;
                ResumeButton.Text = "";
                ResumeButton.ToolTip = "";
                Step.IsInitialized = true;
            }

            if (!InputDialog.IsOpen)
            {
                if (string.IsNullOrEmpty(InputDialog.TextValue))
                {
                    Output += "Entered invalid serial number, it must be 6-9 digits long";
                    Status = "Entered invalid serial number";
                    Step.IsInitialized = false;
                }
                else
                {
                    Output += "Setting serial number to: " + InputDialog.TextValue;
                    TitanPump.SerialNumber = InputDialog.TextValue;
                    Step.Next();
                }
            }
        }

        private void CassetteSerialNumber()
        {
            // display and set the serial number
            AssertRemoteLock();

            string cassetteSerial = PumpProperties["cassette_serial_number"].ToString();

            if (!Step.IsInitialized)
            {
                Output += "*** SET CASSETTE SERIAL NUMBER ***";
                Output += "Current cassette serial number is " + PumpProperties["cassette_serial_number"];
                Status = "Setting cassette serial number";

                InputDialog.Message = "Enter the correct CASSETTE serial number (6-9 digits) below \n (default value is the current value in cassette memory)";
                InputDialog.Type = UserInput.InputType.Text;
                InputDialog.PartialMatchPattern = "^[0-9]{0,9}$";
                InputDialog.FinalMatchPattern = "^[0-9]{6,9}$";
                InputDialog.Buttons = UserInputButton.OK;
                InputDialog.TextValue = cassetteSerial;
                InputDialog.Open();

                SkipButton.OnClick = null;
                ResumeButton.Text = "";
                ResumeButton.ToolTip = "";
                Step.IsInitialized = true;
            }

            if (!InputDialog.IsOpen)
            {
                if (string.IsNullOrEmpty(InputDialog.TextValue))
                {
                    Output += "Entered invalid serial number, it must be 6-9 digits long";
                    Status = "Entered invalid serial number";
                    Step.IsInitialized = false;
                }
                else
                {
                    Output += "Setting serial number to: " + InputDialog.TextValue;
                    PumpProperties["cassette_serial_number"] = InputDialog.TextValue;
                    Step.Next();
                }
            }
        }


        private void ValveTypes()
        {
            AssertRemoteLock();

            if (!Step.IsInitialized)
            {
                Output += "*** SET VALVE TYPE ***";
                Output += "Current pump valve type: " + PumpProperties["valve_type"];
                Output += "Current cassette valve type: " + PumpProperties["cassette_valve_type"];
                Status = "Checking/Setting Valve Types.";




                SkipButton.OnClick = null;
                ResumeButton.Text = "";
                ResumeButton.ToolTip = "";
                Step.IsInitialized = true;
            }

            if (PumpProperties["valve_type"] != 1 || PumpProperties["cassette_valve_type"] != 1)
            {
                if (PumpProperties["valve_type"] != 1)
                {
                    PumpProperties["valve_type"] = 1;

                    Output += "Setting pump valve type to 1";
                }

                if (PumpProperties["cassette_valve_type"] != 1)
                {
                    PumpProperties["cassette_valve_type"] = 1;

                    Output += "Setting cassette valve type to 1";
                }

                Output += "COMPLETE";
                Finish();
            }
            else
            {
                Output += "COMPLETE";
                Finish();
            }
        }

        


        protected override void CheckForHardwareError()
        {
            // This empty implementation prevents the protocl from being stopped when the pump is in ERROR state
        }

        private void AssertRemoteLock()
        {
            if (TitanPump.RemoteLock != RemoteLockEnum.Enabled)
            {
                Output += "Pump in Read-Only mode, enable remote control to continue.";
                Status = "Pump in Read-Only mode";
                Finish();
            }
        }



 

        private dynamic TitanPump
        {
            get
            {
                return Apparatus["titan pump"];
            }
        }

        private dynamic PumpProperties
        {
            get
            {
                return TitanPump.Properties;
            }
        }

    }
}