using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL;

namespace _3D_LayoutOpt
{
    static class anneal_alg
    {
        /* ---------------------------------------------------------------------------------- */
        /*                                                                                    */
        /*                                   ANNEAL_ALG.C                                     */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */

        /* ---------------------------------------------------------------------------------- */
        /* This is the shape annealing algorithm.                                             */
        /* ---------------------------------------------------------------------------------- */
        public static void anneal(Design design)
        {
            Schedule schedule;
            Hustin hustin;
            int iteration, which1, which2, modelflag, column, cost_update, accept_flag, not_frozen, q;
            int k, gen_limit, junk;
            int steps_at_t = 0, hold_temp, accept_count = 0, bad_accept_count = 0;
            char wait;
            double t, step_eval, current_eval, best_eval, Move_size, last_best;

            /*                                VARIABLE DESRCIPTIONS                               */
            /* steps_at_t = counter for number of iterations at a temp.                           */
            /* converged = 1 if converged and 0 of not (currently not used), which1 and which2    */
            /* are which components are being Moved, modelflag is used for writing models to a    */
            /* file, column = counter for which column in the old_obj_values matrix is the next   */
            /* one to update, cost_update = counter to determine if the coefficients should be    */
            /* updated.  mgl (maximum generation limit) is the max number of steps at a given     */
            /* temperature.                                                                       */

#if LOCATE
            Console.WriteLine("Entering anneal");
#endif

            /* Memory allocation. */
            hustin = new Hustin();
            schedule = new Schedule();


            /* Initialize variables and counters, coefficients and calculate initial objective    */
            /* function value.                                                                    */
            init_anneal(design, out best_eval, out current_eval);
            Schedules.calc_statistics(schedule);                       /* IN SCHEDULE.C */
            Schedules.init_schedule(schedule);                         /* IN SCHEDULE.C */

            Console.WriteLine("The initial evaluation value is {0}", current_eval);
            Console.WriteLine("sigma and c_avg are {0} and {1}", schedule.sigma, schedule.c_avg);
            Console.WriteLine("The initial temperature is {0}", schedule.t_initial);

            /* Initialize the hustin structure. */
            Chustin.init_hustin(hustin);           

            /* User provides input into when to switch between thermal anylses. */
            heatbasic.establish_thermal_changes(design);

            /* Initialization */
            t = schedule.t_initial;
            iteration = 0;
            column = 0;
            cost_update = 0;
            gen_limit = 4*schedule.mgl;
            not_frozen = 2;
            junk = 0;


            /* Start annealing with the generation limit set to mgl.  Anneal as long as we are    */
            /* warm enough.  If we exceed mgl without having accepted accept_target steps, we are */
            /* too cool and have to increase the generation limit to 4*mgl.  We then continue     */
            /* annealing until freezing occurs.                                                   */
            /* OUTER LOOP (temperature drops in this loop */
            while (not_frozen == 2)    
                {

                    /* More initialization */
                    steps_at_t = 0;
                    accept_count = 0;
                    bad_accept_count = 0;
                    schedule.in_count = 0;
                    schedule.out_count = 0;
                    hold_temp = 1;
                    schedule.max_delta_c = 0.0;
                    schedule.c_max = 0;
                    schedule.c_min = 10.0;
                    last_best = best_eval;

                    Console.WriteLine("Temperature is now {0}", t);
                    Console.WriteLine("best_eval is {0}", best_eval);
                    Console.WriteLine("current_eval is{0}", current_eval);
                    Console.WriteLine("The box dimensions are {0} {1} {2}", (design.box_max[0] - design.box_min[0]),
	                (design.box_max[1] - design.box_min[1]),(design.box_max[2] - design.box_min[2]));

                    /* INNER LOOP (steps taken at constant temp. in this loop */
                    while(hold_temp == 1)
	                {
	                    ++iteration;
	                    ++steps_at_t;

                        /* Take a step and evaluate it.  Update state by accepting or rejecting step. */
                        TakeStep(design, hustin, out which1, out which2);
                        step_eval = obj_function.evaluate(design, steps_at_t, gen_limit);
                        /*	  fptr2 = fopen("/data.out","a");
                              fConsole.WriteLine(fptr,"iteration %d: eval %lf ",iteration);
                              fclose(fptr2);
                        */
                        /*  Accept or reject step (accept_flag > 0 means accept) */
                        accept_flag = accept(t, step_eval, current_eval, design);
	                    if (accept_flag > 0)
	                    {
                            /* Write evaluation to file. */
	                        using (StreamWriter streamwriter = new StreamWriter("sample.data"))
	                        {
	                            streamwriter.WriteLine("{0}", step_eval);
	                        }


                            /* Do updates.  The hustin delta_c update is a bit fudged.  Essentially, what the following */
                            /* statements do is make a bad step count five times less than a good one.                  */
                            hustin.delta_c[hustin.which_Move] += Math.Abs(current_eval - step_eval);
                            /*	      if (current_eval > step_eval)
		                            hustin.delta_c[hustin.which_Move] += current_eval - step_eval;
	                                  else
		                            hustin.delta_c[hustin.which_Move] += (step_eval - current_eval)/5.0;*/
                            UpdateAccept(design, iteration, accept_flag, column, cost_update, step_eval, best_eval, current_eval);

                            heatbasic.back_up_tfield(design);

                            /* If we have taken more than MIN_SAMPLE steps, update parameters for the */
                            /* equilibrium condition. */
	                        if (steps_at_t > Constants.MIN_SAMPLE)
                                Schedules.equilibrium_update(step_eval, current_eval, schedule, hold_temp); /*schedule.c*/

	                        if (current_eval != step_eval)
		                    {
		                        ++accept_count;
		                        if (accept_flag != 2)
		                        ++bad_accept_count;	    
                                /* Update the current evaluation function value. */
		                        current_eval = step_eval;
		                    }

                        }
	                    else
	                    {
	                        if (accept_flag == 0)
		                    {

                                UpdateReject(design, iteration, which1, which2, current_eval);
                                heatbasic.revert_tfield(design);

                                /* Write evaluation to file. */
                                using (StreamWriter streamwriter = new StreamWriter("sample.data"))
                                {
                                    streamwriter.WriteLine("{0}", step_eval);
                                }
                            
		                    }
	                        else
		                    {
		                        --iteration;
		                        --steps_at_t;
		                        ++junk;
                                UpdateReject(design, 0, which1, which2, current_eval);
                                heatbasic.revert_tfield(design);
		                    }
	                    }

                        /* If we have taken MIN_SAMPLE steps, calculate new statistics. */
	                    if (steps_at_t == Constants.MIN_SAMPLE)
                            Schedules.calc_statistics(schedule);
	  
                        /* If the number of steps at this temperature exceeds the generation limit, go to  */
                        /* the next temperature.                                                           */
	                    if (steps_at_t > gen_limit)
	                        hold_temp = 0;
	                }                         /* END INNER LOOP */

                    Console.WriteLine("\nReducing temperature after {0} steps at this temperature.  ({1} iterations)", steps_at_t, iteration);
                    Console.WriteLine("{0} steps were accepted", accept_count);
                    Console.WriteLine("{0} of them were inferior steps", bad_accept_count);

                    readwrite.write_loop_data(t, steps_at_t, accept_count, bad_accept_count, gen_limit, 1);

                    /* Check frozen condition.  If frozen, change the flag.  If not, do updates. */
                    if (accept_count == 0)
	                    not_frozen = 0;
                    else if (Schedules.frozen_check(schedule))
	                    --not_frozen;
                    else
	                {
	                    not_frozen = 2;
	                    if (accept_count<schedule.problem_size)
                        gen_limit = 8 * schedule.mgl;

                        /* Update the temperature, the Move probabilities, and weights, and write the Move */
                        /* probabilities to a file.                                                        */
                        Schedules.update_temp(t, schedule.sigma);   /* IN SCHEDULE.C */

                        heatbasic.update_heat_param(design, schedule, t); /* IN HEAT.C */

                        Chustin.update_hustin(hustin);              /* IN HUSTIN.C */

                        readwrite.write_probs(hustin, t);             /* IN READWRITE.C */

                        Chustin.reset_hustin(hustin);
                        /*		  Console.WriteLine("\nHit return to continue\n");
		                          getchar(wait);
                        */
	                }
                }                             /* END OUTER LOOP */


                readwrite.write_loop_data(t, steps_at_t, accept_count, bad_accept_count, gen_limit, 0);

                /* Print out evaluation information about the last design. */
                design.choice = 3;
                step_eval = obj_function.evaluate(design, 0, 1000);
                Console.WriteLine("{0} iterations were junked", junk);
                Console.WriteLine("The best eval was {0}", best_eval);
                Console.WriteLine("The final eval was {0} ({1} percent density)", step_eval,(100/design.new_obj_values[0]));

                if (design.new_obj_values[3] > 0.001) Console.WriteLine("*****Still a thermal violation!***** {0}", design.new_obj_values[3]);

                using (StreamWriter streamwriter = new StreamWriter("results"))
                {
                    streamwriter.WriteLine("{0} iterations were taken (junked iterations not counted", iteration);
                    streamwriter.WriteLine("{0} iterations were junked", junk);
                    streamwriter.WriteLine("The best eval was {0}", best_eval);
                    streamwriter.WriteLine("The final eval was {0}", step_eval);
                    streamwriter.WriteLine("The container dimensions are {0} {1} {2}", design.container[0],
                    design.container[1], design.container[2]);
                    streamwriter.WriteLine("The box dimensions are {0} X {1} X {2}", (design.box_max[0] - design.box_min[0]),
                    (design.box_max[1] - design.box_min[1]), (design.box_max[2] - design.box_min[2]));
                }


#if LOCATE
                Console.WriteLine("Leaving anneal");
#endif
}
 
        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION TAKES A STEP AT RANDOM.                                              */
        /* TAKE A STEP.  A STEP IS ONE OF THREE "OPERATORS" - MOVE, ROTATE OR SWAP.           */
        /* MOVE AND ROTATE ARE PERTURBATIONS, SINCE THEY TYPICALLY LEAD TO SMALLER CHANGES IN */
        /* OBJECTIVE FUNCTION.  "MOVE" MOVES A COMPONENT ALONG A RANDOM DIRECTION.  "ROTATE"  */
        /* ROTATES A COMPONENT 90 DEGREES ALONG A RANDOM AXIS.  "SWAP" SWITCHES THE LOCATION  */
        /* OF TWO COMPONENTS.                                                                 */
        /* ---------------------------------------------------------------------------------- */
        public static void TakeStep(Design design, Hustin hustin, out int which_comp1, out int which_comp2)
        {
            /* PICK A COMPONENT TO MOVE */
            which_comp1 = Program.my_random(1, design.comp_count - 1);

            /* GENERATE A RANDOM NUMBER TO PICK A MOVE.  THEN, STEP THROUGH THE MOVE PROBABILITES TO FIND THE APPROPRIATE MOVE.  */
            Random random = new Random();
            double prob = random.NextDouble();
            which_comp2 = 0;

            int i = -1;
            bool TookAStep = false;

            while(++i<Constants.MOVE_NUM || !TookAStep)
            {
                prob -= hustin.prob[i];             //KEEP SUBTRACTING PROBABILITY TILL WE GET TO ZERO, THIS HELP TO TAKE BIGGER STEPS IN THE BEGINNING OF THE ALGORITHM
                if (prob< 0)
	            {
	                ++(hustin.attempts[i]);
	                hustin.which_Move = i;
	                if (i<Constants.TRANS_NUM)
                    {

                        Move(design, which_comp1, hustin.Move_size[i]);
                        TookAStep = true;
	                    if (hustin.Move_size[i] < design.gaussMove) 
		                    design.gauss = 1;
	                }
	                else if (i == Constants.TRANS_NUM)
                    {   /* i.e. if (i < (TRANS_NUM + 1)) */

                        int j = -1;
                        double prob2 = random.NextDouble();
                        while (++j < Constants.ROT_NUM)
                        {
                            prob2 -= hustin.prob[i];             //KEEP SUBTRACTING PROBABILITY TILL WE GET TO ZERO, THIS HELP TO TAKE BIGGER STEPS IN THE BEGINNING OF THE ALGORITHM
                            if (prob2 < 0)
                            {
                                Rotate(design, which_comp1, hustin.rot_size[j]);
                                TookAStep = true;
                                design.gauss = 1;
                            }
                        }
                    }
	            }
	            else                   /* If we reach this, we are at the last Move (Swap) */
	            {    
                    which_comp2 = Program.my_random(1, (design.comp_count - 1));                 
                    Swap(design,which_comp1, which_comp2);
                    TookAStep = true;
                }
	        }
        }

        

        /* ---------------------------------------------------------------------------------- */
        /* This function takes a Move step, moving a component along a random direction for   */
        /* a distance d, where d is a number between 0.1 and 2.5 (the component dimensions    */
        /* range from 5 to 10).                                                               */
        /* ---------------------------------------------------------------------------------- */
        static void Move(Design design, int which, double Move_size)
        {

            double max_dist, d;
            double[] dir_vect = new double[3];
            Component comp = null;

            Console.WriteLine("Entering Move");

            // FIND THE CORRECT COMPONENT AND BACK UP THE COMPONENT INFORMATION IN CASE WE REJECT THE STEP.

            comp = design.components[which];
            Backup(design, comp);

            // PICK A RANDOM DIRECTION AND DISTANCE, AND MOVE THE COMPONENT. 
            Console.WriteLine("Moving {0}", comp.name);
          
            for (int j = 0; j < 3; j++)
            {
                dir_vect[j] = Program.my_double_random(-1.0, 1.0);
            }
            Normalize(dir_vect);

            /*  d = Move_size*my_double_random(0.5,1.0); */

            var TranslateMatrix = new double[,]
                {
                    {1.0, 0.0, 0.0, Move_size*dir_vect[0]},
                    {0.0, 1.0, 0.0, Move_size*dir_vect[1]},
                    {0.0, 0.0, 1.0, Move_size*dir_vect[2]},
                    {0.0, 0.0, 0.0, 1.0}
                };

            comp.ts[0].Transform(TranslateMatrix);

            //UPDATING THE PIN COORDINATES
            foreach (SMD smd in comp.footprint.pads)
            {
                smd.coord = TranslateMatrix.multiply(new[] { smd.coord[0], smd.coord[0], smd.coord[0], 1 });
            }

            //UPDATING THE DESIGN VARIABLES
            for (int i = 0; i < 3; i++)
            {
                design.DesignVars[comp.index][i] = comp.ts[0].Center[i];
            }

            UpdateState(design, comp);
            Console.WriteLine("Leaving Move");
        }


        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION TAKES A DELTA_VECTOR, NORMALIZES IT, AND PUTS THE RESULT IN THE      */
        /* NORMALIZED VECTOR.                                                                 */
        /* ---------------------------------------------------------------------------------- */
        static void Normalize(double[] dir_vect)
        {
            double sum;

            sum = Math.Sqrt(dir_vect[0] * dir_vect[0] + dir_vect[1] * dir_vect[1] + dir_vect[2] * dir_vect[2]);
            dir_vect[0] /= sum;
            dir_vect[1] /= sum;
            dir_vect[2] /= sum;
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION TAKES A ROTATION STEP. 
        /* ---------------------------------------------------------------------------------- */
        static void Rotate(Design design, int which, double rotation_size)
        {
            double[] rot_vect = new double[3];
            Random random = new Random();
            double prob = random.NextDouble();
            Console.WriteLine("Entering Rotate");

            // FIND THE CORRECT COMPONENT AND BACK UP THE COMPONENT INFORMATION IN CASE WE REJECT THE STEP.                                                                          */
            Component comp = design.components[which];
            Backup(design, comp);

            Console.WriteLine("Rotating {0}", comp.name);
            for (int j = 0; j < 3; j++)
            {
                rot_vect[j] = Program.my_double_random(-1.0, 1.0);
            }
            Normalize(rot_vect);

            double[] rot_angles = new double[] { Math.PI * rotation_size * rot_vect[0], Math.PI * rotation_size * rot_vect[1], Math.PI * rotation_size * rot_vect[2] };

            var TransformMatrix = new double[,]
                {
                    {Math.Cos(rot_angles[0]) * Math.Cos(rot_angles[1]), Math.Cos(rot_angles[0]) * Math.Sin(rot_angles[1]) * Math.Sin(rot_angles[2]) - Math.Sin(rot_angles[0]) * Math.Cos(rot_angles[2]), Math.Cos(rot_angles[0]) * Math.Sin(rot_angles[1]) * Math.Cos(rot_angles[2]) + Math.Sin(rot_angles[0]) * Math.Sin(rot_angles[2]), 0},
                    {Math.Sin(rot_angles[0]) * Math.Cos(rot_angles[1]), Math.Sin(rot_angles[0]) * Math.Sin(rot_angles[1]) * Math.Sin(rot_angles[2]) + Math.Cos(rot_angles[0]) * Math.Cos(rot_angles[2]), Math.Sin(rot_angles[0]) * Math.Sin(rot_angles[1]) * Math.Cos(rot_angles[2]) - Math.Cos(rot_angles[0]) * Math.Sin(rot_angles[2]), 0},
                    {-1 * Math.Sin(rot_angles[1]), Math.Cos(rot_angles[1]) * Math.Sin(rot_angles[2]), Math.Cos(rot_angles[1]) * Math.Cos(rot_angles[2]), 0},
                    {0.0, 0.0, 0.0, 1.0}
                };

            comp.ts[0].Transform(TransformMatrix);

            //UPDATING THE PIN COORDINATES
            foreach (SMD smd in comp.footprint.pads)
            {
                smd.coord = TransformMatrix.multiply(new[] { smd.coord[0], smd.coord[0], smd.coord[0], 1 });
            }

            //UPDATING THE DESIGN VARIABLES
            for (int i = 0; i < 3; i++)
            {
                design.DesignVars[comp.index][i+3] += rot_angles[0];
            }

            /* UPDATE THE OVERLAPS AND THE BOUNDING BOX DIMENSIONS FOR THE CHANGED COMPONENT.     */
            UpdateState(design, comp);
            Console.WriteLine("Leaving Rotate");

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function takes a rotation step.  An orientation (different from the current   */
        /* one) is randomly selected and the component dimensions are updated accordingly.    */
        /* ---------------------------------------------------------------------------------- */
        static void Swap(Design design, int which1, int which2)
        {

            Component comp1 = null, comp2 = null;
            Console.WriteLine("Entering Swap");

            /* FIND THE CORRECT COMPONENTS.  WE DON'T NEED TO BACK UP COMPONENT IN CASE WE REJECT */
            /* THE STEP BECAUSE WE DON'T CHANGE DIMENSIONS OR ORIENTATION WHEN SWAPPING.  WE ONLY */
            /* SWITCH COORDINATES.                                                                */

            comp1 = design.components[which1];
            comp2 = design.components[which2];

            Backup(design, comp1, comp2);

            /* SWAP THE COMPONENTS BY SWITCHING THEIR COORDINATES.                                */

            Console.WriteLine("Swapping {0} and {1}", comp1.name, comp2.name);

            var TranslateMatrix1 = new double[,]
                {
                    {1.0, 0.0, 0.0, comp2.ts[0].Center[0] - comp1.ts[0].Center[0]},
                    {0.0, 1.0, 0.0, comp2.ts[0].Center[1] - comp1.ts[0].Center[1]},
                    {0.0, 0.0, 1.0, comp2.ts[0].Center[2] - comp1.ts[0].Center[2]},
                    {0.0, 0.0, 0.0, 1.0}
                };

            var TranslateMatrix2 = new double[,]
                {
                    {1.0, 0.0, 0.0, comp1.ts[0].Center[0] - comp2.ts[0].Center[0]},
                    {0.0, 1.0, 0.0, comp1.ts[0].Center[1] - comp2.ts[0].Center[1]},
                    {0.0, 0.0, 1.0, comp1.ts[0].Center[2] - comp2.ts[0].Center[2]},
                    {0.0, 0.0, 0.0, 1.0}
                };

            comp1.ts[0].Transform(TranslateMatrix1);
            comp2.ts[0].Transform(TranslateMatrix2);

            //UPDATING THE PIN COORDINATES
            foreach (SMD smd in comp1.footprint.pads)
            {
                smd.coord = TranslateMatrix1.multiply(new[] { smd.coord[0], smd.coord[0], smd.coord[0], 1 });
            }


            //UPDATING THE PIN COORDINATES
            foreach (SMD smd in comp2.footprint.pads)
            {
                smd.coord = TranslateMatrix2.multiply(new[] { smd.coord[0], smd.coord[0], smd.coord[0], 1 });
            }

            //UPDATING THE DESIGN VARIABLES
            for (int i = 0; i < 3; i++)
            {
                design.DesignVars[comp1.index][i] = comp1.ts[0].Center[i];
                design.DesignVars[comp2.index][i] = comp2.ts[0].Center[i];
            }

            /* UPDATE THE OVERLAPS AND THE BOUNDING BOX DIMENSIONS FOR THE CHANGED COMPONENTS.    */
            UpdateState(design, comp1);
            UpdateState(design, comp2);

            Console.WriteLine("Leaving Swap");
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION BACKS UP COMPONENT INFORMATION.  BACKUP ARE POINTERS TO COMPONENTS   */
        /* CONTAINING BACKUP INFORMATION ABOUT COMPONENTS IN CASE WE REJECT A STEP AND NEED   */
        /* TO REVERT TO A PREVIOUS DESIGN.  WHICHBACKUP ARE POINTERS TO THE COMPONENTS WHICH  */
        /* ARE BACKED UP, SO THAT WE KNOW WHERE TO WHERE THE OLD INFORMATION SHOULD BE COPIED */
        /* WHEN WE REVERT.  WHICH TELLS US WHICH COMPONENT IS BEING BACKED UP (0 OR 1).       */
        /* ---------------------------------------------------------------------------------- */
        static void Backup(Design design, Component comp1, Component comp2 = null)
        {
            Console.WriteLine("Entering back_up");
            if (comp2 == null)
            {
                comp1.BackupComponent();
                design.OldDesignVars = design.DesignVars;

                for (int j = 0; j < Constants.OBJ_NUM; j++)
                {
                    design.backup_obj_values[j] = design.new_obj_values[j];
                } 
            }
            else
            {
                comp1.BackupComponent();
                comp2.BackupComponent();
                design.OldDesignVars = design.DesignVars;
                for (int j = 0; j < Constants.OBJ_NUM; j++)
                {
                    design.backup_obj_values[j] = design.new_obj_values[j];
                }
            }
            Console.WriteLine("Leaving back_up");
        }

        

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION DOES ALL THE STUFF YOU WANT TO DO WHEN A STEP IS ACCEPTED.           */
        /* ---------------------------------------------------------------------------------- */
        public static void UpdateAccept(Design design, int iteration, int accept_flag, int column, int update, double step_eval, double best_eval, double current_eval)
        {
            /* If accept_flag = 2 then the step is an improvement. */
            if (accept_flag == 2)
            {
                Console.WriteLine("*** Improved step");
            }

#if DEBUG
            Console.WriteLine("Accepting step\n");
#endif
	      
/* Update best evaluation function. */
            if (step_eval < best_eval)
            {
                best_eval = step_eval;
                accept_flag = 3;
            }

#if OBJ_DATA
  /* Write objective function values to file */
            write_step(design, iteration, accept_flag);
#endif

#if TESTS
/* Do consistency checks on the design.  The 1 flag means it's an accepted step. */
            test_it(design, current_eval, 1, iteration);
#endif
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION DOES ALL THE STUFF YOU WANT TO DO WHEN A STEP IS REJECTED.           */
        /* ---------------------------------------------------------------------------------- */
        public static void UpdateReject(Design design, int iteration, int which1, int which2, double current_eval)
        {
#if DEBUG
            Console.WriteLine("Rejecting step\n");
#endif
	      
#if OBJ_DATA
/* Write objective function values to file */
            write_step(design, iteration, 0);
#endif
      
            Revert(design, which1, which2);
#if TESTS
/* Do consistency checks on the design.  The zero flag means it's a rejected step. */
            test_it(design, current_eval, 0, iteration);
#endif
        }


        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION REVERTS TO OLD INFORMATION CONTAINED WHEN A STEP IS REJECTED.        */
        /* WHICH2 TELLS US IF WE ARE REVERTING 1 COMPONENT (WHICH2 = 0) OR TWO (WHICH2 > 0).  */
        /* ---------------------------------------------------------------------------------- */
        static void Revert(Design design, Component comp1, Component comp2 = null)
        {

            Console.WriteLine("Entering revert");

            if (comp2 == null)
            {
                comp1.RevertComponent();
                design.DesignVars = design.OldDesignVars;
                for (int j = 0; j < Constants.OBJ_NUM; j++)
                {
                    design.new_obj_values[j] = design.backup_obj_values[j];
                }
            }
            else
            {
                comp1.RevertComponent();
                comp2.RevertComponent();
                design.DesignVars = design.OldDesignVars;

                // REVERT OBJECTIVE_FUNCTION VALUES TO THE VALUES BEFORE THE STEP.
                for (int j = 0; j < Constants.OBJ_NUM; j++) 
                {
                    design.new_obj_values[j] = design.backup_obj_values[j];
                }
            }
                UpdateState(design, comp1);
                UpdateState(design, comp2);
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION UPDATES THE OVERLAPS AND BOUNDING BOX DIMENSIONS AFTER TAKING A STEP */
        /* OR AFTER REVERTING TO A PREVIOUS DESIGN.                                           */
        /* ---------------------------------------------------------------------------------- */
        static void UpdateState(Design design, Component comp)
        {

            obj_function.UpdateOverlaps(design, comp);  
            obj_function.EvalOverlapContainer(design);
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION RETURNS A 1 OR 2 IF THE STEP SHOULD BE ACCEPTED.  THE VALUE OF 2     */
        /* INDICATES THAT THE EVALUATION FUNCTION HAS IMPROVED.  A VALUE OF -1 OR ZERO IS     */
        /* RETURNED IF THE STEP SHOULD BE REJECTED.  THE VALUE OF -1 INDICATES THAT THE STEP  */
        /* SHOULD NOT BE COUNTED AS IN ITERATION BECAUSE IT IS AN ILLEGAL DESIGN.             */
        /* THE PROBABILITY FUNCTION DECREASES WITH TEMPERATURE.  THE STEP_EVAL/THIS_EVAL TERM */
        /* HAS THE EFFECT THAT THE FARTHER FROM THE CURRENT EVALUATION VALUE A BAD STEP IS,   */
        /* THE LOWER THE PROBABILITY OF ACCEPTING THE BAD STEP.                               */
        /* NOTE THAT THIS FUNCTION ACCEPTS ACCORDING TO A SIMULATED ANNEALING, DOWNHILL OR    */
        /* RANDOM SEARCH ALGORITHM, DEPENDING ON THE #IF STATEMENTS.                          */
        /* ---------------------------------------------------------------------------------- */
        public static int accept(double temp, double step_eval, double this_eval, Design design)
        {
            int i;
            double rnd, prob;

            if (!ComponentsOutsideofContainer(design))
            {
                if (step_eval > this_eval)
	            {
                    Random random = new Random();
                    rnd = random.NextDouble();

	                prob = Math.Exp(-(step_eval-this_eval)/temp);
	                if (rnd<prob)

                        i = 1;
	                else
	                    i = 0;
	            }
                else
	                i = 2;
            }
            else
                i = -1;
            return i;
        }

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION REJECTS ANY STEPS THAT MAKE THE COMPONENTS GO OUTSIDE OF CONTAINER   */
        /* THIS IS DONE BY COMPARING CENTER OF COMPONETS WITH AABB OF THE CONTAINER           */  
        /* ---------------------------------------------------------------------------------- */
        public static bool ComponentsOutsideofContainer(Design design)
        {
            bool ComponentOutsideExists = false;
            foreach (var comp in design.components)
            {
                if (!((comp.ts[0].Center[0] > design.container.ts[0].XMin && comp.ts[0].Center[0] > design.container.ts[0].XMax) &&
                   (comp.ts[0].Center[1] > design.container.ts[0].XMin && comp.ts[0].Center[1] > design.container.ts[0].XMax) &&
                   (comp.ts[0].Center[2] > design.container.ts[0].XMin && comp.ts[0].Center[2] > design.container.ts[0].XMax)))
                {
                    ComponentOutsideExists = true;
                }
            }
            return ComponentOutsideExists;
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function performs various consistency tests on the design.                    */
        /* ---------------------------------------------------------------------------------- */
        static void test_it(Design design, double current_eval, int accept_flag, int iteration)
        {
            Component comp;
            char wait;

            for (int j = 0; j < design.comp_count; j++)
            {
                comp = design.components[j];
                for (int i = 0; i < 3; i++)                 // Test to make sure the bounding box dimensions are correct. 
                {
                    if (((comp.coord[i] - comp.dim[i] / 2) <= design.box_min[i]) &&
                        (design.min_comp[i] != comp))
                    {

                        Console.WriteLine("\n\nERROR in test_it - box_min.\n");

                        return;
                    }
                    if (((comp.coord[i] + comp.dim[i] / 2) >= design.box_max[i]) &&
                        (design.max_comp[i] != comp))
                    {

                        Console.WriteLine("\n\nERROR in test_it - box_max.\n");

                        return;
                    }
                }
            }
        }

            /* Test to see if value reverted to is same as value before taking step. */
            /*  if (!(accept_flag) && (current_eval != obj_function.evaluate(design, iteration)))
                {
                  Console.WriteLine("\n\nERROR in test_it - didn't revert correctly\7\n");
                  exit();
                }*/

        /* ---------------------------------------------------------------------------------- */
        /* This function initializes variables, counters, coefficients, and calculates the    */
        /* initial objective function value.                                                  */
        /* ---------------------------------------------------------------------------------- */
        static void init_anneal(Design design, out double best_eval, out double current_eval)
        {

/* Evaluate the initial design, initialize the obj. function matrix, calculate the    */
/* initial coefficients, and recalculate the initial evaluation, which by definition  */
/* of the coefficients has been Normalized to equal the number of components of the   */
/* objective function times the initial value of the first component.                 */

            current_eval = obj_function.evaluate(design, 0, 1000);       /* In obj_function.c */
            best_eval = current_eval;
            obj_balance.init_obj_values(design);                /* In obj_balance.c */

            Program.calc_c_grav(design);
            Console.WriteLine("The center of gravity is {0}{1}{2}", design.c_grav[0],
            design.c_grav[1], design.c_grav[2]);
        }

        /* ---------------------------------------------------------------------------------- */
        /* This was used to test code.                                                        */
        /* ---------------------------------------------------------------------------------- */
        /*
              if (iteration > 4000) ||
                  ((design.overlap[6][5] > 12.809)&&(design.overlap[6][5] < 12.81)))	   
                {
              Console.WriteLine("*******THIS IS AT THE TOP OF THE LOOP");
              Console.WriteLine("*******CURRENT EVAL IS %lf.  EVAL IS %lf",current_eval, obj_function.evaluate(design));
                  fptr = fopen("/comp.out","a");
                  fConsole.WriteLine(fptr, "Starting iteration #%d",iteration);
                  fclose(fptr);
                  print_overlaps(design);
                  fprint_data(design, 9);
                  fprint_data(design, 14);
                  getchar(wait);
                }
        */

        /* ---------------------------------------------------------------------------------- */
        /* This is the downhill search algorithm.                                             */
        /* ---------------------------------------------------------------------------------- */
        public static void downhill(Design design, double Move_size)
        {
            int iteration, which1 = 0, modelflag, column, cost_update, accept_count, count, max;
            double step_eval, current_eval, best_eval, dx, dy, dz, d;
            double old_eval = 1;
            char wait;

#if LOCATE
            Console.WriteLine("Entering downhill");
#endif

/*  Console.WriteLine("\nHit return to continue\n");
  getchar(wait);
*/

/* Initialization */
            max = Constants.I_LIMIT;  /* Cast a double as an int */
            iteration = 0;
            column = 0;
            count = 0;
            cost_update = 0;
            accept_count = 0;
            var improving = true;
  
            current_eval = obj_function.evaluate(design, max, 1000);
            best_eval = current_eval;
            step_eval = current_eval;
            Console.WriteLine("current_eval is {0}", current_eval);

            while (improving)
            {
                iteration = 0;
                ++count;

                while (++iteration <= 1000)
	            {
	                old_eval = current_eval;

/* Take a step and evaluate it.  Update state by accepting or rejecting step. */

                    downhill_Move(design, which1, Move_size);

/*      dx = design.first_comp.coord[0]-design.c_grav[0];
      dy = design.first_comp.coord[1]-design.c_grav[1];
      dz = design.first_comp.coord[2]-design.c_grav[2];
      d = dx*dx+dy*dy+dz*dz;
*/

                    step_eval = obj_function.evaluate(design, max, 1000);
	                if (step_eval <= current_eval)
	                {

                        UpdateAccept(design, iteration, 2, column, cost_update,
			                step_eval, best_eval, current_eval);

/* Update the current evaluation function value. */
                        current_eval = step_eval;
	                    ++accept_count;
	                    if (current_eval<best_eval)
                            best_eval = current_eval;
	                }
	                else
	                {
                        UpdateReject(design, iteration, which1, 0, current_eval);
	                }
	            }
                if (current_eval/old_eval > 0.99)
	                improving = false;
            }
            readwrite.write_loop_data(0.0, (1000*count), accept_count, 0, 0, 3);

            step_eval = obj_function.evaluate(design, max, 1000);
            Console.WriteLine("The best eval was {0}", best_eval);
            Console.WriteLine("The final eval was {0}", step_eval);

            using (StreamWriter writetext = new StreamWriter("results"))
            {
                writetext.WriteLine("After the downhill search:");
                writetext.WriteLine("The best eval was {0}", best_eval);
                writetext.WriteLine("The final eval was {0}", step_eval);
            }
#if LOCATE
            Console.WriteLine("Leaving downhill");
#endif
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function takes a Move step, moving a component along a random direction for   */
        /* a distance d, where d is a number between 0.1 and 2.5 (the component dimensions    */
        /* range from 5 to 10).                                                               */
        /* ---------------------------------------------------------------------------------- */
        static void downhill_Move(Design design, int which, double Move_size)
        {

            double max_dist, d;
            double[] dir_vect = new double[3];
            Component comp = null;

#if LOCATE
            Console.WriteLine("Entering downhill_Move");
#endif

            which = Program.my_random(1, Constants.COMP_NUM);

/* Find the correct component and back up the component information in case we reject */
/* the step.                                                                          */

            for (int i = 0; i < which; i++)
            {
                comp = design.components[i];
            }

            Backup(design, comp);

/* Pick a random direction and distance, and Move the component. Multiply that vector */
/* by a vector from the center of the component to the center of gravity, to imrove   */
/* chances of having an improvement step (i.e. never Move away from c_grav).          */
#if DEBUG
            Console.WriteLine("Moving {0}", comp.name);
#endif

            for (int j = 0; j < 3; j++)
            {
                dir_vect[j] = Program.my_double_random(0.0, 1.0);
                dir_vect[j] *= design.c_grav[j] - comp.coord[j];
            }
            
            Normalize(dir_vect);
            d = Move_size * Program.my_double_random(0.5,1.0);


            for (int j = 0; j < 3; j++)
            {
                comp.coord[j] += (d * dir_vect[j]);
            }
            

/* Update the overlaps and the bounding box dimensions for the changed component.     */
            UpdateState(design, comp);

#if LOCATE
            Console.WriteLine("Leaving downhill_Move");
#endif
        }


    }
}
