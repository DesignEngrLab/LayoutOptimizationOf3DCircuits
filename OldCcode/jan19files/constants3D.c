/* ---------------------------------------------------------------------------------- */
/*                                    CONSTANTS.C                                     */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/*                                  GENERAL CONSTANTS                                 */
/*                                                                                    */
/* MAX_NAME_LENGTH: Maximum number of characters in a name.                           */
/* COMP_NUM: The number of components.                                                */
/* INITIAL_BOX_SIZE: The initial locations for the components have x-y-z coordinates  */
/*                   in the range +- INITIAL_BOX_SIZE.                                */
/* OBJ_NUM: The number of components in the objective function.                       */
/* BALANCE_AVG: The number of moves averaged to balance components of the objective   */
/*              function.                                                             */
/* UPDATE_INTERVAL: How often (# of iterations) balancing coefficients are updated    */
/* BOX_LIMIT: The value that the bounding box dimensions are not allowed to exceed.   */
/* ---------------------------------------------------------------------------------- */
#define MAX_NAME_LENGTH 25
#define COMP_NUM 5
#define INITIAL_BOX_SIZE 10.0
#define OBJ_NUM 4
#define BALANCE_AVG 1000
#define UPDATE_INTERVAL 1
#define BOX_LIMIT 100.0
#define TINY 0.0001

/* ---------------------------------------------------------------------------------- */
/*                                 ANNEALING SCHEDULE                                 */
/* SAMPLE: The sample size of points used to calculate the initial value of sigma.    */
/* MIN_SAMPLE: The number of points used to calculate statistics at each temperature. */
/* K: Used to define the initial temperature (T1 = K * sigma).  The value of 18.5 was */
/*    selected to give an 85% probability of accepting a design whose cost is 3 sigma */
/*    worse than the current one.                                                     */
/* RANGE: Used to define the "within count" interval.  The interval is defined as     */
/*        average cost plus or minus delta, where delta = RANGE * sigma.              */
/* ERF_RANGE: The error function of the value of RANGE.  ERF(.5) = .38.               */
/* LAMBDA: A Constant used to controle the rate of temperature decrease.              */
/* T_RATIO_MAX: The maximum allowable ratio of Tn to Tn-1 to prevent slow cooling.    */
/* T_RATIO_MIN: The minimum allowable ratio of Tn to Tn-1 to prevent rapid cooling.   */
/* ---------------------------------------------------------------------------------- */
#define SAMPLE 500
#define MIN_SAMPLE 100
#define K 18.5
#define RANGE 0.5
#define ERF_RANGE 0.38
#define LAMBDA 0.7
#define T_RATIO_MAX 0.985
#define T_RATIO_MIN 0.85

/* ---------------------------------------------------------------------------------- */
/*                                  CONTROL OF MOVES                                  */
/*                                                                                    */
/* PERTURB: is the percent probability of selecting a perterubation (move or rotate)  */
/*          as opposed to a larger move (swap).                                       */
/* M_PROB: is the percent probability of moving a component, as opposed to rotating   */
/*         it, when a perturbation move has been selected.                            */
/* MAX_MOVE_DIST: is the maximum distance a component will be moved.                  */
/* MMIN_MOVE_DIST: is the minimum distance a component will be moved.                 */
/* DIMENSION: dimensions of problem, either 2 or 3                                    */
/* ---------------------------------------------------------------------------------- */
#define PERTURB 90
#define M_PROB 80
#define MAX_MOVE_DIST 5.0
#define MIN_MOVE_DIST 0.05
#define I_LIMIT 25000.
#define MOVE_NUM 17
#define TRANS_NUM 15
#define MIN_PROB 0.03
#define DIMENSION 3

/* ---------------------------------------------------------------------------------- */
/*                                CONTROL OF WEIGHTS                                  */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */
#define MAX_WEIGHT_1 1.0
#define MIN_WEIGHT_1 0.1
#define WEIGHT_I_LIMIT 5000.

/* ---------------------------------------------------------------------------------- */
/*                                 NOODLES CONSTANTS                                  */
/*                                                                                    */
/* FACETS: The number of sides used to approximate a cylinder.                        */
/* ---------------------------------------------------------------------------------- */
#define FACETS 24

/* ---------------------------------------------------------------------------------- */
/*                                 THERMAL CONSTANTS                                  */
/*                                                                                    */
/* NODE_NUM: The max number of nodes along side of temperature field.  Includes       */
/* boundary points.  Should be somewhere around BOX_LIMIT/MIN_NODE_SPACE              */
/* OMEGA: The SOR parameter for the Guass - Seidel matrix solver.                     */
/* FRINGE: The amount of space to the boundary of the PCB from the nearest component. */
/* ---------------------------------------------------------------------------------- */
#define OMEGA 1.325
#define SFRINGE 1.0
#define MINIMIZE
#define MIN_NODE_SPACE 7.5
#define NODE_NUM 50

