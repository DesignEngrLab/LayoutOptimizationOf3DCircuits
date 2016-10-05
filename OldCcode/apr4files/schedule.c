/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                   SCHEDULE.C                                     */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* This function updates the temperature.                                             */
/* ---------------------------------------------------------------------------------- */
void update_temp(t, sigma)
double *t, sigma;
{
  double reduce;
  FILE *fptr;
  
  reduce = exp(-(LAMBDA * *t)/sigma);
  if (reduce < T_RATIO_MIN)
    reduce = T_RATIO_MIN;
  else if (reduce > T_RATIO_MAX)
    reduce = T_RATIO_MAX;
  printf("Reducing temperature by a factor of %lf\n",reduce);
  fptr = fopen("output/temperature.out","a");
  fprintf(fptr,"Reducing temperature by a factor of %lf\n",reduce);
  fclose(fptr);
  *t *= reduce;
}

/* ---------------------------------------------------------------------------------- */
/* This function takes an integer and returns the standard devaiation of that many    */
/* numbers read in from a file.                                                       */
/* ---------------------------------------------------------------------------------- */
void calc_statistics(schedule)
struct Schedule *schedule;
{
  int i, j;
  double eval, sum, sum_sqrs;
  FILE *fptr;
  
  fptr = fopen("sample.data","r");
  if (fptr == NULL)
    {	
      printf("The objective function data file does not exist or could not be opened.\n\n");
      exit();
    }
  i = 0;
  sum = 0;
  sum_sqrs = 0;
  while ((fscanf(fptr,"%lf",&eval)) && !(feof(fptr)))
    {
      ++i;
      sum += eval;
      sum_sqrs += eval*eval;
    }
  fclose(fptr);

  if (i >= MIN_SAMPLE)
    {
      schedule->c_avg = sum/(1.0 * i);
      schedule->sigma = sqrt(  (sum_sqrs - i * schedule->c_avg * schedule->c_avg)/(1. * (i-1))  );
      schedule->delta = RANGE * schedule->sigma;
      fptr = fopen("sample.data","w");
      fclose(fptr);
      printf("\nSigma, c_avg and delta for the previous sample are %lf, %lf, %lf\n",
	     schedule->sigma, schedule->c_avg, schedule->delta); 
    }
  else if (i > 1)
    {
      printf("There were not enough data points in the sample.data file\n");
      exit();
    }
/* If we get past the above "else" we don't do anything.  This function gets called   */
/* when we junk the 101st step at a given temperature because the annealing function  */
/* calls this function on the 100th step.                                             */
}

/* ---------------------------------------------------------------------------------- */
/* This function initializes variables, counters, coefficients, and calculates the    */
/* initial objective function value.                                                  */
/* ---------------------------------------------------------------------------------- */
void init_schedule(schedule)
struct Schedule *schedule;
{
  double calc_initial_t();

  /* Initialize variables and counters */
  schedule->t_initial = K * schedule->sigma;
  schedule->mgl = COMP_NUM * (COMP_NUM + 11)/2;
  schedule->within_target = ERF_RANGE*3*COMP_NUM;
  schedule->max_tolerance = (1 - ERF_RANGE)*3*COMP_NUM;
  schedule->problem_size = COMP_NUM;

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
void sample_space(design)
struct Design *design;
{
  void take_step(), init_obj_values(), update_accept(), update_reject();
  void init_hustin();
  int i, which1, which2, column, update, accept_flag;
  double eval, evaluate(), dummy_eval;
  struct Hustin *dummy_hustin;
  FILE *fptr;

  dummy_hustin = (struct Hustin *) malloc(sizeof(struct Hustin));
  init_hustin(dummy_hustin);
  dummy_eval = 0.;
  eval = evaluate(design, 0);        /* IN ANNEAL_ALG.C */
  init_obj_values(design);        /* IN OBJ_BALANCE.C */
  fptr = fopen("sample.data","w");
  column = 0;
  update = 0;
  which1 = 0;
  which2 = 0;
  i = 0;
  while (++i <= SAMPLE)
    {
      take_step(design, dummy_hustin, &which1, &which2);   /* IN ANNEAL_ALG.C */
      eval = evaluate(design, 1);                             /* IN ANNEAL_ALG.C */

/* Sending (eval + 1.0) as the current_eval makes every step an "improvement".  Thus  */
/* every step will be accepted (unless the BOX_LIMIT box is violated).                */
      accept_flag = accept(1., eval, (eval + 1.0), design);
      if (accept_flag > 0)
	{
	  update_accept(design, 0, accept_flag, &column, &update, /* IN ANNEAL_ALG.C */
			&eval, &dummy_eval, (eval+1.0));
	  fprintf(fptr,"%lf\n",eval);
	}
      else
	{
	  --i;
	  update_reject(design, 0, which1, which2, eval); /* IN ANNEAL_ALG.C */
	}
    }
  fclose(fptr);
  free(dummy_hustin);
}

/* ---------------------------------------------------------------------------------- */
/* This function updates parameters for the equilibrium condition.                    */
/* ---------------------------------------------------------------------------------- */
void equilibrium_update(step_eval, current_eval, schedule, hold_temp)
int *hold_temp;
double step_eval, current_eval;
struct Schedule *schedule;
{

/* Update c_min, c_max, and max_delta_c for the frozen condition check. */
  if (step_eval > schedule->c_max)
    schedule->c_max = step_eval;
  if (step_eval < schedule->c_min)
    schedule->c_min = step_eval;
  schedule->delta_c = fabs(current_eval - step_eval);
  if (schedule->delta_c > schedule->max_delta_c)
    schedule->max_delta_c = schedule->delta_c;

/* Increment in_count or out_count. */
  if (fabs(schedule->c_avg - current_eval) <= schedule->delta)
    ++(schedule->in_count);
  else
    ++(schedule->out_count);
  
/* Check if we have exceeded the tolerance limit.  If not, check for equilibrium. */
  if (schedule->out_count > schedule->max_tolerance)
    {
      schedule->out_count = 0;
      schedule->in_count = 0;
    }
  else if (schedule->in_count > schedule->within_target)
    *hold_temp = 0;
}

/* ---------------------------------------------------------------------------------- */
/* This function checks the frozen condition.                                         */
/* ---------------------------------------------------------------------------------- */
frozen_check(schedule)
struct Schedule *schedule;
{
  if ((schedule->c_max - schedule->c_min) == schedule->max_delta_c)
    return (1);
  else
    return (0);
}
