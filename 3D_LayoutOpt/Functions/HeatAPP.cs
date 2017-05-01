namespace _3D_LayoutOpt
{
    class HeatApp
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
            double tempapp, tempMm;
            tempMm = 0.0;
            design.Hcf = 1.0;
            design.Gauss = 0;
            ThermalAnalysisAPP(design);
            tempapp = design.Components[0].Temp;

            HeatMm.ThermalAnalysisMM(design);

            for (var i = 0; i < design.CompCount; i++)
            {
                comp = design.Components[i];
                if (tempMm < comp.Temp)
                    tempMm = comp.Temp;
            }
            design.Hcf = tempMm/tempapp;
        }

        /* ---------------------------------------------------------------------------------- */
        /* This function finds the temperature at the center of each component and places     */
        /* that value in comp.temp.  Because of the nature of this function it needs to      */
        /* re-calculated for each iteration, instead of just being updated.                   */
        /* ---------------------------------------------------------------------------------- */
        public static void ThermalAnalysisAPP(Design design)
        {
            Component comp;
            double tave, rtot, qtot = 0.0, kave = 0.0, have = 0.0;
            double boxXDim, boxYDim, boxZDim, boxArea, boxVolume;

            boxXDim = design.BoxMax[0] - design.BoxMin[0];
            boxYDim = design.BoxMax[1] - design.BoxMin[1];
            boxZDim = design.BoxMax[2] - design.BoxMin[2];

            boxVolume = boxXDim* boxYDim * boxZDim;
            boxArea = 2*(boxXDim* boxYDim + boxXDim* boxZDim + boxYDim* boxZDim);

            for (var i = 0; i < design.CompCount; i++)
            {
                comp = design.Components[i];
                qtot += comp.Q;
                kave += (comp.K) / design.CompCount;
            }

            kave = kave*(design.Volume/boxVolume) + (design.Kb)*(1 - design.Volume/boxVolume);

            have = ((design.H[0]) + (design.H[1]) + (design.H[2]))/Constants.Dimension;

            /*  Rtot = (box_area/(Kave*box_volume)) + 1/(Have*box_area);*/
            rtot = ((boxXDim + boxYDim + boxZDim)/(kave* boxArea)) + 1/(have* boxArea);
            tave = (design.Tamb) + design.Hcf*(qtot* rtot);


            for (var i = 0; i < design.CompCount; i++)
            {
                comp = design.Components[i];                                                                            //SETTING Tave TO ALL COMPONENTS
                comp.Temp = tave;
            }
        }
    }
}
