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
void eval_part_5(design)
struct Design *design;
{
  int i;
  struct Component *comp;
  double calc_thermal_penalty();
  void calc_thermal_matrix();

/*  printf("Calculating Thermal Matrix\n");*/
  calc_thermal_matrix(design);
  design->new_obj_values[3] = 0.0;
  comp = design->first_comp;
  for (i = 1; i <= COMP_NUM; ++i) {
    design->new_obj_values[3] += calc_thermal_penalty(comp->temp, comp->tempcrit);
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
void calc_thermal_matrix(design)
struct Design *design;
{
  struct Component *comp;
  double *flux, **R;
  void set_up_tfield(), set_up_flux(), set_up_coef_matrix();
  void matrix_solver(), find_comp_temp(), matrix_solver3();
  int i, j, node_dim[DIMENSION], tot_nodes, hbw, width;

  set_up_tfield(design, node_dim);
  hbw = node_dim[1]*node_dim[2];
  width = 2*hbw + 1;
  tot_nodes = node_dim[0]*hbw;

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
  set_up_coef_matrix(design, flux, tot_nodes, hbw, node_dim[2], R); 
  matrix_solver(design, R, flux, tot_nodes, hbw);
  find_comp_temp(design, tot_nodes);
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
  struct Component *comp, *find_comp();
  double xx[NODE_NUM], yy[NODE_NUM], zz[NODE_NUM], xfringe, yfringe, zfringe;
  int not_duplicate(); 
  void picksort(), refinemesh();
  int i, j, k, l;

  i = 0;
  j = 0;
  k = 0;
  comp = design->first_comp;
  while (comp != NULL) {
    if (not_duplicate(comp->coord[0], xx, i))
      xx[++i] = comp->coord[0];
    if (not_duplicate(comp->coord[1], yy, j))
      yy[++j] = comp->coord[1];
    if (not_duplicate(comp->coord[2], zz, k))
      zz[++k] = comp->coord[2];
    comp = comp->next_comp;
  }
  node_dim[0] = i;
  node_dim[1] = j;
  node_dim[2] = k;
  picksort(node_dim[0], xx);
  picksort(node_dim[1], yy);
  picksort(node_dim[2], zz);

#ifdef SFRINGE
     xfringe = SFRINGE;
     yfringe = SFRINGE;
     zfringe = SFRINGE;
#endif

#ifdef CFRINGE
  xfringe = (design->container[0] - (design->box_max[0] - design->box_min[0]))/2;
  if (xfringe < 0)
    xfringe = CFRINGE;
  yfringe = (design->container[1] - (design->box_max[1] - design->box_min[1]))/2;
  if (yfringe < 0)
    yfringe = CFRINGE;
  zfringe = (design->container[2] - (design->box_max[2] - design->box_min[2]))/2;
  if (zfringe < 0)
    zfringe = CFRINGE;
#endif

  xx[0] = design->box_min[0] - xfringe; 
  yy[0] = design->box_min[1] - yfringe;
  zz[0] = design->box_min[2] - zfringe;
  xx[(++node_dim[0])] = design->box_max[0] + xfringe;
  yy[(++node_dim[1])] = design->box_max[1] + yfringe;
  zz[(++node_dim[2])] = design->box_max[2] + zfringe;
  refinemesh(node_dim, xx, 0);
  refinemesh(node_dim, yy, 1);
  refinemesh(node_dim, zz, 2);
  ++node_dim[0];
  ++node_dim[1];
  ++node_dim[2];
  printf("%d %d %d\n", node_dim[0], node_dim[1], node_dim[2]);
/* Put coordinates and component number in each node.                                 */
  l = 0;
  for (i = 0; i < node_dim[0]; ++i) {
    for (j = 0; j < node_dim[1]; ++j) {
      for (k = 0; k < node_dim[2]; ++k) {
	design->tfield[l].x_coord = xx[i];
	design->tfield[l].y_coord = yy[j];
	design->tfield[l].z_coord = zz[k];
	design->tfield[l].xcomp = find_comp(design, xx[i], 0);
	design->tfield[l].ycomp = find_comp(design, yy[j], 1);
	design->tfield[l].zcomp = find_comp(design, zz[k], 2);
	if ((design->tfield[l].xcomp == design->tfield[l].ycomp) &&
	    (design->tfield[l].xcomp == design->tfield[l].zcomp))
	  design->tfield[l].comp = design->tfield[l].xcomp;
	else design->tfield[l].comp = NULL;
	++l;
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
    if (num == arr[m])
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
void refinemesh(node_dim, arr, vector)
int vector, node_dim[];
double arr[];
{
  int i, j;

  i = node_dim[vector];
  while ((i > 0) && (1 + node_dim[vector]  < NODE_NUM)) {
    if ((arr[i] - arr[i-1]) > MIN_NODE_SPACE) {
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
struct Component *find_comp(design, num, vector)
struct Design *design;
double num;
int vector;
{
  int i;
  struct Component *comp;

  comp = design->first_comp;
  i = 0;
  while (++i <= COMP_NUM) {
    if (comp->coord[vector] == num) 
      return(comp);
    comp = comp->next_comp;
  }
  return(NULL);
}

/* ---------------------------------------------------------------------------------- */
/* This function initializes the heat flux vector.  This vector corresponds to the    */
/* b in Ax = b.  If no component at node then it equals zero, however if there is     */
/* a component at the node then it equals comp_ptr->q.                                */
/* ---------------------------------------------------------------------------------- */
void set_up_flux(design, flux, tot_nodes)
struct Design *design;
double *flux;
int tot_nodes;

{
  int  k;
  struct Component *comp;

  for (k = 0; k < tot_nodes; ++k) {
    if (design->tfield[k].comp != NULL) { 
      flux[k] = design->tfield[k].comp->q;
    }
    else flux[k] = 0.0;
  }
}

/* ---------------------------------------------------------------------------------- */
/* This function sets up the cooefficient matrix R.  This matrix corresponds to the   */
/* A in Ax = b.  Calculations of all the thermal resistances and boundary conditions  */
/* are done in this subroutine.                                                       */
/* ---------------------------------------------------------------------------------- */
void set_up_coef_matrix(design, flux, tot_nodes, hbw, znodes, R)
struct Design *design;
int tot_nodes, hbw, znodes;
double *flux, **R;
{
  int k;
  double xc, yc, zc;
  double n, e, w, s, u, d;
  double x, y, z, ex, wx, sy, ny, uz, dz, eb, wb, sb, nb, ub, db, dummy;
  double kc, kw, ke, ks, kn, ku, kd, kb = design->kb;
  struct Component *comp;
  void imbedded_node();

/*  Go through each node to fill in each row of the R-matrix  */
  for (k = 0; k < tot_nodes; ++k) {

/* Is node imbedded in component?  If so, set values of x, y, and kc.  If not, zeros.*/
    imbedded_node(design->tfield[k], design, &xc, &yc, &zc, &kc);
    
/* Find dimension n, e, w, s, u, d to surrounding points (0 if boundary). And find   */
/* if the node borders a component; determine kw, ke, ks, kn, ku, kd                 */
/* and wx, ex, sy, ny, uz, dz.                                                       */
    if ((k-hbw) >= 0) {
      w = design->tfield[k].x_coord - design->tfield[(k-hbw)].x_coord;
      imbedded_node(design->tfield[(k-hbw)], design, &wx, &dummy, &dummy, &kw);
    }
    else w = 0.0;

    if ((k+hbw) < tot_nodes) {
      e = design->tfield[(k+hbw)].x_coord - design->tfield[k].x_coord;
      imbedded_node(design->tfield[(k+hbw)], design, &ex, &dummy, &dummy, &ke);
    }
    else e = 0.0;

    if ((k % hbw) >= znodes) {
      s = design->tfield[k].y_coord - design->tfield[k-znodes].y_coord;
      imbedded_node(design->tfield[k-znodes], design, &dummy, &sy, &dummy, &ks);
    }
    else s = 0.0;

    if (((k+znodes) % hbw) >= znodes) {
      n = design->tfield[k+znodes].y_coord - design->tfield[k].y_coord;
      imbedded_node(design->tfield[k+znodes], design, &dummy, &ny, &dummy, &kn);
    }
    else n = 0.0;

    if ((k % znodes) != 0) {
      d = design->tfield[k].z_coord - design->tfield[k-1].z_coord;
      imbedded_node(design->tfield[k-1], design, &dummy, &dummy, &dz, &kd);
    }
    else d = 0.0;

    if (((k+1) % znodes) != 0) {
      u = design->tfield[k+1].z_coord - design->tfield[k].z_coord;
      imbedded_node(design->tfield[k+1], design, &dummy, &dummy, &uz, &ku);
    }
    else u = 0.0;


/* Determine resistance elements to surrounding points.                              */
/* West.  Do all the things necessary to find the resistances to the west.           */
    if (w == 0.0) {
      flux[k] += 0.25*(design->tamb)*(design->hx)*(n + s)*(u + d);
      R[k][hbw] += 0.25*(design->hx)*(n + s)*(u + d);
    }
    else {
/* These two if statements account for the chance that the node and the western node */
/* are in the same component.                                                        */
      x = xc;
      if (w + x <= wx) {
	wx = w;
	x = 0.0;
      }
      if (w + wx <= x) {
	x = w;
	wx = 0.0;
      }
      wb = w - (x + wx);
      if (wb < 0.0) wb = 0.0;
/* These resistances are actually 'admittances' and therefore the total resistance   */
/* is the reciprocal of the sum of the reciprocals.                                  */
      R[k][hbw] += (0.25*kb*kc*kw*(n + s)*(u + d))/((kb*kw*x) + (kc*kw*wb) + (kc*kb*wx));
      R[k][0] = -(0.25*kb*kc*kw*(n + s)*(u + d))/((kb*kw*x) + (kc*kw*wb) + (kc*kb*wx));
    }
/* East.  */
    if (e == 0.0) {
      flux[k] += 0.25*(design->tamb)*(design->hx)*(n + s)*(u + d);
      R[k][hbw] += 0.25*(design->hx)*(n + s)*(u + d);
    }
    else {
      x = xc;
      if (e + x <= ex) {
	ex = e;
	x = 0.0;
      }
      if (e + ex <= x) {
	x = e;
	ex = 0.0;
      }
      eb = e - (x + ex);
      if (eb < 0.0) eb = 0.0;
      R[k][hbw] += (0.25*kb*kc*ke*(n + s)*(u + d))/((kb*ke*x) + (kc*ke*eb) + (kc*kb*ex));
      R[k][(hbw + hbw)] = -(0.25*kb*kc*ke*(n + s)*(u + d))/((kb*ke*x) + (kc*ke*eb) + 
							    (kc*kb*ex));
    }
/* South.  */
    if (s == 0.0) {
      flux[k] += 0.25*(design->tamb)*(design->hy)*(e + w)*(u + d);
      R[k][hbw] += 0.25*(design->hy)*(e + w)*(u + d);
    }
    else {
      y = yc;
      if (s + y <= sy) {
	sy = s;
	y = 0.0;
      }
      if (s + sy <= y) {
	y = s;
	sy = 0.0;
      }
      sb = s - (y + sy);
      if (sb < 0.0) sb = 0.0;
      R[k][hbw] += (0.25*kb*kc*ks*(e + w)*(u + d))/((kb*ks*y) + (kc*ks*sb) + (kc*kb*sy));
      R[k][(hbw - znodes)] = -(0.25*kb*kc*ks*(e + w)*(u + d))/((kb*ks*y) + (kc*ks*sb) + 
							  (kc*kb*sy));
    }
/* North.  */
    if (n == 0.0) {
      flux[k] += 0.25*(design->tamb)*(design->hy)*(e + w)*(u + d);
      R[k][hbw] += 0.25*(design->hy)*(e + w)*(u + d);
    }
    else {
      y = yc;
      if (n + y <= ny) {
	ny = n;
	y = 0.0;
      }
      if (n + ny <= y) {
	y = n;
	ny = 0.0;
      }
      nb = n - (y + ny);
      if (nb < 0.0) nb = 0.0;
      R[k][hbw] += (0.25*kb*kc*kn*(e + w)*(u + d))/((kb*kn*y) + (kc*kn*nb) + (kc*kb*ny));
      R[k][(hbw + znodes)] = -(0.25*kb*kc*kn*(e + w)*(u + d))/((kb*kn*y) + (kc*kn*nb) + 
							  (kc*kb*ny));
    }
/* Down.  */
    if (d == 0.0) {
      flux[k] += 0.25*(design->tamb)*(design->hz)*(e + w)*(n + s);
      R[k][hbw] += 0.25*(design->hz)*(e + w)*(n + s);
    }
    else {
      z = zc;
      if (d + z <= dz) {
	dz = d;
	z = 0.0;
      }
      if (d + dz <= z) {
	z = d;
	dz = 0.0;
      }
      db = d - (z + dz);
      if (db < 0.0) db = 0.0;
      R[k][hbw] += (0.25*kb*kc*kd*(e + w)*(n + s))/((kb*kd*z) + (kc*kd*db) + (kc*kb*dz));
      R[k][(hbw - 1)] = -(0.25*kb*kc*kd*(e + w)*(n + s))/((kb*kd*z) + (kc*kd*db) + 
							  (kc*kb*dz));
    }
/* Up.  */
    if (u == 0.0) {
      flux[k] += 0.25*(design->tamb)*(design->hz)*(e + w)*(n + s);
      R[k][hbw] += 0.25*(design->hz)*(e + w)*(n + s);
    }
    else {
      z = zc;
      if (u + z <= uz) {
	uz = u;
	z = 0.0;
      }
      if (u + uz <= z) {
	z = u;
	uz = 0.0;
      }
      ub = u - (z + uz);
      if (ub < 0.0) ub = 0.0;
      R[k][hbw] += (0.25*kb*kc*ku*(e + w)*(n + s))/((kb*ku*z) + (kc*ku*ub) + (kc*kb*uz));
      R[k][(hbw + 1)] = -(0.25*kb*kc*ku*(e + w)*(n + s))/((kb*ku*z) + (kc*ku*ub) + 
							  (kc*kb*uz));
    }
  }
}

/* ---------------------------------------------------------------------------------- */
/* This function finds out if the node imbedded within a component.  If it is it puts */
/* the necessary values in x, y, and k.  Note: if dealing with neighboring component  */
/* it is only necessary to return either the x or the y, hence the dummy variable.    */
/* ---------------------------------------------------------------------------------- */
void imbedded_node(node, design, x, y, z, k)
struct Temperature_field node;
struct Design *design;
double *x, *y, *z, *k;
{
  int i;
  struct Component *comp;
  
/* Is node center of component? */
  if (node.comp != NULL) {
    *x = node.comp->dim[0]/2;
    *y = node.comp->dim[1]/2;
    *z = node.comp->dim[2]/2;
    *k = node.comp->k;
    return;
  }
/* If not is it imbedded in another component? */
  if ((node.xcomp != NULL) && (node.ycomp != NULL) && (node.zcomp != NULL)) {
    comp = design->first_comp;
    while (comp != NULL) {
      if ((2*(fabs(node.x_coord - comp->coord[0])) < comp->dim[0]) && 
          (2*(fabs(node.y_coord - comp->coord[1])) < comp->dim[1]) &&
	  (2*(fabs(node.z_coord - comp->coord[2])) < comp->dim[2])) {
	*x = (comp->dim[0])/2 - (fabs(node.x_coord - (comp->coord[0])));
	*y = (comp->dim[1])/2 - (fabs(node.y_coord - (comp->coord[1])));
	*z = (comp->dim[2])/2 - (fabs(node.z_coord - (comp->coord[2])));
	*k = comp->k;
	return;
      }
      comp = comp->next_comp;
    }
/* Component is not imbedded */
  }
/* Component has NULL in xcomp, ycomp, or zcomp therefore must be boundary. */
  *x = 0.0;
  *y = 0.0;
  *z = 0.0;
  *k = design->kb;
}

/* ---------------------------------------------------------------------------------- */
/* This function puts the temperatures of the node that lie on components into the    */
/* the component data structure for analysis by eval_part_5.                          */
/* ---------------------------------------------------------------------------------- */
void find_comp_temp(design, tot_nodes)
struct Design *design;
int tot_nodes;
{
  int i = 0, k;
  struct Component *comp;

  for (k = 0; k < tot_nodes; ++k) {
    if (design->tfield[k].comp != NULL) {
      ++i;
      /*printf("%d temperature = %.2f at %.2f %.2f %.2f\n", i,
	     design->tfield[k].temp, design->tfield[k].x_coord, design->tfield[k].y_coord, 
	     design->tfield[k].z_coord);*/
      design->tfield[k].comp->temp = design->tfield[k].temp;
    }
  }
}

/* ---------------------------------------------------------------------------------- */
/* This is the matrix solver for the vector of nodes in tfield.  It uses LU decomp.   */
/* ---------------------------------------------------------------------------------- */
void matrix_solver(design, R, flux, n, hbw)
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
