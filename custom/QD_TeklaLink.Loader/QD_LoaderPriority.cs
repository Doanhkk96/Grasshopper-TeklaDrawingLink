using Grasshopper.Kernel;

namespace QD_TeklaLink.Loader
{
    public class QD_LoaderPriority : GH_AssemblyPriority
    {
        static QD_LoaderPriority()
        {
            QD_LoaderEngine.Initialize();
        }

        public override GH_LoadingInstruction PriorityLoad()
        {
            QD_LoaderEngine.Initialize();
            return GH_LoadingInstruction.Proceed;
        }
    }
}

