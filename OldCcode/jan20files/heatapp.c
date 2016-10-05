/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                    HEAT.C an Approximation                         */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* This function is the new fifth part of the objective function that deals with      */
/* the temperature of components.  If the component's temperature exceeds the         */
/* critical temperature of that component then a penalty will result.                 */
/* ---------------------------------------------------------------------------------- */
void eval_part_5(design)
struct Design *design;
{
  int i;
  struct Component *comp;
  double calc_thermal_penalty(), calc_thermal_matrix();
  double tempave;

/*  printf("Calculating Thermal Matrix\n");*/
  tempave = calc_thermal_matrix(design);
/*  printf("%.2f\n", tempave);*/
  design->new_obj_values[3] = 0.0;
  comp = design->first_comp;
  for (i = 1; i <= COMP_NUM; ++i) {
    design->new_obj_values[3] = calc_thermal_penalty(tempave, comp->tempcrit);
    if (i < COMP_NUM)
      comp = comp->next_comp;
  }
}

/* ---------------------------------------------------------------------------------- */
/* This function returns the value of the penalty function for a temperature in       */
/* excess of the critical temperature.                                                */
/* ---------------------------------------------------------------------------------- */
double calc_thermal_penalty(temp, tempcrit)
double temp, tempcrit;
{
  double value;

  value = 0.0;
  value = (temp*temp)/(tempcrit*tempcrit*COMP_NUM);
#ifdef MINIMIZE
  if (temp > tempcrit)
    value += (temp - tempcrit)*(temp - tempcrit)/(COMP_NUM);
#endif
  return(value);
}

/* ---------------------------------------------------------------------------------- */
/* This function finds the temperature at the center of each component and places     */
/* that value in comp->temp.  Because of the nature of this function it needs to      */
/* re-calculated for each iteration, instead of just being updated.                   */
/* ---------------------------------------------------------------------------------- */
double calc_thermal_matrix(design)
struct Design *design;
{
  struct Component *comp;
  double Tave, Rtot, Qtot = 0.0, Kave = 0.0, Have = 0.0;
  double box_x_dim, box_y_dim, box_z_dim, box_area, box_volume;
  int i;


  box_x_dim = design->box_max[0] - design->box_min[0];
  box_y_dim = design->box_max[1] - design->box_min[1];
  box_z_dim = design->box_max[2] - design->box_min[2];

  box_volume = box_x_dim * box_y_dim * box_z_dim;
  box_area = 2*(box_x_dim*box_y_dim + box_x_dim*box_z_dim + box_y_dim*box_z_dim);

  comp = design->first_comp;
  for (i = 1; i <= COMP_NUM; ++i) {
    Qtot += comp->q;
    Kave += (comp->k)/COMP_NUM;
    comp = comp->next_comp;
  }
  Kave = Kave*(design->volume/box_volume) + (design->kb)*(1 - design->volume/box_volume);

  Have = ((design->hx) + (design->hy) + (design->hz))/DIMENSION;

  Rtot = (box_area/(Kave*box_volume)) + 1/(Have*box_area);
  Tave = (design->tamb) + (Qtot*Rtot);

  return(Tave);
}

/* ---------------------------------------------------------------------------------- */
/* This function reverts to  the previous node temperatures if the new move was       */
/* rejected.                                                                          */
/* ---------------------------------------------------------------------------------- */
void revert_tfield(design)
struct Design *design;
{
  int k;

  for (k = 0; k < NODE_NUM; ++k)
    design->tfield[k].temp = design->tfield[k].old_temp;
}

/* ---------------------------------------------------------------------------------- */
/* This function backs up the current temperatures into old_temp if the step was      */
/* accepted.                                                                          */
/* ---------------------------------------------------------------------------------- */
void back_up_tfield(design)
struct Design *design;
{
  int k;

  for (k = 0; k < NODE_NUM; ++k)
    design->tfield[k].old_temp = design->tfield[k].temp;
}
