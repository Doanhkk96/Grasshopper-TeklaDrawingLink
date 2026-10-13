using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GTDrawingLink.Tools;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GTDrawingLink.Components.QD_Components
{
    public class SortCurvesByMidpointComponent : TeklaComponentBaseNew<SortCurvesByMidpointCommand>
    {
        public override Guid ComponentGuid => new Guid("e224e70e-e30d-45db-b27b-204b73b64c23");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        private static Bitmap _icon;
        protected override Bitmap Icon
        {
            get
            {
                if (_icon == null)
                {
                    // User provided 24x24 image base64
                    var base64 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAKRSURBVEhLtZbdS1NhHMf9A0ICIROpPySCLkOGL033pqV00UUR3QZpOrd59pbadnzZMqgIugiiiy6szEQJgsiy2WYXlqg4c7rYJuR2dp7nG+eZO9vOdmqr9oUvzznf3++cz3leLk4NpbSJUuqqlmsopR5UURKAU4ZK0fczIO+mlXGRKKXM+WKAcHgbt708Jn3+Ao/5p3DfN46U/gSIrgEPfWPgfVOZuv8OJn2S/Rj1eLGy8kUGvJx5hWg0mgOsr2+gt68fFtsQrDaOjZIHbHaM2AaR0tZDbKuDx2ZGv83BeqxDnNx7o/cmPi4tyV/d0qbFp0AgB5ArKqKzj0BePFDGquo8343PwSC7lgHZ6ZW0VP9TT97aS4BgMFQBoEznA6o+gyLA70QJASWiMlZVEYAQgng8jkQiUeB4Yh+JWAyixQQy0I792A+WxVkt25NgzwqCIAO6LvQU7kEwFIKmuRU6Yyf0RhN0BhP0xk6cM3bhkrEDSU0tiOYILhu1aDV0sZpe6jFkeps0LZh9PacOSKfT2NmJILK7m3HkcNzbw15kB2JfC8j1s4h+32aZsk969iCZVAfIFRXR1AFo8qcyVlVJgPJE/Msp6u65iNXVrxUAKvC3tTW06wyYnn4OQUiXCShjBtl3XLl6DQ2NJ3Hq9BmkUkKZe7DwBGTusTIuqfn5BdQerYN50MruGWBjcxNmixV2pxt2p0u21XkLXieHlPY4iPYYJlwcy6Qa53CCc7hgd7jQb7YgEFhmLxRFEU2aZiwufsgBtrbCcA+PwMuPF3iEn8Bd3gNB1wjSUY97/CjLvPwYPHl2uYcRCmVOjaTA8jKSh8e2vCV6+wz0zVNlXKT8/cjqv21yKZcP+EtnAVX9q/gFQfgnQ9nmfpoAAAAASUVORK5CYII=";
                    using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(base64)))
                    {
                        _icon = new Bitmap(ms);
                    }
                }
                return _icon;
            }
        }

        public SortCurvesByMidpointComponent() : base(new GH_InstanceDescription
        {
            Name = "Sort Curves by Midpoint",
            NickName = "SortCurvesMid",
            Description = "Sorts a list of curves based on the X and Y coordinates of their midpoints.",
            Category = "QD",
            SubCategory = "Geometry"
        })
        {
        }

        protected override void InvokeCommand(IGH_DataAccess DA)
        {
            var curves = _command.GetInputValues();
            if (curves == null || curves.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input curve list is empty.");
                return;
            }

            var validCurves = curves.Where(c => c != null).ToList();

            if (validCurves.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input curve list contains only null elements.");
                return;
            }

            var curvesWithMidpoints = validCurves
                .Select(c => new { Curve = c, Mid = c.PointAtNormalizedLength(0.5) })
                .ToList();

            var sortedX = curvesWithMidpoints.OrderBy(item => item.Mid.X).Select(item => item.Curve).ToList();
            var sortedY = curvesWithMidpoints.OrderBy(item => item.Mid.Y).Select(item => item.Curve).ToList();

            _command.SetOutputValues(DA, sortedX, sortedY);
        }
    }

    public class SortCurvesByMidpointCommand : CommandBase
    {
        private readonly InputListParam<Curve> _inCurves = new InputListParam<Curve>(ParamInfos.Curve);

        private readonly OutputListParam<IGH_GeometricGoo> _outSortedX = new OutputListParam<IGH_GeometricGoo>(new GH_InstanceDescription
        {
            Name = "Sorted X",
            NickName = "X",
            Description = "Curves sorted by midpoint X coordinate"
        });

        private readonly OutputListParam<IGH_GeometricGoo> _outSortedY = new OutputListParam<IGH_GeometricGoo>(new GH_InstanceDescription
        {
            Name = "Sorted Y",
            NickName = "Y",
            Description = "Curves sorted by midpoint Y coordinate"
        });

        internal List<Curve> GetInputValues()
        {
            return _inCurves.Value;
        }

        internal Result SetOutputValues(IGH_DataAccess DA, List<Curve> sortedX, List<Curve> sortedY)
        {
            _outSortedX.Value = sortedX.Select(c => new Grasshopper.Kernel.Types.GH_Curve(c)).Cast<IGH_GeometricGoo>().ToList();
            _outSortedY.Value = sortedY.Select(c => new Grasshopper.Kernel.Types.GH_Curve(c)).Cast<IGH_GeometricGoo>().ToList();

            return SetOutput(DA);
        }
    }
}
