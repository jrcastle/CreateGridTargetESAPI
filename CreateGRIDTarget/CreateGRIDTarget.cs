using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using CreateGRIDTarget;
using VMS.TPS.Common.Model.API;

namespace VMS.TPS
{
    public class Script
    {
        public void Execute(ScriptContext context, Window window)
        {
            // Step 1: Get the imaging series UID from context
            string imageStudyInstanceUID = context.Image.Series.Study.UID;
            string imageSeriesInstanceUID = context.Image.Series.UID;
            string patientID = context.Patient.Id;
            string patientName = context.Patient.Name;

            Dictionary<string, string> seriesInfo = new Dictionary<string, string>();
            seriesInfo.Add("StudyInstanceUID", imageStudyInstanceUID);
            seriesInfo.Add("PatientID", patientID);
            seriesInfo.Add("SeriesInstanceUID", imageSeriesInstanceUID);
            seriesInfo.Add("PatientName", patientName);

            window.Title = "GRID";
            window.Height = 650;
            window.Width = 1200;
            CreateGRIDTargetUserControl userControl = new CreateGRIDTargetUserControl();
            window.Content = userControl;
        }
    }
}
