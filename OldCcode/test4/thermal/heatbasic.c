/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                    HEATBASIC.C                                     */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* This function is the new fifth part of the objective function that deals with      */
/* the temperature of components.  Actually, two terms are calculated based on the    */
/* component temperatures.  One is an objective that minimizes the overall temperature*/
/* of all components.  The other is a constraint penalty.  If the component's         */
/* temperature exceeds the critical temperature of that component then a penalty will */
/* result.                                                                            */
/* ---------------------------------------------------------------------------------- */
void heat_eval(design, steps_at_t, gen_limit) 
struct Design *design;
int steps_at_t, gen_limit;
{
    int correction;
    struct Component *comp;
    double calc_temp_penalty(), calc_cool_obj();
    void correct_APP_by_LU(), thermal_analysis_APP();
    void correct_SS_by_LU(), thermal_analysis_SS();
    void thermal_analysis_MM();

    correction = (steps_at_t - 1)%((int) (gen_limit/design->hcf_per_temp) + 1);

    switch (design->choice) {
	case 0:
	    /*if (correction == 0)
		correct_APP_by_LU(design);*/
	    thermal_analysis_APP(design);
	    break;
	case 1:
	    if (correction == 0)
		correct_SS_by_LU(design);
	    thermal_analysis_SS(design);
	    break;
	case 2:
	    if (correction == 0)
		correct_SS_by_LU(design);
	    thermal_analysis_SS(design);
	    break;
	case 3:
	    thermal_analysis_MM(design);
	    break;
	default:
	    printf("ERROR in Thermal Analysis Choice.");
	    exit();
    }
	
    design->new_obj_values[3] = 0.0;
    design->new_obj_values[4] = 0.0;
    comp = design->first_comp;
    while (comp != NULL) {
	design->new_obj_values[3] += calc_temp_penalty(comp->temp, comp->tempcrit);
	design->new_obj_values[4] += calc_cool_obj(comp->temp, comp->tempcrit);
	comp = comp->next_comp;
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function updates the heat parameters such as matrix tolerance and minimum     */
/* node spacing and switches between analysis methods.                                */
/* ---------------------------------------------------------------------------------- */
void update_heat_param(design, schedule, t)
struct Design *design;
struct Schedule *schedule;
double t;
{
    if ((t/(schedule->t_initial)) < design->analysis_switch[0]) {
	design->choice = 0;
	design->hcf_per_temp = 1;
    }
    if ((t/(schedule->t_initial)) < design->analysis_switch[1]) {
	design->choice = 1;
	design->hcf_per_temp = 1;
    }
    if ((t/(schedule->t_initial)) < design->analysis_switch[2]) {
	design->choice = 2;
	design->hcf_per_temp = 2;
    }
    if ((t/(schedule->t_initial)) < design->analysis_switch[3]) {
	design->choice = 3;
	design->hcf_per_temp = 1;
	design->gaussmove = 3.0;
    }
    if ((t/(schedule->t_initial)) < design->analysis_switch[4]) {
	design->choice = 3;
	design->tolerance = 0.0001;
	design->max_iter = 250;
	design->gaussmove = 0.6;
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function returns the value of the penalty function for a temperature in       */
/* excess of the critical temperature.                                                */
/* ---------------------------------------------------------------------------------- */
double calc_temp_penalty(temp, tempcrit)
double temp, tempcrit;
{
  double value = 0.0;

  if (temp > tempcrit) {
      value = (temp - tempcrit)*(temp - tempcrit)/(COMP_NUM);
  }
  return(value);
}

/* ---------------------------------------------------------------------------------- */
/* This function returns the value of the objective function for coolest              */
/* ---------------------------------------------------------------------------------- */
double calc_cool_obj(temp, tempcrit)
double temp, tempcrit;
{
  double value;

  value = (temp*temp)/(tempcrit*tempcrit*COMP_NUM);
  return(value);
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

/* ---------------------------------------------------------------------------------- */
/* This function initializes the heat parameters such as matrix tolerance and minimum */
/* node spacing.                                                                      */
/* ---------------------------------------------------------------------------------- */
void init_heat_param(design)
struct Design *design;
{
    design->tolerance = 0.001;
    design->min_node_space = 50.0;
    design->hcf = 0.1;
    design->gaussmove = 0.0;
    design->gauss = 0;
    design->hcf_per_temp = 4;
    design->max_iter = 100;
    design->choice = 0;
}

/* ---------------------------------------------------------------------------------- */
/* This function is performed after determining the sample space but before
/* the beginning of the annealing run.  The sser provides input into when to
/* switch between thermal anylses. */
/* ---------------------------------------------------------------------------------- */
void establish_thermal_changes(design)
struct Design *design;
{
  int i;
  
  printf("\nPlease define thermal anaylses changes.\n");
  printf("After how many temperature drops should switch to more exact Lumped Method?");
  scanf("%d", &i);
  design->analysis_switch[0] = pow(0.95, i);
  printf("After how many temperature drops should switch from Lumped Method to Sub-Space Method?");
  scanf("%d", &i);
  design->analysis_switch[1] = pow(0.95, i);
  printf("After how many temperature drops should switch to more exact Sub-Space Method?");
  scanf("%d", &i);
  design->analysis_switch[2] = pow(0.95, i);
  printf("After how many temperature drops should switch from Sub-Space Method to Matrix Method?");
  scanf("%d", &i);
  design->analysis_switch[3] = pow(0.95, i);
  printf("After how many temperature drops should switch to more exact Matrix Method?");
  scanf("%d", &i);
  design->analysis_switch[4] = pow(0.95, i);
}

