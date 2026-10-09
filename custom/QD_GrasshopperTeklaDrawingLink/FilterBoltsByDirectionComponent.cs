using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GTDrawingLink.Extensions;
using GTDrawingLink.Tools;
using GTDrawingLink.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using Tekla.Structures.Drawing;
using TSM = Tekla.Structures.Model;
using TSG = Tekla.Structures.Geometry3d;

namespace GTDrawingLink.Components.QD_Components
{
    public class FilterBoltsByDirectionComponent : TeklaComponentBaseNew<FilterBoltsByDirectionCommand>
    {
        public override Guid ComponentGuid => new Guid("6d8fd5c4-1572-4983-be71-1d50b334f6d4");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        private static Bitmap _icon;
        protected override Bitmap Icon
        {
            get
            {
                if (_icon == null)
                {
                    var base64 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAX+SURBVEhLfZVbbJtnGcdzySW3u+OCg7jgcIMYTKAhbRKaKkCjkwYTF6u0gqpSjcLYxMQiira2WUu3oY4W2JqFlIqmaZuENGmcpIljJ875YCd2HCex4/PpOx/82U5+6P0+k7YIzdJfz/Mevvf//J/nfV63qYZxxLSdjpVIvGNtY+tTsSJsbNvFSst6/lYLj8xF4h1Oo9HRls2XrtgNMGtNTKfpWqPWxHL2aRxwCLsJhtNEL0noZfkQRkV5fCwgG2j1fWpNaIsnUhfyZQXTtNBqdTRAN23yxQrR+Da7exnXZrN5pFt9GA+m0CeDLrSWNfzTaBMB1+qBENVb/ajzS+RUi7b4TvpCoaqh6Qa5S5fJvn0RdWCYzXSO7z77Iz7/lSd5+rmjRAIh7P57jK1EePPse/SP+gknkrRfuExnTz87uSLnL3/Exb93kwpHqfbcJW84giB1IV+SMYDM2Utstn2G8toGTeD+mJ8vfPVb+EOLOIUy2sAQ5652cuTFn/O7P17k2o1efvDSCY7/6vf0DgzzwsuneP7lU/gHR9DvjZDXay2CiooqK2Te/YD0B1fJd93AaO6TTOd45eRvUAwbKZ2lcu06u+sxej6+TiQ4R3lrl75/3GTWN4G2l2H45l1G7t5DEmu3+iiYdS9FuZKCphnItoMCyJqJrOoUyxLpbAFVN5F0k/L8MlXfBNbMPPJEkMqY3/VV/zSl0QmM6TmM4CyloVGUTJ5cVact4RLI7iEVWaXqQqMiqUiK5s1LKlUBq0bVrlMVgdQamHVQnaYLzdnHqB94e2p1ZMMiV5S9FAlHMywkRXcPFVA0A8t2MO0apu14Ktw1HVU1yZZL9C6OcD86xUgsQN/qKP7NBXRdnON9LwL3CEoyim6g6zb7NVyyTK7IaiTKZmKXlfAGyb0smu6lrmY2GIuFGIkFuXl7kNfa32F+NcwnC3coS7K7RxCI6+8SFMoKmmnyju8q7/o+onPhLnupHN959od87svf4NvfO8JaJIYtpLcIxmMhZlNrXOvs4diJ15lfWOX2mo9iVXpIUGoRiBQdNOHG/CBPtD/Fdj6D+I1NTvOlrz2FPzhHcx9X+qGC6Ayh5Crj49OcOfc+qb0c/1q6R1ESBMajKfJukW3V2S5kODv6NxyrgWHapNI5jp98zd2saKIGOrLSUhANMb2zzJWr1/nZK6cJzixwO/K4ApcgkUy7jSbyrmomtul4G1Tj8WvaKrCsaJ6CDaFgjUBggXN/+pBcvuQp+H8EwtENy10Q8G6T8A1007sVInJZ9Ugcs8nIepD5VJg/v9fJj39ygtD0ErfDPvKVamvv/xAIBe5C6/BPg2U4LO6s073Qz8DsOJfvdDO0NkVXqM9VLs4QCrJF6SGBSIPXZB7c5vqv744VNz2apqNpGpZpk8gl2cjG2a6kCO/FKMtVDN1w1xVNf0ggaiAIVPGx7uVZROA0mjj1Jk5jH8O0yBRlVraKRJIV1nZKrO9UiKdVYimZ6K7EerLK2m6ZyG7Je50FwY4gEH1gmCQyVdJ5ibJskBSP1+gkU9NzDPsm2Ijv0jm2TWC9yODMFg9WswSjJYZmtxlb3mM6Vsa3mGJ8OcWdUJqhhTSKonkKChXV7dKu8QTffHWE3pk0xUKep7//PJ994os889xRQqtb/HuxyIPxMU6efpPuf95kaWmFV3/bzsX3/8JmLMZbZ87TfuYssyub3Ao+QiBS1KjXONcb4ej5KXJlhf2DAxZXInz9yWeIxRPkpRp9szk+6ermp8dO0XHpQwYGh3np2C/59Rt/4MGEn+MnX+fYL05z3z9P33zeLbb7mhbKsttIpz9e4kzXsiu/5jju+/PGW2+7+cyWFP46HGc5nufGYIDA6i6RZJWe+yF8oSjRtELf+CKDk8v4lrP0BHY8BeKpSOerWHYN9h0Omg6WZblFFz0hrq/4jzYti9hehclwjoVtmeloAX84x1xCYmaz7PlbVRf+cJaKYpApVGkrVeQr9QPcwkqq5aKqmi4kzUIxaofzohltu4Zl1dyAXFg1TEsEYB+OhS1LOg3gP8mL1MguhF0qAAAAAElFTkSuQmCC";
                    using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(base64)))
                    {
                        _icon = new Bitmap(ms);
                    }
                }
                return _icon;
            }
        }

        public FilterBoltsByDirectionComponent() : base(new GH_InstanceDescription
        {
            Name = "Filter Bolts by Direction",
            NickName = "FilterBolts",
            Description = "Filter Bolts by Direction",
            Category = "QD Component",
            SubCategory = "QD_Components"
        })
        {
        }

        protected override void InvokeCommand(IGH_DataAccess DA)
        {
            var (coordinateSystem, modelObjectsTree, paths) = _command.GetInputValues();

            if (modelObjectsTree.Count == 0 || modelObjectsTree.TrueForAll(branch => branch.Count == 0))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input Model Objects tree is empty.");
                return;
            }

            var xPositionsTree = new GH_Structure<GH_Point>();
            var yPositionsTree = new GH_Structure<GH_Point>();
            var zPositionsTree = new GH_Structure<GH_Point>();
            var skewPositionsTree = new GH_Structure<GH_Point>();

            var transform = Transform.PlaneToPlane(coordinateSystem, Plane.WorldXY);
            var teklaModel = new Tekla.Structures.Model.Model();

            for (int i = 0; i < modelObjectsTree.Count; i++)
            {
                var branch = modelObjectsTree[i];
                var path = paths[i];

                foreach (var goo in branch)
                {
                    TSM.BoltGroup boltGroup = null;
                    if (goo is GH_Goo<TSM.ModelObject> modelGoo)
                    {
                        boltGroup = modelGoo.Value as TSM.BoltGroup;
                    }
                    else if (goo != null && goo.CastTo(out TSM.ModelObject modelObject))
                    {
                        boltGroup = modelObject as TSM.BoltGroup;
                    }
                    else if (goo is TeklaDatabaseObjectGoo drawingObject && drawingObject.Value is ModelObject drawingModelObject)
                    {
                        boltGroup = teklaModel.SelectModelObject(drawingModelObject.ModelIdentifier) as TSM.BoltGroup;
                    }

                    if (boltGroup == null)
                        continue;

                    var zDir = GetZDirection(boltGroup.GetCoordinateSystem());
                    zDir.Transform(transform);
                    
                    if (zDir.Length > 0.001)
                    {
                        zDir.Unitize();
                    }

                    var boltPositions = GetBoltPositions(boltGroup.BoltPositions);

                    GH_Structure<GH_Point> targetTree;
                    if (Math.Abs(zDir.X) >= 0.99)
                        targetTree = xPositionsTree;
                    else if (Math.Abs(zDir.Y) >= 0.99)
                        targetTree = yPositionsTree;
                    else if (Math.Abs(zDir.Z) >= 0.99)
                        targetTree = zPositionsTree;
                    else
                        targetTree = skewPositionsTree;

                    foreach (var pt in boltPositions)
                    {
                        var transformedPt = pt;
                        transformedPt.Transform(transform);
                        targetTree.Append(new GH_Point(transformedPt), path);
                    }
                }
            }

            _command.SetOutputValues(DA, xPositionsTree, yPositionsTree, zPositionsTree, skewPositionsTree);
        }

        private List<Point3d> GetBoltPositions(System.Collections.ArrayList boltPositions)
        {
            var points = new List<Point3d>();
            foreach (TSG.Point point in boltPositions)
            {
                points.Add(point.ToRhino());
            }

            return points;
        }

        private Vector3d GetZDirection(TSG.CoordinateSystem coordinateSystem)
        {
            var zDir = coordinateSystem.AxisX.Cross(coordinateSystem.AxisY).GetNormal();
            return zDir.ToRhino();
        }
    }

    public class FilterBoltsByDirectionCommand : CommandBase
    {
        private readonly InputTreeParam<IGH_Goo> _inModelObjects = new InputTreeParam<IGH_Goo>(ParamInfos.ModelObject);
        private readonly InputStructParam<Plane> _inCoordinateSystem = new InputStructParam<Plane>(ParamInfos.DisplayCoordinateSystem);

        private readonly OutputTreeParam<Point3d> _outXPositions = new OutputTreeParam<Point3d>(new GH_InstanceDescription
        {
            Name = "X Positions",
            NickName = "X",
            Description = "Bolt positions parallel to X axis"
        }, 0);
        
        private readonly OutputTreeParam<Point3d> _outYPositions = new OutputTreeParam<Point3d>(new GH_InstanceDescription
        {
            Name = "Y Positions",
            NickName = "Y",
            Description = "Bolt positions parallel to Y axis"
        }, 1);
        
        private readonly OutputTreeParam<Point3d> _outZPositions = new OutputTreeParam<Point3d>(new GH_InstanceDescription
        {
            Name = "Z Positions",
            NickName = "Z",
            Description = "Bolt positions parallel to Z axis"
        }, 2);
        
        private readonly OutputTreeParam<Point3d> _outSkewPositions = new OutputTreeParam<Point3d>(new GH_InstanceDescription
        {
            Name = "Skew Positions",
            NickName = "Skew",
            Description = "Bolt positions with skew direction"
        }, 3);

        internal (Plane coordinateSystem, List<List<IGH_Goo>> modelObjectsTree, IReadOnlyList<GH_Path> paths) GetInputValues()
        {
            return (_inCoordinateSystem.Value, _inModelObjects.Value, _inModelObjects.Paths);
        }

        internal Result SetOutputValues(IGH_DataAccess DA, IGH_Structure xPositions, IGH_Structure yPositions, IGH_Structure zPositions, IGH_Structure skewPositions)
        {
            _outXPositions.Value = xPositions;
            _outYPositions.Value = yPositions;
            _outZPositions.Value = zPositions;
            _outSkewPositions.Value = skewPositions;

            return SetOutput(DA);
        }
    }
}
