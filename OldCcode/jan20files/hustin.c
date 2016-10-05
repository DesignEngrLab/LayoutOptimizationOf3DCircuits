/* ---------------------------------------------------------------------------------- */
/*                                                                                    */
/*                                       HUSTIN.C                                     */
/*                                                                                    */
/* ---------------------------------------------------------------------------------- */

/* ---------------------------------------------------------------------------------- */
/* This function sets the translate move distances and the initial probabilities.     */
/* ---------------------------------------------------------------------------------- */
void init_hustin(hustin)
struct Hustin *hustin;
{
  int i;
  double init_prob, distance, delta_dist, dist;

  dist = MIN_MOVE_DIST;
  delta_dist = (MAX_MOVE_DIST - MIN_MOVE_DIST)/TRANS_NUM;
  init_prob = 1./MOVE_NUM;
  hustin->usable_prob = 1. - MIN_PROB * MOVE_NUM;
  i = -1;
  
  while (++i < TRANS_NUM)
    {
      hustin->move_size[i] = dist;
      dist += delta_dist;
      hustin->prob[i] = init_prob;
      hustin->attempts[i] = 0;
      hustin->delta_c[i] = 0.0;
    }
  while (i < MOVE_NUM)
    {
      hustin->prob[i] = init_prob;
      hustin->attempts[i] = 0;
      hustin->delta_c[i] = 0.0;
      ++i;
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function updates the move set probabilities.                                  */
/* ---------------------------------------------------------------------------------- */
void update_hustin(hustin)
struct Hustin *hustin;
{
  int i;
  double quality_sum;

  quality_sum = 0.0;
  i = -1;
  while (++i < MOVE_NUM)
    {
      if (hustin->attempts[i] > 0)
	  hustin->quality[i] = hustin->delta_c[i]/(1.*hustin->attempts[i]);
      else
	hustin->quality[i] = 0.;
      quality_sum += hustin->quality[i];
    }

  i = -1;

  while (++i < MOVE_NUM)
    {
      hustin->prob[i] = MIN_PROB + hustin->usable_prob * hustin->quality[i] / quality_sum;
    }
}

/* ---------------------------------------------------------------------------------- */
/* This function reinitializes the hustin structure.  This is called after updating   */
/* the probabilities to reset values for the next temperature.                        */
/* ---------------------------------------------------------------------------------- */
void reset_hustin(hustin)
struct Hustin *hustin;
{
  int i;

  i = -1;

  while (++i < MOVE_NUM)
    {
      hustin->attempts[i] = 0;
      hustin->delta_c[i] = 0.0;
    }
}
