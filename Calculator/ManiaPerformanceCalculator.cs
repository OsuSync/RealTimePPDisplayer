using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuRTDataProvider.Mods;
using RealTimePPDisplayer.Beatmap;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.Utility;
using static OsuRTDataProvider.Mods.ModsInfo;

namespace RealTimePPDisplayer.Calculator
{
    public sealed class ManiaPerformanceCalculator : PerformanceCalculatorBase
    {
        private const int c_mode = 3;//Mania
        private const double c_strainStep = 400;
        private const double c_decayWeight = 0.9;
        private const double c_starScalingFactor = 0.018;

        private uint _mods;
        private PPTuple _tuple = PPTuple.Empty;

        private double _stars = 0.0;
        private double _rt_stars = 0.0;
        public override double Stars => _stars;
        public override double RealTimeStars => _rt_stars;

        #region Mania Difficultty Calculate
        private void CalculateStrainValues(int nobjects)
        {
            var prevObject = Beatmap.Objects[0] as ManiaBeatmapObject;

            for (int i = 1; i < nobjects; i++)
            {
                var curObject = Beatmap.Objects[i] as ManiaBeatmapObject;
                Debug.Assert(curObject != null, nameof(curObject) + " != null");
                curObject.ManiaCalculateStrains(prevObject, ModsUtils.GetTimeRate(_mods));
                prevObject = curObject;
            }
        }

        private double CalculateDifficulty(int nobjects)
        {
            double actualStrainStep = c_strainStep * ModsUtils.GetTimeRate(_mods);

            List<double> highestStrains = new List<double>();
            double intervalEndTime = actualStrainStep;
            double maximumStrain = 0;

            ManiaBeatmapObject prev = null;

            for (int i = 0; i < nobjects; i++)
            {
                ManiaBeatmapObject note = Beatmap.Objects[i] as ManiaBeatmapObject;

                Debug.Assert(note != null, nameof(note) + " != null");
                while (note.StartTime > intervalEndTime)
                {
                    highestStrains.Add(maximumStrain);

                    if (prev == null)
                        maximumStrain = 0;
                    else
                    {
                        double individualDecay = Math.Pow(ManiaBeatmapObject.INDIVIDUAL_DECAY_BASE, (intervalEndTime - prev.StartTime) / 1000.0);
                        double overallDecay = Math.Pow(ManiaBeatmapObject.OVERALL_DECAY_BASE, (intervalEndTime - prev.StartTime) / 1000.0);
                        maximumStrain = prev.IndividualStrain * individualDecay + prev.OverallStrain * overallDecay;
                    }

                    intervalEndTime += actualStrainStep;
                }

                double strain = note.IndividualStrain + note.OverallStrain;
                if (strain > maximumStrain)
                    maximumStrain = strain;

                prev = note;
            }

            double diff = 0;
            double weigth = 1;

            highestStrains.Sort((a, b) => b.CompareTo(a));

            foreach (var strain in highestStrains)
            {
                diff += strain * weigth;
                weigth *= c_decayWeight;
            }

            return diff;
        }
        #endregion

        public override PPTuple GetPerformance()
        {
            if (Beatmap == null) return PPTuple.Empty;
            if (Beatmap.Mode != (int)c_mode) return PPTuple.Empty;


            //Calculate Max PP
            if (!_init || _mods != Mods)
            {
                _mods = Mods;
                CalculateStrainValues(Beatmap.ObjectsCount);
                _stars = CalculateDifficulty(Beatmap.ObjectsCount) * c_starScalingFactor;
                CalculatePerformance(_stars, 1000000, 100.0, Beatmap.ObjectsCount, out _tuple.MaxPP, out _tuple.MaxSpeedPP, out _tuple.MaxAccuracyPP);
                Sync.Tools.IO.CurrentIO.Write($"[RTPPD::Mania]Difficulty:{_stars:F2}*");
                _init = true;
            }

            //Calculate RTPP
            int nobjects = GetCurrentObjectCount(Time);
            ReinitializeObjects();

            CalculateStrainValues(nobjects);
            _rt_stars = CalculateDifficulty(nobjects) * c_starScalingFactor;

            CalculatePerformance(_rt_stars, RealScore, Accuracy, nobjects, out _tuple.RealTimePP, out _tuple.RealTimeSpeedPP, out _tuple.RealTimeAccuracyPP);
            //No Fc pp

            return _tuple;
        }

        private bool _init;
        public override void ClearCache()
        {
            base.ClearCache();
            if (Beatmap == null) return;
            _init = false;
            ReinitializeObjects();
        }

        private void ReinitializeObjects()
        {
            if (Beatmap.Mode != (int)c_mode) return;
            foreach (var o in Beatmap.Objects)
            {
                var obj = (ManiaBeatmapObject) o;
                obj.ClearStrainsValue();
            }
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

            double accValue = Math.Max(0.0, 0.2 - ((HitWindow300 - 34) * 0.006667)) * strain
                * Math.Pow((Math.Max(0.0, score - 960000) / 40000.0), 1.1);
            return accValue;
        }

        private double CalculateStrainValue(double stars, int score, int objects)
        {
            double strainValue = Math.Pow(5.0 * Math.Max(1.0, stars / 0.2) - 4.0, 2.2) / 135.0;
            strainValue *= 1 + 0.1 * Math.Min(1.0, objects / 1500.0);

            if (score <= 500000)
                strainValue *= (score / 500000.0) * 0.1;
            else if (score <= 600000)
                strainValue *= (score - 500000.0) / 100000.0 * 0.3;
            else if (score <= 700000)
                strainValue *= 0.30 + (score - 600000.0) / 100000.0 * 0.25;
            else if (score <= 800000)
                strainValue *= 0.55 + (score - 700000.0) / 100000.0 * 0.2;
            else if (score <= 900000)
                strainValue *= 0.75 + (score - 800000.0) / 100000.0 * 0.15;
            else
                strainValue *= 0.90 + (score - 900000.0) / 100000.0 * 0.1;

            return strainValue;
        }

        private int GetCurrentObjectCount(int time)
        {
            int count = 0;
            foreach (var obj in Beatmap.Objects)
            {
                if (obj.StartTime >= time)
                    return count;
                
                count++;
            }

            return count;
        }

        public override double Accuracy
        {
            get
            {
                int total = (Count300 + CountGeki + CountKatu + Count100 + Count50 + CountMiss);
                double acc = 1;
                if(total > 0)
                 acc = ((Count300 + CountGeki) * 300.0 + CountKatu * 200.0 + Count100 * 100.0 + Count50 * 50) / (total * 300.0);
                return acc * 100;
            }
        }

        private double HitWindow300
        {
            get
            {
                double od = Math.Min(10.0, Math.Max(0, 10.0 - RealOverallDifficulty));
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
                double scoreMultiplier = 1.0;
                if (_mods.HasMod(ModsInfo.Mods.Easy)) scoreMultiplier *= 0.5;
                if (_mods.HasMod(ModsInfo.Mods.NoFail)) scoreMultiplier *= 0.5;
                if (_mods.HasMod(ModsInfo.Mods.HalfTime)) scoreMultiplier *= 0.5;
                return (int)(Score / scoreMultiplier);
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
