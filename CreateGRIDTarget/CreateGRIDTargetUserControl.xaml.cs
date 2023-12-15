using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using EvilDICOM.Core;
using EvilDICOM.Network;
using System.Collections.ObjectModel;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Runtime;
using VMS.TPS.Common.Model.API;

namespace CreateGRIDTarget
{
    ////////////////////////////////////////////////
    // CreateGRIDTargetUserControl 
    ////////////////////////////////////////////////
    public partial class CreateGRIDTargetUserControl : UserControl
    {
        ////////////////////////////////////////////////
        // CLASS MEMBERS
        ////////////////////////////////////////////////
        public string message = "";
        ObservableCollection<string> structureList { get; set; }
        public string gtvStructureName;
        public float gridDiameter;
        public float gridSeparation;
        public float gridRotationDegrees;
        public List<float> gtvErodeMarginXYZ;
        public List<float> gridPatternShiftXZ;
        public CancellationTokenSource cancellationToken { get; set; }


        ////////////////////////////////////////////////
        // CONSTRUCTOR 
        ////////////////////////////////////////////////
        public CreateGRIDTargetUserControl(PlanSetup plan, VMS.TPS.Common.Model.API.Image CT, StructureSet struct_set)
        {
            InitializeComponent();

            // Initialize margin/shift lists
            gtvErodeMarginXYZ = new List<float>();
            gridPatternShiftXZ = new List<float>();
            for (int i = 0; i < 3; i++)
            {
                gtvErodeMarginXYZ.Add(0);
                if (i < 2) gridPatternShiftXZ.Add(0);
            }

            // Make default values of 0 for margins, shifts, and rotation
            TextBox_GTVErodeMarginX.Text = "0";
            TextBox_GTVErodeMarginY.Text = "0";
            TextBox_GTVErodeMarginZ.Text = "0";
            TextBox_GridRotation.Text = "0";
            TextBox_GridShiftX.Text = "0";
            TextBox_GridShiftZ.Text = "0";

            // Make list of structures for dropdown
            structureList = new ObservableCollection<string>();
            //for loop
            // structureList.Add()
            //ComboBoxGTVList.ItemsSource = structureList;

            this.DataContext = this;

            // Add closing event
            progressBar.Value = 0;
            this.Loaded += CreateGRIDTargetUserControl_Loaded;
        }


        ////////////////////////////////////////////////
        // GTVSelectionChanged
        ////////////////////////////////////////////////
        private void GTVSelectionChanged(object sender, RoutedEventArgs e)
        {
            gtvStructureName = ComboBoxGTVList.SelectedItem.ToString();
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_GridDiameter_Changed
        ////////////////////////////////////////////////
        private void TextBox_GridDiameter_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                gridDiameter = float.Parse(TextBox_GridDiameter.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                message += "Grid diameter input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_GridSeparation_Changed
        ////////////////////////////////////////////////
        private void TextBox_GridSeparation_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                gridSeparation = float.Parse(TextBox_GridSeparation.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                message += "Grid separation input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_GTVErodeMarginX_Changed
        ////////////////////////////////////////////////
        private void TextBox_GTVErodeMarginX_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                gtvErodeMarginXYZ[0] = float.Parse(TextBox_GTVErodeMarginX.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                message += "X erosion margin input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_GTVErodeMarginY_Changed
        ////////////////////////////////////////////////
        private void TextBox_GTVErodeMarginY_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                gtvErodeMarginXYZ[1] = float.Parse(TextBox_GTVErodeMarginY.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                message += "Y erosion margin input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_GTVErodeMarginZ_Changed
        ////////////////////////////////////////////////
        private void TextBox_GTVErodeMarginZ_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                gtvErodeMarginXYZ[2] = float.Parse(TextBox_GTVErodeMarginZ.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                message += "Z erosion margin input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_GridRotation_Changed
        ////////////////////////////////////////////////
        private void TextBox_GridRotation_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                gridRotationDegrees = float.Parse(TextBox_GridRotation.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                message += "Grid rotation input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_GridShiftX_Changed
        ////////////////////////////////////////////////
        private void TextBox_GridShiftX_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                gridPatternShiftXZ[0] = float.Parse(TextBox_GridShiftX.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                message += "X pattern shift input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_GridShiftZ_Changed
        ////////////////////////////////////////////////
        private void TextBox_GridShiftZ_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                gridPatternShiftXZ[1] = float.Parse(TextBox_GridShiftZ.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                message += "Z pattern shift input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // RunButton_Click
        ////////////////////////////////////////////////
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            cancellationToken = new CancellationTokenSource();
            CreateGRIDTarget(cancellationToken.Token);
        }


        ////////////////////////////////////////////////
        // AbortButton_Click
        ////////////////////////////////////////////////
        private void AbortButton_Click(object sender, RoutedEventArgs e)
        {
            abortButton.IsEnabled = false;
            if (cancellationToken != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    message += "Aborting ...\n";
                    messageTextBlock.Text = message;
                });
                cancellationToken.Cancel();
            }
            runButton.IsEnabled = true;
        }


        ////////////////////////////////////////////////
        // ClearButton_Click
        ////////////////////////////////////////////////
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            message = "";
            messageTextBlock.Text = message;
        }


        ////////////////////////////////////////////////
        // CloseButton_Click
        ////////////////////////////////////////////////
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }


        ////////////////////////////////////////////////
        // CheckRunIsReady
        ////////////////////////////////////////////////
        private void CheckRunIsReady()
        {
            bool ready = true;
            try
            {
                if (structureList.Contains(gtvStructureName))
                {
                    ready = false;
                    message += "GTV structure not found.\n";
                    messageTextBlock.Text = message;
                }
                if (gridDiameter < 0)
                {
                    ready = false;
                    message += "Cannot have negative Grid Diameter.\n";
                    messageTextBlock.Text = message;
                }
                if (gridSeparation < 0)
                {
                    ready = false;
                    message += "Cannot have negative Grid Separation.\n";
                    messageTextBlock.Text = message;
                }
            }
            catch
            {
                ready = false;
            }
            runButton.IsEnabled = ready;
        }

        ///////////////////////////////////////////////////////////
        // CreateGRIDTargetUserControl_Loaded
        ///////////////////////////////////////////////////////////
        private void CreateGRIDTargetUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            window.Closing += OnClosing;
        }

        ///////////////////////////////////////////////////////////
        // OnClosing
        ///////////////////////////////////////////////////////////
        private void OnClosing(object sender, global::System.ComponentModel.CancelEventArgs e)
        {
            if (progressBar.Value > 0 && progressBar.Value < 100)
            {
                string msg = "Script is still running.";
                msg += "Are you sure you want to close this window?";
                MessageBoxResult result = MessageBox.Show(msg, "Close Confirmation",
                    MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (result == MessageBoxResult.Yes)
                    e.Cancel = false;
                else if (result == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }

        ///////////////////////////////////////////////////////////
        // OnClosing
        ///////////////////////////////////////////////////////////
        private async void CreateGRIDTarget(CancellationToken token)
        {
        }

    }
}

