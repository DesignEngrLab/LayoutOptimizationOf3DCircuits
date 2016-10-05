/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                    HEAT.C in 3-D!                                  */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* This function is the new fifth part of the objective function that deals with      */
/* the temperature of components.  If the component's temperature exceeds the         */
/* critical temperature of that component then a penalty will result.                 */
/* ---------------------------------------------------------------------------------- */
void heat_eval(design) 
struct Design *design;
{
  int i;
  struct Component *comp;
  double calc_temp_penalty(), calc_cool_obj();
  void calc_thermal_matrix_app(), calc_thermal_matrix_gauss(), calc_thermal_matrix_LU();

  if ((design->gauss) && (design->small_move)) {
      calc_thermal_matrix_gauss(design);      
      design->small_move = 0;
  }
  else if (design->gauss) {
    calc_thermal_matrix_LU(design);
  }
  else calc_thermal_matrix_app(design);
  /*calc_thermal_matrix_LU(design);*/
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
/* This function corrects the approximation method by comparing it to the LU method.  */
/* The value for design->hcf (heat correction factor) will be used by the app_method. */
/* ---------------------------------------------------------------------------------- */
void correct_by_LU(design)
struct Design *design;
{
    void calc_thermal_matrix_app(), calc_thermal_matrix_LU();
    struct Component *comp;
    double tempapp, tempLU;
    double calc_temp_penalty(), calc_cool_obj();
    
    tempLU = 0.0;
    design->new_obj_values[3] = 0.0;
    design->new_obj_values[4] = 0.0;
    design->hcf = 1.0;
    
    calc_thermal_matrix_app(design);
    tempapp = design->first_comp->temp;
    
    calc_thermal_matrix_LU(design);
    comp = design->first_comp;
    while (comp != NULL) {
	/*tempLU += comp->temp/COMP_NUM;*/
	if (tempLU < comp->temp) tempLU = comp->temp; 
	design->new_obj_values[3] += calc_temp_penalty(comp->temp, comp->tempcrit);
	design->new_obj_values[4] += calc_cool_obj(comp->temp, comp->tempcrit);
    comp = comp->next_comp;
    }
   
    design->hcf = (tempLU - design->tamb)/(tempapp - design->tamb);
    /*printf("Heat Correction Factor = %.2f\n", design->hcf);*/
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
/* This function finds the temperature at the center of each component and places     */
/* that value in comp->temp.  Because of the nature of this function it needs to      */
/* re-calculated for each iteration, instead of just being updated.                   */
/* ---------------------------------------------------------------------------------- */
void calc_thermal_matrix_app(design)
struct Design *design;
{
  struct Component *comp;
  double Tave, Rtot, Qtot = 0.0, Kave = 0.0, Have = 0.0;
  double box_x_dim, box_y_dim, box_z_dim, box_area, box_volume;
  int i;

  /*printf("APP   ");*/
  box_x_dim = design->box_max[0] - design->box_min[0];
  box_y_dim = design->box_max[1] - design->box_min[1];
  box_z_dim = design->box_max[2] - design->box_min[2];

  box_volume = box_x_dim * box_y_dim * box_z_dim;
  box_area = 2*(box_x_dim*box_y_dim + box_x_dim*box_z_dim + box_y_dim*box_z_dim);

  comp = design->first_comp;
  while (comp != NULL) {
    Qtot += comp->q;
    Kave += (comp->k)/COMP_NUM;
    comp = comp->next_comp;
  }
  Kave = Kave*(design->volume/box_volume) + 
	(design->kb)*(1 - design->volume/box_volume);

  Have = ((design->h[0]) + (design->h[1]) + (design->h[2]))/DIMENSION;

/*  Rtot = (box_area/(Kave*box_volume)) + 1/(Have*box_area);*/
  Rtot = ((box_x_dim + box_y_dim + box_z_dim)/(Kave*box_area)) + 1/(Have*box_area);
  Tave = (design->tamb) + design->hcf*(Qtot*Rtot);
  comp = design->first_comp;
  while (comp != NULL) {
    comp->temp = Tave;
    comp = comp->next_comp;
  }
}

/* ---------------------------------------------------------------------------------- */
/* This function finds the temperature at the center of each component and places     */
/* that value in comp->temp.  Because of the nature of this function it needs to      */
/* re-calculated for each iteration, instead of just being updated.                   */
/* ---------------------------------------------------------------------------------- */
void calc_thermal_matrix_gauss(design)
struct Design *design;
{
  struct Component *comp;
  double *flux, **R;
  void set_up_tfield(), set_up_flux(), set_up_coef_matrix();
  void gauss_seidel(), find_comp_temp(), find_contained_nodes();
  int i, j, node_dim[DIMENSION], tot_nodes, hbw, width;

  printf("GS   ");
  set_up_tfield(design, node_dim);
  hbw = node_dim[1]*node_dim[2];
  width = 2*hbw + 1;
  tot_nodes = node_dim[0]*hbw;
  
  find_contained_nodes(design, hbw, node_dim[2]);

  /*  These commands set up the flux vector and R matrix using dynamic memory */
  /*  allocation.                                                             */
  flux = (double *) malloc(sizeof (double) * tot_nodes); 
  for (i = 0; i < tot_nodes; i++)
    flux[i] = 0.0;
  R = (double **) malloc(sizeof (double *) * tot_nodes);
  for (i = 0; i < tot_nodes; i++) {
    R[i] = (double *) malloc(sizeof(double) * width);
    for (j = 0; j < width; ++j)
      R[i][j] = 0.0;
  }
  set_up_flux(design, flux, tot_nodes);
  set_up_coef_matrix(design, flux, R, tot_nodes, hbw, node_dim[2]);
  gauss_seidel(design, R, flux, tot_nodes, hbw, node_dim[2]);
  find_comp_temp(design);
  
  for (i = 0; i < tot_nodes; i++)
    free(R[i]);
  free(R);
  free(flux);
}

/* ---------------------------------------------------------------------------------- */
/* This function finds the temperature at the center of each component and places     */
/* that value in comp->temp.  Because of the nature of this function it needs to      */
/* re-calculated for each iteration, instead of just being updated.                   */
/* ---------------------------------------------------------------------------------- */
void calc_thermal_matrix_LU(design)
struct Design *design;
{
  double *flux, **R;
  void set_up_tfield(), set_up_flux(), set_up_coef_matrix();
  void LU_Decomp(), find_comp_temp(), find_contained_nodes();
  int i, j, node_dim[DIMENSION], tot_nodes, hbw, width;

  printf("LU   ");
  set_up_tfield(design, node_dim);
  hbw = node_dim[1]*node_dim[2];
  width = 2*hbw + 1;
  tot_nodes = node_dim[0]*hbw;
  
  find_contained_nodes(design, hbw, node_dim[2]);

  /*  These commands set up the flux vector and R matrix using dynamic memory */
  /*  allocation.                                                             */
  flux = (double *) malloc(sizeof (double) * tot_nodes); 
  for (i = 0; i < tot_nodes; i++)
    flux[i] = 0.0;
  R = (double **) malloc(sizeof (double *) * tot_nodes);
  for (i = 0; i < tot_nodes; i++) {
    R[i] = (double *) malloc(sizeof(double) * width);
    for (j = 0; j < width; ++j)
      R[i][j] = 0.0;
  }
  set_up_flux(design, flux, tot_nodes);
  set_up_coef_matrix(design, flux, R, tot_nodes, hbw, node_dim[2]);
  LU_Decomp(design, R, flux, tot_nodes, hbw);
  find_comp_temp(design);
  
  for (i = 0; i < tot_nodes; i++)
    free(R[i]);
  free(R);
  free(flux);
}

/* ---------------------------------------------------------------------------------- */
/* This function initializes the temperature field.  This field is a vector not a     */
/* matrix.  This is because it corresponds to the x in Ax = b.  The comp in the       */
/* tfield structure refers to the number of the component in the list.  If zero, then */
/* corresponds to a simple resistor junction.                                         */
/* ---------------------------------------------------------------------------------- */
void set_up_tfield(design, node_dim)
struct Design *design;
int node_dim[];
{
  struct Component *comp;
  double xx[3][NODE_NUM], fringe[3];
  int not_duplicate(); 
  void picksort(), refinemesh(), find_if_comp_center();
  int i[3], j, k, m;

  for (m = 0; m < DIMENSION; m++) {
      i[m] = 0;
  }
  comp = design->first_comp;
  while (comp != NULL) {
    for (m = 0; m < DIMENSION; m++) {
	if (not_duplicate(comp->coord[m], xx[m], i[m]))
	    xx[m][++i[m]] = comp->coord[m];
    }
    comp = comp->next_comp;
  }
  for (m = 0; m < DIMENSION; m++){
    node_dim[m] = i[m];
    picksort(node_dim[m], xx[m]);
  }

#ifdef SFRINGE
    for (m = 0; m < DIMENSION; m++) {
	fringe[m] = SFRINGE;
    }
#endif

#ifdef CFRINGE
    for (m = 0; m < DIMENSION; m++) {
	fringe[m] = (design->container[m] - (design->box_max[m] - design->box_min[m]))/2;
	if (fringe[m] < 0)
	    fringe[m] = CFRINGE;
    }
#endif

  for (m = 0; m < DIMENSION; m++) {
      xx[m][0] = design->box_min[m] - fringe[m];
      xx[m][(++node_dim[m])] = design->box_max[m] + fringe[m];
      refinemesh(node_dim, xx[m], m, design->min_node_space);
      ++node_dim[m];
  }
  printf("%d %d %d\n", node_dim[0], node_dim[1], node_dim[2]);
/* Put coordinates and component number in each node.                                 */
  k = 0;
  for (i[0] = 0; i[0] < node_dim[0]; ++i[0]) {
    for (i[1] = 0; i[1] < node_dim[1]; ++i[1]) {
      for (i[2] = 0; i[2] < node_dim[2]; ++i[2]) {
	for (m = 0; m < DIMENSION; m++) {
	    design->tfield[k].coord[m] = xx[m][i[m]];
	}
	design->tfield[k].comp = NULL;
	find_if_comp_center(design->first_comp, design->tfield, k);
	++k;
    }
   }
  }
}

/* ---------------------------------------------------------------------------------- */
/* This function finds if there already is such a coordinate in the array.            */
/* It returns TRUE if no duplicates and FALSE if duplicates                           */
/* ---------------------------------------------------------------------------------- */
not_duplicate(num, arr, n)
double num, arr[];
int n;
{
  int m;
  
  for (m = 1; m <= n; ++m) {
    if (fabs(num - arr[m]) < CLOSE_NODE)
      return(0);
  }
  return(1);
}

/* ---------------------------------------------------------------------------------- */
/* This function is pick sort from NUMERICAL RECIPES in C.  It sorts the arrays in    */
/* ascending order.                                                                   */
/* ---------------------------------------------------------------------------------- */
void picksort(n, arr)
int n;
double arr[];
{
  int i, j;
  double a;

  for (j = 2; j <= n; j++) {
    a = arr[j];
    i = j - 1;
    while (i > 0 && arr[i] > a) {
      arr[i+1] = arr[i];
      i--;
    }
    arr[i+1] = a;
  }
}

/* ---------------------------------------------------------------------------------- */
/* This function adds nodes in gaps where the space is bigger than MIN_NODE_SPACE.    */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */
void refinemesh(node_dim, arr, vector, min_node_space)
int vector, node_dim[];
double min_node_space, arr[];
{
  int i, j;

  i = node_dim[vector];
  while ((i > 0) && (1 + node_dim[vector]  < NODE_NUM)) {
    if ((arr[i] - arr[i-1]) > min_node_space) {
      ++node_dim[vector];
      for (j = node_dim[vector]; j > i; --j)
	arr[j] = arr[j-1];
      arr[i] = 0.5*(arr[i+1] - arr[i-1]) + arr[i-1];
      i += 2;
    }
    --i;
  }
}

/* ---------------------------------------------------------------------------------- */
/* This function returns the index of the component specified by the coordinates      */
/* x and y.  Returns zero if no such component.                                       */
/* ---------------------------------------------------------------------------------- */
void find_if_comp_center(first_comp, tfield, k)
struct Component *first_comp;
struct Temperature_field tfield[];
int k;
{
  struct Component *comp;

  comp = first_comp;
  while (comp != NULL) {
    if ((fabs(comp->coord[0] - tfield[k].coord[0]) < CLOSE_NODE) && 
	(fabs(comp->coord[1] - tfield[k].coord[1]) < CLOSE_NODE)&&
        (fabs(comp->coord[2] - tfield[k].coord[2]) < CLOSE_NODE)) {
	    comp->node_center = k;
	    return;
	}
    comp = comp->next_comp;
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function finds all the nodes that are contained in components and inner       */
/* component heat sources.                                                            */
/* ---------------------------------------------------------------------------------- */
void find_contained_nodes(design, hbw, znodes)
struct Design *design;
int hbw, znodes;
{
    int k;
    struct Component *comp;
    void find_neighbors();

    comp = design->first_comp;
    while (comp != NULL) {
	design->tfield[comp->node_center].comp = comp;
	comp->nodes = 1;
	find_neighbors(design->tfield, comp, comp->node_center, &comp->nodes, 
		       hbw, znodes, 0);
	comp = comp->next_comp;
	}
}

/* ---------------------------------------------------------------------------------- */
/* This recursive function finds which neighboring nodes are contained within the     */
/* component.                                                                         */
/* ---------------------------------------------------------------------------------- */
void find_neighbors(tfield, comp, k, n, hbw, znodes, from)
struct Temperature_field tfield[];
struct Component *comp;
int k, *n, hbw, znodes, from;
{
    /* This will check neighbors to the west so long as it didn't come FROM the west.*/
    if ((from != -1) &&
	((fabs(tfield[k-hbw].coord[0] - comp->coord[0])) < (comp->dim[0]/2)) &&
	(tfield[k-hbw].comp == NULL))  {
	    tfield[k-hbw].comp = comp;
	    ++*n;
	    find_neighbors(tfield, comp, k-hbw, n, hbw, znodes, 1);
    }		
    /* Checks to the east. */
    if ((from != 1) &&
	((fabs(tfield[k+hbw].coord[0] - comp->coord[0])) < (comp->dim[0]/2)) &&
	(tfield[k+hbw].comp == NULL)) {
	    tfield[k+hbw].comp = comp;
	    ++*n;
	    find_neighbors(tfield, comp, k+hbw, n, hbw, znodes, -1);
    }
    /* Checks to the south. */
    if ((from != -2) &&
	((fabs(tfield[k-znodes].coord[1] - comp->coord[1])) < (comp->dim[1]/2)) &&
	(tfield[k-znodes].comp == NULL)) {
	    tfield[k-znodes].comp = comp;
	    ++*n;
	    find_neighbors(tfield, comp, k-znodes, n, hbw, znodes, 2);
    }
    /* Checks to the north. */
    if ((from != 2) &&
	((fabs(tfield[k+znodes].coord[1] - comp->coord[1])) < (comp->dim[1]/2)) &&
	(tfield[k+znodes].comp == NULL)) {
	    tfield[k+znodes].comp = comp;
	    ++*n;
	    find_neighbors(tfield, comp, k+znodes, n, hbw, znodes, -2);
    }
    /* Checks down. */
    if ((from != -3) &&
	((fabs(tfield[k-1].coord[2] - comp->coord[2])) < (comp->dim[2]/2)) &&
	(tfield[k-1].comp == NULL)) {
	    tfield[k-1].comp = comp;
	    ++*n;
	    find_neighbors(tfield, comp, k-1, n, hbw, znodes, 3);
    }
    /* Checks up. */
    if ((from != 3) && 
	((fabs(tfield[k+1].coord[2] - comp->coord[2])) < (comp->dim[2]/2)) &&
	(tfield[k+1].comp == NULL)) {
	    tfield[k+1].comp = comp;
	    ++*n;
	    find_neighbors(tfield, comp, k+1, n, hbw, znodes, -3);
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function initializes the heat flux vector.  This vector corresponds to the    */
/* b in Ax = b.  If no component at node then it equals zero, however if there is     */
/* a component at the node then it equals comp->q divided by the number of nodes      */
/* inside the component.                                                              */
/* ---------------------------------------------------------------------------------- */
void set_up_flux(design, flux, tot_nodes)
struct Design *design;
double *flux;
int tot_nodes;
{
  struct Component *comp;
  int k;
  
  comp = design->first_comp;
/*  flux[(comp->node_center)] = comp->q;*/
  while (comp != NULL) {
      for (k = 0; k < tot_nodes; k++) {
	  if (design->tfield[k].comp == comp)
	    flux[k] = comp->q/comp->nodes;
      }
      comp = comp->next_comp;
  }
}

/* ---------------------------------------------------------------------------------- */
/* This function sets up the cooefficient matrix R.  This matrix corresponds to the   */
/* A in Ax = b.  Calculations of all the thermal resistances and boundary conditions  */
/* are done in this subroutine.                                                       */
/* ---------------------------------------------------------------------------------- */
void set_up_coef_matrix(design, flux, R, tot_nodes, hbw, znodes)
struct Design *design;
int tot_nodes, hbw, znodes;
double *flux, **R;
{
  int k, i;
  double n, e, w, s, u, d;
  void calc_resistances();

/*  Go through each node to fill in each row of the R-matrix  */
    for (k = 0; k < tot_nodes; ++k) {
/* Find dimension n, e, w, s, u, d to surrounding points (0 if boundary).   */
	if ((k-hbw) >= 0) {
	    w = design->tfield[k].coord[0] - design->tfield[(k-hbw)].coord[0];
	}
	else w = 0.0;

	if ((k+hbw) < tot_nodes) {
	    e = design->tfield[(k+hbw)].coord[0] - design->tfield[k].coord[0];
	}
	else e = 0.0;

	if ((k % hbw) >= znodes) {
	    s = design->tfield[k].coord[1] - design->tfield[k-znodes].coord[1];
	}
	else s = 0.0;

	if (((k+znodes) % hbw) >= znodes) {
	    n = design->tfield[k+znodes].coord[1] - design->tfield[k].coord[1];
	}
	else n = 0.0;

	if ((k % znodes) != 0) {
	    d = design->tfield[k].coord[2] - design->tfield[k-1].coord[2];
	}
	else d = 0.0;

	if (((k+1) % znodes) != 0) {
	    u = design->tfield[k+1].coord[2] - design->tfield[k].coord[2];
	}
	else u = 0.0;
	
	calc_resistances(design, flux, R, hbw, ((u + d)*(n + s)), w, k, -hbw, 0);
	calc_resistances(design, flux, R, hbw, ((u + d)*(n + s)), e, k, hbw, 0);
	calc_resistances(design, flux, R, hbw, ((u + d)*(e + w)), s, k, -znodes, 1);
	calc_resistances(design, flux, R, hbw, ((u + d)*(e + w)), n, k, znodes, 1);
	calc_resistances(design, flux, R, hbw, ((n + s)*(e + w)), d, k, -1, 2);
	calc_resistances(design, flux, R, hbw, ((n + s)*(e + w)), u, k, 1, 2);
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function calculates the resistances between points.  The reason this function */
/* is so complicated is that a connection between a node can either be in open space, */
/* one node contained within a component, both nodes within separate components, or   */
/* both nodes within the same component.                                              */
/* ---------------------------------------------------------------------------------- */
void calc_resistances(design, flux, R, hbw, area, x, k, step, dir)
struct Design *design;
double *flux, **R, area, x;
int k, step, dir, hbw;
{
    double xc, nx, kc, kn;
    int i;
        
    if (x == 0.0) {
      flux[k] += 0.25*(design->tamb)*(design->h[dir])*area;
      R[k][hbw] += 0.25*(design->h[dir])*area;
    }
    else {
	if (design->tfield[k].comp != NULL) {
	    kc = design->tfield[k].comp->k;
	    xc = (design->tfield[k].comp->dim[dir]/2) - fabs(design->tfield[k].comp->coord[dir] - 
		design->tfield[k].coord[dir]);
	}
	else {
	    kc = design->kb;
	    xc = 0.0;
	}
	if (design->tfield[k+step].comp != NULL) {
	    kn = design->tfield[k+step].comp->k;
	    nx = (design->tfield[k+step].comp->dim[dir]/2) - fabs(design->tfield[k+step].comp->coord[dir] - 
		design->tfield[k+step].coord[dir]);
	}
	else {
	    kn = design->kb;
	    nx = 0.0;
	}
/* These two 'if' statements account for the chance that the node and the */
/* neighboring node might be in the same component.                       */
	/*printf("%f  %f  %f \n", x, xc, nx);*/
	if (x + nx <= xc) {
	    xc = x;
	    nx = 0.0;
	}
	if (x + xc <= nx) {
	    nx = x;
	    xc = 0.0;
	}
	x -= xc + nx;
	if (x < 0.0) x = 0.0;
/* These resistances are actually 'admittances' and therefore the total resistance   */
/* is the reciprocal of the sum of the reciprocals.                                  */
	R[k][hbw] += (0.25*(design->kb)*kc*kn*area)/(((design->kb)*kn*xc) +
		     (kc*kn*x) + (kc*(design->kb)*nx));
	R[k][(hbw + step)] = -(0.25*(design->kb)*kc*kn*area)/
			(((design->kb)*kn*xc) + (kc*kn*x) + (kc*(design->kb)*nx));
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function puts the temperatures of the node that lie on components into the    */
/* the component data structure for analysis by eval_part_5.                          */
/* ---------------------------------------------------------------------------------- */
void find_comp_temp(design)
struct Design *design;
{
  struct Component *comp;
  int i = 0;
  
  comp = design->first_comp;
  while (comp != NULL) {
    comp->temp = design->tfield[comp->node_center].temp;
    /*printf("Component %d temperature = %.2f\n", ++i, comp->temp);*/
    comp = comp->next_comp;
  }
}

/* ---------------------------------------------------------------------------------- */
/* This is the matrix solver for the vector of nodes in tfield.  It uses LU decomp.   */
/* ---------------------------------------------------------------------------------- */
void LU_Decomp(design, R, flux, n, hbw)
struct Design *design;
double **R, *flux;
int n, hbw;
{
  int i, j, k, c = hbw;
  double sum;

/*   L-U Decomposition of Banded Matrix; returning values to the R banded matrix  */
  for (i = 1; i <= hbw; ++i)
    R[0][c + i] /= R[0][c];

  for (k = 1; k < (n-1); ++k) {
    for (i = 0; i <= hbw; ++i) {
      if ((k+i) < n) {
	sum = R[k+i][c-i];
	for (j = 1; j <= (hbw-i); ++j)
	  if (j <= k) sum -= R[k-j][c+j]*R[k+i][c-i-j];
	R[k+i][c-i] = sum;
      }
    }
    for (i = 1; i <= hbw; ++i) {
      sum = R[k][c+i];
      for (j = 1; j <= (hbw-i); ++j)
        if (j <= k) sum -= R[k-j][c+j+i]*R[k][c-j];	   
      R[k][c + i] = sum/R[k][c];
    }
  }
  sum = R[n-1][c];
  for (i = 1; i <= hbw; ++i)
    sum -= R[n-1][c-i]*R[n-1-i][c+i];
  R[n-1][c] = sum;

/*   L-U Back-Substition to get temperatures.   */
  for (k = 0; k < n; ++k) {
    sum = flux[k];
    for (i = 1; i <= hbw; ++i)
      if (i <= k) sum -= R[k][c-i]*flux[k-i];
    flux[k] = sum/R[k][c];
  }
  for (k = n-1; k >= 0; --k) {
    sum = flux[k];
    for (i = 1; i <= hbw; ++i)
      if ((k+i) < n) sum -= R[k][c+i]*(design->tfield[k+i].temp);
    design->tfield[k].temp = sum;
  }
}

/* ---------------------------------------------------------------------------------- */
/* This is an iterative matrix solver for the vector of nodes in tfield.              */
/* ---------------------------------------------------------------------------------- */
void gauss_seidel(design, R, flux, tot_nodes, hbw, znodes)
struct Design *design;
double **R, *flux;
int tot_nodes, hbw;
{
    int i, k, pos[] = {1, 0, 0}, iter = 0;
    double tol, rtot;
    void get_initial_guess();

    pos[1] = znodes;
    pos[2] = hbw;

    get_initial_guess(design, tot_nodes, hbw);
 
    /*      Guass - Seidel iteration with SOR      */
    do {
      ++iter;
      tol = 0.0;
      for (k = 0; k < tot_nodes; ++k) {
	design->tfield[k].prev_temp = design->tfield[k].temp;
	rtot = flux[k];
	for (i = 0; i <= 2; ++i) {
	    if ((k-pos[i]) >= 0) {
		rtot -= R[k][hbw - pos[i]]*design->tfield[k - pos[i]].temp;
	    }
	    if ((k+pos[i]) < tot_nodes) {
		rtot -= R[k][hbw + pos[i]]*design->tfield[k + pos[i]].temp;
	    }
	}
	design->tfield[k].temp = rtot/(R[k][hbw]);
	/*   SOR   */
	design->tfield[k].temp = OMEGA*design->tfield[k].temp +
				(1 - OMEGA)*design->tfield[k].prev_temp;
	/* Absolute Tolerance tabulation. */
	tol += fabs(design->tfield[k].temp - design->tfield[k].prev_temp)/tot_nodes;
	}
	/*printf("Iteration %d: Component #1 temperature = %.2f  %f\n", iter, 
	design->tfield[0].temp, tol);*/
    } while ((tol > design->tolerance) && (iter < design->max_iter));
    /*printf("Iterations = %d: Component #1 temperature = %.2f  %f\n", iter, 
	design->tfield[design->first_comp->node_center].temp, tol);*/
}

/* ---------------------------------------------------------------------------------- */
/* This is function establishes the intial guesses used by Gauss-Seidel.  These       */
/* are from the previous iteration.                                                   */
/* ---------------------------------------------------------------------------------- */
void get_initial_guess(design, tot_nodes, hbw)
struct Design *design;
int tot_nodes, hbw;
{
    int k;
    struct Component *comp;
    
    for (k = 0; k < tot_nodes; k++) {
	design->tfield[k].temp = design->tfield[k].old_temp;
    }
    comp = design->first_comp;
    while (comp != NULL) {
	design->tfield[comp->node_center].temp = comp->temp;
	comp = comp->next_comp;
	}
}

/* ---------------------------------------------------------------------------------- */
/* This function initializes the heat parameters such as matrix tolerance and minimum */
/* node spacing.                                                                      */
/* ---------------------------------------------------------------------------------- */
void init_heat_param(design)
struct Design *design;
{
    design->tolerance = 0.001;
    design->max_iter = 100;
    design->min_node_space = 50.0;
    design->hcf = 0.1;
    design->hcf_per_temp = 4;
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
    if (t/schedule->t_initial < 0.00005) {
	design->hcf_per_temp = 10;
    }
    if (t/schedule->t_initial < 0.00005) {
	design->hcf_per_temp = 1;
	design->weight[3] = 0.3;
	design->weight[2] = 0.3;
	design->maxmove = 5.0;
	design->gauss = 1;
    }
    if (t/schedule->t_initial < 0.000005) {
	design->weight[3] = 1;
	design->weight[2] = 1;
	design->maxmove = 1.0;
    }
    if (t/schedule->t_initial < 0.000001) {
	design->tolerance = 0.0001;
	design->max_iter = 250;
	design->maxmove = 0.6;
    }
    if (t/schedule->t_initial < 0.0000005) {
    }

/*    if ((t/schedule->t_initial < 0.01) && (design->min_node_space > 5.0)) {
	design->min_node_space = 10.0;
	printf("UPDATE: min_node_space = %lf\n", design->min_node_space);
    }
    if ((t/schedule->t_initial < 0.001) && (design->min_node_space > 2.0)) {
	design->min_node_space = 5.0;
	printf("UPDATE: min_node_space = %lf\n", design->min_node_space);
    }
    if ((t/schedule->t_initial < 0.0001) && (design->min_node_space > 0.75)) {
	design->min_node_space = 1.0;
	printf("UPDATE: min_node_space = %lf\n", design->min_node_space);
    }*/
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

