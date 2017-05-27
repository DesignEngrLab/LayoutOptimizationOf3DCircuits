using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
namespace _3D_LayoutOpt
{
    class HeatBasic : IInequality
    {
        private Design _design;
        const int Dummy = 100; //todo: remove this!

        internal HeatBasic(Design design)
        {
            this._design = design;
            design.Tolerance = 0.001;
            design.MinNodeSpace = 5.0;
            design.Hcf = 0.1;
            design.GaussMove = 0.0;
            design.Gauss = 0;
            design.HcfPerTemp = 4;
            design.MaxIter = 100;
            design.Choice = 3;
        }
        public double calculate(double[] x)
        {
            var stepsAtT = Dummy;
            var genLimit = Dummy;

            int correction;
            Component comp;
            correction = (stepsAtT - 1) % ((int)(genLimit / _design.HcfPerTemp) + 1);

            switch (_design.Choice)
            {
                case 0:
                    /*if (correction == 0)
		            correct_APP_by_LU(design);*/
                    HeatApp.ThermalAnalysisAPP(_design);
                    break;
                case 1:
                    if (correction == 0)
                        HeatSs.CorrectSSbyLU(_design);
                    HeatSs.ThermalAnalysisSS(_design);
                    break;
                case 2:
                    if (correction == 0)
                        HeatSs.CorrectSSbyLU(_design);
                    HeatSs.ThermalAnalysisSS(_design);
                    break;
                case 3:
                    HeatMm.ThermalAnalysisMM(_design);
                    break;
                default:
                    Console.WriteLine("ERROR in Thermal Analysis Choice.");
                    break;
            }

            _design.NewObjValues[3] = 0.0;

            for (var i = 0; i < _design.CompCount; i++)
            {
                comp = _design.Components[i];
                _design.NewObjValues[3] += CalcTempPenalty(_design, comp);
            }
            Console.Write("heat = {0};  ", _design.NewObjValues[3]);

            return _design.NewObjValues[3];
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the heat parameters such as matrix tolerance and minimum     */
        /* node spacing and switches between analysis methods.                                */
        /* ---------------------------------------------------------------------------------- */
        //public static void update_heat_param(Design design, Schedule schedule, double t)
        //{
        //    if ((t / (schedule.t_initial)) < design.analysis_switch[0])
        //    {
        //        design.choice = 0;
        //        design.hcf_per_temp = 1;
        //    }
        //    if ((t / (schedule.t_initial)) < design.analysis_switch[1])
        //    {
        //        design.choice = 1;
        //        design.hcf_per_temp = 1;
        //    }
        //    if ((t / (schedule.t_initial)) < design.analysis_switch[2])
        //    {
        //        design.choice = 2;
        //        design.hcf_per_temp = 2;
        //    }
        //    if ((t / (schedule.t_initial)) < design.analysis_switch[3])
        //    {
        //        design.choice = 3;
        //        design.hcf_per_temp = 1;
        //        design.gaussMove = 3.0;
        //    }
        //    if ((t / (schedule.t_initial)) < design.analysis_switch[4])
        //    {
        //        design.choice = 3;
        //        design.tolerance = 0.0001;
        //        design.max_iter = 250;
        //        design.gaussMove = 0.6;
        //    }
        //}

        /* ---------------------------------------------------------------------------------- */
        /* This function returns the value of the penalty function for a temperature in       */
        /* excess of the critical temperature.                                                */
        /* ---------------------------------------------------------------------------------- */
        public static double CalcTempPenalty(Design design, Component comp)
        {
            var value = 0.0;

            if (comp.Temp > comp.Tempcrit)
            {
                value = (comp.Temp - comp.Tempcrit) * (comp.Temp - comp.Tempcrit) / (design.CompCount);
            }
            return (value);
        }

    }
}
