using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GTDrawingLink.Extensions;
using GTDrawingLink.Tools;
using GTDrawingLink.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Tekla.Structures.Drawing;
using TSG = Tekla.Structures.Geometry3d;

namespace GTDrawingLink.Components.Annotations
{
    public class CreateArcComponent : CreateDatabaseObjectComponentBaseNew<CreateArcCommand>
    {
        public override GH_Exposure Exposure => GH_Exposure.quarternary;
        protected override Bitmap Icon => Properties.Resources.Arc;

        public CreateArcComponent() : base(ComponentInfos.CreateArcComponent) { }

        protected override IEnumerable<DatabaseObject> InsertObjects(IGH_DataAccess DA)
        {
            (var inputViews, var circles, var attributes) = _command.GetInputValues(out bool mainInputIsCorrect);
            if (!mainInputIsCorrect)
            {
                HandleMissingInput();
                return null;
            }

            if (!DrawingInteractor.IsInTheActiveDrawing(inputViews.First()))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, Messages.Error_ViewFromDifferentDrawing);
                return null;
            }

            var views = new ViewCollection<ViewBase>(inputViews);
            var strategy = GetSolverStrategy(false, circles, attributes);
            var inputMode = strategy.Mode;

            var outputTree = new GH_Structure<TeklaDatabaseObjectGoo>();
            var outputObjects = new List<Arc>();

            for (int i = 0; i < strategy.Iterations; i++)
            {
                var path = strategy.GetPath(i);

                var geometry = circles.Get(i, inputMode);
                if (!(geometry is GH_Arc))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One of the provided objects is not type of Circle");
                    continue;
                }

                var rhinoArc = (geometry as GH_Arc).Value;

                var view = views.Get(path);
                var attribute = attributes.Get(i, inputMode);

                foreach (var arc in InsertArcs(view, rhinoArc, attribute))
                {
                    outputObjects.Add(arc);
                    outputTree.Append(new TeklaDatabaseObjectGoo(arc), path);
                }
            }

            _command.SetOutputValues(DA, outputTree);

            DrawingInteractor.CommitChanges();
            return outputObjects;
        }

        private static List<Arc> InsertArcs(ViewBase view,
                                            Rhino.Geometry.Arc rhinoArc,
                                            Arc.ArcAttributes attributes)
        {
            // A Tekla drawing arc is stored only as (StartPoint, EndPoint, Radius): it is always
            // drawn clockwise from start to end (bulging to the left of the start->end vector)
            // and can span at most 180 degrees. The center passed to the constructor is used only
            // to derive the radius. Tekla 2026 added the IsLargeArc flag for spans above 180.
            bool appearsCounterClockwise = rhinoArc.Plane.Normal.Z >= 0;

            var startPoint = rhinoArc.StartPoint.ToTekla();
            var endPoint = rhinoArc.EndPoint.ToTekla();
            var centerPoint = rhinoArc.Center.ToTekla();

            var insertedArcs = new List<Arc>();
            if (rhinoArc.Angle <= Math.PI)
            {
                insertedArcs.Add(InsertSingleArc(view, startPoint, endPoint, centerPoint, appearsCounterClockwise, attributes));
            }
            else
            {
#if API2026
                // The point order is inverted compared to arcs below 180 degrees,
                // because the large arc wraps around the other side of the chord.
                var arc = appearsCounterClockwise ?
                    new Arc(view, startPoint, endPoint, centerPoint, attributes) :
                    new Arc(view, endPoint, startPoint, centerPoint, attributes);

                arc.IsLargeArc = true;
                arc.Insert();
                insertedArcs.Add(arc);
#else
                // Older versions cannot represent an arc above 180 degrees - split it in half.
                var midPoint = rhinoArc.MidPoint.ToTekla();
                insertedArcs.Add(InsertSingleArc(view, startPoint, midPoint, centerPoint, appearsCounterClockwise, attributes));
                insertedArcs.Add(InsertSingleArc(view, midPoint, endPoint, centerPoint, appearsCounterClockwise, attributes));
#endif
            }

            return insertedArcs;
        }

        private static Arc InsertSingleArc(ViewBase view,
                                           TSG.Point fromPoint,
                                           TSG.Point toPoint,
                                           TSG.Point centerPoint,
                                           bool appearsCounterClockwise,
                                           Arc.ArcAttributes attributes)
        {
            var arc = appearsCounterClockwise ?
                new Arc(view, toPoint, fromPoint, centerPoint, attributes) :
                new Arc(view, fromPoint, toPoint, centerPoint, attributes);

            arc.Insert();
            return arc;
        }
    }

    public class CreateArcCommand : CommandBase
    {
        private readonly InputOptionalListParam<ViewBase> _inView = new InputOptionalListParam<ViewBase>(ParamInfos.View);
        private readonly InputTreeGeometry _inGeometricGoo = new InputTreeGeometry(ParamInfos.Arc, isOptional: true);
        private readonly InputTreeParam<Arc.ArcAttributes> _inAttributes = new InputTreeParam<Arc.ArcAttributes>(ParamInfos.ArcAttributes);

        private readonly OutputTreeParam<Arc> _outArcs = new OutputTreeParam<Arc>(ParamInfos.TeklaArc, 0);

        internal (List<ViewBase> views, TreeData<IGH_GeometricGoo> geometries, TreeData<Arc.ArcAttributes> atrributes) GetInputValues(out bool mainInputIsCorrect)
        {
            var result = (_inView.GetValueFromUserOrNull(), _inGeometricGoo.AsTreeData(), _inAttributes.AsTreeData());

            mainInputIsCorrect = result.Item1.HasItems() && result.Item2.HasItems();

            return result;
        }

        internal Result SetOutputValues(IGH_DataAccess DA, IGH_Structure circles)
        {
            _outArcs.Value = circles;

            return SetOutput(DA);
        }
    }
}
