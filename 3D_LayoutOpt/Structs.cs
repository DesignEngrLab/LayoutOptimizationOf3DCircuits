using System.Collections.Generic;
using TVGL;


namespace _3D_LayoutOpt
{

    public class Component
    {
        public List<TessellatedSolid> ts = null;
        public Footprint footprint = null;
        public double[] orientation = new double[3];
        public int node_center, nodes;                        //TO DO: WHAT ARE NODE and NODE CENTER?
        public double[] dim = new double[3];
        public double temp, tempcrit, q, k;
        public string name;

        public Component(string CmpName, Footprint FP)
        {
            name = CmpName;
            footprint = FP;
            for (int i = 0; i < orientation.Length; i++)
            {
                orientation[i] = 0;
            }
        }

    }

    public class Container
    {
        public string Name;
        public List<TessellatedSolid> ts = null;

        public Container(string containername, List<TessellatedSolid> tessellatedSolids)
        {
            Name = containername;
            ts = tessellatedSolids;
        }
    }

    public class Footprint
    {
        string name;
        List<SMD> pads = null;

        public Footprint(string FPname, List<SMD> SMDpads)
        {
            name = FPname;
            pads = SMDpads;
        }
    }

    public class SMD
    {
        string name;
        double[] coord = new double[2];
        double[] dim = new double[2];

        public SMD(string SMDname, double[] coordinates, double[] dimensions)
        {
            name = SMDname;
            coord = coordinates;
            dim = dimensions;
        }
    }

    

    public class Temperature_field
    {
        /* A matrix of such structures mark each node of the temperature field.  There will   */
        /* be a total of COMP_NUM squared of these components.  A zero in the comp variable   */
        /* means that the node does not refer to the center of a component but merely to a    */
        /* a resistor junction.  A non-zero number refers to that component in the list of    */
        /* components.                                                                        */
        public Component comp, innercomp;
        public double[] coord = new double[3];
        public double prev_temp, old_temp, temp;
        public double vol, k;
    }

    public class Design
    {


        /* Box_min and box_max are the x, y, z bounds for the bounding box.  Overlap is a     */
        /* matrix which contains the overlap between the ith and the jth components.  Note    */
        /* that only the top half of the overlap matrix is used.  half_area is half of the    */
        /* total of all the surface areas of the components.                                  */

        public int comp_count;
        public double[] old_orientation;
        public double[] box_min = new double[3];
        public double[] box_max = new double[3];
        public double[,] overlap = new double[Constants.COMP_NUM, Constants.COMP_NUM];
        public double[] old_coord = new double[3];
        public double[] old_dim = new double[3];
        public double[] c_grav = new double[3];
        //public double[] container = new double[3];                                             //ENCLOSURE DIMENSIONS READ FROM FILE
        public Container container;
        public double half_area, volume, mass;

        /* First comp is a pointer to the first component in the component list.  Min_comp    */
        /* and max_comp are pointers to the components which contribute to the x, y, z max    */
        /* and min bounds (i.e. box_min and box_max). Backup is a pointer to a component that */
        /* contains backup information in case we reject a step and need to revert to a       */
        /* previous design.                                                                   */

        public Component[] min_comp = new Component[3];
        public Component[] max_comp = new Component[3];
        public List<Component> components = new List<Component>();

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

        public double[] new_obj_values = new double[Constants.OBJ_NUM];
        public double[,] old_obj_values = new double[Constants.OBJ_NUM, Constants.BALANCE_AVG];
        public double[] backup_obj_values = new double[Constants.OBJ_NUM];
        public double[] coef = new double[Constants.OBJ_NUM];
        public double[] weight = new double[Constants.OBJ_NUM];

        /* These variables have been added for the thermal analysis.                          */
        /* tfield is the vector of temperature nodes.  It is constrained to NODE_NUM in each  */
        /* direction.                                                                         */
        /* kb is the conductivity of the board.                                               */
        /* h(x,y,z) is the heat transfer coefficient for the convective boundary conditions.  */
        /* tamb is the ambient temperature of the boundary.                                   */
        /* tolerance is the adjustable tolerance used by the Gauss-Seidel solver that         */
        /* the temperatures are solved to.                                                    */
        /* min_node_space is the minimum allowable node spacing.                              */
        /* hcf is the Approximation Method Correction Factor established using Matrix Method. */
        /* hcf_per_temp is the number of times App gets corrected per temperature.            */
        /* gaussmove is the move size under which GS is used instead of LU for translations.  */
        /* gauss is the flag that says to use GS.  GS is always used on rotations and swaps.  */
        /* max_iter is the maximum number of iterations for GS.                               */
        /* analysis_switch[] is the vector of t/initial t steps that the thermal              */
        /* analysis switches.                                                                 */
        /* choice is the flag for the heat_eval H.T. chooser.                                 */

        //public Temperature_field[] tfield = new Temperature_field[Constants.NODE_NUM * Constants.NODE_NUM * Constants.NODE_NUM];
        public Temperature_field[] tfield = InitializeArray<Temperature_field>(Constants.NODE_NUM * Constants.NODE_NUM * Constants.NODE_NUM);
        public double kb, tamb, tolerance, min_node_space;
        public double hcf, gaussmove;
        public double[] h = new double[3];
        public double[] analysis_switch = new double[Constants.SWITCH_NUM];
        public int hcf_per_temp, gauss, max_iter, choice;

        static T[] InitializeArray<T>(int length) where T : new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; ++i)
            {
                array[i] = new T();
            }

            return array;
        }

        public void add_comp(Component comp)
        {
            components.Add(comp);
        }

    }

    public class Schedule
    {
        public int mgl, within_target, max_tolerance, in_count, out_count, equilibrium, problem_size;
        public double t_initial, sigma, c_avg, delta, c_min, c_max, max_delta_c, delta_c;
    }

    public class Hustin
    {
        /* attemts is the number of attempts made for each move, which_move is the move taken */
        /* so we know which move to attribute a cost change to, delta_c is the cumulative     */
        /* change in cost (absolute value) due to each move, quality is the quality factor    */
        /* for each move, probability is the probability if selecting each move, and move_    */
        /* size is the distance for each of the translate moves, usable_prob is the "usable"  */
        /* portion of the probability.  The "unusable" portion (i.e. MIN_PROB * MOVE_NUM) is  */
        /* set aside to give each move a minimum probability.                                 */
        public int[] attempts = new int[Constants.MOVE_NUM];
        public int which_move;
        public double[] delta_c = new double[Constants.MOVE_NUM];
        public double[] quality = new double[Constants.MOVE_NUM];
        public double[] prob = new double[Constants.MOVE_NUM];
        public double[] move_size = new double[Constants.TRANS_NUM];
        public double usable_prob;
    }
}
