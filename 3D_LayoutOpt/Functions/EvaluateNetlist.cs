using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
using TVGL;
namespace _3D_LayoutOpt.Functions
{
    internal class EvaluateNetlist : IObjectiveFunction
    {
        private Design _design;

        internal EvaluateNetlist(Design design)
        {
            this._design = design;
            //var shapes = _design.Components.Select(c => c.Ts).ToList();
            //Presenter.ShowAndHangTransparentsAndSolids(new[] { _design.Container.Ts }, shapes);

        }

        public double calculate(double[] x)
        {
            double sum = 0;
            _design.RatsNest.Clear();
            foreach (var net in _design.Netlist)
            {
                net.CreateEuclidianDistanceQueue(_design);
                net.Route(_design);
                //net.CalcNetDirectLineLength(_design);
                sum += net.NetLength;
            }
            _design.NewObjValues[0] = sum * 100;                 //MANUALLY APPLYING A WEIGHT OF 4
            Console.Write("netlist = {0};  ", _design.NewObjValues[0]);
            return _design.NewObjValues[0];

            var shapes = _design.Components.Select(c => c.Ts).ToList();
            Presenter.ShowAndHangTransparentsAndSolids(new[] { _design.Container.Ts }, shapes);
            Presenter.ShowVertexPathsWithSolid(_design.RatsNest, shapes);
        }


    }
}
