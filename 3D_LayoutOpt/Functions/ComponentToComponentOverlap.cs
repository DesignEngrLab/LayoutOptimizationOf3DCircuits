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
        private Design _design;

        internal ComponentToComponentOverlap(Design design)
        {
            this._design = design;
        }

        public double calculate(double[] x)
        {
            double dx, dy, dz;
            Component comp0, comp1;

            for (var i = 0; i < _design.CompCount; i++)
            {
                comp0 = _design.Components[i];
                var ts0 = comp0.Ts;
                for (var j = i; j < _design.CompCount; j++)
                {
                    comp1 = _design.Components[j];
                    var ts1 = comp1.Ts;
                    List<Vertex> ts1VertsInts0, ts0VertsInts1;
                    List<Vertex> ts1VertsOutts0, ts0VertsOutts1;
                    TVGL.MiscFunctions.FindSolidIntersections(ts0, ts1, out ts0VertsInts1,
                        out ts0VertsOutts1, out ts1VertsInts0, out ts1VertsOutts0, true);
                    ts1VertsInts0.AddRange(ts0VertsInts1);
                    var convexHull = new TVGLConvexHull(ts1VertsInts0, 0.000001);
                    var vol = convexHull.Volume;
                    _design.Overlap[j, i] = vol / (ts0.Volume + ts1.Volume);       //USING OVERLAP VOLUME PERCENTAGE
                }
            }
            double sum;
            sum = 0.0;

            for (var i = 0; i < _design.CompCount; i++)
            {
                for (var j = i; j < _design.CompCount; j++)
                {
                    sum += _design.Overlap[j, i];
                }
            }
            if (sum > 0.0)
                _design.NewObjValues[1] = (0.05 + sum) * _design.NewObjValues[0];
            else
                _design.NewObjValues[1] = 0.0;
            return sum;
        }
    }
}
