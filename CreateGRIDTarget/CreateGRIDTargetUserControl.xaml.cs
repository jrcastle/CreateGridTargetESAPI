using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace CreateGRIDTarget
{
    ////////////////////////////////////////////////
    // LatticeCylinder
    ////////////////////////////////////////////////
    public class LatticeCylinder
    {
        public Vector3D orientation { get; set; }
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
        ObservableCollection<string> latticeList { get; set; }
        public string gtvStructureName { get; set; }
        public string latticeType { get; set; }
        public float latticeDiameter { get; set; }
        public float latticeSeparation { get; set; }
        public float controlDiameter { get; set; }
        public float gridRotationDegrees { get; set; }
        public List<float> gtvErodeMarginXYZ { get; set; }
        public List<float> latticePatternShiftXYZ { get; set; }
        public CancellationTokenSource cancellationToken { get; set; }
        public VMS.TPS.Common.Model.API.Image CT { get; set; }
        public StructureSet structSet { get; set; }
        string version { get; set; }


        ////////////////////////////////////////////////
        // CONSTRUCTOR 
        ////////////////////////////////////////////////
        public CreateGRIDTargetUserControl(VMS.TPS.Common.Model.API.Image img, StructureSet ss)
        {
            
            InitializeComponent();

            // Grab version for GUI
            version = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            VersionLabel.Content = version;

            // Initialize class members for ct, and structure set
            CT = img;
            structSet = ss;

            // Blank message to start
            message = "";

            // Initialize parameters, but don't update UI
            // CheckRunIsReady will look for these values specifically
            // To know if it should enable the run button or not
            latticeDiameter = 8675309;
            latticeSeparation = 8675309;


            // Initialize margin/shift lists
            gtvErodeMarginXYZ = new List<float>();
            latticePatternShiftXYZ = new List<float>();
            for (int i = 0; i < 3; i++)
            {
                gtvErodeMarginXYZ.Add(0);
                latticePatternShiftXYZ.Add(0);
            }

            // Make default values of 0 for margins, shifts, and rotation
            TextBox_GTVErodeMarginX.Text = "0";
            TextBox_GTVErodeMarginY.Text = "0";
            TextBox_GTVErodeMarginZ.Text = "0";
            TextBox_LatticeShiftX.Text = "0";
            TextBox_LatticeShiftY.Text = "0";
            TextBox_LatticeShiftZ.Text = "0";

            gridRotationDegrees = 0;
            TextBox_LatticeRotation.Text = "0";

            // Make list of structures for dropdown
            structureList = new ObservableCollection<string>();
            foreach (var s in structSet.Structures)
            {
                structureList.Add(s.Id.ToString());
            }
            ComboBoxGTVList.ItemsSource = structureList;

            // Make list of possible lattice types
            latticeList = new ObservableCollection<string>();
            latticeList.Add("Cylinders");
            latticeList.Add("Spheres");
            ComboBoxLatticeList.ItemsSource = latticeList;
            ComboBoxLatticeList.SelectedIndex = 0;
            latticeType = "Cylinders";

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
        // LatticeSelectionChanged
        ////////////////////////////////////////////////
        private void LatticeSelectionChanged(object sender, RoutedEventArgs e)
        {
            latticeType = ComboBoxLatticeList.SelectedItem.ToString();
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
        // TextBox_ControlDiameter_Changed
        ////////////////////////////////////////////////
        private void TextBox_ControlDiameter_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                controlDiameter = float.Parse(TextBox_ControlDiameter.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                controlDiameter = 8675309;
                message += "Control diameter input needs to be a number.\n";
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
                message += "Left-right erosion margin input needs to be a number.\n";
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
                message += "Ant-post erosion margin input needs to be a number.\n";
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
                message += "Sup-inf erosion margin input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_LatticeRotation_Changed
        ////////////////////////////////////////////////
        private void TextBox_LatticeRotation_Changed(object sender, RoutedEventArgs e)
        {
            // TODO: Remove this when rotations implemented
            message += "Warning: Lattice rotations are not enabled at this time. This feature will be implemented in the future.\n";
            messageTextBlock.Text = message;

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
                latticePatternShiftXYZ[0] = float.Parse(TextBox_LatticeShiftX.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                latticePatternShiftXYZ[0] = 8675309;
                message += "Left-right pattern shift input needs to be a number.\n";
                messageTextBlock.Text = message;
            }
            CheckRunIsReady();
        }


        ////////////////////////////////////////////////
        // TextBox_LatticeShiftY_Changed
        ////////////////////////////////////////////////
        private void TextBox_LatticeShiftY_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                latticePatternShiftXYZ[1] = float.Parse(TextBox_LatticeShiftY.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                latticePatternShiftXYZ[1] = 8675309;
                message += "Ant-post pattern shift input needs to be a number.\n";
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
                latticePatternShiftXYZ[2] = float.Parse(TextBox_LatticeShiftZ.Text.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                latticePatternShiftXYZ[2] = 8675309;
                message += "Inf-sup pattern shift input needs to be a number.\n";
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
                if (controlDiameter == 8675309) ready = false;
                if (gridRotationDegrees == 8675309) ready = false;
                for (int i = 0; i < 3; i++)
                {
                    if (gtvErodeMarginXYZ[i] == 8675309) ready = false;
                    if (latticePatternShiftXYZ[i] == 8675309) ready = false;
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
                if (latticeType == "Spheres" || latticeType == "")
                {
                    ready = false;
                    message += "Sphere lattice structures are not supported at this time.\n";
                    messageTextBlock.Text = message;
                }
                if (!structureList.Contains(gtvStructureName))
                {
                    ready = false;
                    message += "Structure \"" + gtvStructureName.ToString() + "\" not found in structure set.\n";
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
            
            token.ThrowIfCancellationRequested();
            this.Dispatcher.Invoke(() =>
            {
                runButton.IsEnabled = false;
                abortButton.IsEnabled = true;
                message += "Reading GTV from structure set ...\n";
                messageTextBlock.Text = message;
                progressBar.Value = 1;
            });


            //----- Step 1: Get GTV -----\\
            Structure gtv = structSet.Structures.First(x => x.Id == gtvStructureName);

            // Erode GTV, if desired
            Structure gtvErode = null;
            bool eroded = false;
            if (gtvErodeMarginXYZ[0] != 0 && gtvErodeMarginXYZ[1] != 0 && gtvErodeMarginXYZ[2] != 0)
            {
                eroded = true;

                // Check if it exists first
                string gtvErodeName = CheckStructureIDs("z" + gtvStructureName + "-" + gtvErodeMarginXYZ[0].ToString() + "mmSI" + gtvErodeMarginXYZ[1].ToString() + "mmAP" + gtvErodeMarginXYZ[2] + "mmSI", structSet);
                if (gtvErodeName == "FAIL")
                {
                    // Give up
                    message += "Unable to make eroded GTV structure after 10 attempts. Please delete/rename opti structures and try again.\n";
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
                    gtvErode = structSet.AddStructure("CONTROL", gtvErodeName);
                }

                // Erode
                double x = gtvErodeMarginXYZ[0];
                double y = gtvErodeMarginXYZ[1];
                double z = gtvErodeMarginXYZ[2];
                AxisAlignedMargins margins = new AxisAlignedMargins(StructureMarginGeometry.Inner, x, y, z, x, y, z);
                gtvErode.SegmentVolume = gtv.AsymmetricMargin(margins);
            }

            token.ThrowIfCancellationRequested();
            this.Dispatcher.Invoke(() =>
            {
                progressBar.Value = 5;
            });

            //----- Step 2: Establish bounding box parameters -----\\
            System.Windows.Media.Media3D.Rect3D boundingBox;
            if(eroded) boundingBox = gtvErode.MeshGeometry.Bounds;
            else boundingBox = gtv.MeshGeometry.Bounds;

            List<double> bbStartCoord = new List<double>(){
                boundingBox.X,
                boundingBox.Y,
                boundingBox.Z
            };

            List<double> bbEndCoord = new List<double>() {
                boundingBox.X + boundingBox.SizeX,
                boundingBox.Y + boundingBox.SizeY,
                boundingBox.Z + boundingBox.SizeZ
            };

            List<double> bbSpacingCoord = new List<double>() {
                structSet.Image.XRes,
                structSet.Image.YRes,
                structSet.Image.ZRes
            };

            List<double> bb_size_coord = new List<double>() {
                boundingBox.SizeX,
                boundingBox.SizeY,
                boundingBox.SizeZ
            };

            double max_bb_size = bb_size_coord.Max();

            List<double> bbCenterCoord = new List<double>();
            for (int i = 0; i < 3; i++) bbCenterCoord.Add((bbStartCoord[i] + bbEndCoord[i]) / 2.0);

            // Add user-specified translations
            if (latticePatternShiftXYZ[0] != 0 || latticePatternShiftXYZ[0] == 8675309)
            {
                bbStartCoord[0] += latticePatternShiftXYZ[0];
                bbEndCoord[0] += latticePatternShiftXYZ[0];
                bbCenterCoord[0] += latticePatternShiftXYZ[0];
            }
            if (latticePatternShiftXYZ[1] != 0 || latticePatternShiftXYZ[1] == 8675309)
            {
                bbStartCoord[1] += latticePatternShiftXYZ[1];
                bbEndCoord[1] += latticePatternShiftXYZ[1];
                bbCenterCoord[1] += latticePatternShiftXYZ[1];
            }
            if (latticePatternShiftXYZ[2] != 0 || latticePatternShiftXYZ[2] == 8675309)
            {
                bbStartCoord[2] += latticePatternShiftXYZ[2];
                bbEndCoord[2] += latticePatternShiftXYZ[2];
                bbCenterCoord[2] += latticePatternShiftXYZ[2];
            }

            token.ThrowIfCancellationRequested();
            this.Dispatcher.Invoke(() =>
            {
                progressBar.Value = 10;
            });



            ///// TODO: FUTURE DIRECTIONS /////
            // Create Cylinder object and orient Cylinder direction based on largest dimension of bounding box
            /*
            if (bb_size_coord[0] > bb_size_coord[1] && bb_size_coord[0] > bb_size_coord[2])
                cyl.orientation = new Vector3D(1, 0, 0);
            else if (bb_size_coord[1] > bb_size_coord[0] && bb_size_coord[1] > bb_size_coord[2])
                cyl.orientation = cyl.orientation = new Vector3D(0, 1, 0);
            else
                cyl.orientation = cyl.orientation = new Vector3D(0, 0, 1);
            */
            ///////////////////////////////////

            // Create a volumetric cube around the center point of the bounding box whose side 
            // length is the max dimension of the bounding box. Place a rod, oriented in the z direction,
            // through the central point. Increment outwards in x and y, using the cylinder diameter and 
            // lattice separation until any point in the rod exceeds a cube boundary. Keep whole rods, 
            // even if they exceed the cube border, they will be cropped later.

            ///// TODO: FUTURE DIRECTIONS /////
            // Create a volumetric cube around the center point of the bounding box whose side 
            // length is the max dimension of the bounding box. This will help with future 
            // rotations applied to the structures
            /*
            List<double> cube_low = new List<double>()
            {
                bbCenterCoord[0] - max_bb_size / 2.0,
                bbCenterCoord[1] - max_bb_size / 2.0,
                bbCenterCoord[2] - max_bb_size / 2.0
            };
            List<double> cube_high = new List<double>()
            {
                bbCenterCoord[0] + max_bb_size / 2.0,
                bbCenterCoord[1] + max_bb_size / 2.0,
                bbCenterCoord[2] + max_bb_size / 2.0
            };
            */
            ///////////////////////////////////

            ///// TODO: FUTURE DIRECTIONS /////
            // Apply user-specified rotations
            /*
            // Will need to do some math here to figure out orientation matrix based on user angles...
            Vector3D default_orientation = new Vector3D(0, 0, 1);
            Matrix<double> rotation_matrix = Matrix3D.RotationTo(default_orientation, cyl.orientation);
            Matrix<double> affine_transform = ConstructAffineTransform(rotation_matrix, bbCenterCoord);
            */

            //----- Step 3: Create lattice object and orient in the +z direction -----\\
            int nPts = 360;
            List<List<VVector>> lattice = null;
            List<List<VVector>> controlLattice = null;
            if (latticeType == "Cylinders")
            {
                LatticeCylinder cyl = new LatticeCylinder();
                cyl.radius = latticeDiameter / 2.0;
                cyl.separation = latticeSeparation;
                cyl.orientation = cyl.orientation = new Vector3D(0, 0, 1);
                double controlCylRadius = controlDiameter / 2.0;

                // Opti structure
                this.Dispatcher.Invoke(() =>
                {
                    message += "Creating optimization lattice ...\n";
                    messageTextBlock.Text = message;
                });
                await Task.Run(() =>
                {
                    lattice = CreateRodLattice(nPts, cyl, bbStartCoord, bbEndCoord, bbSpacingCoord, bbCenterCoord);
                });

                // Control Structure
                this.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = 33;
                    message += "Creating control lattice ...\n";
                    messageTextBlock.Text = message;
                });
                await Task.Run(() =>
                {
                    controlLattice = CreateRodLattice(nPts, cyl, bbStartCoord, bbEndCoord, bbSpacingCoord, bbCenterCoord, true, controlCylRadius);
                });
                this.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = 66;
                });
            }
            else if (latticeType == "Spheres")
            {
                // TODO: Implement this
                this.Dispatcher.Invoke(() =>
                {
                    message += "Spheres not supported at this time. Aborting ...\n";
                    messageTextBlock.Text = message;
                });
                return;
            }

            //----- Step 4: Create Optimization Structure -----\\
            Structure optiStruct = CreateStructureAndAddToSet("zLatticeOpti", structSet, lattice);
            token.ThrowIfCancellationRequested();
            this.Dispatcher.Invoke(() =>
            {
                progressBar.Value = 75;
            });

            //----- Step 5: Union of lattice and GTV -----\\
            if(eroded) optiStruct.SegmentVolume = gtvErode.SegmentVolume.And(optiStruct.SegmentVolume);
            else optiStruct.SegmentVolume = gtv.SegmentVolume.And(optiStruct.SegmentVolume);

            //----- Step 6: Create Control Lattice Structure -----\\
            Structure controlStruct = CreateStructureAndAddToSet("zLatticeControl", structSet, controlLattice);
            token.ThrowIfCancellationRequested();
            this.Dispatcher.Invoke(() =>
            {
                progressBar.Value = 95;
            });

            //----- Step 7: Union of lattice control and GTV -----\\
            if (eroded) controlStruct.SegmentVolume = gtvErode.SegmentVolume.And(controlStruct.SegmentVolume);
            else controlStruct.SegmentVolume = gtv.SegmentVolume.And(controlStruct.SegmentVolume);

            //----- Script complete -----\\
            this.Dispatcher.Invoke(() =>
            {
                message += "Process complete! This window can now be closed.\n";
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
        // CreateRodLattice
        ///////////////////////////////////////////////////////////
        List<List<VVector>> CreateRodLattice(int nPts, LatticeCylinder cyl, List<double> bbStartCoord, List<double> bbEndCoord, List<double> bbSpacingCoord, List<double> bbCenterCoord, bool isControl=false, double controlCylRadius=0)
        {
            ///// FUTURE DIRECTIONS: /////
            // Add in rotation of coordinates before adding to points[iRod]
            //////////////////////////////

            // TODO:
            // Modify to support control structures
            // - Offset central point by a diagonal and then repeat procedure
            // - Will need boolean as fn argument isControl to signal to the function to handle this, make 2nd to last arguement, default false
            // - Will need controlCyl as optional arguement, make last arguement, default null?

            List<List<VVector>> points = new List<List<VVector>>();
            bool first = true;

            double zStart = bbStartCoord[2] - bbSpacingCoord[2];
            double zEnd = bbEndCoord[2] + bbSpacingCoord[2];
            double xStart = bbCenterCoord[0];
            double yStart = bbCenterCoord[1];
            double r = cyl.radius;
            if (isControl)
            {
                xStart += cyl.radius + latticeSeparation / 2.0;
                yStart += cyl.radius + latticeSeparation / 2.0;
                r = controlCylRadius;
            }



            for (double z = zStart; z <= zEnd; z += bbSpacingCoord[2])
            {
                int iRod = -1;

                //----- Central rod points and Rods in +X -----\\
                int rodCountX = 0;
                bool withinX = true;
                while (withinX)
                {
                    iRod++;
                    if (first) points.Add(new List<VVector>());

                    double testMaxX = xStart + (double)rodCountX * (2.0 * cyl.radius + latticeSeparation) + r;
                    if (testMaxX > bbEndCoord[0])
                    {
                        withinX = false;
                        break; // remove this if you want the rod to exceed the bounding box
                    }
                    for (int i = 0; i < nPts; i++)
                    {
                        double x = xStart + (double)rodCountX * (2.0 * cyl.radius + latticeSeparation) + r * Math.Cos((double)i * 2.0 * Math.PI / (double)nPts);
                        double y = yStart + r * Math.Sin((double)i * 2.0 * Math.PI / (double)nPts);
                        points[iRod].Add(new VVector(x, y, z));
                    }

                    //----- For this value of +X, add rods in +Y -----\\
                    int rodCountY = 1;
                    bool withinY = true;
                    while (withinY)
                    {
                        iRod++;
                        if (first) points.Add(new List<VVector>());

                        double test_max_y = yStart + (double)rodCountY * (2.0 * cyl.radius + latticeSeparation) + r;
                        if (test_max_y > bbEndCoord[1])
                        {
                            withinY = false;
                            break; // remove this if you want the rod to exceed the bounding box
                        }
                        for (int i = 0; i < nPts; i++)
                        {
                            double x = xStart + (double)rodCountX * (2.0 * cyl.radius + latticeSeparation) + r * Math.Cos((double)i * 2.0 * Math.PI / (double)nPts);
                            double y = yStart + (double)rodCountY * (2.0 * cyl.radius + latticeSeparation) + r * Math.Sin((double)i * 2.0 * Math.PI / (double)nPts);
                            points[iRod].Add(new VVector(x, y, z));
                        }
                        rodCountY++;
                    }

                    //----- For this value of +X, add rods in -Y -----\\
                    rodCountY = 1;
                    withinY = true;
                    while (withinY)
                    {
                        iRod++;
                        if (first) points.Add(new List<VVector>());

                        double testMinY = yStart - (double)rodCountY * (2.0 * cyl.radius + latticeSeparation) - r;
                        if (testMinY < bbStartCoord[1])
                        {
                            withinY = false;
                            break; // remove this if you want the rod to exceed the bounding box
                        }
                        for (int i = 0; i < nPts; i++)
                        {
                            double x = xStart + (double)rodCountX * (2.0 * cyl.radius + latticeSeparation) + r * Math.Cos((double)i * 2.0 * Math.PI / (double)nPts);
                            double y = yStart - (double)rodCountY * (2.0 * cyl.radius + latticeSeparation) + r * Math.Sin((double)i * 2.0 * Math.PI / (double)nPts);
                            points[iRod].Add(new VVector(x, y, z));
                        }
                        rodCountY++;
                    }

                    rodCountX++;
                }

                //----- Rods in -X -----\\
                rodCountX = 1;
                withinX = true;
                while (withinX)
                {

                    iRod++;
                    if (first) points.Add(new List<VVector>());

                    double testMinX = xStart - (double)rodCountX * (2.0 * cyl.radius + latticeSeparation) - r;
                    if (testMinX < bbStartCoord[0])
                    {
                        withinX = false;
                        break; // remove this if you want the rod to exceed the bounding box
                    }
                    for (int i = 0; i < nPts; i++)
                    {
                        double x = xStart - (double)rodCountX * (2.0 * cyl.radius + latticeSeparation) + r * Math.Cos((double)i * 2.0 * Math.PI / (double)nPts);
                        double y = yStart + r * Math.Sin((double)i * 2.0 * Math.PI / (double)nPts);
                        points[iRod].Add(new VVector(x, y, z));
                    }

                    //----- For this value of -X, add rods in +Y -----\\
                    int rodCountY = 1;
                    bool withinY = true;
                    while (withinY)
                    {
                        iRod++;
                        if (first) points.Add(new List<VVector>());

                        double test_max_y = yStart + (double)rodCountY * (2.0 * cyl.radius + latticeSeparation) + r;
                        if (test_max_y > bbEndCoord[1])
                        {
                            withinY = false;
                            break; // remove this if you want the rod to exceed the bounding box
                        }
                        for (int i = 0; i < nPts; i++)
                        {
                            double x = xStart - (double)rodCountX * (2.0 * cyl.radius + latticeSeparation) + r * Math.Cos((double)i * 2.0 * Math.PI / (double)nPts);
                            double y = yStart + (double)rodCountY * (2.0 * cyl.radius + latticeSeparation) + r * Math.Sin((double)i * 2.0 * Math.PI / (double)nPts);
                            points[iRod].Add(new VVector(x, y, z));
                        }
                        rodCountY++;
                    }

                    //----- For this value of -X, add rods in -Y -----\\
                    rodCountY = 1;
                    withinY = true;
                    while (withinY)
                    {
                        iRod++;
                        if (first) points.Add(new List<VVector>());

                        double testMinY = yStart - (double)rodCountY * (2.0 * cyl.radius + latticeSeparation) - r;
                        if (testMinY < bbStartCoord[1])
                        {
                            withinY = false;
                            break; // remove this if you want the rod to exceed the bounding box
                        }
                        for (int i = 0; i < nPts; i++)
                        {
                            double x = xStart - (double)rodCountX * (2.0 * cyl.radius + latticeSeparation) + r * Math.Cos((double)i * 2.0 * Math.PI / (double)nPts);
                            double y = yStart - (double)rodCountY * (2.0 * cyl.radius + latticeSeparation) + r * Math.Sin((double)i * 2.0 * Math.PI / (double)nPts);
                            points[iRod].Add(new VVector(x, y, z));
                        }
                        rodCountY++;
                    }

                    rodCountX++;
                }

                first = false;
            }

            return points;
        }


        ///////////////////////////////////////////////////////////
        // CheckStructureIDs
        ///////////////////////////////////////////////////////////
        string CheckStructureIDs(string name, StructureSet ss)
        {
            string structName = "FAIL";
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
                    if (!structSet.Structures.Any(e => e.Id.ToLower() == name + i.ToString()))
                    {
                        // Structure name not found in structure set, make it
                        structName = name + i.ToString();
                        break;
                    }
                }
            }
            else
            {
                structName = name;
            }
            return structName;
        }


        ///////////////////////////////////////////////////////////
        // CreateStructureAndAddToSet
        ///////////////////////////////////////////////////////////
        Structure CreateStructureAndAddToSet(string name, StructureSet structSet, List<List<VVector>> lattice)
        {
            Structure newStruct = null;

            // Check if it exists first
            string structName = CheckStructureIDs(name, structSet);
            if (structName == "FAIL")
            {
                // Give up
                message += "Unable to make " + name + " structure after 10 attempts. Please delete/rename structures and try again.\n";
                this.Dispatcher.Invoke(() =>
                {
                    runButton.IsEnabled = true;
                    abortButton.IsEnabled = false;
                    messageTextBlock.Text = message;
                    progressBar.Value = 0;
                });
                return null;
            }
            else
            {
                // Structure name not found in structure set, make it
                newStruct = structSet.AddStructure("CONTROL", structName);
            }

            // Fill contour slice-by-slice
            double imageResZ = structSet.Image.ZRes;
            var points = new List<VVector>();
            VVector[] arrayPoints;

            for (int i = 0; i < lattice.Count(); i++)
            {
                int oldSlice = -1;
                bool first = true;

                for (int j = 0; j < lattice[i].Count(); j++)
                {
                    int slice = Convert.ToInt32((lattice[i][j][2] - structSet.Image.Origin.z) / imageResZ);
                    if (slice != oldSlice && !first)
                    {
                        arrayPoints = points.ToArray();
                        newStruct.AddContourOnImagePlane(arrayPoints, slice);
                        points.Clear();
                        oldSlice = slice;
                    }
                    else
                    {
                        points.Add(lattice[i][j]);
                    }
                    if (first)
                    {
                        first = false;
                        oldSlice = slice;
                    }
                }
            }
            return newStruct;
        }

        ///////////////////////////////////////////////////////////
        // ConstructAffineTransform
        ///////////////////////////////////////////////////////////
        Matrix<double> ConstructAffineTransform(Matrix<double> R, List<double> P)
        {
            Vector<double> v = Vector<double>.Build.DenseOfArray(P.ToArray());
            Vector<double> transl = v - R * v;
            double[,] t = new double[,] {
                { R[0,0], R[0,1], R[0,2], transl[0] },
                { R[1,0], R[1,1], R[1,2], transl[1] },
                { R[2,0], R[2,1], R[2,2], transl[2] },
                { 0,      0,      0,      1 }
            };
            Matrix<double> affineTransform = Matrix<double>.Build.DenseOfArray(t);
            return affineTransform;
        }
    }
}

