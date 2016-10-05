/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                   OBJ_FUNCTION.C                                   */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* This function returns an evaluation of the current design.                         */
/* ---------------------------------------------------------------------------------- */
double evaluate(design, approximate)
struct Design *design;
int approximate;
{
  void eval_part_1(), eval_part_2(), eval_part_3(), heat_eval();
  void update_coef(), correct_by_LU();
  int i, k;
  double eval;
  char wait;
  FILE *fptr;

#ifdef LOCATE
  printf("Entering evaluate\n");
#endif

/* Evaluate the four components of the objective function. */
  eval_part_1(design);
  eval_part_2(design);
  eval_part_3(design);
  if (approximate)
    heat_eval(design);
  else correct_by_LU(design);

  update_coef(design);

/* Add up the individual evaluations. */
  eval = 0.0;
  for (i = 0; i < OBJ_NUM; ++i) {
      eval += design->weight[i]*design->new_obj_values[i];
  }

  printf("%.2f %.2f %.2f   %f\n", design->weight[0]*design->new_obj_values[0], 
	design->weight[3]*design->new_obj_values[3], 
	design->weight[4]*design->new_obj_values[4], 
	eval);	     
/*  fptr = fopen("output/eval.out","a");
  fprintf(fptr,"%.2f %.2f %.2f %.2f\n", (design->coef[0] * design->weight[0] * design->new_obj_values[0]), (design->coef[1] * design->weight[1] * design->new_obj_values[1]), (design->coef[2] * design->weight[2] * design->new_obj_values[2]), (design->coef[3] * design->weight[3] * design->new_obj_values[3]));
  fclose(fptr);*/

#ifdef LOCATE
  printf("Leaving evaluate\n");
#endif
  
  return(eval);
}

/* ---------------------------------------------------------------------------------- */
/* This function sets the value of the first part of the objective function;          */
/* inverse density.                                                                   */
/* ---------------------------------------------------------------------------------- */
void eval_part_1(design)
struct Design *design;
{
  double box_x_dim, box_y_dim, box_z_dim, half_box_area, box_volume;

  box_x_dim = design->box_max[0] - design->box_min[0];
  box_y_dim = design->box_max[1] - design->box_min[1];
  box_z_dim = design->box_max[2] - design->box_min[2];

/*  half_box_area = (box_x_dim*box_y_dim) + (box_x_dim*box_z_dim) + (box_y_dim*box_z_dim);
  design->new_obj_values[0] = half_box_area/design->half_area;*/

  box_volume = box_x_dim * box_y_dim * box_z_dim;
  design->new_obj_values[0] = box_volume/design->volume;

}

/* ---------------------------------------------------------------------------------- */
/* This function sets the value of the second part of the objective function, which   */
/* is the amount of overlap in a design.                                              */
/* Note that only the top half of the overlap matrix is used.                         */
/* Remember that an nXn matrix has elements numbered from [0][0] to [n-1][n-1]        */
/* ---------------------------------------------------------------------------------- */
void eval_part_2(design)
struct Design *design;
{
  int i, j;
  double sum;

  sum = 0.0;
  i = -1;
  while (++i < COMP_NUM)
    {
      j = i;
      while (++j < COMP_NUM)
	  sum += design->overlap[j][i];
    }
  if (sum > 0.0)
    design->new_obj_values[1] = (0.05+sum)*design->new_obj_values[0];
  else
    design->new_obj_values[1] = 0.0;
}

/* ---------------------------------------------------------------------------------- */
/* This function sets the value of the third part of the objective function, which    */
/* is the amount of overlap with the container.                                       */
/* ---------------------------------------------------------------------------------- */
void eval_part_3(design)
struct Design *design;
{
  int i;
  double difference, box_penalty;

  box_penalty = 0.0;
  i = -1;
  while (++i <= 2)
    {
      difference = (design->box_max[i] - design->box_min[i]) - design->container[i];
      if (difference > 0.0)
	box_penalty += difference;
    }
  design->new_obj_values[2] = box_penalty*box_penalty;
}  

/* ---------------------------------------------------------------------------------- */
/* This function returns the max overlap in a design.                                 */
/* Note that only the top half of the overlap matrix is used.                         */
/* Remember that an nXn matrix has elements numbered from [0][0] to [n-1][n-1]        */
/* ---------------------------------------------------------------------------------- */
double max_overlap(design)
struct Design *design;
{
  int i, j;
  double max;

  max = 0.0;
  i = -1;
  while (++i < COMP_NUM)
    {
      j = i;
      while (++j < COMP_NUM)
	{
	  if (design->overlap[j][i] > max)
	    max = design->overlap[j][i];
	}
    }
  return(max);
}

/* ---------------------------------------------------------------------------------- */
/* This function returns a multiplier to determine the penalty due to overlap.        */
/* The multiplier is a function of temperature, so that the penalty for overlap       */
/* increases as the annealing proceeds.                                               */
/* ---------------------------------------------------------------------------------- */
double multiplier(t)
double t;
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
void init_overlaps(design)
struct Design *design;
{
  int i, j;
  double dx, dy, dz;
  struct Component *comp1, *comp2;

  i = 0;
  comp1 = design->first_comp;
  while (++i < COMP_NUM)
    {
      j = i;
      comp2 = comp1;
      while (++j <= COMP_NUM)
	{
	  comp2 = comp2->next_comp;
	  dx = (comp1->dim[0] + comp2->dim[0])/2.0 - fabs(comp1->coord[0] - comp2->coord[0]);
	  dy = (comp1->dim[1] + comp2->dim[1])/2.0 - fabs(comp1->coord[1] - comp2->coord[1]);
	  dz = (comp1->dim[2] + comp2->dim[2])/2.0 - fabs(comp1->coord[2] - comp2->coord[2]);

/* Calculate the average percentage of overlap */
	  if ((dx > 0) && (dy > 0) && (dz > 0))
	    design->overlap[j-1][i-1] = 2.*dx*dy*dz/(comp1->volume + comp2->volume);
	  else
	    design->overlap[j-1][i-1] = 0.0;
	}
      comp1 = comp1->next_comp;
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function updates the overlap matrix for the components.                       */
/* The variable which determines which component to update.                           */
/* The overlap calculated is the Manhattan distance between near corners.             */
/* Note that only the top half of the overlap matrix is used.                         */
/* Remember that an nXn matrix has elements numbered from [0][0] to [n-1][n-1]        */
/* ---------------------------------------------------------------------------------- */
void update_overlaps(design, temp1, which)
struct Design *design;
struct Component *temp1;
int which;
{
  int i, index;
  double dx, dy, dz, d1, d2;
  char wait;
  struct Component *temp2;

#ifdef LOCATE
  printf("Entering update_overlaps\n");
#endif

/* Check overlap with components.  Since we are only using the top half of a symmetric */
/* matrix, there is an if statement which switches the indices of the matrix elements. */
  i = 0;
  index = which - 1;
  temp2 = design->first_comp;
  while (i <= (COMP_NUM-1))
    {
      dx = (temp1->dim[0] + temp2->dim[0])/2.0 - fabs(temp1->coord[0] - temp2->coord[0]);
      dy = (temp1->dim[1] + temp2->dim[1])/2.0 - fabs(temp1->coord[1] - temp2->coord[1]);
      dz = (temp1->dim[2] + temp2->dim[2])/2.0 - fabs(temp1->coord[2] - temp2->coord[2]);

/*  Recalculate overlaps if both objects are cylinders with aligned z axes. */
      if ((temp1->shape_type[0] == 'C') && (temp2->shape_type[0] == 'C'))
	{
	  if (((temp1->orientation*temp2->orientation) == 3) && (dz > 0.0))
	    {
	      d1 = temp1->coord[0] - temp2->coord[0];
	      d2 = temp1->coord[1] - temp2->coord[1];
	      dx = (temp1->dim[0] + temp2->dim[0])/2.0 - sqrt(d1*d1 + d2*d2);
	      dy = dx;
	    }
	  else if (((temp1->orientation*temp2->orientation) == 8) && (dy > 0.0))
	    {
	      d1 = temp1->coord[0] - temp2->coord[0];
	      d2 = temp1->coord[2] - temp2->coord[2];
	      dx = (temp1->dim[0] + temp2->dim[0])/2.0 - sqrt(d1*d1 + d2*d2);
	      dz = dx;
	    }
	  else if (((temp1->orientation*temp2->orientation) == 30) && (dx > 0.0))
	    {
	      d1 = temp1->coord[1] - temp2->coord[1];
	      d2 = temp1->coord[2] - temp2->coord[2];
	      dy = (temp1->dim[1] + temp2->dim[1])/2.0 - sqrt(d1*d1 + d2*d2);
	      dz = dy;
	    }
	}

/* Set overlap value in matrix. */
      if (i < index)
	{
	  if ((dx > 0.0) && (dy > 0.0) && (dz > 0.0))
	    design->overlap[index][i] = 2.*dx*dy*dz/(temp1->volume + temp2->volume);
	  else
	    design->overlap[index][i] = 0.0;
	}
      else if (i > index)
	{
	  if ((dx > 0.0) && (dy > 0.0) && (dz > 0.0))
	    design->overlap[i][index] = 2.*dx*dy*dz/(temp1->volume + temp2->volume);
	  else
	    design->overlap[i][index] = 0.0;
	}
      ++i;
      temp2 = temp2->next_comp;
    }

#ifdef LOCATE
  printf("Leaving update_overlaps\n");
#endif

}
