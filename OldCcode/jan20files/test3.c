/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                       TEST3.C                                      */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* SEARCH ALGORITHM TYPE (uncomment one)                                              */
/* ---------------------------------------------------------------------------------- */
#define SA

/* ---------------------------------------------------------------------------------- */
/* OBJECTIVE FUNCTION (uncomment one)                                                 */
/* NOTE: This version currently only supports the "BOTH" option because of the        */
/*       balancing functions.  Don't use the other options until adjustments are made */
/*       to take the balancing into account for non-BOTH options.                     */
/* ---------------------------------------------------------------------------------- */
#define BOTH

/* ---------------------------------------------------------------------------------- */
/* DEBUGGING FLAGS                                                                    */
/* DEBUG prints things out along the way.                                             */
/* MAKEMODEL makes an initial model and a couple along the way.                       */
/* LOCATE prints out messages when entering and leaving most functions.               */
/* OBJ_DATA writes objective function values to a file for all improvement steps.     */
/* TESTS performs consistency checks on the design at each annealing step.            */
/* OUTPUT prints out iteration number, step taken and acceptance information.         */
/* ---------------------------------------------------------------------------------- */
#define DEBUG
/*#define WAIT
*/
/*#define MAKEMODEL
*/
/*#define LOCATE
*/
/*#define OBJ_DATA
*/
/*#define TESTS
*/
/*#define OUTPUT
*/

/* ---------------------------------------------------------------------------------- */
/*                                  INCLUDE FILES                                     */
/* ---------------------------------------------------------------------------------- */
#include <math.h>
#include <stdio.h>
#include <time.h>
#include "constants.c"
#include "structs.c"
#include "schedule.c"
#include "anneal_alg.c"
#include "hustin.c"
#include "obj_function.c"
#include "obj_balance.c"
#include "readwrite.c"
#include "heat3D.c"

/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                       MAIN                                         */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */
main()
{
  void setseed(), writedata(), initializations(), anneal(), print_data();
  void sample_space(), downhill();
  void find_step();
  int comp_num, i, which, start_time, end_time;
  double eval, h, w, l;
  char wait;
  struct Design *design;
  struct Component *comp;
  FILE *fptr;

/*  BootNoodles();*/
  start_time = get_time();
  setseed();

  design = (struct Design *) malloc(sizeof(struct Design));

/* The component data is read in from a file and the number of components is returned */
  comp_num = getdata(design);
/*  printf("%d components were read in from the file.\n\n",comp_num);*/
  if (comp_num != COMP_NUM)
    {
      printf("The number of components read in is not the number expected.\n");
      exit();
    }

/*  find_step(design);
  exit();
*/
/* Call initialization functions. */
  initializations(design);

/*  printf("Sampling points in design space\n\n");*/
  sample_space(design);  /* IN SCHEDULE.C */

#ifdef WAIT
  printf("\nHit return to continue.\n\n");
  getchar(wait);
#endif

#ifdef BOTH
  printf("Problem set up as minimization of weighted sum of area_ratio and overlap.\n");
#endif

/*  printf("Ready to begin search using simulated annealing algorithm.\n\n");*/

#ifdef WAIT
  printf("\nHit return to continue.\n\n");
  getchar(wait);
#endif

  anneal(design);                        /* This function is in anneal_alg.c */
  save_design(design);
  save_tfield(design);
/*  printf("Begninning downhill search\n");
  downhill(design, MIN_MOVE_DIST);      */
  end_time = get_time();
  fptr = fopen("results","a");
  if (design->new_obj_values[1] != 0.0)
    {
      fprintf(fptr,"*** THE FINAL OVERLAP WAS NOT ZERO!!!\n");
      printf("*** THE FINAL OVERLAP WAS NOT ZERO!!!\n");
    }
  fprintf(fptr, "The elapsed time was %d seconds\n",(end_time - start_time));
  printf("The elapsed time was %d seconds\n",(end_time - start_time));
  fclose(fptr);
 
/*fptr = fopen("testfile2","r");
fscanf(fptr,"%lf %lf %lf",&h, &w, &l);
fclose(fptr);
fptr = fopen("testfile2","w");
if (h > 11.25)
  fprintf(fptr,"%lf %lf %lf\n",h-1.25, w, l);
else
  fprintf(fptr,"%lf %lf %lf\n",40.0, w-1.25, l);
fclose(fptr);*/
}

/* ---------------------------------------------------------------------------------- */
/* This function sets the seed for the random number generator.                       */
/* ---------------------------------------------------------------------------------- */
void setseed()
{
  int seconds;
  FILE *fptr;

  seconds = get_time();
/*  seconds = 784669576;*/
  printf("\nSetting seed for random number generator to %d.\n\n", seconds);
  srandom(seconds);

/*  fptr = fopen("seed","r");
  fscanf(fptr,"%d",&seconds);
*/
  srandom(seconds);
  
  fptr = fopen("output/seed.out","a");
  fprintf(fptr, "The seed is %d\n\n",seconds);
  fclose(fptr);
  fptr = fopen("results","a");
  fprintf(fptr,"\nThe seed is %d\n",seconds);
  fclose(fptr);
}

/* ---------------------------------------------------------------------------------- */
/* This function returns the current time in seconds.                                 */
/* ---------------------------------------------------------------------------------- */
get_time()
{
  int seconds;
  long longseconds;

  longseconds = time(NULL);
  seconds = longseconds;
  return(seconds);
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

  fptr = fopen("testfile2", "r");
  if (fptr == NULL)
    {	
      printf("The file called \"testfile2\" does not exist or could not be opened.\n\n");
      exit();
    }

  printf("Reading container dimensions from file.\n");
  fscanf(fptr, "%lf%lf%lf%lf%lf%lf%lf%lf", &(design->container[0]),
	 &(design->container[1]), &(design->container[2]), &(design->kb), &(design->hx),
	 &(design->hy), &(design->hz), &(design->tamb));
  fclose(fptr);

  fptr = fopen("testfile.3D", "r");
  if (fptr == NULL)
    {	
      printf("The file called \"testfile\" does not exist or could not be opened.\n\n");
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
/* This function calls several other initialization functions.                        */
/* ---------------------------------------------------------------------------------- */
void initializations(design)
struct Design *design;
{
  void init_locations(), init_bounds(), init_overlaps(), init_weights();

  design->tolerance = 0.000001;

  printf("Initializing locations.\n\n");
  init_locations(design);

  printf("Initializing box bounds.\n\n");
  init_bounds(design);

  printf("Initializing overlaps.\n\n");
  init_overlaps(design);          /* This function is in obj_function.c */

/* Initialize the objective function weights (currently to 1.0) */
  printf("Initializing weights.\n\n");
  init_weights(design);
}

/* ---------------------------------------------------------------------------------- */
/* This function sets the initial objective function weights to 1.0.                  */
/* ---------------------------------------------------------------------------------- */
void init_weights(design)
struct Design *design;
{
  int i;

  for (i = 0; i < OBJ_NUM; ++i)
    design->weight[i] = 1.0;
/*  design->weight[3] = 0.2;*/
}

/* ---------------------------------------------------------------------------------- */
/* This function initializes the component locations.                                 */
/* ---------------------------------------------------------------------------------- */
void init_locations(design)
struct Design *design;
{
  void update_dim();
  int i, j;
  double my_double_random();
  struct Component *temp_comp;

#ifdef LOCATE
  printf("Entering init_locations\n");
#endif

  i = 1;

  temp_comp = design->first_comp;
  while (i <= COMP_NUM)
    {
      temp_comp->orientation = my_random(1,6);
      temp_comp->orientation = 1;
      update_dim(temp_comp);
      j = -1;
      while (++j <= DIMENSION-1)
	temp_comp->coord[j] = my_double_random(-INITIAL_BOX_SIZE, INITIAL_BOX_SIZE);
      if (DIMENSION == 2)
	temp_comp->coord[2] = 0.0;
      printf("%d Dimensional Initial Placement\n", DIMENSION);
      if (i < COMP_NUM)
	temp_comp = temp_comp->next_comp;
      ++i;
    }

/* Set the initial max and min bounding box dimensions */
  i = 0;
  while (i <= 2)
    {
      design->box_min[i] = temp_comp->coord[i];
      design->box_max[i] = temp_comp->coord[i];
      ++i;
    }

#ifdef LOCATE
  printf("Leavinging init_locations\n");
#endif

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
/* This function returns a random integer between integers rndmin and rndmax.         */
/* ---------------------------------------------------------------------------------- */
my_random(rndmin, rndmax)
int rndmin, rndmax;
{
  int rnd;
  double rnddouble;
  
  do
    {
      rnddouble = random()/2147483649.;
      rnd = 1.*rndmin + rnddouble*(rndmax - rndmin + 1);
    }
  while (rnd < rndmin || rnd > rndmax);
  return (rnd);
}

/* ---------------------------------------------------------------------------------- */
/* This function returns a random integer between integers rndmin and rndmax.         */
/* ---------------------------------------------------------------------------------- */
double my_double_random(rndmin, rndmax)
double rndmin, rndmax;
{
  double rnd, rnddouble;
  do
    {
      rnddouble = random()/2147483649.;
      rnd = 1.*rndmin + rnddouble*(rndmax - rndmin);
    }
  while (rnd < rndmin || rnd > rndmax);
  return (rnd);
}

/* ---------------------------------------------------------------------------------- */
/* This function updates the x-y-z dimensions of a component based in its initial     */
/* dimensions and its current orientation.                                            */
/* ---------------------------------------------------------------------------------- */
void update_dim(comp)
struct Component *comp;
{
  switch(comp->orientation)
    {
    case 1:
      comp->dim[0] = comp->dim_initial[0];
      comp->dim[1] = comp->dim_initial[1];
      comp->dim[2] = comp->dim_initial[2];
      break;
    case 2:
      comp->dim[0] = comp->dim_initial[0];
      comp->dim[1] = comp->dim_initial[2];
      comp->dim[2] = comp->dim_initial[1];
      break;
    case 3:
      comp->dim[0] = comp->dim_initial[1];
      comp->dim[1] = comp->dim_initial[0];
      comp->dim[2] = comp->dim_initial[2];
      break;
    case 4:
      comp->dim[0] = comp->dim_initial[1];
      comp->dim[1] = comp->dim_initial[2];
      comp->dim[2] = comp->dim_initial[0];
      break;
    case 5:
      comp->dim[0] = comp->dim_initial[2];
      comp->dim[1] = comp->dim_initial[0];
      comp->dim[2] = comp->dim_initial[1];
      break;
    case 6:
      comp->dim[0] = comp->dim_initial[2];
      comp->dim[1] = comp->dim_initial[1];
      comp->dim[2] = comp->dim_initial[0];
      break;
    default:
      printf("\nCase error in update_dim\n");
      exit();
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function updates the max and min x, y and z bounds for the bounding box.      */
/* ---------------------------------------------------------------------------------- */
void update_bounds(design, comp)
struct Design *design;
struct Component *comp;
{
  void update_min_bounds(), update_max_bounds();
  int i, j;
  struct Component *temp_comp;
  char wait;

#ifdef LOCATE
  printf("Entering update_bounds\n");
#endif

/* First test to see if we are moving the min_comp.  If we are, not, we just update   */
/* min bounds for this component.  If we are, we have to update bounds for all the    */
/* elements to find the new one (which may the the same as the current one).  To      */
/* correctly update the bounds, we reset the box_min (since we've moved the min_comp  */
/* the old value is no longer valid).                                                 */
  i = -1;
  while (++i <= 2)
    {

/*      printf("i = %d\n",i);
      printf("The min and max comps are\n%s, %s, %s\n%s, %s, %s\n",design->min_comp[0]->comp_name,
	 design->min_comp[1]->comp_name,design->min_comp[2]->comp_name,
	 design->max_comp[0]->comp_name,design->max_comp[1]->comp_name,
	 design->max_comp[2]->comp_name);
*/

      if (comp != design->min_comp[i])
	update_min_bounds(design, comp, i);
      else
	{
/*	  printf("Min comp may have changed - recomputing min bounds\n");
*/
	  design->box_min[i] = comp->coord[i];
	  j = 0;
	  temp_comp = design->first_comp;
	  while (++j <= COMP_NUM)
	    {
	      update_min_bounds(design, temp_comp, i);
	      if (j < COMP_NUM)
		temp_comp = temp_comp->next_comp;
	    }
	}
    }

/* Now do the same for the max_comp. */
  i = -1;
  while (++i <= 2)
    {
      if (comp != design->max_comp[i])
	update_max_bounds(design, comp, i);
      else
	{
/*	  printf("Max comp may have changed - recomputing max bounds\n");
*/
	  design->box_max[i] = comp->coord[i];
	  j = 0;
	  temp_comp = design->first_comp;
	  while (++j <= COMP_NUM)
	    {
	      update_max_bounds(design, temp_comp, i);
	      if (j < COMP_NUM)
		temp_comp = temp_comp->next_comp;
	    }
	}
    }

#ifdef LOCATE
  printf("Leaving update_bounds\n");
#endif

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
/* This function was used to test the updating routines.                              */
/* ---------------------------------------------------------------------------------- */
/*void test(design)
struct Design *design
{
  printf("The min and max comps are\n%s, %s, %s\n%s, %s, %s\n",design->min_comp[0]->comp_name,
	 design->min_comp[1]->comp_name,design->min_comp[2]->comp_name,
	 design->max_comp[0]->comp_name,design->max_comp[1]->comp_name,
	 design->max_comp[2]->comp_name);
 printf("Hit return to continue.\n\n");
  getchar(wait);
  design->first_comp->coord[1] -= 20.0;
  update_bounds(design, design->first_comp);
  printf("The min and max comps are\n%s, %s, %s\n%s, %s, %s\n",design->min_comp[0]->comp_name,
	 design->min_comp[1]->comp_name,design->min_comp[2]->comp_name,
	 design->max_comp[0]->comp_name,design->max_comp[1]->comp_name,
	 design->max_comp[2]->comp_name);
 printf("Hit return to continue.\n\n");
  getchar(wait);
  design->first_comp->coord[1] += 20.0;
  update_bounds(design, design->first_comp);
  printf("The min and max comps are\n%s, %s, %s\n%s, %s, %s\n",design->min_comp[0]->comp_name,
	 design->min_comp[1]->comp_name,design->min_comp[2]->comp_name,
	 design->max_comp[0]->comp_name,design->max_comp[1]->comp_name,
	 design->max_comp[2]->comp_name);
}
*/

/* ---------------------------------------------------------------------------------- */
/* This function calculates the center of gravity.                                    */
/* ---------------------------------------------------------------------------------- */
void calc_c_grav(design)
struct Design *design;
{
  int i, j;
  double sum[3], mass;
  struct Component *comp;

  mass = 0.0;
  sum[0] = 0.0;
  sum[1] = 0.0;
  sum[2] = 0.0;

  i = 0;
  comp = design->first_comp;
  while (++i <= COMP_NUM)
    {
      mass += comp->mass;
      j = -1;
      while (++j <= 2)
	sum[j] += comp->mass * comp->coord[j];
      if (i < COMP_NUM)
	comp = comp->next_comp;
    }
  i = -1;
  while (++i <= 2)
    design->c_grav[i] = sum[i]/mass;
}

/* ---------------------------------------------------------------------------------- */
/* This function finds a good downhill step size.                                     */
/* ---------------------------------------------------------------------------------- */
void find_step(design)
struct Design *design;
{
  double move_size, eval, min_eval;
  FILE *fptr;

  move_size = 0.05;
  min_eval = 5.;
  fptr = fopen("size.dat","w");
  while (move_size <= 1.25)
    {
      fprintf(fptr,"%lf",move_size);
      restore_design(design);
      downhill(design, move_size);
      eval = evaluate(design);
      fprintf(fptr," %lf\n",eval);
      if (eval < min_eval)
	min_eval = eval;
      move_size += .05;
    }
  fprintf(fptr,"THE MIN WAS %lf",min_eval);
  fclose(fptr);
}

