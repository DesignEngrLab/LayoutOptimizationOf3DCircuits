using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    static class Schedules
    {
        /* ---------------------------------------------------------------------------------- */
        /*                                                                                    */
        /*                                   SCHEDULE.C                                     */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the temperature.                                             */
        /* ---------------------------------------------------------------------------------- */
        public static void update_temp(double t, double sigma)
        {
            double reduce = Math.Exp(-(Math.Pow(Constants.LAMBDA, t)) / sigma);
            if (reduce < Constants.T_RATIO_MIN)
                reduce = Constants.T_RATIO_MIN;
            else if (reduce > Constants.T_RATIO_MAX)
                reduce = Constants.T_RATIO_MAX;
            Console.WriteLine("Reducing temperature by a factor of %lf\n", reduce);
            using (StreamWriter streamwriter = new StreamWriter("output / temperature.out"))
            {
                streamwriter.WriteLine("Reducing temperature by a factor of %lf\n", reduce);
            }
            t = reduce;
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function takes an integer and returns the standard devaiation of that many    */
        /* numbers read in from a file.                                                       */
        /* ---------------------------------------------------------------------------------- */


        public static void calc_statistics(Schedule schedule)
        {
            int i, j;
            double eval, sum, sum_sqrs;

            using (StreamReader streamreader = new StreamReader("sample.data"))
            {
                i = 0;
                sum = 0;
                sum_sqrs = 0;
                string line;
                while ((line = streamreader.ReadLine()) != null)
                {
                    ++i;
                    eval = Convert.ToDouble(line);
                    sum += eval;
                    sum_sqrs += eval * eval;
                }
            }


            if (i >= Constants.MIN_SAMPLE)
            {
                schedule.c_avg = sum / (1.0 * i);
                schedule.sigma = Math.Sqrt((sum_sqrs - i * schedule.c_avg * schedule.c_avg) / (1) * (i - 1));
                schedule.delta = Constants.RANGE * schedule.sigma;
                /*fptr = fopen("sample.data","w");
      fclose(fptr);
      Console.WriteLine("\nSigma, c_avg and delta for the previous sample are %lf, %lf, %lf\n",
	     schedule.sigma, schedule.c_avg, schedule.delta); */
            }
            else if (i > 1)
            {
                Console.WriteLine("There were not enough data points in the sample.data file\n");
            }
            /* If we get past the above "else" we don't do anything.  This function gets called   */
            /* when we junk the 101st step at a given temperature because the annealing function  */
            /* calls this function on the 100th step.                                             */
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function initializes variables, counters, coefficients, and calculates the    */
        /* initial objective function value.                                                  */
        /* ---------------------------------------------------------------------------------- */
        public static void init_schedule(Schedule schedule)

        {
            //double calc_initial_t ();

            /* Initialize variables and counters */
            schedule.t_initial = Constants.K * schedule.sigma;
            schedule.mgl = Constants.COMP_NUM * (Constants.COMP_NUM + 11) / 2;
            schedule.mgl = Constants.COMP_NUM * (Constants.COMP_NUM + 11);
            schedule.within_target = (int)Constants.ERF_RANGE * 3 * Constants.COMP_NUM;
            schedule.max_tolerance = (int)(1 - Constants.ERF_RANGE) * 3 * Constants.COMP_NUM;
            schedule.problem_size = Constants.COMP_NUM;

            /* Note on calculation of mgl: the maximum generation limit, which is defined as the  */
            /* number of states which can be reached from a given state.  The "move" operator     */
            /* counts as one even though an infinite number of states can actually be reached,    */
            /* since the move distance is discrete.                                               */
            /* For a move, any component may be selected and moved: 1 * COMP_NUM.  For a rotation */
            /* any component may be selected and have its rotation changed to any of 5 other      */
            /* rotations: 5 * COMP_NUM.  For a swap, any component may be selected, any remaining */
            /* component may be selected (COMP_NUM - 1) and divide by 2 since swapping A and B is */
            /* the same as swapping B and A: COMP_NUM * (COMP_NUM - 1)/2.                         */
            /* (1 * COMP_NUM) + (5 * COMP_NUM) + COMP_NUM * (COMP_NUM - 1)/2 is equal to          */
            /* COMP_NUM * (COMP_NUM + 11)/2 = 195 for 15 components.                              */
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function does a random walk to generate a sample of the design space.         */
        /* ---------------------------------------------------------------------------------- */
        public static void sample_space(Design design)
        {
            int i, which1, which2, column, update, accept_flag;
            double eval, dummy_eval;
            Hustin dummy_hustin = new Hustin();

            Chustin.init_hustin(dummy_hustin);
            dummy_eval = 0;
            eval = obj_function.evaluate(design, 0, 1000); 
            obj_balance.init_obj_values(design); 

            using (StreamWriter streamwriter = new StreamWriter("sample.data"))
            {
                column = 0;
                update = 0;
                which1 = 0;
                which2 = 0;
                i = 0;

                while (++i <= Constants.SAMPLE)
                {
                    anneal_alg.take_step(design, dummy_hustin, out which1, out which2); 
                    eval = obj_function.evaluate(design, i, Constants.SAMPLE); 

                    /* Sending (eval + 1.0) as the current_eval makes every step an "improvement".  Thus  */
                    /* every step will be accepted (unless the BOX_LIMIT box is violated).                */
                    accept_flag = anneal_alg.accept(1, eval, (eval + 1.0), design);
                    if (accept_flag > 0)
                    {
                        anneal_alg.update_accept(design, 0, accept_flag, column, update, /* IN ANNEAL_ALG.C */
                            eval, dummy_eval, (eval + 1.0));
                        streamwriter.WriteLine("%lf\n", eval);
                    }
                    else
                    {
                        --i;

                        anneal_alg.update_reject(design, 0, which1, which2, eval); /* IN ANNEAL_ALG.C */
                    }
                }
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function updates parameters for the equilibrium condition.                    */
        /* ---------------------------------------------------------------------------------- */
        public static void equilibrium_update(double step_eval, double current_eval, Schedule schedule, int hold_temp)
        {
            /* Update c_min, c_max, and max_delta_c for the frozen condition check. */
            if (step_eval > schedule.c_max)
                schedule.c_max = step_eval;
            if (step_eval < schedule.c_min)
                schedule.c_min = step_eval;
            schedule.delta_c = Math.Abs(current_eval - step_eval);
            if (schedule.delta_c > schedule.max_delta_c)
                schedule.max_delta_c = schedule.delta_c;

            /* Increment in_count or out_count. */
            if (Math.Abs(schedule.c_avg - current_eval) <= schedule.delta)
                ++(schedule.in_count);
            else
                ++(schedule.out_count);

            /* Check if we have exceeded the tolerance limit.  If not, check for equilibrium. */
            if (schedule.out_count > schedule.max_tolerance)
            {
                schedule.out_count = 0;
                schedule.in_count = 0;
            }
            else if (schedule.in_count > schedule.within_target)
                hold_temp = 0;
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function checks the frozen condition.                                         */
        /* ---------------------------------------------------------------------------------- */
        public static bool frozen_check(Schedule schedule)
        {
            if ((schedule.c_max - schedule.c_min) == schedule.max_delta_c)
                return true;
            else
                return false;
        }
    }
}