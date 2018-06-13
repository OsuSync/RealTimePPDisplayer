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
        private double m_beatmap_stars;
        PPTuple tuple = PPTuple.Empty;

        #region Mania Difficultty Calculate
        private void CalculateStrainValues(int nobjects)
        {
            var prev_object = Beatmap.Objects[0] as ManiaBeatmapObject;

            for (int i = 1; i < nobjects; i++)
            {
                var cur_object = Beatmap.Objects[i] as ManiaBeatmapObject;
                cur_object.ManiaCalculateStrains(prev_object, m_mods.TimeRate);
                prev_object = cur_object;
            }
        }

        private double CalculateDifficulty(int nobjects)
        {
            double actual_strain_step = STRAIN_STEP * m_mods.TimeRate;

            List<double> highest_strains = new List<double>();
            double interval_end_time = actual_strain_step;
            double maximum_strain = 0;

            ManiaBeatmapObject prev = null;

            for (int i = 0; i < nobjects; i++)
            {
                ManiaBeatmapObject note = Beatmap.Objects[i] as ManiaBeatmapObject;

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

        private int _nobjects = 0;

        public override PPTuple GetPP()
        {
            if (Beatmap == null) return PPTuple.Empty;
            if (Beatmap.Mode != s_mode) return PPTuple.Empty;

            //Calculate Max PP
            if (!_init || m_mods != Mods)
            {
                m_mods = Mods;
                CalculateStrainValues(Beatmap.ObjectsCount);
                m_beatmap_stars = CalculateDifficulty(Beatmap.ObjectsCount) * STAR_SCALING_FACTOR;
                CalculatePerformance(m_beatmap_stars, 1000000, 100.0, Beatmap.ObjectsCount, out tuple.MaxPP, out tuple.MaxSpeedPP, out tuple.MaxAccuracyPP);
                Sync.Tools.IO.CurrentIO.Write($"[RTPPD::Mania]Difficulty:{m_beatmap_stars:F2}*");
                _init = true;
            }

            //Calculate RTPP
            int nobjects = GetCurrentObjectCount(Time);
            if (nobjects != _nobjects)
            {
                ReinitializeObjects();

                CalculateStrainValues(nobjects);
                double stars = CalculateDifficulty(nobjects) * STAR_SCALING_FACTOR;

                double acc = Accuracy * 100;

                CalculatePerformance(stars, RealScore, acc, nobjects, out tuple.RealTimePP, out tuple.RealTimeSpeedPP, out tuple.RealTimeAccuracyPP);
                _nobjects = nobjects;
            }
            //No Fc pp

            return tuple;
        }

        private bool _init = false;
        public override void ClearCache()
        {
            base.ClearCache();
            if (Beatmap == null) return;
            _nobjects = 0;
            _init = false;
            ReinitializeObjects();
        }

        private void ReinitializeObjects()
        {
            if (Beatmap.Mode != s_mode) return;
            foreach (ManiaBeatmapObject obj in Beatmap.Objects)
                obj.ClearStrainsValue();
        }

        private void CalculatePerformance(double stars, int score, double accuracy, int objects, out double total, out double strain, out double acc)
        {
            strain = CalculateStrainValue(stars, score, objects);
            acc = CalculateAccuracyValue(accuracy, score, strain, objects);

            double multiplier = 0.8;

            if (Mods.HasMod(ModsInfo.Mods.NoFail))
                multiplier *= 0.9;

            if (Mods.HasMod(ModsInfo.Mods.SpunOut))
                multiplier *= 0.95f;

            if (Mods.HasMod(ModsInfo.Mods.Easy))
                multiplier *= 0.50f;

            total = Math.Pow(Math.Pow(acc, 1.1) + Math.Pow(strain, 1.1), 1.0 / 1.1) * multiplier;
        }

        private double CalculateAccuracyValue(double acc, int score, double strain, int objects)
        {
            if (HitWindow300 <= 0)
            {
                return 0;
            }

            double acc_value = Math.Max(0.0, 0.2 - ((HitWindow300 - 34) * 0.006667)) * strain
                * Math.Pow((Math.Max(0.0, score - 960000) / 40000.0), 1.1);
            return acc_value;
        }

        private double CalculateStrainValue(double stars, int score, int objects)
        {
            double strain_value = Math.Pow(5.0 * Math.Max(1.0, stars / 0.2) - 4.0, 2.2) / 135.0;
            strain_value *= 1 + 0.1 * Math.Min(1.0, objects / 1500.0);

            if (score <= 500000)
                strain_value *= (score / 500000.0) * 0.1;
            else if (score <= 600000)
                strain_value *= (score - 500000.0) / 100000.0 * 0.3;
            else if (score <= 700000)
                strain_value *= 0.30 + (score - 600000.0) / 100000.0 * 0.25;
            else if (score <= 800000)
                strain_value *= 0.55 + (score - 700000.0) / 100000.0 * 0.2;
            else if (score <= 900000)
                strain_value *= 0.75 + (score - 800000.0) / 100000.0 * 0.15;
            else
                strain_value *= 0.90 + (score - 900000.0) / 100000.0 * 0.1;

            return strain_value;
        }

        private int GetCurrentObjectCount(int time)
        {
            for (int i = 0; i < Beatmap.ObjectsCount; i++)
                if (Beatmap.Objects[i].StartTime > time)
                    return i + 1;
            return Beatmap.ObjectsCount;
        }

        public override double Accuracy{
            get
            {
                int total = (Count300 + CountGeki + CountKatu + Count100 + Count50 + CountMiss);
                double acc = 1.0;
                if(total > 0)
                 acc = ((Count300 + CountGeki) * 300.0 + CountKatu * 200.0 + Count100 * 100.0 + Count50 * 50) / (total * 300.0);
                return acc;
            }
        }

        private double HitWindow300
        {
            get
            {
                double od = Math.Min(10.0, Math.Max(0, 10.0 - Beatmap.OverallDifficulty));
                od = 34 + 3 * od;

                if (Mods.HasMod(ModsInfo.Mods.Easy))
                    od *= 1.4;

                if (Mods.HasMod(ModsInfo.Mods.HardRock))
                    od /= 1.4;

                if (Mods.HasMod(ModsInfo.Mods.HalfTime))
                    od *= 0.75;

                if (Mods.HasMod(ModsInfo.Mods.DoubleTime))
                    od *= 1.5;

                return (int)od;
            }
        }

        private int RealScore
        {
            get
            {
                double score_multiplier = 1.0;
                if (m_mods.HasMod(ModsInfo.Mods.Easy)) score_multiplier *= 0.5;
                if (m_mods.HasMod(ModsInfo.Mods.NoFail)) score_multiplier *= 0.5;
                if (m_mods.HasMod(ModsInfo.Mods.HalfTime)) score_multiplier *= 0.5;
                return (int)(Score / score_multiplier);
            }
        }

        private double RealOverallDifficulty{
            get
            {
                double od=Beatmap.OverallDifficulty;

                if (Mods.HasMod(ModsInfo.Mods.Easy))
                    od = Math.Max(0, od / 2);

                if (Mods.HasMod(ModsInfo.Mods.HardRock))
                    od = Math.Min(10, od * 1.4);

                return od;
            }
        }

    }
}
