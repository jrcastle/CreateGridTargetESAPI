using System.Windows;
using System.Windows.Forms;
using CreateGRIDTarget;
using System.Reflection;
using VMS.TPS.Common.Model.API;

[assembly: AssemblyVersion("1.0.0.16")]
[assembly: AssemblyFileVersion("1.0.0.16")]
[assembly: AssemblyInformationalVersion("1.0")]
[assembly: ESAPIScript(IsWriteable = true)]


namespace VMS.TPS
{
    public class Script
    {
        ////////////////////////////////////////////////
        // Execute
        ////////////////////////////////////////////////
        public void Execute(ScriptContext context, Window window)
        {

            //----- Step 0: Enable script to modify the database -----\\
            context.Patient.BeginModifications();


            //----- Step 1: Get the planning CT -----\\
            Image CT;
            try
            {
                CT = context.Image;
                string name = CT.Name; // If no CT loaded, this triggers an error.
            }
            catch
            {
                DialogResult result = ThrowPatientLoadError();
                window.Close();
                return;
            }


            //----- Step 2: Get the structure set -----\\
            StructureSet struct_set;
            try
            {
                struct_set = context.StructureSet;
                string name = struct_set.Name; // If no structure set loaded loaded, this triggers an error.
            }
            catch
            {
                DialogResult result = ThrowPatientLoadError();
                window.Close();
                return;
            }


            //----- Step 3: Load UI -----\\
            window.Title = "Lattice Therapy";
            window.Height = 400;
            window.Width = 900;
            CreateGRIDTargetUserControl userControl = new CreateGRIDTargetUserControl(CT, struct_set);
            window.Content = userControl;
        }


        ////////////////////////////////////////////////
        // ThrowPatientLoadError
        ////////////////////////////////////////////////
        private DialogResult ThrowPatientLoadError()
        {
            string message = "Please have a structure set loaded before executing this script.";
            string caption = "Error Detected in Input";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            DialogResult result = System.Windows.Forms.MessageBox.Show(message, caption, buttons);
            return result;
        }
    }
}
