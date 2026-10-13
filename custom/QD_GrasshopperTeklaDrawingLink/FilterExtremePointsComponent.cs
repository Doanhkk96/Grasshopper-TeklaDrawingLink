using Grasshopper.Kernel;
using GTDrawingLink.Components;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GTDrawingLink.Components.QD_Components
{
    public class FilterExtremePointsComponent : TeklaComponentBase
    {
        public override Guid ComponentGuid => new Guid("7f4f603c-8eb3-4791-8848-18e38adcdb98");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        private static Bitmap _icon;
        protected override Bitmap Icon
        {
            get
            {
                if (_icon == null)
                {
                    var bmp = new Bitmap(24, 24);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.Clear(Color.Transparent);

                        // Draw coordinate axes
                        using (var penAxis = new Pen(Color.FromArgb(140, 140, 140), 1.2f))
                        {
                            g.DrawLine(penAxis, 3, 20, 21, 20); // X axis
                            g.DrawLine(penAxis, 3, 20, 3, 3);   // Y axis
                        }

                        // Draw extreme points
                        using (var brushX = new SolidBrush(Color.FromArgb(230, 81, 0)))
                        using (var brushY = new SolidBrush(Color.FromArgb(25, 118, 210)))
                        using (var penBorder = new Pen(Color.White, 1f))
                        {
                            // Extreme point on Y
                            g.FillEllipse(brushX, 5, 5, 5, 5);
                            g.DrawEllipse(penBorder, 5, 5, 5, 5);

                            // Intermediate point
                            g.FillEllipse(brushY, 11, 12, 5, 5);
                            g.DrawEllipse(penBorder, 11, 12, 5, 5);

                            // Extreme point on X
                            g.FillEllipse(brushX, 16, 6, 5, 5);
                            g.DrawEllipse(penBorder, 16, 6, 5, 5);
                        }
                    }
                    _icon = bmp;
                }
                return _icon;
            }
        }

        public FilterExtremePointsComponent() : base(new GH_InstanceDescription
        {
            Name = "Filter Extreme Points",
            NickName = "FilterExtPts",
            Description = "Filters extreme boundary points along X and Y axes within a coordinate tolerance",
            Category = "QD",
            SubCategory = "Geometry"
        })
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Input points to filter", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Keep Max Y", "MaxY", "If true, keeps point with maximum Y coordinate for each X; if false, keeps minimum Y", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Keep Max X", "MaxX", "If true, keeps point with maximum X coordinate for each Y; if false, keeps minimum X", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Tolerance", "T", "Tolerance for grouping coordinates (in drawing units / mm)", GH_ParamAccess.item, 1.0);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points by X", "PtsX", "Points filtered by X coordinate and sorted ascending by X", GH_ParamAccess.list);
            pManager.AddPointParameter("Points by Y", "PtsY", "Points filtered by Y coordinate and sorted ascending by Y", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var points = new List<Point3d>();
            if (!DA.GetDataList(0, points) || points.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid points found.");
                return;
            }

            bool keepMaxY = true;
            DA.GetData(1, ref keepMaxY);

            bool keepMaxX = true;
            DA.GetData(2, ref keepMaxX);

            double tolerance = 1.0;
            DA.GetData(3, ref tolerance);

            if (tolerance <= 0)
                tolerance = 1.0;

            var (sortedXPoints, sortedYPoints) = FilterPoints(points, keepMaxY, keepMaxX, tolerance);

            DA.SetDataList(0, sortedXPoints);
            DA.SetDataList(1, sortedYPoints);
        }

        private (List<Point3d> sortedXPoints, List<Point3d> sortedYPoints) FilterPoints(
            List<Point3d> points,
            bool keepMaxY,
            bool keepMaxX,
            double tolerance)
        {
            long ToGridKey(double val) => (long)Math.Round(val / tolerance);

            var uniqueX = new Dictionary<long, Point3d>();
            var uniqueY = new Dictionary<long, Point3d>();

            foreach (var pt in points)
            {
                if (!pt.IsValid)
                    continue;

                long keyX = ToGridKey(pt.X);
                long keyY = ToGridKey(pt.Y);

                // 1. Group by X, select extreme Y
                if (!uniqueX.TryGetValue(keyX, out var currentYPt) ||
                    (keepMaxY ? pt.Y > currentYPt.Y : pt.Y < currentYPt.Y))
                {
                    uniqueX[keyX] = pt;
                }

                // 2. Group by Y, select extreme X
                if (!uniqueY.TryGetValue(keyY, out var currentXPt) ||
                    (keepMaxX ? pt.X > currentXPt.X : pt.X < currentXPt.X))
                {
                    uniqueY[keyY] = pt;
                }
            }

            var sortedXPoints = uniqueX.Values.OrderBy(p => p.X).ToList();
            var sortedYPoints = uniqueY.Values.OrderBy(p => p.Y).ToList();

            return (sortedXPoints, sortedYPoints);
        }
    }
}
