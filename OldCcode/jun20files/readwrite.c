/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                    READWRITE.C                                      */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* This function prints out component information.                                    */
/* ---------------------------------------------------------------------------------- */
void fprint_data(design, which)
struct Design *design;
int which;
{
  int i;
  struct Component *comp;
  FILE *fptr;

  fptr = fopen("output/comp.out","a");

  comp = design->first_comp;
  i = 1;
  while (++i <= which)
    comp = comp->next_comp;
  
  fprintf(fptr,"\nComponent name is %s and the orientation is %d\n",comp->comp_name, 
	                                                          comp->orientation);

  i = -1;
  while (++i < 3)
    fprintf(fptr,"dim %d is %lf\n",i,comp->dim[i]);

  i = -1;
  while (++i < 3)
    fprintf(fptr,"dim_initial %d is %lf\n",i,comp->dim_initial[i]);

  i = -1;
  while (++i < 3)
    fprintf(fptr,"current dim %d is %lf\n",i,comp->dim[i]);

  i = -1;
  while (++i < 3)
    fprintf(fptr,"coord %d is %lf\n",i,comp->coord[i]);

  fprintf(fptr,"\n");
  fclose(fptr);
}

/* ---------------------------------------------------------------------------------- */
/* This function writes accepted steps to a file.                                     */
/* ---------------------------------------------------------------------------------- */
void write_step(design, iteration, flag)
struct Design *design;
int iteration, flag;
{
  FILE *fptr1, *fptr2, *fptr3, *fptr4, *fptr5;

  if (iteration)
    {
      fptr1 = fopen("output/ratio.out","a");
      fptr2 = fopen("output/container.out","a");
      fptr3 = fopen("output/overlap.out","a");
      fptr4 = fopen("output/coef.out","a");
      fptr5 = fopen("output/overlap2.out","a");
      
      if (flag != 0)
	{
	  fprintf(fptr1,"%d %lf\n",iteration, design->new_obj_values[0]);
	  fprintf(fptr3,"%d %lf\n",iteration,
		  (design->new_obj_values[1]*design->weight[1]));
	}
      
/*      if (flag == 0)
	{
	  fprintf(fptr1,"%d %lf R\n",iteration, design->new_obj_values[0]);
	  fprintf(fptr2,"%d %lf R\n",iteration, 
		  (design->new_obj_values[2]*design->coef[2]*design->weight[2]));
	  fprintf(fptr3,"%d %lf R\n",iteration, (design->new_obj_values[1]*design->coef[1]));

	  fprintf(fptr5,"%d %lf R\n",iteration, design->new_obj_values[1]);

	  fprintf(fptr4,"%d %lf R\n",iteration, design->coef[1]);
	}
      else if (flag == 3)
	{
	  fprintf(fptr1,"%d %lf A  *\n",iteration, design->new_obj_values[0]);
	  fprintf(fptr2,"%d %lf A  *\n",iteration, 
		  (design->new_obj_values[2]*design->coef[2]*design->weight[2]));
	  fprintf(fptr3,"%d %lf A  *\n",iteration, (design->new_obj_values[1]*design->coef[1]));

	  fprintf(fptr5,"%d %lf A  *\n",iteration, design->new_obj_values[1]);

	  fprintf(fptr4,"%d %lf A  *\n",iteration, design->coef[1]);
	}
      else
	{
	  fprintf(fptr1,"%d %lf A\n",iteration, design->new_obj_values[0]);
	  fprintf(fptr2,"%d %lf A\n",iteration, 
		  (design->new_obj_values[2]*design->coef[2]*design->weight[2]));
	  fprintf(fptr3,"%d %lf A\n",iteration, (design->new_obj_values[1]*design->coef[1]));

	  fprintf(fptr5,"%d %lf A\n",iteration, design->new_obj_values[1]);

	  fprintf(fptr4,"%d %lf A\n",iteration, design->coef[1]);
	}
*/
      
      fclose(fptr1);
      fclose(fptr2);
      fclose(fptr3);
      fclose(fptr4);
      fclose(fptr5);
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function writes data regarding the last temperature to a file.                */
/* ---------------------------------------------------------------------------------- */
void write_loop_data(t, steps_at_t, accept_count, bad_accept_count, gen_limit, flag)
int steps_at_t, accept_count, bad_accept_count, gen_limit, flag;
double t;
{
  FILE *fptr;
  
  fptr = fopen("output/temperature.out","a");
  if (flag == 1)
    {
      fprintf(fptr,"Temperature set at %lf\n",t);
      fprintf(fptr,"At this temperature %d steps were taken.  %d were accepted\n",
	      steps_at_t, accept_count);
      fprintf(fptr,"Of the accepted steps, %d were inferior steps\n",bad_accept_count);
      fprintf(fptr,"Equilibrium was ");
      if (steps_at_t > gen_limit)
	fprintf(fptr,"not ");
      fprintf(fptr,"reached at this temperature\n\n");
    }
  else if (flag == 2)
    fprintf(fptr,"Design is now frozen\n\n\n");
  else if (flag == 3)
    {
      fprintf(fptr,"Temperature set at infinity (Downhill search)\n");
      fprintf(fptr,"%d steps were taken.  %d were accepted\n\n\n",
	      steps_at_t, accept_count);
    }
  fclose(fptr);
}

/* ---------------------------------------------------------------------------------- */
/* This function prints out component information.                                    */
/* ---------------------------------------------------------------------------------- */
void print_data(design, which)
struct Design *design;
int which;
{
  int i;
  struct Component *comp;

  comp = design->first_comp;
  i = 1;
  while (++i <= which)
    comp = comp->next_comp;
  
  printf("\nComponent name is %s and the orientation is %d\n",comp->comp_name, comp->orientation);

  i = -1;
  while (++i < 3)
    printf("dim %d is %lf\n",i,comp->dim[i]);

  i = -1;
  while (++i < 3)
    printf("dim_initial %d is %lf\n",i,comp->dim_initial[i]);

  i = -1;
  while (++i < 3)
    printf("coord %d is %lf\n",i,comp->coord[i]);
  printf("\n");
}

/* ---------------------------------------------------------------------------------- */
/* This function writes component data to a file.                                     */
/* ---------------------------------------------------------------------------------- */
void print_comp_data(design)
struct Design *design;
{
  void fprint_data();
  int i;

  i = 0;
  while (++i <= COMP_NUM)
    fprint_data(design, i);
}

/* ---------------------------------------------------------------------------------- */
/* This function prints out overlap data.                                             */
/* ---------------------------------------------------------------------------------- */
void print_overlaps(design)
struct Design *design;
{
  int i, j;

  i = -1;
  while (++i < COMP_NUM)
    {
      j = -1;
      while (++j <= i)
	printf("%lf ",design->overlap[i][j]);
      printf("\n");
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function writes design data to a file.                                        */
/* ---------------------------------------------------------------------------------- */
void save_design(design)
struct Design *design;
{
  int i, j;
  double avg_old_value;
  struct Component *comp;
  FILE *fptr;
  
  printf("Saving current design\n");
  
  i = -1;
  avg_old_value = 0.0;
  while (++i < BALANCE_AVG)
    avg_old_value += design->old_obj_values[1][i];
  avg_old_value /= BALANCE_AVG;
  
fptr = fopen("output/design.data","w");
  i = 0;
  comp = design->first_comp;
  while (++i <= COMP_NUM)
    {
      fprintf(fptr, "%s %s %d\n", comp->comp_name, comp->shape_type, comp->orientation);
      fprintf(fptr, "%lf %lf %lf\n", comp->dim_initial[0], comp->dim_initial[1], comp->dim_initial[2]);
      fprintf(fptr, "%lf %lf %lf\n", comp->dim[0], comp->dim[1], comp->dim[2]);
      fprintf(fptr, "%lf %lf %lf\n", comp->coord[0], comp->coord[1], comp->coord[2]);
      fprintf(fptr, "%lf %lf   %lf\n", comp->half_area, comp->mass, comp->temp);
      if (i < COMP_NUM)
	comp = comp->next_comp;
    }
  fprintf(fptr,"%lf %lf %lf %lf\n",design->new_obj_values[1], design->coef[1],
	  design->weight[1], avg_old_value);
  fclose(fptr);
}

/* ---------------------------------------------------------------------------------- */
/* This function reads design data from a file.                                       */
/* ---------------------------------------------------------------------------------- */
void restore_design(design)
struct Design *design;
{
  void init_bounds(), init_overlaps();
  int i, j;
  double avg_old_value;
  struct Component *comp;
  FILE *fptr;

  printf("Restoring saved design\n");
  fptr = fopen("cube.data","r");
  i = 0;
  comp = design->first_comp;
  while (++i <= COMP_NUM)
    {
      fscanf(fptr, "%s %s %d", comp->comp_name, comp->shape_type, &(comp->orientation));
      fscanf(fptr, "%lf %lf %lf", &(comp->dim_initial[0]), &(comp->dim_initial[1]), &(comp->dim_initial[2]));
      fscanf(fptr, "%lf %lf %lf", &(comp->dim[0]), &(comp->dim[1]), &(comp->dim[2]));
      fscanf(fptr, "%lf %lf %lf", &(comp->coord[0]), &(comp->coord[1]), &(comp->coord[2]));
      fscanf(fptr, "%lf %lf %lf", &(comp->half_area), &(comp->mass), &(comp->temp));
      if (i < COMP_NUM)
	comp = comp->next_comp;
    }
  fscanf(fptr,"%lf %lf %lf %lf",&(design->new_obj_values[1]), &(design->coef[1]), &(design->weight[1]), &avg_old_value);
  fclose(fptr);
  init_bounds(design);
  init_overlaps(design);
  if (design->new_obj_values[1] > 0.0)
    {
      printf("The design read in has overlaps.  Fix restore_design() in readwrite.c\n");
      exit();
    }
  else
    {
      i = -1;
      while (++i < OBJ_NUM)
	{
	  j = -1;
	  while (++j < BALANCE_AVG)
	    design->old_obj_values[i][j]  = avg_old_value;
	}
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function writes to a file: the current move probabilities, the percentage     */
/* change in delta_c due to each move, and the percentage of attempts for each move.  */
/* ---------------------------------------------------------------------------------- */
void write_probs(hustin, temp)
struct Hustin *hustin;
double temp;
{
  int i, total_attempts;
  double total_delta_c;
  FILE *fptr1, *fptr2, *fptr3;

  total_attempts = 0;
  total_delta_c = 0.;

  i = -1;
  while (++i < MOVE_NUM)
    {
      total_attempts += hustin->attempts[i];
      total_delta_c += hustin->delta_c[i];
    }

  fptr1 = fopen("output/probs.out","a");
  fptr2 = fopen("output/delta_c.out","a");
  fptr3 = fopen("output/attempts.out","a");
  fprintf(fptr1,"%lf ",temp);
  fprintf(fptr2,"%lf ",temp);
  fprintf(fptr3,"%lf ",temp);

  i = -1;
  while (++i < MOVE_NUM)
    {
      fprintf(fptr1,"%lf ",hustin->prob[i]);
      fprintf(fptr2,"%lf ",hustin->delta_c[i]/total_delta_c);
      fprintf(fptr3,"%lf ",1.*hustin->attempts[i]/total_attempts);
    }
  fprintf(fptr1,"\n");
  fprintf(fptr2,"\n");
  fprintf(fptr3,"\n");
  fclose(fptr1);
  fclose(fptr2);
  fclose(fptr3);
}

/* ---------------------------------------------------------------------------------- */
/* This function writes the final temperature field to a file.                        */
/* ---------------------------------------------------------------------------------- */
void save_tfield(design)
struct Design *design;
{
  int k = 0;
  FILE *fptr;
  
  printf("Saving current tfield\n");
  fptr = fopen("output/tfield.data","w");
 
  while (design->tfield[k].temp != 0.0) {
    fprintf(fptr, "%lf %lf %lf %lf\n", design->tfield[k].coord[0], 
	design->tfield[k].coord[1], design->tfield[k].coord[2],
	design->tfield[k].temp);
    /*if (design->tfield[k].coord[2] == 0.0) 
	fprintf(fptr, "%lf %lf %lf\n", design->tfield[k].coord[0], 
	design->tfield[k].coord[1], design->tfield[k].temp);*/
    ++k;
  }
  fclose(fptr);
}

/* ---------------------------------------------------------------------------------- */
/* This function restores the temperature field to a old_temp's.                       */
/* ---------------------------------------------------------------------------------- */
void restore_tfield(design)
struct Design *design;
{
  int k = 0;
  FILE *fptr;
  
  printf("Restoring tfield\n");
  fptr = fopen("tfield.data","r");
 
  while (!(feof(fptr))) {
    fscanf(fptr, "%lf %lf %lf %lf\n", &(design->tfield[k].coord[0]), 
	&(design->tfield[k].coord[1]), &(design->tfield[k].coord[2]),
	&(design->tfield[k].old_temp));
    /*if (design->tfield[k].coord[2] == 0.0) 
	fscanf(fptr, "%lf %lf %lf\n", design->tfield[k].coord[0], 
	design->tfield[k].coord[1], design->tfield[k].temp);*/
    ++k;
  }
  fclose(fptr);
}
