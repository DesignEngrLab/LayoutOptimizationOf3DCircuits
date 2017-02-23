using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    static class Chustin
    {
        /* ---------------------------------------------------------------------------------- */
        /*                                                                                    */
        /*                                       HUSTIN.C                                     */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */

        /* ---------------------------------------------------------------------------------- */
        /* This function sets the translate move distances and the initial probabilities.     */
        /* ---------------------------------------------------------------------------------- */
        public static void init_hustin(Hustin hustin)
        {
            int i;
            double init_prob, distance, delta_dist, dist;

            dist = Constants.MIN_MOVE_DIST;
            delta_dist = (Constants.MAX_MOVE_DIST - Constants.MIN_MOVE_DIST)/ Constants.TRANS_NUM;
            init_prob = 1.0/ Constants.MOVE_NUM;
            hustin.usable_prob = 1 - Constants.MIN_PROB * Constants.MOVE_NUM;
            i = -1;
  
            while (++i< Constants.TRANS_NUM)
            {
                hustin.move_size[i] = dist;
                dist += delta_dist;
                hustin.prob[i] = init_prob;
                hustin.attempts[i] = 0;
                hustin.delta_c[i] = 0.0;
            }
            while (i< Constants.MOVE_NUM)
            {
                hustin.prob[i] = init_prob;
                hustin.attempts[i] = 0;
                hustin.delta_c[i] = 0.0;
                ++i;
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the move set probabilities.                                  */
        /* ---------------------------------------------------------------------------------- */
        public static void update_hustin(Hustin hustin)
        {
            int i;
            double quality_sum;

            quality_sum = 0.0;
            i = -1;
            while (++i<Constants.MOVE_NUM)
            {
                if (hustin.attempts[i] > 0)
	                hustin.quality[i] = hustin.delta_c[i]/(1* hustin.attempts[i]);
                else
	                hustin.quality[i] = 0;
                quality_sum += hustin.quality[i];
            }

            i = -1;

            while (++i<Constants.MOVE_NUM)
            {
                hustin.prob[i] = Constants.MIN_PROB + hustin.usable_prob * hustin.quality[i] / quality_sum;
            }
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function reinitializes the hustin structure.  This is called after updating   */
        /* the probabilities to reset values for the next temperature.                        */
        /* ---------------------------------------------------------------------------------- */
        public static void reset_hustin(Hustin hustin)
        {
            int i;

            i = -1;

            while (++i<Constants.MOVE_NUM)
            {
                hustin.attempts[i] = 0;
                hustin.delta_c[i] = 0.0;
            }
        }

    }
}
