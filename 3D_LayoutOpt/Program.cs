using _3D_LayoutOpt.Functions;
using OptimizationToolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace _3D_LayoutOpt
{
    class Program
    {

        private static readonly string[] FileNames = {
            "../../../TestFiles/ABF.ply"
            // "../../../TestFiles/Beam_Boss.STL",
            // //"../../../TestFiles/bigmotor.amf",
            // //"../../../TestFiles/DxTopLevelPart2.shell",
            };

        static void Main(string[] args)
        {
            int i, which;
            double eval, h, w, l;
            char wait;
            Design design = new Design();
            Component comp;


            Directory.SetCurrentDirectory("../../workspace");
            var stopwatch = new Stopwatch();
            stopwatch.Start();


            // IMPORTING CAD MODELS, COMPONENT AND CONTAINER FEATURES
            IO.ImportData(design);
            Console.WriteLine("{0} components were read in from the file.\n", design.comp_count);

            //INITIALIZING THE PROCESS

            Console.WriteLine("Initializing locations.\n");
            InitLocations(design);

            Console.WriteLine("Initializing weights.\n");
            InitWeights(design);

            optimize(design);

            IO.SaveDesign(design);
            IO.SaveContainer(design);
            IO.SaveTfield(design);

            /* DownHill(design, MIN_MOVE_DIST);      */
            stopwatch.Stop();
            var timeElapsed = stopwatch.Elapsed;
            using (StreamWriter writetext = new StreamWriter("results"))
            {
                if (design.new_obj_values[1] != 0.0)
                {
                    writetext.WriteLine("*** THE FINAL OVERLAP WAS NOT ZERO!!!");
                    Console.WriteLine("*** THE FINAL OVERLAP WAS NOT ZERO!!!");
                }
                writetext.WriteLine("The elapsed time was {0} seconds", timeElapsed);
                Console.WriteLine("The elapsed time was {0} seconds", timeElapsed);
            }
            Console.ReadKey();


        }

        private static void optimize(Design design)
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
            var dsd = new DesignSpaceDescription(design.comp_count * 6);
            var bounds = design.container.ts.Bounds;
            for (var i = 0; i < design.comp_count; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    dsd[4 * i + j] = new VariableDescriptor(bounds[j][0], bounds[j][1], 0.1);
                }
                for (int j = 0; j < 3; j++)
                {
                    dsd[4 * i + 3 + j] = new VariableDescriptor(0, 360, 36);
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
            var fStar = opty.Run(out xStar, design.comp_count * 6);
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function sets the initial objective function weights to 1.0.                  */
        /* ---------------------------------------------------------------------------------- */
        static void InitWeights(Design design)
        {
            int i;
            for (i = 0; i < Constants.OBJ_NUM; ++i)
                design.weight[i] = 1.0;
            design.weight[3] = 0.01;
            design.weight[1] = 2.5;

        }

        /* ---------------------------------------------------------------------------------- */
        /* This function initializes the component locations.                                 */
        /* ---------------------------------------------------------------------------------- */
        static void InitLocations(Design design)
        {
            Component temp_comp = null;
            Console.WriteLine("Placing components at zero");

            double[,] BackTransformMatrix = null;
            foreach (var comp in design.components)
                comp.ts.SetToOriginAndSquareTesselatedSolid(out BackTransformMatrix);
        }
    }
}
