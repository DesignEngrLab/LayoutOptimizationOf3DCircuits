using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
using TVGL;

namespace _3D_LayoutOpt.Functions
{
    /* ---------------------------------------------------------------------------------- */
    /* THIS FUNCTION SETS THE VALUE OF THE THIRD PART OF THE OBJECTIVE FUNCTION, WHICH    */
    /* IS THE AMOUNT OF OVERLAP WITH THE CONTAINER.                                       */
    /* ---------------------------------------------------------------------------------- */

    class ComponentToContainerOverlap : IInequality
    {
        private Design _design;

        public ComponentToContainerOverlap(Design design)
        {
            this._design = design;
        }

        public double calculate(double[] x)
        {
            double boxPenalty;
            boxPenalty = 0.0;
            var ts0 = _design.Container.Ts;
            foreach (var comp in _design.Components)
            {
                var ts1 = comp.Ts;
                List<Vertex> ts1VertsInts0, ts0VertsInts1;
                List<Vertex> ts1VertsOutts0, ts0VertsOutts1;
                TVGL.MiscFunctions.FindSolidIntersections(ts0, ts1, out ts0VertsInts1,
                            out ts0VertsOutts1, out ts1VertsInts0, out ts1VertsOutts0, false);
                ts1VertsOutts0.AddRange(ts0VertsOutts1);
                var convexHull = new TVGLConvexHull(ts1VertsOutts0, 0.000001);
                var vol = convexHull.Volume;
                boxPenalty += vol;

            }
            _design.NewObjValues[2] = boxPenalty;
            return boxPenalty;
        }
    }
}
