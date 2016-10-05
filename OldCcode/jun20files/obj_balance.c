/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                    OBJ_BALANCE.C                                   */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* This function initializes the normalizing coefficients to zero.                    */
/* ---------------------------------------------------------------------------------- */
void init_coef(design)
struct Design *design;
{
  int i, j;

  for (i = 0; i < OBJ_NUM; i++) {
    for (j = BALANCE_AVG - 1; j > 0; j--) {
	design->old_obj_values[i][j] = design->old_obj_values[i][j-1];
    }
    design->old_obj_values[i][0] = 1.0;
  }
}

/* ---------------------------------------------------------------------------------- */
/* This function updates the normalizing coefficients.                                */
/* ---------------------------------------------------------------------------------- */
void update_coef(design)
struct Design *design;
{
  int i, j;

  /*for (i = 0; i < OBJ_NUM; i++) {
    if (design->old_obj_values[i][0] <= design->new_obj_values[i]) {
	design->old_obj_values[i][0] = design->new_obj_values[i];
	design->coef[i] = 0.0;
	for (j = 0; j < BALANCE_AVG; j++)
	    design->coef[i] += design->old_obj_values[i][j]/BALANCE_AVG;
    }
  }*/
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
  }
}
