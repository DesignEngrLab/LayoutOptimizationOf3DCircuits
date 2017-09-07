using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;

namespace _3D_LayoutOpt.Functions
{
    class CenterOfGravity : IInequality
    {
        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION CALCULATES THE CENTER OF GRAVITY.                                    */
        /* ---------------------------------------------------------------------------------- */

        private Design _design;

        internal CenterOfGravity(Design design)
        {
            this._design = design;
        }



        public double calculate(double[] x)
        {

            double vol;
            var sum = new double[3];
            var CGrav = new double[3];

            vol = 0.0;
            sum[0] = 0.0;
            sum[1] = 0.0;
            sum[2] = 0.0;

            foreach (var comp in _design.Components)
            {
                vol += comp.Ts.Volume;
                for (var i = 0; i < 3; i++)
                {
                    sum[i] += comp.Ts.Volume * comp.Ts.Center[i];
                }
            }
            for (var i = 0; i < 3; i++)
            {
                CGrav[i] = sum[i] / vol;
            }

            double CenterofGravityPenalty =
                    (CGrav[0] - _design.Container.Ts.Center[0]) * (CGrav[0] - _design.Container.Ts.Center[0])
                + (CGrav[1] - _design.Container.Ts.Center[1]) * (CGrav[1] - _design.Container.Ts.Center[1])
                + (CGrav[2] - _design.Container.Ts.Center[2]) * (CGrav[2] - _design.Container.Ts.Center[2]);


            _design.NewObjValues[5] = CenterofGravityPenalty * _design.objWeight[5];
            if (_design.NewObjValues[5] < _design.minObjValues[5])
                _design.minObjValues[5] = _design.NewObjValues[5];
            if (_design.NewObjValues[5] > _design.maxObjValues[5])
                _design.maxObjValues[5] = _design.NewObjValues[5];
            _design.rangeObjValues[5] = _design.maxObjValues[5] - _design.minObjValues[5];
            Console.WriteLine("BBox = {0} min = {1} max = {2} range = {3};  ", _design.NewObjValues[5], _design.minObjValues[5], _design.maxObjValues[5], _design.rangeObjValues[5]);
            return _design.NewObjValues[5];
        }
    }
}
