using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.Displayer;

namespace RealTimePPDisplayer.Calculator
{
    class ManiaPerformanceCalculator : PerformanceCalculatorBase
    {
        private const OsuPlayMode s_mode = OsuPlayMode.Mania;
        private const double STRAIN_STEP = 400;
        private const double DECAY_WEIGHT = 0.9;
        private const double STAR_SCALING_FACTOR = 0.018;

        private ModsInfo m_mods;
        private double m_stars;

        #region Mania Difficultty Calculate
        private void CalculateStrainValues()
        {
            var prev_object = Beatmap.ObjectList[0] as ManiaBeatmapObject;

            for (int i = 1; i < Beatmap.ObjectList.Count; i++)
            {
                var cur_object = Beatmap.ObjectList[i] as ManiaBeatmapObject;
                cur_object.ManiaCalculateStrains(prev_object, m_mods.TimeRate);
                prev_object = cur_object;
            }
        }

        private double CalculateDifficulty()
        {
            double actual_strain_step = STRAIN_STEP * m_mods.TimeRate;

            List<double> highest_strains = new List<double>();
            double interval_end_time = actual_strain_step;
            double maximum_strain = 0;

            ManiaBeatmapObject prev = null;

            foreach (ManiaBeatmapObject note in Beatmap.ObjectList)
            {
                while (note.StartTime > interval_end_time)
                {
                    highest_strains.Add(maximum_strain);

                    if (prev == null)
                        maximum_strain = 0;
                    else
                    {
                        double individual_decay = Math.Pow(ManiaBeatmapObject.INDIVIDUAL_DECAY_BASE, (interval_end_time - prev.StartTime) / 1000.0);
                        double overall_decay = Math.Pow(ManiaBeatmapObject.OVERALL_DECAY_BASE, (interval_end_time - prev.StartTime) / 1000.0);
                        maximum_strain = prev.IndividualStrain * individual_decay + prev.OverallStrain * overall_decay;
                    }

                    interval_end_time += actual_strain_step;
                }

                double strain = note.IndividualStrain + note.OverallStrain;
                if (strain > maximum_strain)
                    maximum_strain = strain;

                prev = note;
            }

            double diff = 0;
            double weigth = 1;

            highest_strains.Sort((a, b) => b.CompareTo(a));

            foreach (var strain in highest_strains)
            {
                diff += strain * weigth;
                weigth *= DECAY_WEIGHT;
            }

            return diff;
        }
        #endregion

        public override PPTuple GetPP(ModsInfo mods)
        {
            if (Beatmap == null) return PPTuple.Empty;


            if (!_init||m_mods!=mods)
            {
                m_mods = mods;
                CalculateStrainValues();
                m_stars = CalculateDifficulty() * STAR_SCALING_FACTOR;
                Sync.Tools.IO.CurrentIO.Write($"难度:{m_stars}*");
                _init = true;
            }


            return PPTuple.Empty;
        }

        private bool _init = false;

        public override void ClearCache()
        {
            if (Beatmap == null) return;
            _init = false;
            foreach (var obj in Beatmap.ObjectList)
                (obj as ManiaBeatmapObject).ClearStrainsValue();
        }
    }
}
