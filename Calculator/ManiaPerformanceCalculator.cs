using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuRTDataProvider.Listen;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.Displayer;
using static OsuRTDataProvider.Mods.ModsInfo;

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
            var prev_object = Beatmap.Objects[0] as ManiaBeatmapObject;

            for (int i = 1; i < Beatmap.Objects.Count; i++)
            {
                var cur_object = Beatmap.Objects[i] as ManiaBeatmapObject;
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

            foreach (ManiaBeatmapObject note in Beatmap.Objects)
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

            PPTuple tuple = PPTuple.Empty;

            int nobjects = GetCurrentObjectCount(Time);
            double acc = CalculateManiaAccuracy(Count300, CountGeki, CountKatu, Count100, Count50, CountMiss) *100.0;

            CalculatePerformance(Score,acc, nobjects, out tuple.RealTimePP, out tuple.RealTimeSpeedPP, out tuple.RealTimeAccuracyPP);
            CalculatePerformance(1000000,100.0, Beatmap.ObjectCount, out tuple.MaxPP, out tuple.MaxSpeedPP, out tuple.MaxAccuracyPP);
            //No Fc pp

            return tuple;
        }

        private void CalculatePerformance(int score,double accuracy,int objects,out double total,out double strain,out double acc)
        {
            strain = CalculateStrainValue(score,objects);
            acc = CalculateAccuracyValue(accuracy, objects);
            total = Math.Pow(Math.Pow(acc, 1.1) + Math.Pow(strain, 1.1), 1 / 1.1) * 1.1;
        }

        private double CalculateAccuracyValue(double acc,int objects)
        {
            return Math.Pow((150.0 / (64 - 3 * Beatmap.OverallDifficulty)) * Math.Pow(acc / 100.0, 16), 1.8) * 2.5 * Math.Min(1.15, Math.Pow(objects / 1500.0, 0.3));
        }

        private double CalculateStrainValue(int score,int objects)
        {
            double strain_multipler;
            if (score <= 500000)
                strain_multipler = (score / 500000.0) * 0.1;
            else if (score <= 600000)
                strain_multipler = (score - 500000) / 100000.0 * 0.2 + 0.1;
            else if (score <= 700000)
                strain_multipler = (score - 600000) / 100000.0 * 0.35 + 0.3;
            else if (score <= 800000)
                strain_multipler = (score - 700000) / 100000.0 * 0.2 + 0.65;
            else if (score <= 900000)
                strain_multipler = (score - 800000) / 100000.0 * 0.1 + 0.85;
            else
                strain_multipler = (score - 900000) / 100000.0 * 0.05 + 0.95;

            return (Math.Pow(5 * Math.Max(1, m_stars / 0.0825) - 4, 3) / 110000) * (1 + 0.1 * Math.Min(1, objects / 1500.0)) * strain_multipler;
        }

        private static double CalculateManiaAccuracy(int n300,int n300g,int n200,int n100,int n50,int nmiss)
        {
            return ((n300 + n300g) * 300.0 + n200 * 200.0 + n100 * 100.0 + n50 * 50) / ((n300+n300g+n200+n100+n50+nmiss)*300.0);
        }

        private int GetCurrentObjectCount(int time)
        {
            for (int i = 0; i < Beatmap.ObjectCount; i++)
                if (Beatmap.Objects[i].StartTime > time)
                    return i;
            return Beatmap.ObjectCount;
        }

        private bool _init = false;
        public override void ClearCache()
        {
            if (Beatmap == null) return;
            _init = false;
            foreach (var obj in Beatmap.Objects)
                (obj as ManiaBeatmapObject).ClearStrainsValue();
        }
    }
}
