using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
namespace _3D_LayoutOpt.Functions
{
    internal class EvaluateNetlist : IObjectiveFunction
    {
        private Design design;

       internal EvaluateNetlist(Design design)
        {
            this.design = design;
        }

        public double calculate(double[] x)
        {
            double sum = 0;

            foreach (var net in design.Netlist)
            {
                net.CalcNetDirectLineLength(design);
                sum += net.NetLength;
            }

            design.new_obj_values[3] = sum;
            return sum;
        }
        
    }
}
