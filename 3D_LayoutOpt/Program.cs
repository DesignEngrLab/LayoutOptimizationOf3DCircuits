using _3D_LayoutOpt.Functions;
using OptimizationToolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL;



namespace _3D_LayoutOpt
{
    class Program
    {

        [STAThread]
        static void Main(string[] args)
        {

            var design = new Design();


            Directory.SetCurrentDirectory("../../workspace");
            var stopwatch = new Stopwatch();
            stopwatch.Start();


            #region Initialization

            // IMPORTING CAD MODELS, COMPONENT AND CONTAINER FEATURES
            Io.ImportData(design);
            Console.WriteLine("{0} components were read in from the file.\n", design.CompCount);

            //INITIALIZING THE PROCESS
            design.InitializeOverlapMatrix();

            Console.WriteLine("INITIALIZING LOCATIONS.\n");
            InitLocations(design);

            Console.WriteLine("Initializing weights.\n");
            InitWeights(design);

            InitObjValues(design);

            #endregion

            var shapes = design.Components.Select(c => c.Ts).ToList();
            //shapes.Add(design.Container.Ts);
            Presenter.ShowAndHangTransparentsAndSolids(new [] { design.Container.Ts }, shapes);
            OptimizeByGA(design);

            

            Presenter.ShowAndHangTransparentsAndSolids(new [] { design.Container.Ts }, shapes);
			Presenter.ShowVertexPathsWithSolid(design.RatsNest, shapes);
            //Presenter.ShowAndHang(design.grid, "grid", Plot2DType.Points, true, OxyPlot.MarkerType.Circle);

            stopwatch.Stop();
            var timeElapsed = stopwatch.Elapsed;
            
            Console.WriteLine("The elapsed time was {0} seconds", timeElapsed);
            Console.ReadKey();
        }

        #region Geneti Algorithm
        private static void OptimizeByGA(Design design)
        {
            //var opty = new GradientBasedOptimization();
            //var opty = new HillClimbing();
            var opty = new GeneticAlgorithm(420);
            
            /* here is the Dependent Analysis. */
            opty.Add(design);
            // this is the objective function
            opty.Add(new EvaluateNetlist(design));
            // here are three inequality constraints
            opty.Add(new ComponentToComponentOverlap(design));
            opty.Add(new ComponentToContainerOverlap(design));
            opty.Add(new HeatBasic(design));
            //opty.Add(new CenterOfGravity(design));
            //opty.Add(new EvaluateBoundingBox(design));

            /******** Set up Design Space *************/
            /* for the GA and the Hill Climbing, a compete discrete space is needed. Face width and
             * location parameters should be continuous though. Consider removing the 800's below
             * when using a mixed optimization method. */
            var dsd = new DesignSpaceDescription(design.CompCount * 6);
            var bounds = design.Container.Ts.Bounds;
            for (var i = 0; i < design.CompCount; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    //dsd[6 * i + j] = new VariableDescriptor(bounds[0][j], bounds[1][j], 0.2);
                    dsd[6 * i + j] = new VariableDescriptor(1.2*design.DesignSpaceMin[j], 1.2*design.DesignSpaceMax[j], 0.1);
                }
                for (var j = 0; j < 3; j++)
                {
                    dsd[6 * i + 3 + j] = new VariableDescriptor(0, 2*Math.PI, 100);
                }
            }
            opty.Add(dsd);
            /******** Set up Optimization *************/
            /* the following mish-mash is similiar to previous project - just trying to find a 
             * combination of methods that'll lead to the optimial optimization algorithm. */
            //abstractSearchDirection searchDirMethod = new SteepestDescent();
            //opty.Add(searchDirMethod);
            //abstractLineSearch lineSearchMethod = new ArithmeticMean(0.0001, 1, 100);
            //opty.Add(lineSearchMethod);
            opty.Add(new LatinHyperCube(dsd, VariablesInScope.OnlyDiscrete));
            opty.Add(new GACrossoverBitString(dsd));
            opty.Add(new GAMutationBitString(dsd));
            opty.Add(new PNormProportionalSelection(OptimizationToolbox.optimize.minimize, true));
            //opty.Add(new RandomNeighborGenerator(dsd,3000));
            //opty.Add(new KeepSingleBest(optimize.minimize));
            opty.Add(new squaredExteriorPenalty(opty, 1));
            //opty.Add(new DeltaFConvergence(.01));
            opty.Add(new MaxAgeConvergence(5, 0.01));
            opty.Add(new MaxFnEvalsConvergence(1000000));
            opty.Add(new MaxIterationsConvergence(15));
            //opty.Add(new MaxSpanInPopulationConvergence(15));
            double[] xStar;
            Parameters.Verbosity = OptimizationToolbox.VerbosityLevels.AboveNormal;
            // this next line is to set the Debug statements from OOOT to the Console.
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var timer = Stopwatch.StartNew();
            var fStar = opty.Run(out xStar, design.CompCount * 6);
        }

        #endregion

        #region NelderMeads
        private static void OptimizeByPattern(Design design)
        {
            //var opty = new GradientBasedOptimization();
            //var opty = new HillClimbing();
            var opty = new NelderMead();


            /* here is the Dependent Analysis. */
            opty.Add(design);
            // this is the objective function
            opty.Add(new EvaluateNetlist(design));
            // here are three inequality constraints
            opty.Add(new ComponentToComponentOverlap(design));
            opty.Add(new ComponentToContainerOverlap(design));
            opty.Add(new HeatBasic(design));
            opty.Add(new CenterOfGravity(design));


            /******** Set up Optimization *************/
            /* the following mish-mash is similiar to previous project - just trying to find a 
             * combination of methods that'll lead to the optimial optimization algorithm. */

            opty.Add(new squaredExteriorPenalty(opty, 10));
           // opty.Add(new MaxAgeConvergence(40, 0.001));
          //  opty.Add(new MaxFnEvalsConvergence(10000));
            opty.Add(new MaxIterationsConvergence(50));
          //  opty.Add(new MaxSpanInPopulationConvergence(1.5));
            double[] xStar;
            Parameters.Verbosity = OptimizationToolbox.VerbosityLevels.AboveNormal;
            // this next line is to set the Debug statements from OOOT to the Console.
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var timer = Stopwatch.StartNew();

            var fStar = opty.Run(out xStar, design.CompCount * 6);
        }
        #endregion

        #region Simulated Annealing

        private static void OptimizeBySA(Design design)
        {
            //var opty = new GradientBasedOptimization();
            //var opty = new HillClimbing();
            var opty = new SimulatedAnnealing(optimize.minimize);


            /* here is the Dependent Analysis. */
            opty.Add(design);
            // this is the objective function
            opty.Add(new EvaluateNetlist(design));
            // here are three inequality constraints
            opty.Add(new ComponentToComponentOverlap(design));
            opty.Add(new ComponentToContainerOverlap(design));
            opty.Add(new HeatBasic(design));
            //opty.Add(new CenterOfGravity(design));
            opty.Add(new EvaluateBoundingBox(design));



            /******** Set up Design Space *************/
            /* for the GA and the Hill Climbing, a compete discrete space is needed. Face width and
             * location parameters should be continuous though. Consider removing the 800's below
             * when using a mixed optimization method. */
            var dsd = new DesignSpaceDescription(design.CompCount * 6);
            var bounds = design.Container.Ts.Bounds;
            for (var i = 0; i < design.CompCount; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    dsd[6 * i + j] = new VariableDescriptor(bounds[0][j], bounds[1][j], 0.1);
                }
                for (var j = 0; j < 3; j++)
                {
                    dsd[6 * i + 3 + j] = new VariableDescriptor(0, 360, 36);
                }
            }
            opty.Add(dsd);
            opty.Add(new LatinHyperCube(dsd, VariablesInScope.BothDiscreteAndReal));
            opty.Add(new squaredExteriorPenalty(opty, 10));
            opty.Add(new RandomNeighborGenerator(dsd, 100));
            opty.Add(new SACoolingSangiovanniVincentelli(100));
            opty.Add(new MaxTimeConvergence(new TimeSpan(0, 5, 0)));

            opty.ConvergenceMethods.RemoveAll(a => a is MaxSpanInPopulationConvergence);
            /* the deltaX convergence needs to be removed as well, since RHC will end many iterations
             * at the same point it started. */
            opty.ConvergenceMethods.RemoveAll(a => a is DeltaXConvergence);
            double[] xStar;
            Parameters.Verbosity = OptimizationToolbox.VerbosityLevels.AboveNormal;
            // this next line is to set the Debug statements from OOOT to the Console.
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var timer = Stopwatch.StartNew();

            var fStar = opty.Run(out xStar, design.CompCount * 6);
        }

        #endregion

        #region Random Hill Climbing
        private static void OptimizeByRHC(Design design)
        { 
            SearchIO.output("******************Random Hill Climbing ***********************");
            var opty = new HillClimbing();
            /* here is the Dependent Analysis. */
            opty.Add(design);
            // this is the objective function
            opty.Add(new EvaluateNetlist(design));
            // here are three inequality constraints
            opty.Add(new ComponentToComponentOverlap(design));
            opty.Add(new ComponentToContainerOverlap(design));
            opty.Add(new HeatBasic(design));
            //opty.Add(new CenterOfGravity(design));
            opty.Add(new EvaluateBoundingBox(design));

            /******** Set up Design Space *************/
            /* for the GA and the Hill Climbing, a compete discrete space is needed. Face width and
             * location parameters should be continuous though. Consider removing the 800's below
             * when using a mixed optimization method. */
            var dsd = new DesignSpaceDescription(design.CompCount * 6);
            var bounds = design.Container.Ts.Bounds;
            for (var i = 0; i < design.CompCount; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    dsd[6 * i + j] = new VariableDescriptor(bounds[0][j], bounds[1][j], 0.1);
                }
                for (var j = 0; j < 3; j++)
                {
                    dsd[6 * i + 3 + j] = new VariableDescriptor(0, 360, 36);
                }
            }
            opty.Add(dsd);
            opty.Add(new LatinHyperCube(dsd, VariablesInScope.BothDiscreteAndReal));
            opty.Add(new squaredExteriorPenalty(opty, 10));
            opty.Add(new RandomNeighborGenerator(dsd, 100));
            opty.Add(new KeepSingleBest(optimize.minimize));
            //opty.Add(new SACoolingSangiovanniVincentelli(100));
            opty.Add(new MaxTimeConvergence(new TimeSpan(0, 5, 0)));

            opty.ConvergenceMethods.RemoveAll(a => a is MaxSpanInPopulationConvergence);
            /* the deltaX convergence needs to be removed as well, since RHC will end many iterations
             * at the same point it started. */
            opty.ConvergenceMethods.RemoveAll(a => a is DeltaXConvergence);
            double[] xStar;
            Parameters.Verbosity = OptimizationToolbox.VerbosityLevels.AboveNormal;
            // this next line is to set the Debug statements from OOOT to the Console.
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var timer = Stopwatch.StartNew();

            var fStar = opty.Run(out xStar, design.CompCount * 6);


        }

        #endregion

        /* ---------------------------------------------------------------------------------- */
        /* THIS FUNCTION SETS THE INITIAL OBJECTIVE FUNCTION WEIGHTS.                         */
        /* ---------------------------------------------------------------------------------- */

        static void InitWeights(Design design)
        {
            design.objWeight[0] = .08;                  //Netlist
            design.objWeight[1] = 120;                  //Comp2Comp
            design.objWeight[2] = 15;                   //Comp2Cont
            design.objWeight[3] = .0001;                    //Heat
            design.objWeight[4] = 40;                    //BBox
            design.objWeight[5] = 10;                   //CGravity
        }

        static void InitObjValues(Design design)
        {
            for (int i = 0; i < Constants.ObjNum; i++)
            {
                design.NewObjValues[i] = double.PositiveInfinity;
                design.minObjValues[i] = double.PositiveInfinity;
                design.maxObjValues[i] = 0;
            }

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function initializes the component locations.                                 */
        /* ---------------------------------------------------------------------------------- */

        static void InitLocations(Design design)
        {
            Console.WriteLine("Placing components at zero");
            design.DesignVars = new double[design.CompCount,6];
            design.OldDesignVars = new double[design.CompCount,6];
            design.Container.SetToZero();
            design.DesignSpaceMax = new[] { design.Container.Ts.XMax, design.Container.Ts.YMax, design.Container.Ts.ZMax };
            design.DesignSpaceMin = new[] { design.Container.Ts.XMin, design.Container.Ts.YMin, design.Container.Ts.ZMin };
            foreach (var comp in design.Components)
            {
                comp.SetCompToZero();
                for (int i = 0; i < 6; i++)
                {
                    if (i < 3)
                    {
                        design.DesignVars[comp.Index, i] = comp.Ts.Center[i];
                        design.OldDesignVars[comp.Index, i] = comp.Ts.Center[i];
                    }
                    else
                    {
                        design.DesignVars[comp.Index, i] = 0;
                        design.OldDesignVars[comp.Index, i] = 0;
                    }
                }

            }

        }
    }
}