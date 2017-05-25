using System.Collections.Generic;
using TVGL;
using OptimizationToolbox;
using System;
using System.Linq;

namespace _3D_LayoutOpt
{
    public class Design : IDependentAnalysis
    {
        public double[,] DesignVars = null;
        public double[,] OldDesignVars = null;
        public List<Net> Netlist = new List<Net>();
		public IList<double[]> RatsNest = new List<double[]>();


        /* Box_min and box_max are the x, y, z bounds for the bounding box.  Overlap is a     */
        /* matrix which contains the overlap between the ith and the jth components.  Note    */
        /* that only the top half of the overlap matrix is used.  half_area is half of the    */
        /* total of all the surface areas of the components.                                  */

        public int CompCount;
        public double[] OldOrientation;
        public double[] BoxMin = new double[3];
        public double[] BoxMax = new double[3];
        public double[,] Overlap;
        public double[] OldCoord = new double[3];
        public double[] OldDim = new double[3];
        public double[] CGrav = new double[3];
        //public double[] container = new double[3];                                             //ENCLOSURE DIMENSIONS READ FROM FILE
        public Container Container;

        /* First comp is a pointer to the first component in the component list.  Min_comp    */
        /* and max_comp are pointers to the components which contribute to the x, y, z max    */
        /* and min bounds (i.e. box_min and box_max). Backup is a pointer to a component that */
        /* contains backup information in case we reject a step and need to revert to a       */
        /* previous design.                                                                   */

        public Component[] MinComp = new Component[3];
        public Component[] MaxComp = new Component[3];
        public List<Component> Components = new List<Component>();

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

        public double[] NewObjValues = new double[Constants.ObjNum];
        public double[,] OldObjValues = new double[Constants.ObjNum, Constants.BalanceAvg];
        public double[] BackupObjValues = new double[Constants.ObjNum];
        public double[] Coef = new double[Constants.ObjNum];
        public double[] Weight = new double[Constants.ObjNum];

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
        /* gaussMove is the Move size under which GS is used instead of LU for translations.  */
        /* gauss is the flag that says to use GS.  GS is always used on rotations and Swaps.  */
        /* max_iter is the maximum number of iterations for GS.                               */
        /* analysis_switch[] is the vector of t/initial t steps that the thermal              */
        /* analysis switches.                                                                 */
        /* choice is the flag for the HeatEval H.T. chooser.                                 */

        //public TemperatureNode[] tfield = new TemperatureNode[Constants.NODE_NUM * Constants.NODE_NUM * Constants.NODE_NUM];
        public TemperatureNode[] Tfield = InitializeArray<TemperatureNode>(Constants.NodeNum * Constants.NodeNum * Constants.NodeNum);
        public double Kb, Tamb, Tolerance, MinNodeSpace;
        public double Hcf, GaussMove;
        public double[] H = new double[3];
        public double[] AnalysisSwitch = new double[Constants.SwitchNum];
        public int HcfPerTemp, Gauss, MaxIter, Choice;

        static T[] InitializeArray<T>(int length) where T : new()
        {
            var array = new T[length];
            for (var i = 0; i < length; ++i)
            {
                array[i] = new T();
            }

            return array;
        }

        public void add_comp(Component comp)
        {
            Components.Add(comp);
        }

        public void calculate(double[] x)
        {
            BoxMax = new[] { Container.Ts.XMax, Container.Ts.YMax, Container.Ts.ZMax };
            BoxMin = new[] { Container.Ts.XMin, Container.Ts.YMin, Container.Ts.ZMin };
            var k = 0;
            for (var i = 0; i < CompCount; i++)
            {
                var move = new double[6];
                for (var j = 0; j < 6; j++)
                {
                    DesignVars[i,j] = x[k++];
                    move[j] = DesignVars[i,j] - OldDesignVars[i,j];
                }
                Components[i].Update(move);
                //for (var j = 0; j < 3; j++)
                //{
                //    if (BoxMin[j] > Components[i].Ts.Bounds[0][j])
                //        BoxMin[j] = Components[i].Ts.Bounds[0][j];
                //    if (BoxMax[j] < Components[i].Ts.Bounds[1][j])
                //        BoxMax[j] = Components[i].Ts.Bounds[1][j];
                //}
            }
            OldDesignVars = (double[,])DesignVars.Clone();

            //var shapes = Components.Select(c => c.Ts).ToList();
            //Presenter.ShowAndHangTransparentsAndSolids(new[] { Container.Ts }, shapes);
            //Presenter.ShowVertexPathsWithSolid(RatsNest, shapes);
        }

        public void InitializeOverlapMatrix()
        {
            Overlap = new double[CompCount,CompCount];
            for (int i = 0; i < CompCount; i++)
            {
                for (int j = 0; j < CompCount; j++)
                {
                    Overlap[i, j] = 0;
                }
            }

        }
    }
}