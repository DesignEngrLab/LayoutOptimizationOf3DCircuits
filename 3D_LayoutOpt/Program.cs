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

        static void Main(string[] args)
        {
            int i;
            double eval, h, w, l;
            char wait;
            var design = new Design();
            Component comp;


            Directory.SetCurrentDirectory("../../workspace");
            var stopwatch = new Stopwatch();
            stopwatch.Start();


            // IMPORTING CAD MODELS, COMPONENT AND CONTAINER FEATURES
            Io.ImportData(design);
            Console.WriteLine("{0} components were read in from the file.\n", design.CompCount);

            //INITIALIZING THE PROCESS
            design.InitializeOverlapMatrix();

            Console.WriteLine("INITIALIZING LOCATIONS.\n");
            InitLocations(design);

            Console.WriteLine("Initializing weights.\n");
            InitWeights(design);

            Optimize(design);

            Io.SaveDesign(design);
            Io.SaveContainer(design);
            Io.SaveTfield(design);
            foreach (var component in design.Components)
            {
                    Presenter.ShowAndHang(component.Ts);
            }

            /* DownHill(design, MIN_MOVE_DIST);      */
            stopwatch.Stop();
            var timeElapsed = stopwatch.Elapsed;
            using (var writetext = new StreamWriter("results"))
            {
                if (design.NewObjValues[1] != 0.0)
                {
                    writetext.WriteLine("*** THE FINAL OVERLAP WAS NOT ZERO!!!");
                    Console.WriteLine("*** THE FINAL OVERLAP WAS NOT ZERO!!!");
                }
                writetext.WriteLine("The elapsed time was {0} seconds", timeElapsed);
                Console.WriteLine("The elapsed time was {0} seconds", timeElapsed);
            }
            Console.ReadKey();
        }

        private static void Optimize(Design design)
        {
            //var opty = new GradientBasedOptimization();
            //var opty = new HillClimbing();
            var opty = new GeneticAlgorithm();


            /* here is the Dependent Analysis. */
            opty.Add(design);
            // this is the objective function
            opty.Add(new EvaluateNetlist(design));
            // here are three inequality constraints
            opty.Add(new ComponentToComponentOverlap(design));
            opty.Add(new ComponentToContainerOverlap(design));
            opty.Add(new HeatBasic(design));

            /******** Set up Design Space *************/
            /* for the GA and the Hill Climbing, a compete discrete space is needed. Face width and
             * location parameters should be continuous though. Consider removing the 800's below
             * when using a mixed optimization method. */
            var dsd = new DesignSpaceDescription(design.CompCount*6);
            var bounds = design.Container.Ts.Bounds;
            for (var i = 0; i < design.CompCount; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    dsd[6*i + j] = new VariableDescriptor(bounds[0][j], bounds[1][j], 0.1);
                }
                for (var j = 0; j < 3; j++)
                {
                    dsd[6*i + 3 + j] = new VariableDescriptor(0, 360, 36);
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
            opty.Add(new LatinHyperCube(dsd, VariablesInScope.BothDiscreteAndReal));
            opty.Add(new GACrossoverBitString(dsd));
            opty.Add(new GAMutationBitString(dsd));
            opty.Add(new PNormProportionalSelection(OptimizationToolbox.optimize.minimize, true, 0.7));
            //opty.Add(new RandomNeighborGenerator(dsd,3000));
            //opty.Add(new KeepSingleBest(optimize.minimize));
            opty.Add(new squaredExteriorPenalty(opty, 10));
            opty.Add(new MaxAgeConvergence(40, 0.001));
            opty.Add(new MaxFnEvalsConvergence(10000));
            opty.Add(new MaxSpanInPopulationConvergence(15));
            double[] xStar;
            Parameters.Verbosity = OptimizationToolbox.VerbosityLevels.AboveNormal;
            // this next line is to set the Debug statements from OOOT to the Console.
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var timer = Stopwatch.StartNew();
            var fStar = opty.Run(out xStar, design.CompCount*6);
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function sets the initial objective function weights to 1.0.                  */
        /* ---------------------------------------------------------------------------------- */

        static void InitWeights(Design design)
        {
            int i;
            for (i = 0; i < Constants.ObjNum; ++i)
                design.Weight[i] = 1.0;
            design.Weight[3] = 0.01;
            design.Weight[1] = 2.5;
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function initializes the component locations.                                 */
        /* ---------------------------------------------------------------------------------- */

        static void InitLocations(Design design)
        {
            Console.WriteLine("Placing components at zero");
            design.DesignVars = new double[design.CompCount][];
            design.OldDesignVars = new double[design.CompCount][];
            foreach (var comp in design.Components)
            {
                comp.SetCompToZero();
                design.DesignVars[comp.Index] = new double[] {0,0,0,0,0,0};
                design.OldDesignVars[comp.Index] = new double[] { 0, 0, 0, 0, 0, 0 };
            }
  
        }
    }
}