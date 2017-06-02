using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;

namespace _3D_LayoutOpt.Functions
{
    class EvaluateBoundingBox : IInequality
    {
        private Design _design;

        internal EvaluateBoundingBox(Design design)
        {
            this._design = design;
        }



        public double calculate(double[] x)
        {

            double volPenalty = 0;

            var boundingBoxVolume = (_design.BoxMax[0] - _design.BoxMin[0]) * (_design.BoxMax[1] - _design.BoxMin[1]) * (_design.BoxMax[2] - _design.BoxMin[2]);
            volPenalty = boundingBoxVolume - _design.Container.Ts.Volume;
            if (volPenalty < 0)
                volPenalty = 0;

            var dimensionPenaltyX = ((_design.BoxMax[0] - _design.BoxMin[0]) - (_design.Container.Ts.XMax - _design.Container.Ts.XMin))/ (_design.BoxMax[0] - _design.BoxMin[0]);
            if (dimensionPenaltyX < 0)
                dimensionPenaltyX = 0;
            var dimensionPenaltyY = ((_design.BoxMax[1] - _design.BoxMin[1]) - (_design.Container.Ts.YMax - _design.Container.Ts.YMin))/ (_design.BoxMax[1] - _design.BoxMin[1]);
            if (dimensionPenaltyY < 0)
                dimensionPenaltyY = 0;
            var dimensionPenaltyZ = ((_design.BoxMax[2] - _design.BoxMin[2]) - (_design.Container.Ts.ZMax - _design.Container.Ts.ZMin))/ (_design.BoxMax[2] - _design.BoxMin[2]);
            if (dimensionPenaltyZ < 0)
                dimensionPenaltyZ = 0;

            var sumOfBoundingBoxDimensions = _design.BoxMax[0] - _design.BoxMin[0] + _design.BoxMax[1] - _design.BoxMin[1] + _design.BoxMax[2] - _design.BoxMin[2];

            var dimensionPenalty = dimensionPenaltyX 
                                + dimensionPenaltyY
                                + dimensionPenaltyZ;
            //var boundingBoxPenalty = volPenalty/ _design.Container.Ts.Volume + dimensionPenalty;
            //var boundingBoxPenalty = volPenalty / boundingBoxVolume;
            //var boundingBoxPenalty = dimensionPenalty / sumOfBoundingBoxDimensions;
            var boundingBoxPenalty = dimensionPenalty;
            _design.NewObjValues[2] = 8 * boundingBoxPenalty;  //MANUALLY APPLYING A WEIGHT OF 2
            Console.Write("Bounding Box Penalty = {0};  ", 8*boundingBoxPenalty);
            return 8*(boundingBoxPenalty);
        }
    }
}
