using Grasshopper.Kernel;
using GTDrawingLink.Extensions;
using GTDrawingLink.Tools;
using System;
using System.Drawing;
using Tekla.Structures.Drawing;

namespace GTDrawingLink.Components.QD_Components
{
    public class FitViewScaleComponent : TeklaComponentBaseNew<FitViewScaleCommand>
    {
        public override Guid ComponentGuid => new Guid("93cc47b0-3a3b-43de-ab91-4ba697b3b87a");
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

                        // Outer dashed/boundary frame (dark blue)
                        using (var penOuter = new Pen(Color.FromArgb(41, 128, 185), 1.5f))
                        {
                            g.DrawRectangle(penOuter, 2, 2, 19, 19);
                        }

                        // Inner view body (light blue fill)
                        using (var brushInner = new SolidBrush(Color.FromArgb(174, 214, 241)))
                        {
                            g.FillRectangle(brushInner, 6, 6, 11, 11);
                        }

                        using (var penInner = new Pen(Color.FromArgb(31, 97, 141), 1.2f))
                        {
                            g.DrawRectangle(penInner, 6, 6, 11, 11);
                        }

                        // Red scale adjustment corner arrows pointing inwards to fit
                        using (var penArrow = new Pen(Color.FromArgb(192, 57, 43), 1.5f))
                        {
                            // Top-left corner
                            g.DrawLine(penArrow, 3, 3, 5, 5);
                            // Top-right corner
                            g.DrawLine(penArrow, 20, 3, 18, 5);
                            // Bottom-left corner
                            g.DrawLine(penArrow, 3, 20, 5, 18);
                            // Bottom-right corner
                            g.DrawLine(penArrow, 20, 20, 18, 18);
                        }
                    }
                    _icon = bmp;
                }
                return _icon;
            }
        }

        public FitViewScaleComponent() : base(new GH_InstanceDescription
        {
            Name = "Fit View Scale",
            NickName = "FitViewScale",
            Description = "Automatically adjusts View Scale in increments divisible by 5 to fit view frame geometry within maximum width and height, keeping the view as large as possible.",
            Category = "QD",
            SubCategory = QD_PanelHeadings.Drawing
        })
        {
        }

        protected override void InvokeCommand(IGH_DataAccess DA)
        {
            var (view, maxWidth, maxHeight, step, apply) = _command.GetInputValues();

            if (view == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Provided View is null.");
                return;
            }

            view.Select();

            if (!IsInActiveDrawing(view))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The supplied view belongs to a different drawing than the currently opened.");
                return;
            }

            if (maxWidth <= 0 || maxHeight <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Max Width and Max Height must be greater than 0.");
                return;
            }

            if (step <= 0)
                step = 5;

            double originalScale = view.Attributes.Scale;
            double currentScale = originalScale;
            double currentWidth = view.Width;
            double currentHeight = view.Height;

            // 1. If view frame already fits within user limits, keep current scale to maximize view size
            if (currentWidth <= maxWidth && currentHeight <= maxHeight)
            {
                _command.SetOutputValues(DA, view, currentScale, currentWidth, currentHeight, true);
                return;
            }

            // 2. Analytical lower-bound scale estimation for fast convergence
            double ratioW = currentWidth / maxWidth;
            double ratioH = currentHeight / maxHeight;
            double maxRatio = Math.Max(ratioW, ratioH);

            double theoreticalScale = currentScale * maxRatio;
            int targetScale = (int)(Math.Ceiling(theoreticalScale / step) * step);
            if (targetScale <= (int)currentScale)
            {
                targetScale = ((int)currentScale / step + 1) * step;
            }

            // 3. Tekla verification loop (handles annotations, margins, mark texts)
            int maxIterations = 20;
            int iteration = 0;
            bool isFit = false;

            while (iteration < maxIterations)
            {
                iteration++;
                view.Attributes.Scale = targetScale;
                view.Modify();
                view.Select();

                currentWidth = view.Width;
                currentHeight = view.Height;

                if (currentWidth <= maxWidth && currentHeight <= maxHeight)
                {
                    isFit = true;
                    break;
                }

                targetScale += step;
            }

            if (!isFit)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Reached maximum iterations without fitting within the requested dimensions.");
            }

            // 4. Handle preview / apply
            if (!apply)
            {
                // Restore original scale in preview mode
                view.Attributes.Scale = originalScale;
                view.Modify();
                view.Select();
            }
            else
            {
                CommitDrawingChanges();
            }

            _command.SetOutputValues(DA, view, targetScale, currentWidth, currentHeight, isFit);
        }

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, ParamInfos.RecomputeObjects.Name, RecomputeComponent).ToolTipText = ParamInfos.RecomputeObjects.Description;
        }

        private static bool IsInActiveDrawing(View view)
        {
            try
            {
                var dh = new DrawingHandler();
                var dwg = dh.GetActiveDrawing();
                if (dwg == null || view == null) return false;

                var viewDwgId = view.GetPropertyValue("DrawingIdentifier") as Tekla.Structures.Identifier;
                var activeDwgId = dwg.GetPropertyValue("Identifier") as Tekla.Structures.Identifier;

                if (viewDwgId != null && activeDwgId != null)
                {
                    return viewDwgId.ID == activeDwgId.ID;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        private static bool CommitDrawingChanges()
        {
            try
            {
                var dh = new DrawingHandler();
                var dwg = dh.GetActiveDrawing();
                if (dwg != null)
                {
                    return dwg.CommitChanges();
                }
            }
            catch
            {
            }
            return false;
        }
    }

    public class FitViewScaleCommand : CommandBase
    {
        private readonly InputParam<View> _inView = new InputParam<View>(ParamInfos.View);

        private readonly InputStructParam<double> _inMaxWidth = new InputStructParam<double>(new GH_InstanceDescription
        {
            Name = "Max Width",
            NickName = "MaxW",
            Description = "Maximum allowed width of view frame geometry (mm)"
        });

        private readonly InputStructParam<double> _inMaxHeight = new InputStructParam<double>(new GH_InstanceDescription
        {
            Name = "Max Height",
            NickName = "MaxH",
            Description = "Maximum allowed height of view frame geometry (mm)"
        });

        private readonly InputOptionalStructParam<int> _inStep = new InputOptionalStructParam<int>(new GH_InstanceDescription
        {
            Name = "Step",
            NickName = "S",
            Description = "Scale step increment (divisible by 5, default is 5)"
        }, 5);

        private readonly InputOptionalStructParam<bool> _inApply = new InputOptionalStructParam<bool>(new GH_InstanceDescription
        {
            Name = "Apply",
            NickName = "App",
            Description = "If true, commits scale changes to drawing; if false, runs in preview mode without saving"
        }, true);

        private readonly OutputParam<View> _outView = new OutputParam<View>(ParamInfos.View);

        private readonly OutputParam<double> _outScale = new OutputParam<double>(ParamInfos.Scale);

        private readonly OutputParam<double> _outWidth = new OutputParam<double>(new GH_InstanceDescription
        {
            Name = "Width",
            NickName = "W",
            Description = "Actual frame width after fitting (mm)"
        });

        private readonly OutputParam<double> _outHeight = new OutputParam<double>(new GH_InstanceDescription
        {
            Name = "Height",
            NickName = "H",
            Description = "Actual frame height after fitting (mm)"
        });

        private readonly OutputParam<bool> _outFits = new OutputParam<bool>(new GH_InstanceDescription
        {
            Name = "Fits",
            NickName = "F",
            Description = "True if view frame geometry fits within requested limits"
        });

        internal (View view, double maxWidth, double maxHeight, int step, bool apply) GetInputValues()
        {
            return (
                _inView.Value,
                _inMaxWidth.Value,
                _inMaxHeight.Value,
                _inStep.Value,
                _inApply.Value
            );
        }

        internal Result SetOutputValues(IGH_DataAccess DA, View view, double scale, double width, double height, bool fits)
        {
            _outView.Value = view;
            _outScale.Value = scale;
            _outWidth.Value = width;
            _outHeight.Value = height;
            _outFits.Value = fits;

            return SetOutput(DA);
        }
    }
}
