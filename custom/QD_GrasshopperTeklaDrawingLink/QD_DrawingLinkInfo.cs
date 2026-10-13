using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GTDrawingLink.Components.QD_Components
{
    public class QD_DrawingLinkInfo : GH_AssemblyInfo
    {
        public override string Name => "QD Tekla Drawing Link";
        public override Bitmap Icon => null;
        public override string Description => "Custom Tekla Drawing Link components by QD";
        public override Guid Id => new Guid("4a2c29b7-9dc5-4ce3-9381-8071e569d130");
        public override string AuthorName => "Quoc Doanh";
        public override string AuthorContact => "";
    }
}
