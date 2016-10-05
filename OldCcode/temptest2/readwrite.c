/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                    READWRITE.C                                      */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

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
  
  printf("\n\nSaving current design\n");
  fptr = fopen("output/design.data","w");
  i = 0;
  comp = design->first_comp;
  while (++i <= COMP_NUM)
    {
      fprintf(fptr, "%s %s\n", comp->comp_name, comp->shape_type);
      fprintf(fptr, "%lf %lf %lf\n", comp->dim[0], comp->dim[1], comp->dim[2]);
      fprintf(fptr, "%lf %lf %lf\n", comp->coord[0], comp->coord[1], comp->coord[2]);
      fprintf(fptr, "\t\t%lf\n", comp->temp);
      if (i < COMP_NUM)
	comp = comp->next_comp;
    }
  fclose(fptr);
}

/* ---------------------------------------------------------------------------------- */
/* This function writes container data to a file.                                        */
/* ---------------------------------------------------------------------------------- */
void save_container(design)
struct Design *design;
{
  int i, j;
  double avg_old_value;
  struct Component *comp;
  FILE *fptr;
  
  printf("Saving current container\n");
  
  fptr = fopen("output/container.data","w");
  fprintf(fptr, "%lf %lf %lf\n", design->box_min[0], design->box_max[0]);
  fprintf(fptr, "%lf %lf %lf\n", design->box_min[1], design->box_max[1]);
  fprintf(fptr, "%lf %lf %lf\n", design->box_min[2], design->box_max[2]);
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

  printf("Restoring comp dimensions and coordinates.\n");
  fptr = fopen("output/comp.data","r");
  i = 0;
  comp = design->first_comp;
  while (++i <= COMP_NUM)
    {
      fscanf(fptr, "%s %s", comp->comp_name, comp->shape_type);
      fscanf(fptr, "%lf %lf %lf", &(comp->dim[0]), &(comp->dim[1]), &(comp->dim[2]));
      fscanf(fptr, "%lf %lf %lf", &(comp->coord[0]), &(comp->coord[1]), &(comp->coord[2]));
      if (i < COMP_NUM)
	comp = comp->next_comp;
    }
  fclose(fptr);
  init_bounds(design);
  init_overlaps(design);
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
    if (design->tfield[k].coord[2] == 0.0)
	fprintf(fptr, "%lf %lf %lf\n", design->tfield[k].coord[0], 
		design->tfield[k].coord[1], design->tfield[k].temp);
    else 
	fprintf(fptr, "%lf %lf %lf %lf\n", design->tfield[k].coord[0], 
		design->tfield[k].coord[1], design->tfield[k].coord[2],
		design->tfield[k].temp);
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
    if (design->tfield[k].coord[2] == 0.0) 
	fscanf(fptr, "%lf %lf %lf\n", design->tfield[k].coord[0], 
		design->tfield[k].coord[1], design->tfield[k].temp);
    else 
	fscanf(fptr, "%lf %lf %lf %lf\n", &(design->tfield[k].coord[0]), 
		&(design->tfield[k].coord[1]), &(design->tfield[k].coord[2]),
		&(design->tfield[k].old_temp));
    ++k;
  }
  fclose(fptr);
}
