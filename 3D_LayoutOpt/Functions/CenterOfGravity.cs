using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
namespace _3D_LayoutOpt.Functions
{
    class CenterOfGravity :IEquality
    {
        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION CALCULATES THE CENTER OF GRAVITY.                                    */
        /* ---------------------------------------------------------------------------------- */
        public static void CalcCenterofGravity(Design design)
        {
            double mass;
            var sum = new double[3];

            mass = 0.0;
            sum[0] = 0.0;
            sum[1] = 0.0;
            sum[2] = 0.0;

            foreach (var comp in design.Components)
            {
                mass += comp.Ts.Mass;
                for (var i = 0; i < 3; i++)
                {
                    sum[i] += comp.Ts.Mass * comp.Ts.Center[i];
                }
            }
            for (var i = 0; i < 3; i++)
            {
                design.CGrav[i] = sum[i] / mass;
            }
        }

        public double calculate(double[] x)
        {
            throw new NotImplementedException();
        }
    }
}
