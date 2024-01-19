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
using MathNet.Numerics.LinearAlgebra;
using VMS.TPS.Common.Model.API;

namespace CreateGRIDTarget
{
    ////////////////////////////////////////////////
    // LatticeCylinder
    ////////////////////////////////////////////////
    public class LatticeCylinder
    {
        public Vector<double> orientation { get; set; }
        public double radius { get; set; }
        public double separation { get; set; }
    }

    ////////////////////////////////////////////////
    // CreateGRIDTargetUserControl 
    ////////////////////////////////////////////////
    public partial class CreateGRIDTargetUserControl : UserControl
    {
        ////////////////////////////////////////////////
        // CLASS MEMBERS
        ////////////////////////////////////////////////
        public string message { get; set; }
        ObservableCollection<string> structureList { get; set; }
        public string gtvStructureName { get; set; }
        public float latticeDiameter { get; set; }
        public float latticeSeparation { get; set; }
        public float gridRotationDegrees { get; set; }
        public List<float> gtvErodeMarginXYZ { get; set; }
        public List<float> latticePatternShiftXZ { get; set; }
        public CancellationTokenSource cancellationToken { get; set; }
        public PlanSetup plan { get; set; }
        public VMS.TPS.Common.Model.API.Image CT { get; set; }
        public StructureSet struct_set { get; set; }


        ////////////////////////////////////////////////
        // CONSTRUCTOR 
        ////////////////////////////////////////////////
        public CreateGRIDTargetUserControl(PlanSetup p, VMS.TPS.Common.Model.API.Image img, StructureSet ss)
        {
            InitializeComponent();

            // Initialize class members for plan, ct, and structure set
            plan = p;
            CT = img;
            struct_set = ss;

            // Blank message to start
            message = "";

            // Initialize parameters, but don't update UI
            // CheckRunIsReady will look for these values specifically
            // To know if it should enable the run button or not
            latticeDiameter = 8675309;
            latticeSeparation = 8675309;
            

            // Initialize margin/shift lists
            gtvErodeMarginXYZ = new List<float>();
            latticePatternShiftXZ = new List<float>();
            for (int i = 0; i < 3; i++)
            {
                gtvErodeMarginXYZ.Add(0);
                if (i < 2) latticePatternShiftXZ.Add(0);
            }

            // Make default values of 0 for margins, shifts, and rotation
            TextBox_GTVErodeMarginX.Text = "0";
            TextBox_GTVErodeMarginY.Text = "0";
            TextBox_GTVErodeMarginZ.Text = "0";
            TextBox_LatticeShiftX.Text = "0";
            TextBox_LatticeShiftZ.Text = "0";

            gridRotationDegrees = 0;
            TextBox_LatticeRotation.Text = "0";

            // Make list of structures for dropdown
            structureList = new ObservableCollection<string>();
            foreach (var s in struct_set.Structures)
            {
                structureList.Add(s.Id.ToString());
            }
            ComboBoxGTVList.ItemsSource = structureList;
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
        // TextBox_LatticeDiameter_Changed
        ////////////////////////////////////////////////
        private void TextBox_LatticeDiameter_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                latticeDiameter = float.Parse(TextBox_LatticeDiameter.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                latticeDiameter = 8675309;
                message += "Grid diameter input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_LatticeSeparation_Changed
        ////////////////////////////////////////////////
        private void TextBox_LatticeSeparation_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                latticeSeparation = float.Parse(TextBox_LatticeSeparation.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                latticeSeparation = 8675309;
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
                gtvErodeMarginXYZ[0] = 8675309;
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
                gtvErodeMarginXYZ[1] = 8675309;
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
                gtvErodeMarginXYZ[2] = 8675309;
                message += "Z erosion margin input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_LatticeRotation_Changed
        ////////////////////////////////////////////////
        private void TextBox_LatticeRotation_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                gridRotationDegrees = float.Parse(TextBox_LatticeRotation.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                gridRotationDegrees = 8675309;
                message += "Lattice rotation input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_LatticeShiftX_Changed
        ////////////////////////////////////////////////
        private void TextBox_LatticeShiftX_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                latticePatternShiftXZ[0] = float.Parse(TextBox_LatticeShiftX.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                latticePatternShiftXZ[0] = 8675309;
                message += "X pattern shift input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_LatticeShiftZ_Changed
        ////////////////////////////////////////////////
        private void TextBox_LatticeShiftZ_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                latticePatternShiftXZ[1] = float.Parse(TextBox_LatticeShiftZ.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                latticePatternShiftXZ[1] = 8675309;
                message += "Z pattern shift input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
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
                if (latticeDiameter == 8675309) ready = false;
                if (latticeSeparation == 8675309) ready = false;
                if (gridRotationDegrees == 8675309) ready = false;
                for (int i = 0; i < 3; i++)
                {
                    if (gtvErodeMarginXYZ[i] == 8675309) ready = false;
                    if (i < 2 && latticePatternShiftXZ[i] == 8675309) ready = false;
                }
                if (!structureList.Contains(gtvStructureName))
                {
                    ready = false;
                    message += "Structure \"" + gtvStructureName.ToString() + "\" not found in structure set.\n";
                    messageTextBlock.Text = message;
                }
                if (latticeDiameter < 0)
                {
                    ready = false;
                    message += "Cannot have negative Grid Diameter.\n";
                    messageTextBlock.Text = message;
                }
                if (latticeSeparation < 0)
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


        ////////////////////////////////////////////////
        // RunButton_Click
        ////////////////////////////////////////////////
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            cancellationToken = new CancellationTokenSource();
            CreateGRIDTarget(cancellationToken.Token);
        }


        ///////////////////////////////////////////////////////////
        // CreateGRIDTarget
        ///////////////////////////////////////////////////////////
        private async void CreateGRIDTarget(CancellationToken token)
        {
            // Might need to wrap the funtion with this in the future:
            //await Task.Factory.StartNew(async () =>
            //{
            //});

            token.ThrowIfCancellationRequested();
            this.Dispatcher.Invoke(() =>
            {
                runButton.IsEnabled = false;
                abortButton.IsEnabled = true;
                message += "Reading GTV from structure set ...\n";
                messageTextBlock.Text = message;
                progressBar.Value = 1;
            });


            // Get GTV
            var gtv = struct_set.Structures.First(x => x.Id == gtvStructureName);

            // Get the list of slices spanning the GTV
            IEnumerable<int> slices = GetMeshBounds(gtv, struct_set);

            // Establish bounding box parameters
            var bounding_box = gtv.MeshGeometry.Bounds;

            List<double> bb_start_coord = new List<double>(){
                bounding_box.X,
                bounding_box.Y,
                bounding_box.Z
            };

            List<double> bb_end_coord = new List<double>() {
                bounding_box.X + bounding_box.SizeX,
                bounding_box.Y + bounding_box.SizeY,
                bounding_box.Z + bounding_box.SizeZ
            };

            List<double> bb_spacing_coord = new List<double>() {
                struct_set.Image.XRes,
                struct_set.Image.YRes,
                struct_set.Image.ZRes
            };

            List<double> bb_size_coord = new List<double>() {
                bounding_box.SizeX,
                bounding_box.SizeY,
                bounding_box.SizeZ
            };

            double max_bb_size = bb_size_coord.Max();

            List<double> bb_center_coord = new List<double>();
            for (int i = 0; i < 3; i++) bb_center_coord[i] = (bb_start_coord[i] + bb_end_coord[i]) / 2.0;

            // Create Cylinder object and orient Cylinder direction based on largest dimension of bounding box
            LatticeCylinder cyl = new LatticeCylinder();
            cyl.radius = latticeDiameter / 2.0;
            cyl.separation = latticeSeparation;
            if (bb_size_coord[0] > bb_size_coord[1] && bb_size_coord[0] > bb_size_coord[2])
                cyl.orientation = Vector<double>.Build.DenseOfArray(new double[] { 1, 0 ,0 });
            else if (bb_size_coord[1] > bb_size_coord[0] && bb_size_coord[1] > bb_size_coord[2])
                cyl.orientation = cyl.orientation = Vector<double>.Build.DenseOfArray(new double[] { 0, 1, 0 });
            else
                cyl.orientation = cyl.orientation = Vector<double>.Build.DenseOfArray(new double[] { 0, 0, 1 });


            // Create a volumetric cube around the center point of the bounding box whose side 
            // length is the max dimension of the bounding box. Place a rod, oriented in the z direction,
            // through the central point. Increment outwards in x and y, using the cylinder diameter and 
            // lattice separation until any point in the rod exceeds a cube boundary. Keep whole rods, 
            // even if they exceed the cube border, they will be cropped later.
            int N_pts = 25;
            List<double> cube_low = new List<double>()
            {
                bb_center_coord[0] - max_bb_size / 2.0,
                bb_center_coord[1] - max_bb_size / 2.0,
                bb_center_coord[2] - max_bb_size / 2.0
            };
            List<double> cube_high = new List<double>()
            {
                bb_center_coord[0] + max_bb_size / 2.0,
                bb_center_coord[1] + max_bb_size / 2.0,
                bb_center_coord[2] + max_bb_size / 2.0
            };
            List<List<double>> rod_points_list = new List<List<double>>();            
            for (double z = cube_low[2]; z <= cube_high[2]; z += bb_spacing_coord.Min())
            {

                // Central rod points
                for (int i = 0; i < N_pts; i++)
                {
                    double x = bb_center_coord[0] + cyl.radius * Math.Cos((double)i * 2.0 * Math.PI / (double)N_pts);
                    double y = bb_center_coord[1] + cyl.radius * Math.Sin((double)i * 2.0 * Math.PI / (double)N_pts);
                    rod_points_list.Add(new List<double>() { x, y, z });
                }

                //----- Rods in X -----\\
                int rod_count_x = 1;
                bool within_x = true;
                while (within_x)
                {
                    // Cube is symmetric, only need to test one boundary, will test the +X boundary
                    double test_max_x = bb_center_coord[0] + (double)rod_count_x * (2.0 * cyl.radius + latticeSeparation) + cyl.radius;
                    if (test_max_x > cube_high[0]) within_x = false;
                    for (int i = 0; i < N_pts; i++)
                    {
                        // Add rod in +X
                        double x = bb_center_coord[0] + (double)rod_count_x * (2.0 * cyl.radius + latticeSeparation) + cyl.radius * Math.Cos((double)i * 2.0 * Math.PI / (double)N_pts);
                        double y = bb_center_coord[1] + cyl.radius * Math.Sin((double)i * 2.0 * Math.PI / (double)N_pts);
                        rod_points_list.Add(new List<double>() { x, y, z });

                        // Add rod in -X
                        x = bb_center_coord[0] - (double)rod_count_x * (2.0 * cyl.radius + latticeSeparation) + cyl.radius * Math.Cos((double)i * 2.0 * Math.PI / (double)N_pts);
                        rod_points_list.Add(new List<double>() { x, y, z });
                    }

                    //----- Rods in Y -----\\
                    // For this x rod increment, add rods in +Y
                    int rod_count_y = 1;
                    bool within_y = true;
                    while (within_y)
                    {
                        // Cube is symmetric, only need to test one boundary, will test the +Y boundary
                        double test_max_y = bb_center_coord[1] + (double)rod_count_y * (2.0 * cyl.radius + latticeSeparation) + cyl.radius;
                        if (test_max_y > cube_high[1]) within_y = false;
                        for (int i = 0; i < N_pts; i++)
                        {
                            // Add rod in +Y for +X
                            double x = bb_center_coord[0] + (double)rod_count_x * (2.0 * cyl.radius + latticeSeparation) + cyl.radius * Math.Cos((double)i * 2.0 * Math.PI / (double)N_pts);
                            double y = bb_center_coord[1] + (double)rod_count_y * (2.0 * cyl.radius + latticeSeparation) + cyl.radius * Math.Sin((double)i * 2.0 * Math.PI / (double)N_pts);
                            rod_points_list.Add(new List<double>() { x, y, z });

                            // Add rod in -Y for +X
                            y = bb_center_coord[1] - (double)rod_count_y * (2.0 * cyl.radius + latticeSeparation) + cyl.radius * Math.Sin((double)i * 2.0 * Math.PI / (double)N_pts);
                            rod_points_list.Add(new List<double>() { x, y, z });

                            // Add rod in +Y for -X
                            x = bb_center_coord[0] - (double)rod_count_x * (2.0 * cyl.radius + latticeSeparation) + cyl.radius * Math.Cos((double)i * 2.0 * Math.PI / (double)N_pts);
                            y = bb_center_coord[1] + (double)rod_count_y * (2.0 * cyl.radius + latticeSeparation) + cyl.radius * Math.Sin((double)i * 2.0 * Math.PI / (double)N_pts);
                            rod_points_list.Add(new List<double>() { x, y, z });

                            // Add rod in -Y for -X
                            x = bb_center_coord[0] - (double)rod_count_x * (2.0 * cyl.radius + latticeSeparation) + cyl.radius * Math.Cos((double)i * 2.0 * Math.PI / (double)N_pts);
                            y = bb_center_coord[1] - (double)rod_count_y * (2.0 * cyl.radius + latticeSeparation) + cyl.radius * Math.Sin((double)i * 2.0 * Math.PI / (double)N_pts);
                            rod_points_list.Add(new List<double>() { x, y, z });
                        }
                        rod_count_y++;
                    }
                    rod_count_x++;
                }
                break;
            }

            // List of Lists to Matrix
            double[,] tmp = new double[rod_points_list.Count(), 3];
            for (int i = 0; i < rod_points_list.Count(); i++)
            {
                for (int j = 0; j < rod_points_list[i].Count(); j++)
                    tmp[i, j] = rod_points_list[i][j];
            }
            Matrix<double> rod_points = Matrix<double>.Build.DenseOfArray(tmp);
            message += rod_points.ToString() + "\n";

            // Create Optimization Structure
            Structure opti_struct;
           
            // Check if it exists first
            string opti_struct_name = CheckStructureIDs("zLatticeOpti", struct_set);
            if (opti_struct_name == "FAIL") {
                // Give up
                message += "Unable to make GTV structure after 10 attempts. Please delete/rename opti structures and try again.\n";
                this.Dispatcher.Invoke(() =>
                {
                    runButton.IsEnabled = true;
                    abortButton.IsEnabled = false;
                    messageTextBlock.Text = message;
                    progressBar.Value = 0;
                });
                return;
            }           
            else
            {
                // Structure name not found in structure set, make it
                opti_struct = struct_set.AddStructure("CONTROL", "zLatticeOpti");
            }

            // TODO: Rotate points to direction of largest GTV size

            // TODO: Apply user-specified translations

            // TODO: Apply user-specified rotations

            // TODO: Fill contour slice-by-slice

            // TODO: Union of rods and GTV
            //var overlap = ss.AddStructure("CONTROL", CheckStructureIds(ss, "organ_ovl");
            //overlap.SegmentVolume = gtv.SegmentVolume.And(oar.SegmentVolume);

            // Script complete
            this.Dispatcher.Invoke(() =>
            {
                messageTextBlock.Text = message;
                runButton.IsEnabled = true;
                abortButton.IsEnabled = false;
                progressBar.Value = 100;
            });
        }


        ///////////////////////////////////////////////////////////
        // GetSlice
        ///////////////////////////////////////////////////////////
        public static int GetSlice(double z, StructureSet SS)
        {
            var imageRes = SS.Image.ZRes;
            return Convert.ToInt32((z - SS.Image.Origin.z) / imageRes);
        }


        ///////////////////////////////////////////////////////////
        // GetMeshBounds
        ///////////////////////////////////////////////////////////
        public static IEnumerable<int> GetMeshBounds(Structure structure, StructureSet SS)
        {
            var mesh = structure.MeshGeometry.Bounds;
            var meshLow = GetSlice(mesh.Z, SS);
            var meshUp = GetSlice(mesh.Z + mesh.SizeZ, SS) + 1;
            return Enumerable.Range(meshLow, meshUp);
        }


        ///////////////////////////////////////////////////////////
        // CheckStructureIDs
        ///////////////////////////////////////////////////////////
        string CheckStructureIDs(string name, StructureSet ss)
        {
            string struct_name = "FAIL";
            if (ss.Structures.Any(e => e.Id.ToLower() == name))
            {
                for (int i = 1; i <= 10; i++)
                {
                    if (i == 1) message += "Structure \"" + name + "\" already exists. Attempting to create structure \"zLatticeOpti" + i.ToString() + "\" instead ...\n";
                    else message += "Structure \"" + name + (i - 1).ToString() + "\" already exists. Attempting to create structure \"" + name + i.ToString() + "\" instead ...\n";
                    this.Dispatcher.Invoke(() =>
                    {
                        messageTextBlock.Text = message;
                    });

                    // Check this attempt
                    if (!struct_set.Structures.Any(e => e.Id.ToLower() == "zLatticeOpti" + i.ToString()))
                    {
                        // Structure name not found in structure set, make it
                        struct_name = name + i.ToString();
                        break;
                    }
                }
            }
            else
            {
                struct_name = name;
            }
            return struct_name;
        }


    }
}

