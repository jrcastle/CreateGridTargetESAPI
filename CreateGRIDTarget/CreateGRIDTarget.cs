using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using CreateGRIDTarget;
using VMS.TPS.Common.Model.API;

namespace VMS.TPS
{
    public class Script
    {
        ////////////////////////////////////////////////
        // Execute
        ////////////////////////////////////////////////
        public void Execute(ScriptContext context, Window window)
        {

            PlanSetup plan;
            Image CT;
            StructureSet struct_set;

            //----- Step 1: Get the plan -----\\
            try
            {
                plan = context.PlanSetup; 
                string name = plan.Name; // If no plan loaded, this triggers an error.
            }
            catch
            {
                DialogResult result = ThrowPatientLoadError();
                window.Close();
                return;
            }

            //----- Step 2: Get the planning CT -----\\
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

            //----- Step 3: Get the structure set -----\\
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

            //----- Step 4: Load UI -----\\
            window.Title = "Lattice Therapy";
            window.Height = 400;
            window.Width = 900;
            CreateGRIDTargetUserControl userControl = new CreateGRIDTargetUserControl(plan, CT, struct_set);
            window.Content = userControl;
        }


        ////////////////////////////////////////////////
        // ThrowPatientLoadError
        ////////////////////////////////////////////////
        private DialogResult ThrowPatientLoadError()
        {
            string message = "Please have a patient and plan loaded before executing this script.";
            string caption = "Error Detected in Input";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            DialogResult result = System.Windows.Forms.MessageBox.Show(message, caption, buttons);
            return result;
        }
    }
}
