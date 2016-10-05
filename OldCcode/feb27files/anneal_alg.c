/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                   ANNEAL_ALG.C                                     */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* This is the shape annealing algorithm.                                             */
/* ---------------------------------------------------------------------------------- */
void anneal(design)
struct Design *design;
{
  struct Schedule *schedule;
  struct Hustin *hustin;
  void init_anneal(), init_schedule(), init_coef(), take_step(), update_temp(), fprint_data();
  void update_weights(), update_accept(), update_reject(), print_comp_data();
  void write_loop_data(), calc_statistics(), equilibrium_update(), update_coef();
  void init_hustin(), update_hustin(), reset_hustin(), write_probs();
  void update_heat_param(), back_up_tfield(), revert_tfield(), calc_thermal_matrix_LU();
  int iteration, which1, which2, modelflag, column, cost_update, accept_flag, not_frozen, q;
  int k, steps_at_t, hold_temp, accept_count, bad_accept_count, gen_limit, junk, frozen_check();
  char wait;
  double t, step_eval, current_eval, best_eval, evaluate(), max_overlap(), move_size, last_best;
  FILE *fptr, *fptr2;
/*                                VARIABLE DESRCIPTIONS                               */
/* steps_at_t = counter for number of iterations at a temp.                           */
/* converged = 1 if converged and 0 of not (currently not used), which1 and which2    */
/* are which components are being moved, modelflag is used for writing models to a    */
/* file, column = counter for which column in the old_obj_values matrix is the next   */
/* one to update, cost_update = counter to determine if the coefficients should be    */
/* updated.  mgl (maximum generation limit) is the max number of steps at a given     */
/* temperature.                                                                       */

#ifdef LOCATE
  printf("Entering anneal\n");
#endif

/* Memory allocation. */
  schedule = (struct Schedule *) malloc(sizeof(struct Schedule));
  hustin = (struct Hustin *) malloc(sizeof(struct Hustin));

/* Initialize variables and counters, coefficients and calculate initial objective    */
/* function value.                                                                    */
  init_anneal(design, &best_eval, &current_eval);
  calc_statistics(schedule);                       /* IN SCHEDULE.C */
  init_schedule(schedule);                         /* IN SCHEDULE.C */

  printf("The initial evaluation value is %lf\n",current_eval);
  printf("sigma and c_avg are %lf and %lf\n",schedule->sigma, schedule->c_avg);
  printf("The initial temperature is %lf\n",schedule->t_initial);

/* Initialize the hustin structure. */
  init_hustin(hustin);            /* IN HUSTIN.C */

/*  printf("\nHit return to continue\n\n");
  getchar(wait);
*/

/* Initialization */
  t = schedule->t_initial;
  iteration = 0;
  column = 0;
  cost_update = 0;
  gen_limit = 4*schedule->mgl;
  not_frozen = 2;
  junk = 0;

/* Start annealing with the generation limit set to mgl.  Anneal as long as we are    */
/* warm enough.  If we exceed mgl without having accepted accept_target steps, we are */
/* too cool and have to increase the generation limit to 4*mgl.  We then continue     */
/* annealing until freezing occurs.                                                   */
/* OUTER LOOP (temperature drops in this loop */
  while (not_frozen)    
    {

/* More initialization */
      steps_at_t = 0;
      accept_count = 0;
      bad_accept_count = 0;
      schedule->in_count = 0;
      schedule->out_count = 0;
      hold_temp = 1;
      schedule->max_delta_c = 0.0;
      schedule->c_max = 0.;
      schedule->c_min = 10.0;
      last_best = best_eval;
      init_coef(design);

      printf("Temperature is now %lf\n",t);      
      printf("best_eval is %lf\n",best_eval);
      printf("current_eval is %lf\n",current_eval);
      printf("The box dimensions are %lf %lf %lf\n",(design->box_max[0] - design->box_min[0]),
	     (design->box_max[1] - design->box_min[1]),(design->box_max[2] - design->box_min[2]));

      /*calc_thermal_matrix_LU(design);*/
/* INNER LOOP (steps taken at constant temp. in this loop */
      while(hold_temp)
	{
	  ++iteration;
	  ++steps_at_t;
	  
/* Take a step and evaluate it.  Update state by accepting or rejecting step. */
	  take_step(design, hustin, &which1, &which2);
	  step_eval = evaluate(design);
/*	  fptr2 = fopen("output/data.out","a");
	  fprintf(fptr,"iteration %d: eval %lf ",iteration);
	  fclose(fptr2);
*/
/*  Accept or reject step (accept_flag > 0 means accept) */
	  accept_flag = accept(t, step_eval, current_eval, design);
	  if (accept_flag > 0)
	    {
/* Write evaluation to file. */
	      fptr = fopen("sample.data","a");
	      fprintf(fptr,"%lf\n",step_eval);
	      fclose(fptr);

/* Do updates.  The hustin delta_c update is a bit fudged.  Essentially, what the following */
/* statements do is make a bad step count five times less than a good one.                  */
	      hustin->delta_c[hustin->which_move] += fabs(current_eval - step_eval);
/*	      if (current_eval > step_eval)
		hustin->delta_c[hustin->which_move] += current_eval - step_eval;
	      else
		hustin->delta_c[hustin->which_move] += (step_eval - current_eval)/5.0;*/
	      update_accept(design, iteration, accept_flag, &column, &cost_update,
			    &step_eval, &best_eval, current_eval);
	      back_up_tfield(design);

/* If we have taken more than MIN_SAMPLE steps, update parameters for the */
/* equilibrium condition. */
	      if (steps_at_t > MIN_SAMPLE)
		equilibrium_update(step_eval, current_eval, schedule, &hold_temp); /*schedule.c*/

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
		  revert_tfield(design);
/* Write evaluation to file. */
		  fptr = fopen("sample.data","a");
		  fprintf(fptr,"%lf\n",step_eval);
		  fclose(fptr);
		}
	      else
		{
		  --iteration;
		  --steps_at_t;
		  ++junk;
		  update_reject(design, 0, which1, which2, current_eval);
		  revert_tfield(design);
		}
	    }

/* If we have taken MIN_SAMPLE steps, calculate new statistics. */
	  if (steps_at_t == MIN_SAMPLE)
	    calc_statistics(schedule);
	  
/* If the number of steps at this temperature exceeds the generation limit, go to  */
/* the next temperature.                                                           */
	  if (steps_at_t > gen_limit)
	    hold_temp = 0;
	}                         /* END INNER LOOP */

      printf("Reducing temperature after %d steps at this temperature.  (%d iterations)\n",
	     steps_at_t, iteration);
      printf("%d steps were accepted\n",accept_count);
      printf("%d of them were inferior steps\n",bad_accept_count);

      write_loop_data(t, steps_at_t, accept_count, bad_accept_count, gen_limit, 1);

/* Check frozen condition.  If frozen, change the flag.  If not, do updates. */
      if (accept_count == 0)
	not_frozen = 0;
      else if (frozen_check(schedule))
	--not_frozen;
      else
	{
	  not_frozen = 2;
	  if (accept_count < schedule->problem_size)
	    gen_limit = 8 * schedule->mgl;

/* Update the temperature, the move probabilities, and weights, and write the move */
/* probabilities to a file.                                                        */
       	  update_temp(&t, schedule->sigma);   /* IN SCHEDULE.C */
	  update_heat_param(design, schedule, t);          /* IN HEAT.C */
	  update_hustin(hustin);              /* IN HUSTIN.C */
	  write_probs(hustin, t);             /* IN READWRITE.C */
	  reset_hustin(hustin);
/*		  printf("\nHit return to continue\n\n");
		  getchar(wait);
*/
	}
    }                             /* END OUTER LOOP */
  
  write_loop_data(t, steps_at_t, accept_count, bad_accept_count, gen_limit, 0);

/* Print out evaluation information about the last design. */
  step_eval = evaluate(design);
  printf("%d iterations were junked\n",junk);
  printf("The best eval was %lf\n",best_eval);
  printf("The final eval was %lf (%lf percent density)\n",step_eval,(100./design->new_obj_values[0]));

  if (design->new_obj_values[3] > 0.001) printf("*****Still a thermal violation!***** %.3f\n", 
	design->new_obj_values[3]);

  fptr = fopen("results","a");
  fprintf(fptr,"%d iterations were taken (junked iterations not counted\n",iteration);
  fprintf(fptr,"%d iterations were junked\n",junk);
  fprintf(fptr,"The best eval was %lf\n",best_eval);
  fprintf(fptr,"The final eval was %lf\n",step_eval);
  fprintf(fptr,"The container dimensions are %lf %lf %lf\n", design->container[0],
	  design->container[1], design->container[2]);
  fprintf(fptr,"The box dimensions are %lf X %lf X %lf\n",(design->box_max[0]-design->box_min[0]),
	  (design->box_max[1] - design->box_min[1]), (design->box_max[2] - design->box_min[2]));
  fclose(fptr);

#ifdef LOCATE
  printf("Leaving anneal\n");
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
void take_step(design, hustin, which1, which2)
struct Design *design;
struct Hustin *hustin;
int *which1, *which2;
{
  void move(), rotate(), swap();
  int i;
  double prob;

/* Pick a component to move */
  *which1 = my_random(1,COMP_NUM);

/* Generate a random number to pick a move.  Then, step through the move probabilites */
/* to find the appropriate move.                                                      */
  prob = random()/2147483649.;
  i = -1;

  *which2 = 0;

  i = -1;
  while(++i < MOVE_NUM)
    {
      prob -= hustin->prob[i];
      if (prob < 0.)
	{
	  ++(hustin->attempts[i]);
	  hustin->which_move = i;
	  if (i < TRANS_NUM)
	    move(design, *which1, hustin->move_size[i]);
	  else if (i == TRANS_NUM)    /* i.e. if (i < (TRANS_NUM + 1)) */
	    rotate(design, *which1);
	  else                   /* If we reach this, we are at the last move (swap) */
	    {
/* Pick at random a second component (different from the first) to swap.              */
	      *which2 = my_random(1,(COMP_NUM - 1));
	      if (*which2 >= *which1)
		++(*which2);
	      swap(design, *which1, *which2);
	    }
/* Set i to MOVE_NUM to break out of the loop since we took a step */
	  i = MOVE_NUM;
	}
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function takes a move step, moving a component along a random direction for   */
/* a distance d, where d is a number between 0.1 and 2.5 (the component dimensions    */
/* range from 5 to 10).                                                               */
/* ---------------------------------------------------------------------------------- */
void move(design, which, move_size)
struct Design *design;
int which;
double move_size;
{
  void normalize(), back_up(), print_data(), update_state();
  int i;
  double max_dist, d, dir_vect[3], my_double_random();
  struct Component *comp;

#ifdef LOCATE
  printf("Entering move\n");
#endif

/* Find the correct component and back up the component information in case we reject */
/* the step.                                                                          */
  i = 1;
  comp = design->first_comp;
  while (++i <= which)
    comp = comp->next_comp;
  back_up(design, comp);

/* Pick a random direction and distance, and move the component.                      */
#ifdef OUTPUT
  printf("Moving %s\n",comp->comp_name);
#endif
  i = -1;
  while (++i <= 2)
      dir_vect[i] = my_double_random(-1.0,1.0);
  if (DIMENSION == 2)
    dir_vect[2] = 0.0;
  normalize(dir_vect);

/*  d = move_size*my_double_random(0.5,1.0);*/
  
  i = -1;
  while (++i <= 2)
      comp->coord[i] += (move_size * dir_vect[i]);
/*      comp->coord[i] += (d * dir_vect[i]);*/

/* Update the overlaps and the bounding box dimensions for the changed component.     */
  update_state(design, comp, which); 

#ifdef LOCATE
  printf("Leaving move\n");
#endif
}

/* ---------------------------------------------------------------------------------- */
/* This function takes a delta_vector, normalizes it, and puts the result in the      */
/* normalized vector.                                                                 */
/* ---------------------------------------------------------------------------------- */
void normalize(dir_vect)
double *dir_vect;
{
  double sum;

  sum = sqrt(dir_vect[0]*dir_vect[0]+dir_vect[1]*dir_vect[1]+dir_vect[2]*dir_vect[2]);
  dir_vect[0] /= sum;
  dir_vect[1] /= sum;
  dir_vect[2] /= sum;
}

/* ---------------------------------------------------------------------------------- */
/* This function takes a rotation step.  An orientation (different from the current   */
/* one) is randomly selected and the component dimensions are updated accordingly.    */
/* ---------------------------------------------------------------------------------- */
void rotate(design, which)
struct Design *design;
int which;
{
  void back_up(), print_data(), update_dim(), update_state();
  int i, new_orientation;
  struct Component *comp;

#ifdef LOCATE
  printf("Entering rotate\n");
#endif

/* Find the correct component and back up the component information in case we reject */
/* the step.                                                                          */
  i = 1;
  comp = design->first_comp;
  while (++i <= which)
    comp = comp->next_comp;
  back_up(design, comp);

/* Pick a random orientation different from the current one and rotate the component. */
#ifdef OUTPUT
  printf("Rotating %s\n",comp->comp_name);
#endif

  if (DIMENSION == 3) {
    new_orientation = my_random(1,5);
    if (new_orientation >= comp->orientation)
      ++new_orientation;
    comp->orientation = new_orientation;
  }
  if (DIMENSION == 2) {
    if (comp->orientation == 1)
      comp->orientation = 3;
    else comp->orientation = 1;
  }
  update_dim(comp);    /* IN TEST3.C */

/* Update the overlaps and the bounding box dimensions for the changed component.     */
  update_state(design, comp, which); 

#ifdef LOCATE
  printf("Leaving rotate\n");
#endif
}

/* ---------------------------------------------------------------------------------- */
/* This function takes a rotation step.  An orientation (different from the current   */
/* one) is randomly selected and the component dimensions are updated accordingly.    */
/* ---------------------------------------------------------------------------------- */
void swap(design, which1, which2)
struct Design *design;
int which1, which2;
{
  void print_data(), update_dim(), update_state();
  int i;
  double temp_coord;
  struct Component *comp1, *comp2;

#ifdef LOCATE
  printf("Entering swap\n");
#endif

/* Find the correct components.  We don't need to back up component in case we reject */
/* the step because we don't change dimensions or orientation when swapping.  We only */
/* switch coordinates.                                                                */
  i = 1;
  comp1 = design->first_comp;
  while (++i <= which1)
    comp1 = comp1->next_comp;

  i = 1;
  comp2 = design->first_comp;
  while (++i <= which2)
    comp2 = comp2->next_comp;

/* Swap the components by switching their coordinates.                                */
#ifdef OUTPUT
  printf("Swapping %s and %s\n", comp1->comp_name, comp2->comp_name);
#endif
  i = -1;
  while (++i <= 2)
    {
      temp_coord = comp1->coord[i];
      comp1->coord[i] = comp2->coord[i];
      comp2->coord[i] = temp_coord;
    }
    
/* Update the overlaps and the bounding box dimensions for the changed components.    */
  update_state(design, comp1, which1);
  update_state(design, comp2, which2);  

#ifdef LOCATE
  printf("Leaving swap\n");
#endif
}

/* ---------------------------------------------------------------------------------- */
/* This function backs up component information.  Backup are pointers to components   */
/* containing backup information about components in case we reject a step and need   */
/* to revert to a previous design.  Whichbackup are pointers to the components which  */
/* are backed up, so that we know where to where the old information should be copied */
/* when we revert.  Which tells us which component is being backed up (0 or 1).       */
/* ---------------------------------------------------------------------------------- */
void back_up(design, comp)
struct Design *design;
struct Component *comp;
{
  int i;
  struct Component *comp1;

#ifdef LOCATE
  printf("Entering back_up\n");
#endif

/* Back up coordinates and dimensions. */
/*  printf("The component being backed up is %s\n",comp->comp_name);
*/
  design->old_orientation = comp->orientation;
  i = -1;
  while (++i <= 2)
    {
      design->old_coord[i] = comp->coord[i];
      design->old_dim[i] = comp->dim[i];
    }

/* Back up current objective_function values. */
  i = -1;
  while (++i < OBJ_NUM)
    design->backup_obj_values[i] = design->new_obj_values[i];

#ifdef LOCATE
  printf("Leaving back_up\n");
#endif
}

/* ---------------------------------------------------------------------------------- */
/* This function does all the stuff you want to do when a step is accepted.           */
/* ---------------------------------------------------------------------------------- */
void update_accept(design, iteration, accept_flag, column, update, step_eval,
		   best_eval, current_eval)
struct Design *design;
int iteration, accept_flag, *column, *update;
double *step_eval, *best_eval, current_eval;
{
  void write_step();

  
/* If accept_flag = 2 then the step is an improvement. */
  if (accept_flag == 2)
    {
#ifdef OUTPUT
      printf("*** Improved step\n");
#endif
    }

#ifdef OUTPUT
  printf("Accepting step\n\n");
#endif
	      
/* Update best evaluation function. */
  if (*step_eval < *best_eval)
    {
      *best_eval = *step_eval;
      accept_flag = 3;
    }
	      
#ifdef OBJ_DATA
/* Write objective function values to file */
  write_step(design, iteration, accept_flag);   
#endif

#ifdef TESTS
/* Do consistency checks on the design.  The 1 flag means it's an accepted step. */
  test_it(design, current_eval, 1, iteration);
#endif
}

/* ---------------------------------------------------------------------------------- */
/* This function does all the stuff you want to do when a step is rejected.           */
/* ---------------------------------------------------------------------------------- */
void update_reject(design, iteration, which1, which2, current_eval)
struct Design *design;
int iteration, which1, which2;
double current_eval;
{
  void revert(), write_step();
#ifdef OUTPUT
  printf("Rejecting step\n\n");
#endif
	      
#ifdef OBJ_DATA
/* Write objective function values to file */
  write_step(design, iteration, 0);
#endif
      
  revert(design, which1, which2);
#ifdef TESTS
/* Do consistency checks on the design.  The zero flag means it's a rejected step. */
  test_it(design, current_eval, 0, iteration);
#endif
}
/* ---------------------------------------------------------------------------------- */
/* This function reverts to old information contained when a step is rejected.        */
/* Which2 tells us if we are reverting 1 component (which2 = 0) or two (which2 > 0).  */
/* ---------------------------------------------------------------------------------- */
void revert(design, which1, which2)
struct Design *design;
int which1, which2;
{
  void update_state();
  int i;
  double temp_coord;
  struct Component *comp1, *comp2;

#ifdef LOCATE
  printf("Entering revert\n");
#endif

/* Find the first component. */
  i = 1;
  comp1 = design->first_comp;
  while (++i <= which1)
    comp1 = comp1->next_comp;

  if (!which2)
    {
      comp1->orientation = design->old_orientation;
      i = -1;
      while (++i <= 2)
	{
	  comp1->coord[i] = design->old_coord[i];
	  comp1->dim[i] = design->old_dim[i];
	}

/* Update the overlaps and bounding box dimensions back to how they were since we reverted */
      update_state(design, comp1, which1);    
    }

  else
    {
/* Find the second component. */
      i = 1;
      comp2 = design->first_comp;
      while (++i <= which2)
	comp2 = comp2->next_comp;

      i = -1;
      while (++i <= 2)
	{
	  temp_coord = comp1->coord[i];
	  comp1->coord[i] = comp2->coord[i];
	  comp2->coord[i] = temp_coord;
	}

/* Update the overlaps and bounding box dimensions back to how they were since we reverted */
      update_state(design, comp1, which1);
      update_state(design, comp2, which2);
    }

/* Revert objective_function values to the values before the step. */
  i = -1;
  while (++i < OBJ_NUM)
    design->new_obj_values[i] = design->backup_obj_values[i];

#ifdef LOCATE
  printf("Leaving revert\n");
#endif
}

/* ---------------------------------------------------------------------------------- */
/* This function updates the overlaps and bounding box dimensions after taking a step */
/* or after reverting to a previous design.                                           */
/* ---------------------------------------------------------------------------------- */
void update_state(design, comp, which)
struct Design *design;
struct Component *comp;
int which;
{
  void update_overlaps(), update_bounds();

  update_overlaps(design, comp, which);  /* THIS FUNCTION IS IN OBJ.FUNCTION.C */
  update_bounds(design, comp);
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
/* random search algorithm, depending on the #ifdef statements.                       */
/* ---------------------------------------------------------------------------------- */
accept(temp, step_eval, this_eval, design)
struct Design *design;
double temp, step_eval, this_eval;
{
  int i;
  double rnd, prob;

  if (not_too_big(design))
    {
      if (step_eval > this_eval)
	{
	  rnd = random()/2147483647.;
	  prob = exp(-(step_eval-this_eval)/temp);
	  if (rnd < prob)
	    i = 1;
	  else
	    i = 0;
	}
      else
	i = 2;
    }
  else
    i = -1;
  return (i);
}

/* ---------------------------------------------------------------------------------- */
/* This function rejects any steps that make the bounding box too big.                */
/* ---------------------------------------------------------------------------------- */
not_too_big(design)
struct Design *design;
{
  int i, small;
  double difference;

  small = 1;
  i = -1;
  difference = 0.0;
  while (++i <= 2)
    {
      difference = (design->box_max[i] - design->box_min[i]) - BOX_LIMIT;
      if (difference > 0.0)
	small = 0;
    }
  return(small);
}

/* ---------------------------------------------------------------------------------- */
/* This function performs various consistency tests on the design.                    */
/* ---------------------------------------------------------------------------------- */
void test_it(design, current_eval, accept_flag, iteration)
struct Design *design;
double current_eval;
int accept_flag, iteration;
{
  int i, j;
  struct Component *comp;
  char wait;

  j = 0;
  comp = design->first_comp;
  while (++j <= COMP_NUM)
    {
/* Test to make sure the bounding box dimensions are correct. */
      i = -1;
      while (++i <= 2)
	{
	  if (((comp->coord[i] - comp->dim[i]/2) <= design->box_min[i]) &&
	      (design->min_comp[i] != comp))
	    {
	      printf("\n\nERROR in test_it - box_min.\7\n\n");
	      exit();
	    }
	  if (((comp->coord[i] + comp->dim[i]/2) >= design->box_max[i]) &&
	      (design->max_comp[i] != comp))
	    {
	      printf("\n\nERROR in test_it - box_max.\7\n\n");
	      exit();
	    }
	}
      if (j < COMP_NUM)
	comp = comp->next_comp;
    }

/* Test to see if value reverted to is same as value before taking step. */
/*  if (!(accept_flag) && (current_eval != evaluate(design, iteration)))
    {
      printf("\n\nERROR in test_it - didn't revert correctly\7\n\n");
      exit();
    }*/
}

/* ---------------------------------------------------------------------------------- */
/* This function initializes variables, counters, coefficients, and calculates the    */
/* initial objective function value.                                                  */
/* ---------------------------------------------------------------------------------- */
void init_anneal(design, best_eval, current_eval)
struct Design *design;
double *best_eval, *current_eval;
{
  void init_obj_values(), calc_c_grav();
  double evaluate();

/* Evaluate the initial design, initialize the obj. function matrix, calculate the    */
/* initial coefficients, and recalculate the initial evaluation, which by definition  */
/* of the coefficients has been normalized to equal the number of components of the   */
/* objective function times the initial value of the first component.                 */

  *current_eval = evaluate(design, 0);       /* In obj_function.c */
  *best_eval = *current_eval;
  init_obj_values(design);                /* In obj_balance.c */

  calc_c_grav(design);
  printf("The center of gravity is %lf %lf %lf\n",design->c_grav[0],
	 design->c_grav[1],design->c_grav[2]);
}

/* ---------------------------------------------------------------------------------- */
/* This was used to test code.                                                        */
/* ---------------------------------------------------------------------------------- */
/*
	  if (iteration > 4000) ||
	      ((design->overlap[6][5] > 12.809)&&(design->overlap[6][5] < 12.81)))	   
	    {
	  printf("*******THIS IS AT THE TOP OF THE LOOP\n");
	  printf("*******CURRENT EVAL IS %lf.  EVAL IS %lf\n",current_eval, evaluate(design));
	      fptr = fopen("output/comp.out","a");
	      fprintf(fptr, "Starting iteration #%d\n",iteration);
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
void downhill(design, move_size)
struct Design *design;
double move_size;
{
  void downhill_move(), update_weights(), update_accept(), update_reject(), write_loop_data();
  int iteration, which1, modelflag, column, cost_update, accept_count, improving, count, max;
  double step_eval, current_eval, best_eval, old_eval, evaluate(), dx, dy, dz, d;
  char wait;
  FILE *fptr;

#ifdef LOCATE
  printf("Entering downhill\n");
#endif

/*  printf("\nHit return to continue\n\n");
  getchar(wait);
*/

/* Initialization */
  max = I_LIMIT;  /* Cast a double as an int */
  iteration = 0;
  column = 0;
  count = 0;
  cost_update = 0;
  accept_count = 0;
  improving = 1;
  
  current_eval = evaluate(design, max);
  best_eval = current_eval;
  step_eval = current_eval;
  printf("current_eval is %lf\n",current_eval);

  while (improving)
    {
      iteration = 0;
      ++count;

      while (++iteration <= 1000)
	{
	  old_eval = current_eval;

/* Take a step and evaluate it.  Update state by accepting or rejecting step. */
  
	  downhill_move(design, &which1, move_size);

/*      dx = design->first_comp->coord[0]-design->c_grav[0];
      dy = design->first_comp->coord[1]-design->c_grav[1];
      dz = design->first_comp->coord[2]-design->c_grav[2];
      d = dx*dx+dy*dy+dz*dz;
*/
  
	  step_eval = evaluate(design, max);
	  if (step_eval <= current_eval)
	    {
	      update_accept(design, iteration, 2, &column, &cost_update,
			    &step_eval, &best_eval, current_eval);
	   
/* Update the current evaluation function value. */
	      current_eval = step_eval;
	      ++accept_count;
	      if (current_eval < best_eval)
		best_eval = current_eval;
	    }
	  else
	    {
	      update_reject(design, iteration, which1, 0, current_eval);
	    }
	}
      if (current_eval/old_eval > 0.99)
	improving = 0;
    }
  write_loop_data(0.0, (1000*count), accept_count, 0, 0, 3);

  step_eval = evaluate(design, max);
  printf("The best eval was %lf\n",best_eval);
  printf("The final eval was %lf\n",step_eval);
  fptr = fopen("results","a");
  fprintf(fptr,"After the downhill search:\n");
  fprintf(fptr,"The best eval was %lf\n",best_eval);
  fprintf(fptr,"The final eval was %lf\n",step_eval);
  fclose(fptr);

#ifdef LOCATE
  printf("Leaving downhill\n");
#endif
}
 
/* ---------------------------------------------------------------------------------- */
/* This function takes a move step, moving a component along a random direction for   */
/* a distance d, where d is a number between 0.1 and 2.5 (the component dimensions    */
/* range from 5 to 10).                                                               */
/* ---------------------------------------------------------------------------------- */
void downhill_move(design, which, move_size)
struct Design *design;
int *which;
double move_size;
{
  void normalize(), back_up(), print_data(), update_state();
  int i;
  double max_dist, d, dir_vect[3], my_double_random();
  struct Component *comp;

#ifdef LOCATE
  printf("Entering downhill_move\n");
#endif

  *which = my_random(1,COMP_NUM);

/* Find the correct component and back up the component information in case we reject */
/* the step.                                                                          */
  i = 1;
  comp = design->first_comp;
  while (++i <= *which)
    comp = comp->next_comp;
  back_up(design, comp);

/* Pick a random direction and distance, and move the component. Multiply that vector */
/* by a vector from the center of the component to the center of gravity, to imrove   */
/* chances of having an improvement step (i.e. never move away from c_grav).          */
#ifdef OUTPUT
  printf("Moving %s\n",comp->comp_name);
#endif
  i = -1;
  while (++i <=2)
    {
      dir_vect[i] = my_double_random(0.0,1.0);
      dir_vect[i] *= design->c_grav[i] - comp->coord[i];
    }

  normalize(dir_vect);
  d = move_size*my_double_random(0.5,1.0);
  
  i = -1;
  while (++i <= 2)
      comp->coord[i] += (d * dir_vect[i]);

/* Update the overlaps and the bounding box dimensions for the changed component.     */
  update_state(design, comp, *which); 

#ifdef LOCATE
  printf("Leaving downhill_move\n");
#endif
}
