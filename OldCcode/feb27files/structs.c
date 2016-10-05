/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                      STRUCTS.C                                     */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/*                                 STRUCTURE DEFINITIONS                              */
/* ---------------------------------------------------------------------------------- */

struct Component
{
  int orientation, node_center, nodes;
  double coord[3], dim[3], dim_initial[3], half_area, volume, mass;
  double temp, tempcrit, q, k;
  char comp_name[MAX_NAME_LENGTH], shape_type[5];
  struct Component *next_comp, *prev_comp;
};

struct Temperature_field
{
/* A matrix of such structures mark each node of the temperature field.  There will   */
/* be a total of COMP_NUM squared of these components.  A zero in the comp variable   */
/* means that the node does not refer to the center of a component but merely to a    */
/* a resistor junction.  A non-zero number refers to that component in the list of    */
/* components.                                                                        */
  struct Component *comp, *innercomp;
  double coord[3], temp;
  double prev_temp, old_temp;
};

struct Design
{
/* Box_min and box_max are the x, y, z bounds for the bounding box.  Overlap is a     */
/* matrix which contains the overlap between the ith and the jth components.  Note    */
/* that only the top half of the overlap matrix is used.  half_area is half of the    */
/* total of all the surface areas of the components.                                  */

  int old_orientation;
  double box_min[3], box_max[3], overlap[COMP_NUM][COMP_NUM], old_coord[3], old_dim[3];
  double half_area, c_grav[3], container[3], volume, mass;

/* First comp is a pointer to the first component in the component list.  Min_comp    */
/* and max_comp are pointers to the components which contribute to the x, y, z max    */
/* and min bounds (i.e. box_min and box_max). Backup is a pointer to a component that */
/* contains backup information in case we reject a step and need to revert to a       */
/* previous design.                                                                   */

  struct Component *first_comp, *min_comp[3], *max_comp[3];

/* THESE HAVE BEEN ADDED FOR BALANCING THE COMPONENTS OF THE OBJECTIVE FUNCTION       */
/* new_obj_values are the latest values of the components of the objective function.  */
/* They are copied to a column of old_obj_values matrix (only on improvement steps).  */
/* old_obj_values keeps track of past values of the components of the objective       */
/* function.  Currently, the 0th line in the matrix are values of the area ratio and  */
/* the 1st line in the matrix are values of the overlap, coef is an array of          */
/* coefficients which the components of the objective function get multiplied by to   */
/* balance it.  (The 0th coefficient is ignored.).  The weights are also multiplied   */
/* with the components of the objective function, to specify relative importance of   */
/* the different components.  Backup_obj_values is used to back up the array of       */
/* new_obj_values before taking a step.  When a step is rejected, the revert function */
/* copies the backed up values to new_obj_values to restore the previous state.       */

  double new_obj_values[OBJ_NUM], old_obj_values[OBJ_NUM][BALANCE_AVG];
  double backup_obj_values[OBJ_NUM], coef[OBJ_NUM], weight[OBJ_NUM];

/* These variables have been added for the thermal analysis.                          */
/* kb is the conductivity of the board.                                               */
/* h(x,y,z) is the heat transfer coefficient for the convective boundary conditions.  */
/* tamb is the ambient temperature of the boundary.                                   */
/* tolerance is the adjustable tolerance of which the temperatures are solved to.     */
/* min_node_space is the minimum allowable node spacing.                              */

  struct Temperature_field tfield[NODE_NUM*NODE_NUM*NODE_NUM];
  double kb, h[3], tamb, tolerance, min_node_space;
};

struct Schedule
{
  int mgl, within_target, max_tolerance, in_count, out_count, equilibrium, problem_size;
  double t_initial, sigma, c_avg, delta, c_min, c_max, max_delta_c, delta_c;
};

struct Hustin
{
/* attemts is the number of attempts made for each move, which_move is the move taken */
/* so we know which move to attribute a cost change to, delta_c is the cumulative     */
/* change in cost (absolute value) due to each move, quality is the quality factor    */
/* for each move, probability is the probability if selecting each move, and move_    */
/* size is the distance for each of the translate moves, usable_prob is the "usable"  */
/* portion of the probability.  The "unusable" portion (i.e. MIN_PROB * MOVE_NUM) is  */
/* set aside to give each move a minimum probability.                                 */
  int attempts[MOVE_NUM], which_move;
  double delta_c[MOVE_NUM], quality[MOVE_NUM], prob[MOVE_NUM], move_size[TRANS_NUM];
  double usable_prob;
};
