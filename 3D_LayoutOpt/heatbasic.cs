using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    static class heatbasic
    {

        public static void heat_eval(Design design, int steps_at_t, int gen_limit)
        {
            int correction;
            Component comp;
            correction = (steps_at_t - 1)%((int) (gen_limit/design.hcf_per_temp) + 1);

            switch (design.choice)
            {
	            case 0:
                    /*if (correction == 0)
		            correct_APP_by_LU(design);*/
                    HeatAPP.thermal_analysis_APP(design);
	                break;
	            case 1:
	                if (correction == 0)
                      heatSS.correct_SS_by_LU(design);
                    heatSS.thermal_analysis_SS(design);
	                break;
	            case 2:
	                if (correction == 0)
                        heatSS.correct_SS_by_LU(design);
                    heatSS.thermal_analysis_SS(design);
	                break;
	            case 3:
                    heatMM.thermal_analysis_MM(design);
	                break;
	            default:
                    Console.WriteLine("ERROR in Thermal Analysis Choice.");
                    break;
            }

            design.new_obj_values[3] = 0.0;
            design.new_obj_values[4] = 0.0;

            for (int i = 0; i < design.comp_count; i++)
            {
                comp = design.components[i];
                design.new_obj_values[3] += calc_temp_penalty(comp.temp, comp.tempcrit);
            }

        }

/* ---------------------------------------------------------------------------------- */
/* This function updates the heat parameters such as matrix tolerance and minimum     */
/* node spacing and switches between analysis methods.                                */
/* ---------------------------------------------------------------------------------- */
        public static void update_heat_param(Design design, Schedule schedule, double t)
        {
            if ((t/(schedule.t_initial)) < design.analysis_switch[0])
            {
	            design.choice = 0;
	            design.hcf_per_temp = 1;
            }
            if ((t/(schedule.t_initial)) < design.analysis_switch[1])
            {
	            design.choice = 1;
	            design.hcf_per_temp = 1;
            }
            if ((t/(schedule.t_initial)) < design.analysis_switch[2])
            {
	            design.choice = 2;
	            design.hcf_per_temp = 2;
            }
            if ((t/(schedule.t_initial)) < design.analysis_switch[3])
            {
	            design.choice = 3;
	            design.hcf_per_temp = 1;
	            design.gaussMove = 3.0;
            }
            if ((t/(schedule.t_initial)) < design.analysis_switch[4])
            {
	            design.choice = 3;
	            design.tolerance = 0.0001;
	            design.max_iter = 250;
	            design.gaussMove = 0.6;
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function returns the value of the penalty function for a temperature in       */
/* excess of the critical temperature.                                                */
/* ---------------------------------------------------------------------------------- */
        public static double calc_temp_penalty(double temp, double tempcrit)
        {
            double value = 0.0;

            if (temp > tempcrit)
            {
                value = (temp - tempcrit)*(temp - tempcrit)/(Constants.COMP_NUM);
            }
            return(value);
        }


/* ---------------------------------------------------------------------------------- */
/* This function reverts to  the previous node temperatures if the new Move was       */
/* rejected.                                                                          */
/* ---------------------------------------------------------------------------------- */
        public static void revert_tfield(Design design)
        {
            int k;

            for (k = 0; k<Constants.NODE_NUM; ++k)
                design.tfield[k].temp = design.tfield[k].old_temp;
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function backs up the current temperatures into old_temp if the step was      */
        /* Accepted.                                                                          */
        /* ---------------------------------------------------------------------------------- */
        public static void back_up_tfield(Design design)
        {
            int k;

            for (k = 0; k<Constants.NODE_NUM; ++k)
                design.tfield[k].old_temp = design.tfield[k].temp;
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function initializes the heat parameters such as matrix tolerance and minimum */
        /* node spacing.                                                                      */
        /* ---------------------------------------------------------------------------------- */
        public static void InitHeatParam(Design design)
        {
            design.tolerance = 0.001;
            design.min_node_space = 50.0;
            design.hcf = 0.1;
            design.gaussMove = 0.0;
            design.gauss = 0;
            design.hcf_per_temp = 4;
            design.max_iter = 100;
            design.choice = 0;
        }

/* ---------------------------------------------------------------------------------- */
/* This function is performed after determining the sample space but before
/* the beginning of the annealing run.  The sser provides input into when to
/* switch between thermal anylses. */
/* ---------------------------------------------------------------------------------- */
        public static void establish_thermal_changes(Design design)
        {
            int i;
            Console.WriteLine("\nPlease define thermal anaylses changes.\n");
            Console.WriteLine("After how many temperature drops should switch to more exact Lumped Method?");
            i = Convert.ToInt16(Console.ReadLine());
            design.analysis_switch[0] = Math.Pow(0.95, i);
            Console.WriteLine("After how many temperature drops should switch from Lumped Method to Sub-Space Method?");
            i = Convert.ToInt16(Console.ReadLine());
            design.analysis_switch[1] = Math.Pow(0.95, i);
            Console.WriteLine("After how many temperature drops should switch to more exact Sub-Space Method?");
            i = Convert.ToInt16(Console.ReadLine());
            design.analysis_switch[2] = Math.Pow(0.95, i);
            Console.WriteLine("After how many temperature drops should switch from Sub-Space Method to Matrix Method?");
            i = Convert.ToInt16(Console.ReadLine());
            design.analysis_switch[3] = Math.Pow(0.95, i);
            Console.WriteLine("After how many temperature drops should switch to more exact Matrix Method?");
            i = Convert.ToInt16(Console.ReadLine());
            design.analysis_switch[4] = Math.Pow(0.95, i);
        }

    }
}
