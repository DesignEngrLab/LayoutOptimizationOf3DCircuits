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
        private Design design;

        public ComponentToContainerOverlap(Design design)
        {
            this.design = design;
        }

        public double calculate(double[] x)
        {
            double box_penalty;
            box_penalty = 0.0;
            var ts0 = design.container.ts;
            foreach (var comp in design.components)
            {
                var ts1 = comp.ts;
                List<Vertex> ts1VertsInts0, ts0VertsInts1;
                List<Vertex> ts1VertsOutts0, ts0VertsOutts1;
                TVGL.MiscFunctions.FindSolidIntersections(ts0, ts1, out ts0VertsInts1,
                            out ts0VertsOutts1, out ts1VertsInts0, out ts1VertsOutts0, false);
                ts1VertsOutts0.AddRange(ts0VertsOutts1);
                var convexHull = new TVGLConvexHull(ts1VertsOutts0, 0.000001);
                var vol = convexHull.Volume;
                box_penalty += vol;

            }
            design.new_obj_values[2] = box_penalty;
            return box_penalty;
        }
    }
}
