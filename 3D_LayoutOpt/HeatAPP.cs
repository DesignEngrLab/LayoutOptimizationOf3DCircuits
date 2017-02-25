namespace _3D_LayoutOpt
{
    class HeatAPP
    {

        /* ---------------------------------------------------------------------------------- */
        /*                                                                                    */
        /*                        HEATAPP.C -- Approximation Method                           */
        /*                                                                                    */
        /* ---------------------------------------------------------------------------------- */

        /* ---------------------------------------------------------------------------------- */
        /* This function corrects the approximation method by comparing it to the LU method.  */
        /* The value for design.hcf (heat correction factor) will be used by the app_method. */
        /* ---------------------------------------------------------------------------------- */
        public static void correct_APP_by_LU(Design design)
        {
            Component comp;
            double tempapp, tempMM;
            tempMM = 0.0;
            design.hcf = 1.0;
            design.gauss = 0;
            thermal_analysis_APP(design);
            tempapp = design.components[0].temp;


            heatMM.thermal_analysis_MM(design);
            int i = 0;
            comp = design.components[i];
            while (comp != null)
            {
	        /*tempMM += comp.temp/COMP_NUM;*/
	            if (tempMM<comp.temp)
                    tempMM = comp.temp;
                i++;
                comp = design.components[i];
            }
            design.hcf = tempMM/tempapp;
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function finds the temperature at the center of each component and places     */
        /* that value in comp.temp.  Because of the nature of this function it needs to      */
        /* re-calculated for each iteration, instead of just being updated.                   */
        /* ---------------------------------------------------------------------------------- */
        public static void thermal_analysis_APP(Design design)
        {
            Component comp;
            double Tave, Rtot, Qtot = 0.0, Kave = 0.0, Have = 0.0;
            double box_x_dim, box_y_dim, box_z_dim, box_area, box_volume;
            int i = 0;

            box_x_dim = design.box_max[0] - design.box_min[0];
            box_y_dim = design.box_max[1] - design.box_min[1];
            box_z_dim = design.box_max[2] - design.box_min[2];

            box_volume = box_x_dim* box_y_dim * box_z_dim;
            box_area = 2*(box_x_dim* box_y_dim + box_x_dim* box_z_dim + box_y_dim* box_z_dim);

            comp = design.components[i];
            while (i < design.components.Count)
            {
                Qtot += comp.q;
                Kave += (comp.k)/Constants.COMP_NUM;
                i++;
                if (i < Constants.COMP_NUM - 1)
                    comp = design.components[i];
            }
            Kave = Kave*(design.volume/box_volume) + (design.kb)*(1 - design.volume/box_volume);

            Have = ((design.h[0]) + (design.h[1]) + (design.h[2]))/Constants.DIMENSION;

            /*  Rtot = (box_area/(Kave*box_volume)) + 1/(Have*box_area);*/
            Rtot = ((box_x_dim + box_y_dim + box_z_dim)/(Kave* box_area)) + 1/(Have* box_area);
            Tave = (design.tamb) + design.hcf*(Qtot* Rtot);

            i = 0;
            comp = design.components[i];
            while (i < design.components.Count)
            {
                comp.temp = Tave;
                i++;
                if (i < Constants.COMP_NUM - 1)
                    comp = design.components[i];
            }
        }
    }
}
