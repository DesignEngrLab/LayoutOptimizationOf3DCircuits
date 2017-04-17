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
            double[] sum = new double[3];

            mass = 0.0;
            sum[0] = 0.0;
            sum[1] = 0.0;
            sum[2] = 0.0;

            foreach (var comp in design.components)
            {
                mass += comp.ts[0].Mass;
                for (int i = 0; i < 3; i++)
                {
                    sum[i] += comp.ts[0].Mass * comp.ts[0].Center[i];
                }
            }
            for (int i = 0; i < 3; i++)
            {
                design.c_grav[i] = sum[i] / mass;
            }
        }

        public double calculate(double[] x)
        {
            throw new NotImplementedException();
        }
    }
}
