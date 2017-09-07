using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
using TVGL;
using System.IO;

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

            

            _design.NewObjValues[0] = sum * _design.objWeight[0];
            _design.aveObjValues[0] += (sum * _design.objWeight[0]) / 300;
            _design.ObjValuesCounter[0]++;

            if (_design.NewObjValues[0] < _design.minObjValues[0])
                _design.minObjValues[0] = _design.NewObjValues[0];
            if (_design.NewObjValues[0] > _design.maxObjValues[0])
                _design.maxObjValues[0] = _design.NewObjValues[0];
            _design.rangeObjValues[0] = _design.maxObjValues[0] - _design.minObjValues[0];
            Console.WriteLine("netlist = {0} min = {1} max = {2} range = {3};  ", _design.NewObjValues[0], _design.minObjValues[0], _design.maxObjValues[0], _design.rangeObjValues[0]);

            //Write results to a file
            using (FileStream fileStream = new FileStream("Designs/simple/results.txt", FileMode.Append))
            {
                using (var writetext = new StreamWriter(fileStream))
                {
                    writetext.WriteLine("{0}", _design.NewObjValues[0] / _design.objWeight[0]);

                }
            }
            if (_design.ObjValuesCounter[0] % 300 == 0)
                _design.aveObjValues[0] = 0;

            return _design.NewObjValues[0];

        }


    }
}
