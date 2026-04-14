using System;
using System.Collections.Generic;

namespace AccsaberLeaderboard.Calculators
{
    internal class APCalc
    {
        internal List<(double, double)> PointList { get; } = new List<(double, double)>()
        {
            ( 0, 0 ),
            ( 0.9409324581850277, 0.22864617746193472 ),
            ( 0.9421364984358537, 0.2344589535514457 ),
            ( 0.943340858876901, 0.24028320272237846 ),
            ( 0.9445528451823, 0.24615905401143912 ),
            ( 0.9457521950057954, 0.2519929536323373 ),
            ( 0.9469652757511278, 0.25791818908545416 ),
            ( 0.9481613689691947, 0.26378971348304325 ),
            ( 0.9493682127874609, 0.26974882033448855 ),
            ( 0.9505744372202971, 0.27574542941084734 ),
            ( 0.9517783524884541, 0.2817769019690621 ),
            ( 0.9529892330175649, 0.287896269816159 ),
            ( 0.9541947185853665, 0.2940478274623703 ),
            ( 0.9554044516127758, 0.30028781869718807 ),
            ( 0.9566054381494079, 0.30655637705110717 ),
            ( 0.957807698698665, 0.31291317977345784 ),
            ( 0.9590221672423604, 0.3194262585871035 ),
            ( 0.9602231628864696, 0.32596720585696376 ),
            ( 0.961433998563471, 0.3326729095043305 ),
            ( 0.9626279572859802, 0.339405506964074 ),
            ( 0.963842342827163, 0.3463882776436415 ),
            ( 0.965050103040447, 0.3534815900715788 ),
            ( 0.96624960935703, 0.36068799949007235 ),
            ( 0.9674529869368587, 0.3680959084197356 ),
            ( 0.9686591348667645, 0.3757183982279203 ),
            ( 0.9698668993297, 0.38356990598376894 ),
            ( 0.9710750806787853, 0.39166641973515987 ),
            ( 0.9722824425660789, 0.4000257103100814 ),
            ( 0.9734877233230004, 0.408667608162045 ),
            ( 0.9746896497445529, 0.41761433619185573 ),
            ( 0.9759017934808459, 0.42700834856603836 ),
            ( 0.977108280868104, 0.43677295486970896 ),
            ( 0.9783078742661878, 0.44694216753773164 ),
            ( 0.9795145289674262, 0.4576932612610576 ),
            ( 0.9807121922419519, 0.46894998821359746 ),
            ( 0.981930401543003, 0.48108071091217086 ),
            ( 0.9831227248036967, 0.4937134311802671 ),
            ( 0.9843344315883069, 0.5074369975183908 ),
            ( 0.9855345565106794, 0.5220472890164811 ),
            ( 0.9867538435462135, 0.5381018792987169 ),
            ( 0.9879462160499057, 0.5551918317559756 ),
            ( 0.989158767583543, 0.5742496799950565 ),
            ( 0.9903616429313051, 0.5951727896238259 ),
            ( 0.9915723216173138, 0.6187204908473445 ),
            ( 0.9927779343719173, 0.6452713618738384 ),
            ( 0.9939826353978779, 0.6757582832177143 ),
            ( 0.9951928260723995, 0.7116318568448161 ),
            ( 0.99638391362715, 0.7539893920553304 ),
            ( 0.9975978174482817, 0.8078649708462118 ),
            ( 0.9988016676122579, 0.8810362590039038 ),
            ( 0.9997988680153226, 1 ),
            ( 1, 1 )
        };
        public readonly string StarLabel = "<color=#00FF00>Complexity</color>";
        public static readonly APCalc Instance = new();
        private APCalc()
        {
            PointList.Reverse();
        }
        public float GetPp(float acc, float complexity) => GetCurve(acc) * (complexity + 18) * 61;
        public float GetAccDeflated(float deflatedPp, float complexity, int precision = -1)
        {
            if (deflatedPp > GetPp(1.0f, complexity)) return precision < 0 ? 1.0f : 100.0f;
            float outp = InvertCurve(deflatedPp / (complexity + 18) * 61);
            return precision < 0 ? outp : (float)Math.Round(outp * 100.0f, precision);
        }
        public float GetCurve(float acc) => GetCurve(acc, PointList);
        public float InvertCurve(double curveOutput) => GetInvertCurve(curveOutput, PointList);
        public static float GetCurve(float acc, List<(double, double)> curve)
        {
            int i = 1;
            while (i < curve.Count && curve[i].Item1 > acc) i++;
            double middle_dis = (acc - curve[i - 1].Item1) / (curve[i].Item1 - curve[i - 1].Item1);
            return (float)(curve[i - 1].Item2 + middle_dis * (curve[i].Item2 - curve[i - 1].Item2));
        }
        public static float GetInvertCurve(double curveOutput, List<(double, double)> curve)
        {
            int i = 1;
            while (i < curve.Count && curve[i].Item2 > curveOutput) i++;
            double middle_dis = (curveOutput - curve[i - 1].Item2) / (curve[i].Item2 - curve[i - 1].Item2);
            return (float)(curve[i - 1].Item1 + middle_dis * (curve[i].Item1 - curve[i - 1].Item1));
        }
    }
}
