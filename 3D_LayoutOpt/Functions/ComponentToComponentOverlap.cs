using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
using TVGL;

namespace _3D_LayoutOpt.Functions
{
    class ComponentToComponentOverlap : IInequality
    {
        private Design design;

        internal ComponentToComponentOverlap(Design design)
        {
            this.design = design;
        }

        public double calculate(double[] x)
        {
            double dx, dy, dz;
            Component comp0, comp1;

            for (int i = 0; i < design.comp_count; i++)
            {
                comp0 = design.components[i];
                var ts0 = comp0.ts;
                for (int j = i; j < design.comp_count; j++)
                {
                    comp1 = design.components[j];
                    var ts1 = comp1.ts;
                    List<Vertex> ts1VertsInts0, ts0VertsInts1;
                    List<Vertex> ts1VertsOutts0, ts0VertsOutts1;
                    TVGL.MiscFunctions.FindSolidIntersections(ts0, ts1, out ts0VertsInts1,
                        out ts0VertsOutts1, out ts1VertsInts0, out ts1VertsOutts0, false);
                    ts1VertsInts0.AddRange(ts0VertsInts1);
                    var convexHull = new TVGLConvexHull(ts1VertsInts0, 0.000001);
                    var vol = convexHull.Volume;
                    design.overlap[j, i] = vol / (ts0.Volume + ts1.Volume);       //USING OVERLAP VOLUME PERCENTAGE
                }
            }
            double sum;
            sum = 0.0;

            for (int i = 0; i < design.comp_count; i++)
            {
                for (int j = i; j < design.comp_count; j++)
                {
                    sum += design.overlap[j, i];
                }
            }
            if (sum > 0.0)
                design.new_obj_values[1] = (0.05 + sum) * design.new_obj_values[0];
            else
                design.new_obj_values[1] = 0.0;
            return sum;
        }
    }
}
