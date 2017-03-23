using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    static class obj_function
    {
        /* ---------------------------------------------------------------------------------- */
        /*                                                                                    */
        /*                                   OBJ_FUNCTION.C                                   */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */

        /* ---------------------------------------------------------------------------------- */
        /* This function returns an evaluation of the current design.                         */
        /* ---------------------------------------------------------------------------------- */
        public static double evaluate(Design design, int steps_at_t, int gen_limit)
        {
            int i, k;
            double eval;
            char wait;

#if LOCATE
            Console.WriteLine("Entering evaluate");
#endif

            /* Evaluate the four components of the objective function. */
            eval_overlap_comp(design);
            eval_overlap_container(design);
            heatbasic.heat_eval(design, steps_at_t, gen_limit);
            obj_balance.update_coef(design);                            //WHAT DOES IT DO?

        /* Add up the individual evaluations. */
            eval = 0.0;
            for (i = 0; i< Constants.OBJ_NUM; i++)
            {
                eval += design.weight[i]*design.new_obj_values[i];
            }
  
            if ((design.choice != 0) && (design.choice != 1))
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6}", design.choice,
                design.weight[0]*design.new_obj_values[0],
                design.weight[1]*design.new_obj_values[1],
                design.weight[2]*design.new_obj_values[2],
                design.weight[3]*design.new_obj_values[3],
                design.weight[4]*design.new_obj_values[4],
                eval);
/*  fptr = fopen("/eval.out","a");
  fConsole.WriteLine(fptr,"%.2f %.2f %.2f %.2f", (design.coef[0] * design.weight[0] * design.new_obj_values[0]), (design.coef[1] * design.weight[1] * design.new_obj_values[1]), (design.coef[2] * design.weight[2] * design.new_obj_values[2]), (design.coef[3] * design.weight[3] * design.new_obj_values[3]));
  fclose(fptr);*/

#if LOCATE
            Console.WriteLine("Leaving evaluate");
#endif
  
            return(eval);
        }

/* ---------------------------------------------------------------------------------- */
/* This function sets the value of the second part of the objective function, which   */
/* is the amount of overlap in a design.                                              */
/* Note that only the top half of the overlap matrix is used.                         */
/* Remember that an nXn matrix has elements numbered from [0][0] to [n-1][n-1]        */
/* ---------------------------------------------------------------------------------- */
        static void eval_overlap_comp(Design design)
        {
            double sum;
            sum = 0.0;

            for (int i = 0; i < design.comp_count; i++)
            {
                for (int j = i; j < design.comp_count; j++)
                {
                    sum += design.overlap[j, i];
                }
            }
            if (sum > 0.0)
                design.new_obj_values[1] = (0.05 + sum) * design.new_obj_values[0];
            else
                design.new_obj_values[1] = 0.0;
        }

/* ---------------------------------------------------------------------------------- */
/* This function sets the value of the third part of the objective function, which    */
/* is the amount of overlap with the container.                                       */
/* ---------------------------------------------------------------------------------- */
        static void eval_overlap_container(Design design)
        {
            double difference, box_penalty;
            box_penalty = 0.0;

            for (int i = 0; i < 3; i++)
            {
                difference = (design.box_max[i] - design.box_min[i]) - design.container[i];
                if (difference > 0.0)
                    box_penalty += difference;
            }
            design.new_obj_values[2] = box_penalty * box_penalty;               //THIS ALWAYS PRODUCES POSITIVE VALUES EVEN WHEN THE BOX IS SMALLER THAN THE CONTAINER
        }  

/* ---------------------------------------------------------------------------------- */
/* This function returns the max overlap in a design.                                 */
/* Note that only the top half of the overlap matrix is used.                         */
/* Remember that an nXn matrix has elements numbered from [0][0] to [n-1][n-1]        */
/* ---------------------------------------------------------------------------------- */
        public static double max_overlap(Design design)
        {
            double max;

            max = 0.0;

            for (int i = 0; i < design.comp_count; i++)
            {
                for (int j = i; j < design.comp_count; j++)
                {
                    if (design.overlap[j, i] > max)
                        max = design.overlap[j, i];
                }
            }
            return(max);

            //while (++i<Constants.COMP_NUM)
            //{
            //    j = i;
            //    while (++j<Constants.COMP_NUM)
	           // {
	           //     if (design.overlap[j,i] > max)
	           //         max = design.overlap[j,i];
	           // }
            //}
            //return(max);
        }

/* ---------------------------------------------------------------------------------- */
/* This function returns a multiplier to determine the penalty due to overlap.        */
/* The multiplier is a function of temperature, so that the penalty for overlap       */
/* increases as the annealing proceeds.                                               */
/* ---------------------------------------------------------------------------------- */
        public static double multiplier(double t)
        {
/* This multiplier value starts out at .2055 and ends up at around 100.              */
            return (20.55/t);
        }

/* ---------------------------------------------------------------------------------- */
/* This function initializes the overlap matrix for the components.                   */
/* The penalty is calculated as the average percentage of overlap                     */
/* Note that only the top half of the overlap matrix is used.                         */
/* Remember that an nXn matrix has elements numbered from [0][0] to [n-1][n-1]        */
/* ---------------------------------------------------------------------------------- */
        public static void init_overlaps(Design design)
        {
            double dx, dy, dz;
            Component comp1, comp2;

            for (int i = 0; i < design.comp_count; i++)
            {
                comp1 = design.components[i];
                for (int j = i; j < design.comp_count; j++)
                {
                    comp2 = design.components[j];
                    dx = (comp1.dim[0] + comp2.dim[0]) / 2.0 - Math.Abs(comp1.coord[0] - comp2.coord[0]);
                    dy = (comp1.dim[1] + comp2.dim[1]) / 2.0 - Math.Abs(comp1.coord[1] - comp2.coord[1]);
                    dz = (comp1.dim[2] + comp2.dim[2]) / 2.0 - Math.Abs(comp1.coord[2] - comp2.coord[2]);

                    /* Calculate the average percentage of overlap */

                    if ((dx > 0) && (dy > 0) && (dz > 0))
                        design.overlap[j, i ] = 2 * dx * dy * dz / (comp1.volume + comp2.volume);
                    else
                        design.overlap[j, i ] = 0.0;
                }
            }
        }

/* ---------------------------------------------------------------------------------- */
/* This function updates the overlap matrix for the components.                       */
/* The variable which determines which component to update.                           */
/* The overlap calculated is the Manhattan distance between near corners.             */
/* Note that only the top half of the overlap matrix is used.                         */
/* Remember that an nXn matrix has elements numbered from [0][0] to [n-1][n-1]        */
/* ---------------------------------------------------------------------------------- */
        public static void update_overlaps(Design design, Component temp1, int which)
        {
            int index;
            double dx, dy, dz, d1, d2;
            char wait;
            Component temp2;

#if LOCATE
            Console.WriteLine("Entering update_overlaps");
#endif

            /* Check overlap with components.  Since we are only using the top half of a symmetric */
            /* matrix, there is an if statement which switches the indices of the matrix elements. */

            index = which - 1;

            for (int i = 0; i < design.comp_count; i++)
            {
                temp2 = design.components[i];

                dx = (temp1.dim[0] + temp2.dim[0]) / 2.0 - Math.Abs(temp1.coord[0] - temp2.coord[0]);
                dy = (temp1.dim[1] + temp2.dim[1]) / 2.0 - Math.Abs(temp1.coord[1] - temp2.coord[1]);
                dz = (temp1.dim[2] + temp2.dim[2]) / 2.0 - Math.Abs(temp1.coord[2] - temp2.coord[2]);

                if ((temp1.shape_type[0] == 'C') && (temp2.shape_type[0] == 'C'))
                {
                    if (((temp1.orientation * temp2.orientation) == 3) && (dz > 0.0))
                    {
                        d1 = temp1.coord[0] - temp2.coord[0];
                        d2 = temp1.coord[1] - temp2.coord[1];
                        dx = (temp1.dim[0] + temp2.dim[0]) / 2.0 - Math.Sqrt(d1 * d1 + d2 * d2);
                        dy = dx;
                    }
                    else if (((temp1.orientation * temp2.orientation) == 8) && (dy > 0.0))
                    {
                        d1 = temp1.coord[0] - temp2.coord[0];
                        d2 = temp1.coord[2] - temp2.coord[2];
                        dx = (temp1.dim[0] + temp2.dim[0]) / 2.0 - Math.Sqrt(d1 * d1 + d2 * d2);
                        dz = dx;
                    }
                    else if (((temp1.orientation * temp2.orientation) == 30) && (dx > 0.0))
                    {
                        d1 = temp1.coord[1] - temp2.coord[1];
                        d2 = temp1.coord[2] - temp2.coord[2];
                        dy = (temp1.dim[1] + temp2.dim[1]) / 2.0 - Math.Sqrt(d1 * d1 + d2 * d2);
                        dz = dy;
                    }
                }

                /* Set overlap value in matrix. */
                if (i < index)
                {
                    if ((dx > 0.0) && (dy > 0.0) && (dz > 0.0))
                        design.overlap[index, i] = 2 * dx * dy * dz / (temp1.volume + temp2.volume);
                    else
                        design.overlap[index, i] = 0.0;
                }
                else if (i > index)
                {
                    if ((dx > 0.0) && (dy > 0.0) && (dz > 0.0))
                        design.overlap[i, index] = 2 * dx * dy * dz / (temp1.volume + temp2.volume);
                    else
                        design.overlap[i, index] = 0.0;
                }
            }

#if LOCATE
            Console.WriteLine("Leaving update_overlaps");
#endif


        }
    }
}
