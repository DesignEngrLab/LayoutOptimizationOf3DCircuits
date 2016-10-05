/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                    OBJ_BALANCE.C                                   */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* This function updates coefficients and values in the objective function matrix.    */
/* ---------------------------------------------------------------------------------- */
void update_coef(design, column, update)
struct Design *design;
int *column, *update;
{
  void calc_c_grav();
  double sum[OBJ_NUM];
  int i, j;

/* Update obj. function value matrix on since step is an improvement. */
  ++(*column);
  if (*column == BALANCE_AVG)
    *column = 0;
  for (i = 0; i < OBJ_NUM; ++i) {
    if (design->new_obj_values[i] == 0.0)
      design->old_obj_values[i][*column] = TINY;
    else design->old_obj_values[i][*column] = design->new_obj_values[i];
    }

/* If it is time to recalculate the coefficients, do it.   Since we have updated the 
coefficients, we have to update the step_eval which will become the current_eval below */
  ++(*update);
  if (*update == UPDATE_INTERVAL)
    {
      *update = 0;
      for (i = 0; i < OBJ_NUM; ++i) {
	sum[i] = 0.0;
	for (j = 0; j < BALANCE_AVG; ++j)
	  sum[i] += design->old_obj_values[i][j];
	design->coef[i] = sum[0]/sum[i];
	if (design->coef[i] > 1000.0) design->coef[i] = 1000.0;
/*	printf("                        design->coef[%d] = %lf\n", i, design->coef[i]);*/
      }

/* While we're at it, update the center of gravity */
      calc_c_grav(design);
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function calculates initial coefficients based on the initial evaluation.     */
/* ---------------------------------------------------------------------------------- */
void init_obj_values(design)
struct Design *design;
{
  int i, j;
  double sum[OBJ_NUM];

/* Fill the objective function value matrix entirely with the initial values.         */
  for (i = 0; i < OBJ_NUM; ++i) {
    for (j = 0; j < BALANCE_AVG; ++j) {

/* This if statement initializes the matrix with the objective function values only   */
/* if they are non-zero.  If they are zero, the row is initialized with values of     */
/* design->new_obj_values[0], which results in an initial coefficient of 1.           */
      if (design->new_obj_values[i] != 0.0)
	design->old_obj_values[i][j] = design->new_obj_values[i];
      else design->old_obj_values[i][j] = design->new_obj_values[0];
    }
    design->coef[i] = (design->new_obj_values[0])/(design->old_obj_values[i][0]);
/*    printf("design->coef[%d] = %lf\n", i, design->coef[i]);*/
  }
}

/* ---------------------------------------------------------------------------------- */
/* This function updates the objective function component weights.                    */
/* ---------------------------------------------------------------------------------- */
void update_weights(design, t)
struct Design *design;
double t;
{
  int i;
  double avg_delta_ratio;

  design->weight[3] = 0.001/(t*t) + 1.0;
  if (design->weight[3] > 100.0) design->weight[3] = 100.0;
/*  avg_delta_ratio = 0.;
  for (i = 0; i < BALANCE_AVG; ++i)
    avg_delta_ratio += fabs(design->old_obj_values[3][i] - 
			    design->old_obj_values[3][i+1])/(1.0*BALANCE_AVG);
  design->weight[3] = 3.0/avg_delta_ratio;
  printf("\tdesign->weight[3] = %lf\n", design->weight[3]);*/
}







