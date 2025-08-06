using System;
using System.Collections.Generic;
using System.Linq;

namespace Velo
{
    public class MacroDetection : Module
    {
        private struct InputState
        {
            public byte State;

            public InputState(Player player)
            {
                State = (byte)(
                    (player.leftHeld    ? 1 << 0 : 0) |
                    (player.rightHeld ? 1 << 1 : 0) |
                    (player.jumpHeld ? 1 << 2 : 0) |
                    (player.grappleHeld && (player.canGrapple || player.grappling) 
                                           ? 1 << 3 : 0) |
                    (player.slideHeld && !player.grappling && !player.slideCancelled &&
                        ((player.actor.Velocity.Length() >= 300f && (player.game_time.TotalGameTime - player.slideTime).TotalSeconds > 0.5) || player.sliding)
                                           ? 1 << 4 : 0)
                    );
            }

            public bool Grappling
            {
                get
                {
                    return ((State >> 3) & 1) == 1;
                }
                set
                {
                    State = (byte)(State & ~(1 << 3));
                    State = (byte)(State | (value ? 1 << 3 : 0));
                }
            }

            public override bool Equals(object obj)
            {
                return obj is InputState state &&
                       State == state.State;
            }

            public override int GetHashCode()
            {
                return -1319491066 + State.GetHashCode();
            }
        }

        private class InputSequence
        {
            public List<InputState> Inputs = new List<InputState>();

            public InputSequence Clone()
            {
                return new InputSequence { Inputs = Inputs.ToList() };
            }

            public override bool Equals(object obj)
            {
                return obj is InputSequence sequence && Inputs.SequenceEqual(sequence.Inputs);
            }

            public override int GetHashCode()
            {
                const int seed = 487;
                const int modifier = 31;

                unchecked
                {
                    return Inputs.Aggregate(seed, (current, item) =>
                        (current * modifier) + item.GetHashCode());
                }
            }
        }

        public static double[] AvgAndStdDeviation(IEnumerable<double> values, int ignoreOutliers)
        {
            double sum = 0;
            double avg;
            foreach (double value in values)
            {
                sum += value;
            }

            List<int> ignore = new List<int>();

            for (int i = 0; i < ignoreOutliers; i++)
            {
                double max = double.NegativeInfinity;
                int maxK = 0;
                double maxVal = 0d;
                int k = 0;
                avg = sum / (values.Count() - ignore.Count);
                foreach (double value in values)
                {
                    if (ignore.Contains(k))
                    {
                        k++;
                        continue;
                    }
                    double diff = Math.Abs(value - avg);
                    if (diff > max)
                    {
                        max = diff;
                        maxK = k;
                        maxVal = value;
                    }
                    k++;
                }
                ignore.Add(maxK);
                sum -= maxVal;
            }

            double variance = 0d;
            int j = 0;
            avg = sum / (values.Count() - ignore.Count);
            foreach (double value in values)
            {
                if (ignore.Contains(j++))
                    continue;
                double diff = Math.Abs(value - avg);
                variance += diff * diff;
            }
            variance /= values.Count() - ignore.Count;
            return new double[] { avg, Math.Sqrt(variance) };
        }

        public struct Timing
        {
            public double Avg;
            public double StdDeviation;
            public int InputIndex;
            public int Padding;

            public Timing(int inputIndex, IEnumerable<double> values, int ignoreOutliers)
            {
                InputIndex = inputIndex;
                double[] avgAndStdDeviation = AvgAndStdDeviation(values, ignoreOutliers);
                Avg = avgAndStdDeviation[0];
                StdDeviation = avgAndStdDeviation[1];
                Padding = 0;
            }
        }

        private bool reset = false;
        private TimeSpan start;
        private InputSequence currentInputs = new InputSequence();
        // stores for each input sequence (of length 4 to 8) the time of when the last input was performed
        private readonly Dictionary<InputSequence, Queue<double>> allSequences = new Dictionary<InputSequence, Queue<double>>();

        private MacroDetection() : base("Macro Detection")
        {

        }

        public static MacroDetection Instance = new MacroDetection();

        public override void Init()
        {
            base.Init();

            Velo.AddOnMainPlayerReset(OnMainPlayerReset);
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            if (!Velo.Ingame || Velo.Online || OfflineGameMods.Instance.RecordingAndReplay.IsPlaybackRunning)
                return;

            if (Velo.ModuleSolo != null)
            {
                if (Velo.Ingame && Velo.PausedPrev && !Velo.Paused)
                {
                    reset = true;
                }
            }

            if (reset)
            {
                start = Velo.RealTime;
                currentInputs = new InputSequence();
                currentInputs.Inputs.Add(new InputState(Velo.MainPlayer));
                reset = false;
            }

            if (currentInputs.Inputs.Count == 0)
                return;

            InputState input = new InputState(Velo.MainPlayer);

            if (input.State != currentInputs.Inputs[currentInputs.Inputs.Count - 1].State)
            {
                currentInputs.Inputs.Add(input);

                if (currentInputs.Inputs.Count <= 8)
                {
                    if (!allSequences.ContainsKey(currentInputs))
                        allSequences.Add(currentInputs.Clone(), new Queue<double>());
                    Queue<double> queue = allSequences[currentInputs];
                    if (queue.Count >= 12)
                        queue.Dequeue();
                    queue.Enqueue((Velo.RealTime - start).TotalSeconds);
                }
            }
        }

        public List<Timing> GetTimings()
        {
            List<Timing> timings = new List<Timing>();
            for (int i = 2; i <= 8 && i < currentInputs.Inputs.Count; i++)
            {
                InputSequence sequence = new InputSequence
                {
                    Inputs = currentInputs.Inputs.Take(i).ToList()
                };
                if (!allSequences.ContainsKey(sequence))
                    break;
                Queue<double> times = allSequences[sequence];
                if (times.Count < 10)
                    break;
                timings.Add(new Timing(i, times, times.Count - 10));
            }
            return timings;
        }

        private void OnMainPlayerReset()
        {
            reset = true;
        }
    }
}
