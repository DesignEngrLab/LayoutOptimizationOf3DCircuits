/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                      TEMPTEST.C                                    */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/*                                  INCLUDE FILES                                     */
/* ---------------------------------------------------------------------------------- */
#include <math.h>
#include <stdio.h>
#include <time.h>
#include "constants.c"
#include "structs.c"
#include "heatbasic.c"
#include "heatMM.c"
#include "readwrite.c"

/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                       MAIN                                         */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */
main()
{
  int getdata();
  void restore_design(), heat_eval(), save_design(), save_tfield(), init_heat_param();
  struct Design *design;

  design = (struct Design *) malloc(sizeof(struct Design));
  printf("%d\n", getdata(design));
  init_heat_param(design);
  restore_design(design);
  heat_eval(design, 0, 0);
  save_design(design);
  save_tfield(design);
}

/* ---------------------------------------------------------------------------------- */
/* This function initializes the bounds for the bounding box.                         */
/* ---------------------------------------------------------------------------------- */
void init_bounds(design)
struct Design *design;
{
  void update_min_bounds(), update_max_bounds();
  int i, j;
  struct Component *comp;
  
  design->box_min[0] = 1000;
  design->box_min[1] = 1000;
  design->box_min[2] = 1000;
  design->box_max[0] = -1000;
  design->box_max[1] = -1000;
  design->box_max[2] = -1000;

  i = 0;
  comp = design->first_comp;
  while (++i <= COMP_NUM)
    {
      j = 0;
      while (j <=2)
	{
	  update_min_bounds(design, comp, j);
	  update_max_bounds(design, comp, j);
	  ++j;
	}
      if (i < COMP_NUM)
	comp = comp->next_comp;
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function updates the  min x, y and z bounds for the bounding box.             */
/* ---------------------------------------------------------------------------------- */
void update_min_bounds(design, comp, i)
struct Design *design;
struct Component *comp;
int i;
{
  double location;

#ifdef LOCATE
  printf("Entering update_min_bounds\n");
#endif

/*  printf("updating min bounds %d for %s\n",i,comp->comp_name);
*/

  location = comp->coord[i] - comp->dim[i]/2.0;
    if (location < design->box_min[i])
      {
	design->box_min[i] = location;
	design->min_comp[i] = comp;
      }
  
#ifdef LOCATE
  printf("Leaving update_min_bounds\n");
#endif

}

/* ---------------------------------------------------------------------------------- */
/* This function updates the max and min x, y and z bounds for the bounding box.      */
/* ---------------------------------------------------------------------------------- */
void update_max_bounds(design, comp, i)
struct Design *design;
struct Component *comp;
int i;
{
  double location;

#ifdef LOCATE
  printf("Entering update_max_bounds\n");
#endif

/*  printf("updating max bounds %d for %s\n",i,comp->comp_name);
*/

  location = comp->coord[i] + comp->dim[i]/2.0;
    if (location > design->box_max[i])
      {
	design->box_max[i] = location;
	design->max_comp[i] = comp;
      }

#ifdef LOCATE
  printf("Leaving update_max_bounds\n");
#endif

}

/* ---------------------------------------------------------------------------------- */
/* This function gets component data from a file.                                     */
/* ---------------------------------------------------------------------------------- */
getdata(design)
struct Design *design;
{
  int i;
  double x_dim, y_dim, z_dim, tempcrit, q, k, pi;
  char name[MAX_NAME_LENGTH], type[MAX_NAME_LENGTH], shape[5];
  struct Component *comp_ptr;

  FILE *fptr;

  pi = 4.0*atan(1.0);
  design->first_comp = (struct Component *) malloc(sizeof(struct Component));
  comp_ptr = design->first_comp;

  fptr = fopen("datafile2", "r");
  if (fptr == NULL)
    {	
      printf("The file called \"datafile2\" does not exist or could not be opened.\n\n");
      exit();
    }

  printf("Reading container dimensions from file.\n");
  fscanf(fptr, "%lf%lf%lf%lf%lf%lf%lf%lf", &(design->container[0]),
	 &(design->container[1]), &(design->container[2]), &(design->kb), &(design->h[0]),
	 &(design->h[1]), &(design->h[2]), &(design->tamb));
  fclose(fptr);

  fptr = fopen("datafile1", "r");
  if (fptr == NULL)
    {	
      printf("The file called \"datafile1\" does not exist or could not be opened.\n\n");
      exit();
    }

  printf("Reading component data from file.\n");
  design->half_area = 0.0;
  design->volume = 0.0;
  design->mass = 0.0;
  i = 0;
  comp_ptr->prev_comp = NULL;
  while ((fscanf(fptr, "%s%s%lf%lf%lf%lf%lf%lf",name, shape, &x_dim, &y_dim, &z_dim, 
		 &tempcrit, &q, &k)) && !(feof(fptr))) {
    ++i;
    strcpy(comp_ptr->comp_name, name);
    strcpy(comp_ptr->shape_type, shape);
    comp_ptr->dim_initial[0] = x_dim;
    comp_ptr->dim_initial[1] = y_dim;
    comp_ptr->dim_initial[2] = z_dim;
    comp_ptr->tempcrit = tempcrit;
    comp_ptr->q = q;
    comp_ptr->k = k;
    comp_ptr->orientation = 0;
    if (comp_ptr->shape_type[0] == 'B')
      {
	comp_ptr->half_area = (x_dim * y_dim) + (y_dim * z_dim) + (x_dim * z_dim);
	comp_ptr->volume = x_dim * y_dim * z_dim;
	comp_ptr->mass = x_dim * y_dim * z_dim;
      }      
    else
      {
	comp_ptr->half_area = (pi * x_dim * x_dim / 4.0) + (pi * z_dim * x_dim / 2.0);
	comp_ptr->volume = pi * x_dim * x_dim * z_dim / 4.0;
	comp_ptr->mass = pi * x_dim * x_dim * z_dim / 4.0;
      }
    design->half_area += comp_ptr->half_area;
    design->volume += comp_ptr->volume;
    design->mass += comp_ptr->mass;
    comp_ptr->next_comp = (struct Component *) malloc(sizeof(struct Component));
    comp_ptr->next_comp->prev_comp = comp_ptr;
    comp_ptr = comp_ptr->next_comp;      
  }
  comp_ptr->prev_comp->next_comp = NULL;
  
  printf("EOF reached in the input file.\n\n");
  fclose(fptr);
  return (i);
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
