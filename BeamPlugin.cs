using System;
using System.Windows.Forms;
using System.Collections.Generic;
using T3D = Tekla.Structures.Geometry3d;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Plugins;
using TSM = Tekla.Structures.Model;

namespace BeamPlugin
{
    public class StructuresData
    {
        [StructuresField("LengthFactor")]
        public double LengthFactor;
        [StructuresField("Profile")]
        public string Profile;
        [StructuresField("Angle")]
        public int Angle;
    }

    /// <summary>
    /// This is the same example found in the Open API Reference PluginBase section, 
    /// but implemented using Windows Forms. The plugin asks the user to pick two points.
    /// The plug-in then calculates new insertion points using a double parameter from the 
    /// dialog and creates a beam.
    /// </summary>
    [Plugin("BeamPlugin")] // Mandatory field which defines that the class is a plug-in-and stores the name of the plug-in to the system.
    [PluginUserInterface("BeamPlugin.BeamPluginForm")] // Mandatory field which defines the user interface the plug-in uses. A windows form class of a .inp file.
    public class BeamPlugin : PluginBase
    {
        private StructuresData Data { get; set; }

        private TSM.Model Model;

        private double _LengthFactor;
        private string _Profile;
        private int _Angle;

        private TSM.Beam Beam;
        private TSM.Beam BeamLittle1;
        private TSM.Beam BeamLittle2;

        private int littleBeamPositioningAngle = 0;
        private double littleBeamLength = 2000;
        T3D.CoordinateSystem CoordinateSystem = new T3D.CoordinateSystem();

        // The constructor argument defines the database class StructuresData and set the data to be used in the plug-in.
        public BeamPlugin(StructuresData data)
        {
            Model = new TSM.Model();
            Data = data;
            GetValuesFromDialog();
        }

        //Defines the inputs to be passed to the plug-in.
        public override List<InputDefinition> DefineInput()
        {
            Picker BeamPicker = new Picker();
            List<InputDefinition> PointList = new List<InputDefinition>();
                        
            T3D.Point Point1 = BeamPicker.PickPoint();
            T3D.Point Point2 = BeamPicker.PickPoint();

            InputDefinition Input1 = new InputDefinition(Point1);
            InputDefinition Input2 = new InputDefinition(Point2);

            //Add inputs to InputDefinition list.
            PointList.Add(Input1);
            PointList.Add(Input2);

            return PointList;
        }

        private static TSM.Beam CreateBeam(T3D.Point Point1, T3D.Point Point2, double offsetX, string _Profile)
        {
            TSM.Beam MyBeam = new TSM.Beam(Point1, Point2);

            MyBeam.StartPointOffset.Dx = offsetX;
            MyBeam.Position.Depth = TSM.Position.DepthEnum.MIDDLE;
            MyBeam.Position.DepthOffset = 0;
            MyBeam.Position.Plane = TSM.Position.PlaneEnum.MIDDLE;
            MyBeam.Position.PlaneOffset = 0;
            MyBeam.Position.Rotation = TSM.Position.RotationEnum.TOP;
            MyBeam.Position.RotationOffset = 0;
            MyBeam.StartPoint = Point1;
            MyBeam.EndPoint = Point2;
            MyBeam.Profile.ProfileString = _Profile;
            MyBeam.Finish = "PAINT";
            MyBeam.Insert();

            return MyBeam;
        }

        // Gets the values from the dialog and sets the default values if needed
        private void GetValuesFromDialog()
        {
            _LengthFactor = Data.LengthFactor;
            if (Width(Data.Profile) > 0.001)
            {
                _Profile = Data.Profile;
            }
            _Angle = Data.Angle;
            if (IsDefaultValue(_LengthFactor))
            {
                _LengthFactor = 2.0;
            }
            if (IsDefaultValue(_Profile))
            {
                _Profile = "RHS150*5";
            }
            if (_Angle == 0 || _Angle == 90 || _Angle == 180 || _Angle == 270)
            {
                littleBeamPositioningAngle = _Angle;
            }

        }

        //Main method of the plug-in.
        public override bool Run(List<InputDefinition> Input)
        {
            try
            {
                GetValuesFromDialog();

                T3D.Point Point1 = (T3D.Point)(Input[0]).GetInput();
                T3D.Point Point2 = (T3D.Point)(Input[1]).GetInput();
                T3D.Point LengthVector = new T3D.Point(Point2.X - Point1.X, Point2.Y - Point1.Y, Point2.Z - Point1.Z);

                if (_LengthFactor > 0)
                {
                    Point2.X = _LengthFactor * LengthVector.X + Point1.X;
                    Point2.Y = _LengthFactor * LengthVector.Y + Point1.Y;
                    Point2.Z = _LengthFactor * LengthVector.Z + Point1.Z;
                }

                Beam = CreateBeam(Point1, Point2, 0, _Profile);

                BeamLittle1 = CreateBeam(new T3D.Point(0, 0, 0), new T3D.Point(1000, 0, 0), 0, _Profile);
                BeamLittle2 = CreateBeam(new T3D.Point(0, 0, 0), new T3D.Point(1000, 0, 0), 0, _Profile);
                    
                // Update beams to the right position
                UpdateLittleBeams();

                // Set new angle
                littleBeamPositioningAngle += 90;
                if (littleBeamPositioningAngle == 360)
                {
                    littleBeamPositioningAngle = 0;
                }

                // Update beams to the new angle
                UpdateLittleBeams();
              
            }
            catch (Exception Ex)
            {
            }

            return true;
        }

        private void UpdateLittleBeams()
        {
            double littleBeamOffsetX = Width(Beam.Profile.ProfileString) / 2;
            double littleBeamOffsetY = 0;

            BeamLittle1.Position.RotationOffset = 0;
            BeamLittle2.Position.RotationOffset = 0;

            T3D.Point Point1 = Beam.StartPoint;
            T3D.Point Point2 = Beam.EndPoint;

            T3D.Vector Up = Beam.GetCoordinateSystem().AxisY;
            T3D.Vector Axial = Beam.GetCoordinateSystem().AxisX;
            switch (littleBeamPositioningAngle)
            {
                case 0:
                    CoordinateSystem = new T3D.CoordinateSystem(Point1, Up, Axial);
                    break;
                case 90:
                    T3D.Vector BeamAxisRight = new T3D.Vector(Axial.Y * Up.Z - Axial.Z * Up.Y, (-1.0) * (Axial.X * Up.Z - Axial.Z * Up.X), Axial.X * Up.Y - Axial.Y * Up.X);
                    CoordinateSystem = new T3D.CoordinateSystem(Point1, BeamAxisRight, Axial);
                    break;
                case 180:
                    T3D.Vector BeamAxisOpposite = new T3D.Vector(-1.0 * Up.X, -1.0 * Up.Y, -1.0 * Up.Z);
                    CoordinateSystem = new T3D.CoordinateSystem(Point1, BeamAxisOpposite, Axial);
                    break;
                case 270:
                    T3D.Vector BeamAxisLeft = new T3D.Vector((-1.0) * (Axial.Y * Up.Z - Axial.Z * Up.Y), Axial.X * Up.Z - Axial.Z * Up.X, (-1.0) * (Axial.X * Up.Y - Axial.Y * Up.X));
                    CoordinateSystem = new T3D.CoordinateSystem(Point1, BeamAxisLeft, Axial);
                    break;
            }

            T3D.Point LittleBeamVector = StretchVector(CoordinateSystem.AxisX, littleBeamLength);

            BeamLittle1.StartPoint = Point1;
            BeamLittle1.EndPoint = new T3D.Point(Point1.X + LittleBeamVector.X, Point1.Y + LittleBeamVector.Y, Point1.Z + LittleBeamVector.Z);
            BeamLittle1.StartPointOffset.Dx = littleBeamOffsetX;
            BeamLittle1.Modify();
            
            double RotationAngle = AngleBetweenVectors(Beam.GetCoordinateSystem().AxisX, BeamLittle1.GetCoordinateSystem().AxisY);
            BeamLittle1.Position.RotationOffset = RotationAngle;
            BeamLittle1.Modify();
            double CheckAngle = AngleBetweenVectors(Beam.GetCoordinateSystem().AxisX, BeamLittle1.GetCoordinateSystem().AxisY);
            if (0.00001 < CheckAngle)
            {
                RotationAngle = -1.0 * RotationAngle;
                BeamLittle1.Position.RotationOffset = RotationAngle;
            }
            BeamLittle1.Position.DepthOffset = littleBeamOffsetY;
            BeamLittle1.Modify();

            BeamLittle2.StartPoint = Point2;
            BeamLittle2.EndPoint = new T3D.Point(Point2.X + LittleBeamVector.X, Point2.Y + LittleBeamVector.Y, Point2.Z + LittleBeamVector.Z);
            BeamLittle2.StartPointOffset.Dx = littleBeamOffsetX;
            BeamLittle2.Position.RotationOffset = RotationAngle;
            BeamLittle2.Position.DepthOffset = -littleBeamOffsetY;
            BeamLittle2.Modify();

            MessageBox.Show("Beams updated", "Message", MessageBoxButtons.YesNo);

            Model.CommitChanges();
        }

        private double LengthOfVector(T3D.Point Point)
        {
            return Math.Sqrt(Point.X * Point.X + Point.Y * Point.Y + Point.Z * Point.Z);
        }

        private double LengthOfVector(T3D.Vector Vector)
        {
            return Math.Sqrt(Vector.X * Vector.X + Vector.Y * Vector.Y + Vector.Z * Vector.Z);
        }

        private T3D.Vector UnitVector(T3D.Vector Vector)
        {
            double vectorLength = LengthOfVector(Vector);
            return new T3D.Vector(Vector.X / vectorLength, Vector.Y / vectorLength, Vector.Z / vectorLength);
        }

        private T3D.Vector StretchVector(T3D.Vector Vector, double length)
        {
            T3D.Vector UnitVectorBeforeStreching = UnitVector(Vector);
            return new T3D.Vector(UnitVectorBeforeStreching.X * length, UnitVectorBeforeStreching.Y * length, UnitVectorBeforeStreching.Z * length);
        }

        private double AngleBetweenVectors(T3D.Vector Point1, T3D.Vector Point2)
        {
            double radians = Math.Acos((Point1.X * Point2.X + Point1.Y * Point2.Y + Point1.Z * Point2.Z) / (LengthOfVector(Point1) * LengthOfVector(Point2)));
            return 360.0 / (2.0 * Math.PI) * radians;
        }

        private double Width(string str)
        {
            str = str.Replace(" ", String.Empty);
            if (!str.Substring(0, 3).Equals("RHS"))
            {
                return 0;
            }
            str = str.Substring(3);
            string[] words = str.Split('*');
            if (words.Length > 2)
            {
                return 0;
            }
            double returnValue;
            if (double.TryParse(words[0], out returnValue))
            {
                return returnValue;
            }
            return 0;
        }
    }
}
