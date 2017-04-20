using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_LayoutOpt
{
    public enum AcceptFlag
    {
        ComponentOutside = -1,
        RejectedBadMove = 0,
        AcceptedBadMove = 1,
        AcceptedGoodMove = 2
    }
    public static class Constants
    {

        public const int DesginVarNum = 6;
        

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
        /* BALANCE_AVG: The number of Moves averaged to balance components of the objective   */
        /*              function.                                                             */
        /* UPDATE_INTERVAL: How often (# of iterations) balancing coefficients are updated    */
        /* BOX_LIMIT: The value that the bounding box dimensions are not allowed to exceed.   */
        /* ---------------------------------------------------------------------------------- */
        public const int MaxNameLength = 25;
        public const double InitialBoxSize = 10.0;
        public const int ObjNum = 4;
        public const int BalanceAvg = 45;
        public const int UpdateInterval = 1;
        public const double BoxLimit = 30.0;
        public const double Tiny = 0.0001;

        /* ---------------------------------------------------------------------------------- */
        /*                                 ANNEALING SCHEDULE                                 */
        /* SAMPLE: The sample size of points used to calculate the initial value of sigma.    */
        /* MIN_SAMPLE: The number of points used to calculate statistics at each temperature. */
        /* K: Used to define the initial temperature (T1 = K * sigma).  The value of 18.5 was */
        /*    selected to give an 85% probability of Accepting a design whose cost is 3 sigma */
        /*    worse than the current one.                                                     */
        /* RANGE: Used to define the "within count" interval.  The interval is defined as     */
        /*        average cost plus or minus delta, where delta = RANGE * sigma.              */
        /* ERF_RANGE: The error function of the value of RANGE.  ERF(.5) = .38.               */
        /* LAMBDA: A Constant used to controle the rate of temperature decrease.              */
        /* T_RATIO_MAX: The maximum allowable ratio of Tn to Tn-1 to prevent slow cooling.    */
        /* T_RATIO_MIN: The minimum allowable ratio of Tn to Tn-1 to prevent rapid cooling.   */
        /* ---------------------------------------------------------------------------------- */
        public const int Sample = 500;
        public const int MinSample  =100;
        public const int K = 37;
        public const double Range = 0.2;
        public const double ErfRange = 0.18;
        public const double Lambda = 10.0;
        public const double RatioMax = 0.97;
        public const double RatioMin = 0.6;

        /* ---------------------------------------------------------------------------------- */
        /*                                  CONTROL OF MOVES                                  */
        /*                                                                                    */
        /* PERTURB: is the percent probability of selecting a perterubation (Move or Rotate)  */
        /*          as opposed to a larger Move (Swap).                                       */
        /* M_PROB: is the percent probability of moving a component, as opposed to rotating   */
        /*         it, when a perturbation Move has been selected.                            */
        /* MAX_MOVE_DIST: is the maximum distance a component will be Moved.                  */
        /* MMIN_MOVE_DIST: is the minimum distance a component will be Moved.                 */
        /* DIMENSION: dimensions of problem, either 2 or 3                                    */
        /* ---------------------------------------------------------------------------------- */
        public const int Perturb = 90;
        public const int MProb = 80;
        public const double MaxMoveDist = 15.0;
        public const double MinMoveDist = 0.1;
        public const int Limit = 25000;
        public const int MoveNum = 17;                                                         //WHAT IS THIS
        public const int TransNum = 15;                                                       //WHAT IS THIS
        public const int RotNum = 10;
        public const double MinProb = 0.03;
        public const int Dimension = 3;

        /* ---------------------------------------------------------------------------------- */
        /*                                CONTROL OF WEIGHTS                                  */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */
        public const double MaxWeight1 = 1.0;
        public const double MinWeight1 = 0.1;
        public const int WeightILimit = 5000;

        /* ---------------------------------------------------------------------------------- */
        /*                                 NOODLES CONSTANTS                                  */
        /*                                                                                    */
        /* FACETS: The number of sides used to approximate a cylinder.                        */
        /* ---------------------------------------------------------------------------------- */
        public const int Facets = 24;

        /* ---------------------------------------------------------------------------------- */
        /*                                 THERMAL CONSTANTS                                  */
        /*                                                                                    */
        /* NODE_NUM: The max number of nodes along side of temperature field.  Includes       */
        /* boundary points.  Should be somewhere around BOX_LIMIT/MIN_NODE_SPACE              */
        /* OMEGA: The SOR parameter for the Guass - Seidel matrix solver.                     */
        /* E: The amount of space to the boundary of the PCB from the nearest component. */
        /* CLOSE_NODE: If nodes are this close than they are combined as one.                 */
        /* ---------------------------------------------------------------------------------- */
        public const double Omega = 1.85;
        public const double Cfringe = 1.0;
        public const int NodeNum = 15;
        public const double CloseNode = 0.1;
        public const int SwitchNum = 5;

    }
}
