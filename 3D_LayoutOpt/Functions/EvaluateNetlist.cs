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
        private Design _design;

       internal EvaluateNetlist(Design design)
        {
            this._design = design;

        }

        public double calculate(double[] x)
        {
            double sum = 0;

            foreach (var net in _design.Netlist)
            {
				net.CreateEuclidianDistanceQueue(_design);
				net.Route(_design);
				//net.CalcNetDirectLineLength(_design);
                sum += net.NetLength;
            }

			_design.NewObjValues[0] = sum*100000;                 //MANUALLY APPLYING A WEIGHT OF 4
			return sum*10000 ;
        }

        //public double deriv_wrt_xi(double[] x, int i)
        //{
        //    switch (i)
        //    {
        //        case 0:
                                        
        //    }
        //}
    }
}
