using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            double t, step_eval, current_eval, best_eval, move_size, last_best;

            /*                                VARIABLE DESRCIPTIONS                               */
            /* steps_at_t = counter for number of iterations at a temp.                           */
            /* converged = 1 if converged and 0 of not (currently not used), which1 and which2    */
            /* are which components are being moved, modelflag is used for writing models to a    */
            /* file, column = counter for which column in the old_obj_values matrix is the next   */
            /* one to update, cost_update = counter to determine if the coefficients should be    */
            /* updated.  mgl (maximum generation limit) is the max number of steps at a given     */
            /* temperature.                                                                       */

#if LOCATE
            Console.WriteLine("Entering anneal\n");
#endif

            /* Memory allocation. */
            hustin = new Hustin();
            schedule = new Schedule();


            /* Initialize variables and counters, coefficients and calculate initial objective    */
            /* function value.                                                                    */
            init_anneal(design, out best_eval, out current_eval);
            Schedules.calc_statistics(schedule);                       /* IN SCHEDULE.C */
            Schedules.init_schedule(schedule);                         /* IN SCHEDULE.C */

            Console.WriteLine("The initial evaluation value is %lf\n", current_eval);
            Console.WriteLine("sigma and c_avg are %lf and %lf\n", schedule.sigma, schedule.c_avg);
            Console.WriteLine("The initial temperature is %lf\n", schedule.t_initial);

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

                    Console.WriteLine("Temperature is now %lf\n", t);
                    Console.WriteLine("best_eval is %lf\n", best_eval);
                    Console.WriteLine("current_eval is %lf\n", current_eval);
                    Console.WriteLine("The box dimensions are %lf %lf %lf\n",(design.box_max[0] - design.box_min[0]),
	                (design.box_max[1] - design.box_min[1]),(design.box_max[2] - design.box_min[2]));

                    /* INNER LOOP (steps taken at constant temp. in this loop */
                    while(hold_temp == 1)
	                {
	                    ++iteration;
	                    ++steps_at_t;

                        /* Take a step and evaluate it.  Update state by accepting or rejecting step. */
                        take_step(design, hustin, out which1, out which2);
                        step_eval = obj_function.evaluate(design, steps_at_t, gen_limit);
                        /*	  fptr2 = fopen("output/data.out","a");
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
	                            streamwriter.WriteLine("%lf\n", step_eval);
	                        }


                            /* Do updates.  The hustin delta_c update is a bit fudged.  Essentially, what the following */
                            /* statements do is make a bad step count five times less than a good one.                  */
                            hustin.delta_c[hustin.which_move] += Math.Abs(current_eval - step_eval);
                            /*	      if (current_eval > step_eval)
		                            hustin.delta_c[hustin.which_move] += current_eval - step_eval;
	                                  else
		                            hustin.delta_c[hustin.which_move] += (step_eval - current_eval)/5.0;*/
                            update_accept(design, iteration, accept_flag, column, cost_update, step_eval, best_eval, current_eval);

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

                                update_reject(design, iteration, which1, which2, current_eval);
                                heatbasic.revert_tfield(design);

                                /* Write evaluation to file. */
                                using (StreamWriter streamwriter = new StreamWriter("sample.data"))
                                {
                                    streamwriter.WriteLine("%lf\n", step_eval);
                                }
                            
		                    }
	                        else
		                    {
		                        --iteration;
		                        --steps_at_t;
		                        ++junk;
                                update_reject(design, 0, which1, which2, current_eval);
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

                    Console.WriteLine("\nReducing temperature after %d steps at this temperature.  (%d iterations)\n", steps_at_t, iteration);
                    Console.WriteLine("%d steps were accepted\n", accept_count);
                    Console.WriteLine("%d of them were inferior steps\n", bad_accept_count);

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

                        /* Update the temperature, the move probabilities, and weights, and write the move */
                        /* probabilities to a file.                                                        */
                        Schedules.update_temp(t, schedule.sigma);   /* IN SCHEDULE.C */

                        heatbasic.update_heat_param(design, schedule, t); /* IN HEAT.C */

                        Chustin.update_hustin(hustin);              /* IN HUSTIN.C */

                        readwrite.write_probs(hustin, t);             /* IN READWRITE.C */

                        Chustin.reset_hustin(hustin);
                        /*		  Console.WriteLine("\nHit return to continue\n\n");
		                          getchar(wait);
                        */
	                }
                }                             /* END OUTER LOOP */


                readwrite.write_loop_data(t, steps_at_t, accept_count, bad_accept_count, gen_limit, 0);

                /* Print out evaluation information about the last design. */
                design.choice = 3;
                step_eval = obj_function.evaluate(design, 0, 1000);
                Console.WriteLine("%d iterations were junked\n", junk);
                Console.WriteLine("The best eval was %lf\n", best_eval);
                Console.WriteLine("The final eval was %lf (%lf percent density)\n", step_eval,(100/design.new_obj_values[0]));

                if (design.new_obj_values[3] > 0.001) Console.WriteLine("*****Still a thermal violation!***** %.3f\n", design.new_obj_values[3]);

                using (StreamWriter streamwriter = new StreamWriter("results"))
                {
                    streamwriter.WriteLine("%d iterations were taken (junked iterations not counted\n", iteration);
                    streamwriter.WriteLine("%d iterations were junked\n", junk);
                    streamwriter.WriteLine("The best eval was %lf\n", best_eval);
                    streamwriter.WriteLine("The final eval was %lf\n", step_eval);
                    streamwriter.WriteLine("The container dimensions are %lf %lf %lf\n", design.container[0],
                    design.container[1], design.container[2]);
                    streamwriter.WriteLine("The box dimensions are %lf X %lf X %lf\n", (design.box_max[0] - design.box_min[0]),
                    (design.box_max[1] - design.box_min[1]), (design.box_max[2] - design.box_min[2]));
                }


#if LOCATE
                Console.WriteLine("Leaving anneal\n");
#endif
}
 
        /* ---------------------------------------------------------------------------------- */
        /* This function takes a step at random.                                              */
        /* Take a step.  A step is one of three "operators" - move, rotate or swap.           */
        /* Move and rotate are perturbations, since they typically lead to smaller changes in */
        /* objective function.  "Move" moves a component along a random direction.  "Rotate"  */
        /* rotates a component 90 degrees along a random axis.  "Swap" switches the location  */
        /* of two components.                                                                 */
        /* ---------------------------------------------------------------------------------- */
        public static void take_step(Design design, Hustin hustin, out int which1, out int which2)
                {
                    int i;
                    double prob;

                    /* Pick a component to move */
                    which1 = Program.my_random(1, Constants.COMP_NUM);

                    /* Generate a random number to pick a move.  Then, step through the move probabilites */
                    /* to find the appropriate move. 
                                                     */
                    Random random = new Random();
                    prob = random.NextDouble();

                    i = -1;
                    which2 = 0;

                    i = -1;
                    while(++i<Constants.MOVE_NUM)
                    {
                        prob -= hustin.prob[i];
                        if (prob< 0)
	                    {
	                        ++(hustin.attempts[i]);
	                        hustin.which_move = i;
	                        if (i<Constants.TRANS_NUM)
                            {

                                move(design, which1, hustin.move_size[i]);
	                            if (hustin.move_size[i] < design.gaussmove) 
		                            design.gauss = 1;
	                        }
	                        else if (i == Constants.TRANS_NUM)
                            {   /* i.e. if (i < (TRANS_NUM + 1)) */

                                rotate(design, which1);
                                design.gauss = 1;
	                        }
	                        else                   /* If we reach this, we are at the last move (swap) */
	                        {
/* Pick at random a second component (different from the first) to swap.              */
                                which2 = Program.my_random(1, (Constants.COMP_NUM - 1));
	                            if (which2 >= which1)
		                            ++(which2);

                                swap(design,which1, which2);
	      /*design.gauss = 1;*/
	                        }
/* Set i to MOVE_NUM to break out of the loop since we took a step */
	                        i = Constants.MOVE_NUM;
	                    }
                    }
                }

        /* ---------------------------------------------------------------------------------- */
        /* This function takes a move step, moving a component along a random direction for   */
        /* a distance d, where d is a number between 0.1 and 2.5 (the component dimensions    */
        /* range from 5 to 10).                                                               */
        /* ---------------------------------------------------------------------------------- */
        static void move(Design design, int which, double move_size)
        {

            double max_dist, d;
            double[] dir_vect = new double[3];
            Component comp = null;

#if LOCATE
            Console.WriteLine("Entering move\n");
#endif

            /* Find the correct component and back up the component information in case we reject */
            /* the step.                                                                          */
            
            for (int i = 0; i < which; i++)
            {
                comp = design.components[i];
            }

            back_up(design, comp);

            /* Pick a random direction and distance, and move the component.                      */
#if OUTPUT
            Console.WriteLine("Moving %s\n", comp.comp_name);
#endif
          
            for (int j = 0; j < 3; j++)
            {
                dir_vect[j] = Program.my_double_random(-1.0, 1.0);
            }
            if (Constants.DIMENSION == 2)
                dir_vect[2] = 0.0;
            normalize(dir_vect);

            /*  d = move_size*my_double_random(0.5,1.0); */

            for (int i = 0; i < 3; i++)
            {
                comp.coord[i] += (move_size * dir_vect[i]);
            }

            /*      comp.coord[i] += (d * dir_vect[i]);*/

            /* Update the overlaps and the bounding box dimensions for the changed component.     */
            update_state(design, comp, which);

#if LOCATE
            Console.WriteLine("Leaving move\n");
#endif
}

/* ---------------------------------------------------------------------------------- */
/* This function takes a delta_vector, normalizes it, and puts the result in the      */
/* normalized vector.                                                                 */
/* ---------------------------------------------------------------------------------- */
        static void normalize(double[] dir_vect)
        {
            double sum;

            sum = Math.Sqrt(dir_vect[0] * dir_vect[0] + dir_vect[1] * dir_vect[1] + dir_vect[2] * dir_vect[2]);
            dir_vect[0] /= sum;
            dir_vect[1] /= sum;
            dir_vect[2] /= sum;
        }

/* ---------------------------------------------------------------------------------- */
/* This function takes a rotation step.  An orientation (different from the current   */
/* one) is randomly selected and the component dimensions are updated accordingly.    */
/* ---------------------------------------------------------------------------------- */
        static void rotate(Design design, int which)
        {
            int new_orientation;
            Component comp = null;

#if LOCATE
            Console.WriteLine("Entering rotate\n");
#endif

/* Find the correct component and back up the component information in case we reject */
/* the step.                                                                          */
            for (int i = 0; i < which; i++)
            {
                comp = design.components[i];
            }
            back_up(design, comp);

/* Pick a random orientation different from the current one and rotate the component. */
#if OUTPUT
            Console.WriteLine("Rotating %s\n", comp.comp_name);
#endif

            if (Constants.DIMENSION == 3)
            {
                new_orientation = Program.my_random(1,5);
                if (new_orientation >= comp.orientation)
                    ++new_orientation;
                comp.orientation = new_orientation;
            }
            if (Constants.DIMENSION == 2)
            {
                if (comp.orientation == 1)
                    comp.orientation = 3;
                else
                    comp.orientation = 1;
            }
            Program.update_dim(comp);    /* IN TEST3.C */

/* Update the overlaps and the bounding box dimensions for the changed component.     */
            update_state(design, comp, which);

#if LOCATE
            Console.WriteLine("Leaving rotate\n");
#endif
}

/* ---------------------------------------------------------------------------------- */
/* This function takes a rotation step.  An orientation (different from the current   */
/* one) is randomly selected and the component dimensions are updated accordingly.    */
/* ---------------------------------------------------------------------------------- */
            static void swap(Design design, int which1, int which2)
            {

                double temp_coord;
                Component comp1 = null, comp2 = null;

#if LOCATE
                Console.WriteLine("Entering swap\n");
#endif

/* Find the correct components.  We don't need to back up component in case we reject */
/* the step because we don't change dimensions or orientation when swapping.  We only */
/* switch coordinates.                                                                */

                for (int i = 0; i < which1; i++)
                {
                    comp1 = design.components[i];
                }

                for (int i = 0; i < which2; i++)
                {
                    comp2 = design.components[i];
                }


            /* Swap the components by switching their coordinates.                                */
#if OUTPUT
                Console.WriteLine("Swapping %s and %s\n", comp1.comp_name, comp2.comp_name);
    #endif
                for (int j = 0; j < 3; j++)
                {
                    temp_coord = comp1.coord[j];
                    comp1.coord[j] = comp2.coord[j];
                    comp2.coord[j] = temp_coord;
                }
            

    /* Update the overlaps and the bounding box dimensions for the changed components.    */
                update_state(design, comp1, which1);
                update_state(design, comp2, which2);

    #if LOCATE
                Console.WriteLine("Leaving swap\n");
    #endif
            }

        /* ---------------------------------------------------------------------------------- */
        /* This function backs up component information.  Backup are pointers to components   */
        /* containing backup information about components in case we reject a step and need   */
        /* to revert to a previous design.  Whichbackup are pointers to the components which  */
        /* are backed up, so that we know where to where the old information should be copied */
        /* when we revert.  Which tells us which component is being backed up (0 or 1).       */
        /* ---------------------------------------------------------------------------------- */
        static void back_up(Design design, Component comp)
        {
            int i;
            Component comp1;

#if LOCATE
            Console.WriteLine("Entering back_up\n");
#endif

/* Back up coordinates and dimensions. */
/*  Console.WriteLine("The component being backed up is %s\n",comp.comp_name);
*/
            design.old_orientation = comp.orientation;

            for (int j = 0; j < 3; j++)
            {
                design.old_coord[j] = comp.coord[j];
                design.old_dim[j] = comp.dim[j];
            }

            /* Back up current objective_function values. */

            for (int j = 0; j < Constants.OBJ_NUM; j++)
            {
                design.backup_obj_values[j] = design.new_obj_values[j];
            }


#if LOCATE
            Console.WriteLine("Leaving back_up\n");
#endif
}

/* ---------------------------------------------------------------------------------- */
/* This function does all the stuff you want to do when a step is accepted.           */
/* ---------------------------------------------------------------------------------- */
   public static void update_accept(Design design, int iteration, int accept_flag, int column, int update, double step_eval, double best_eval, double current_eval)
        {

  
/* If accept_flag = 2 then the step is an improvement. */
            if (accept_flag == 2)
            {
#if OUTPUT
                Console.WriteLine("*** Improved step\n");
#endif
            }

#if OUTPUT
            Console.WriteLine("Accepting step\n\n");
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
/* This function does all the stuff you want to do when a step is rejected.           */
/* ---------------------------------------------------------------------------------- */
   public static     void update_reject(Design design, int iteration, int which1, int which2, double current_eval)
        {
#if OUTPUT
            Console.WriteLine("Rejecting step\n\n");
#endif
	      
#if OBJ_DATA
/* Write objective function values to file */
            write_step(design, iteration, 0);
#endif
      
            revert(design, which1, which2);
#if TESTS
/* Do consistency checks on the design.  The zero flag means it's a rejected step. */
            test_it(design, current_eval, 0, iteration);
#endif
}
        /* ---------------------------------------------------------------------------------- */
        /* This function reverts to old information contained when a step is rejected.        */
        /* Which2 tells us if we are reverting 1 component (which2 = 0) or two (which2 > 0).  */
        /* ---------------------------------------------------------------------------------- */
        static void revert(Design design, int which1, int which2)
        {

            double temp_coord;
            Component comp1 = null, comp2 = null;

#if LOCATE
            Console.WriteLine("Entering revert\n");
#endif

            /* Find the first component. */

            for (int j = 0; j < which1; j++)
            {
                comp1 = design.components[j];
            }


            if (which2 > 0)
            {
                comp1.orientation = design.old_orientation;

                for (int j = 0; j < 3; j++)
                {
                    comp1.coord[j] = design.old_coord[j];
                    comp1.dim[j] = design.old_dim[j];
                }


/* Update the overlaps and bounding box dimensions back to how they were since we reverted */
            update_state(design, comp1, which1);    
            }

            else
            {
/* Find the second component. */

                for (int i = 0; i < which2; i++)
                {
                    comp2 = design.components[i];
                }

                for (int j = 0; j < 3; j++)
                {
                    temp_coord = comp1.coord[j];
                    comp1.coord[j] = comp2.coord[j];
                    comp2.coord[j] = temp_coord;
                }


/* Update the overlaps and bounding box dimensions back to how they were since we reverted */
                update_state(design, comp1, which1);
                update_state(design, comp2, which2);
            }

/* Revert objective_function values to the values before the step. */
            
            for (int j = 0; j < Constants.OBJ_NUM; j++)
            {
                design.new_obj_values[j] = design.backup_obj_values[j];
            }
            

#if LOCATE
            Console.WriteLine("Leaving revert\n");
#endif
}

        /* ---------------------------------------------------------------------------------- */
        /* This function updates the overlaps and bounding box dimensions after taking a step */
        /* or after reverting to a previous design.                                           */
        /* ---------------------------------------------------------------------------------- */
        static void update_state(Design design, Component comp, int which)
        {

            obj_function.update_overlaps(design, comp, which);  /* THIS FUNCTION IS IN OBJ.FUNCTION.C */
            Program.update_bounds(design, comp);
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function returns a 1 or 2 if the step should be accepted.  The value of 2     */
        /* indicates that the evaluation function has improved.  A value of -1 or zero is     */
        /* returned if the step should be rejected.  The value of -1 indicates that the step  */
        /* should not be counted as in iteration because it is an illegal design.             */
        /* The probability function decreases with temperature.  The step_eval/this_eval term */
        /* has the effect that the farther from the current evaluation value a bad step is,   */
        /* the lower the probability of accepting the bad step.                               */
        /* Note that this function accepts according to a simulated annealing, downhill or    */
        /* random search algorithm, depending on the #if statements.                       */
        /* ---------------------------------------------------------------------------------- */
        public static int accept(double temp, double step_eval, double this_eval, Design design)
        {
            int i;
            double rnd, prob;

            if (not_too_big(design))
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
        /* This function rejects any steps that make the bounding box too big.                */
        /* ---------------------------------------------------------------------------------- */
        public static bool not_too_big(Design design)
        {

            bool small;
            double difference;

            small = true;
            difference = 0.0;

            for (int i = 0; i < 3; i++)
            {
                difference = (design.box_max[i] - design.box_min[i]) - Constants.BOX_LIMIT;
                if (difference > 0.0)
                    small = false;
            }
            return(small);
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

                        Console.WriteLine("\n\nERROR in test_it - box_min.\n\n");

                        return;
                    }
                    if (((comp.coord[i] + comp.dim[i] / 2) >= design.box_max[i]) &&
                        (design.max_comp[i] != comp))
                    {

                        Console.WriteLine("\n\nERROR in test_it - box_max.\n\n");

                        return;
                    }
                }
            }
        }

            /* Test to see if value reverted to is same as value before taking step. */
            /*  if (!(accept_flag) && (current_eval != obj_function.evaluate(design, iteration)))
                {
                  Console.WriteLine("\n\nERROR in test_it - didn't revert correctly\7\n\n");
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
/* of the coefficients has been normalized to equal the number of components of the   */
/* objective function times the initial value of the first component.                 */

            current_eval = obj_function.evaluate(design, 0, 1000);       /* In obj_function.c */
            best_eval = current_eval;
            obj_balance.init_obj_values(design);                /* In obj_balance.c */

            Program.calc_c_grav(design);
            Console.WriteLine("The center of gravity is %lf %lf %lf\n", design.c_grav[0],
            design.c_grav[1], design.c_grav[2]);
        }

        /* ---------------------------------------------------------------------------------- */
        /* This was used to test code.                                                        */
        /* ---------------------------------------------------------------------------------- */
        /*
              if (iteration > 4000) ||
                  ((design.overlap[6][5] > 12.809)&&(design.overlap[6][5] < 12.81)))	   
                {
              Console.WriteLine("*******THIS IS AT THE TOP OF THE LOOP\n");
              Console.WriteLine("*******CURRENT EVAL IS %lf.  EVAL IS %lf\n",current_eval, obj_function.evaluate(design));
                  fptr = fopen("output/comp.out","a");
                  fConsole.WriteLine(fptr, "Starting iteration #%d\n",iteration);
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
        public static void downhill(Design design, double move_size)
        {
            int iteration, which1 = 0, modelflag, column, cost_update, accept_count, count, max;
            double step_eval, current_eval, best_eval, dx, dy, dz, d;
            double old_eval = 1;
            char wait;

#if LOCATE
            Console.WriteLine("Entering downhill\n");
#endif

/*  Console.WriteLine("\nHit return to continue\n\n");
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
            Console.WriteLine("current_eval is %lf\n", current_eval);

            while (improving)
            {
                iteration = 0;
                ++count;

                while (++iteration <= 1000)
	            {
	                old_eval = current_eval;

/* Take a step and evaluate it.  Update state by accepting or rejecting step. */

                    downhill_move(design, which1, move_size);

/*      dx = design.first_comp.coord[0]-design.c_grav[0];
      dy = design.first_comp.coord[1]-design.c_grav[1];
      dz = design.first_comp.coord[2]-design.c_grav[2];
      d = dx*dx+dy*dy+dz*dz;
*/

                    step_eval = obj_function.evaluate(design, max, 1000);
	                if (step_eval <= current_eval)
	                {

                        update_accept(design, iteration, 2, column, cost_update,
			                step_eval, best_eval, current_eval);

/* Update the current evaluation function value. */
                        current_eval = step_eval;
	                    ++accept_count;
	                    if (current_eval<best_eval)
                            best_eval = current_eval;
	                }
	                else
	                {
                        update_reject(design, iteration, which1, 0, current_eval);
	                }
	            }
                if (current_eval/old_eval > 0.99)
	                improving = false;
            }
            readwrite.write_loop_data(0.0, (1000*count), accept_count, 0, 0, 3);

            step_eval = obj_function.evaluate(design, max, 1000);
            Console.WriteLine("The best eval was %lf\n", best_eval);
            Console.WriteLine("The final eval was %lf\n", step_eval);

            using (StreamWriter writetext = new StreamWriter("results"))
            {
                writetext.WriteLine("After the downhill search:\n");
                writetext.WriteLine("The best eval was %lf\n", best_eval);
                writetext.WriteLine("The final eval was %lf\n", step_eval);
            }
#if LOCATE
            Console.WriteLine("Leaving downhill\n");
#endif
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function takes a move step, moving a component along a random direction for   */
        /* a distance d, where d is a number between 0.1 and 2.5 (the component dimensions    */
        /* range from 5 to 10).                                                               */
        /* ---------------------------------------------------------------------------------- */
        static void downhill_move(Design design, int which, double move_size)
        {

            double max_dist, d;
            double[] dir_vect = new double[3];
            Component comp = null;

#if LOCATE
            Console.WriteLine("Entering downhill_move\n");
#endif

            which = Program.my_random(1, Constants.COMP_NUM);

/* Find the correct component and back up the component information in case we reject */
/* the step.                                                                          */

            for (int i = 0; i < which; i++)
            {
                comp = design.components[i];
            }

            back_up(design, comp);

/* Pick a random direction and distance, and move the component. Multiply that vector */
/* by a vector from the center of the component to the center of gravity, to imrove   */
/* chances of having an improvement step (i.e. never move away from c_grav).          */
#if OUTPUT
            Console.WriteLine("Moving %s\n", comp.comp_name);
#endif

            for (int j = 0; j < 3; j++)
            {
                dir_vect[j] = Program.my_double_random(0.0, 1.0);
                dir_vect[j] *= design.c_grav[j] - comp.coord[j];
            }
            
            normalize(dir_vect);
            d = move_size * Program.my_double_random(0.5,1.0);


            for (int j = 0; j < 3; j++)
            {
                comp.coord[j] += (d * dir_vect[j]);
            }
            

/* Update the overlaps and the bounding box dimensions for the changed component.     */
            update_state(design, comp, which);

#if LOCATE
            Console.WriteLine("Leaving downhill_move\n");
#endif
        }


    }
}
