// based on SimpleGate v1.10 © 2006, ChunkWare Music Software, OPEN-SOURCE

using NAudio.Dsp;
using NAudio.Utils;

namespace RealtimeInteractiveConsole
{
    class SimpleGate : AttRelEnvelope
    {
        // transfer function
        private double _threshdB;	// threshold (dB)
        private double _thresh;		// threshold (linear)
        
        // runtime variables
        private double env;		// over-threshold envelope (linear)

        //public SimpleGate()
        //    : base(10.0, 10.0, 44100.0)
        //{
        //    _threshdB = 0.0;
        //    _thresh = 1.0;
        //    env = DC_OFFSET;
        //}

        public SimpleGate(double threshold=0,double attackMs=20.0, double releaseMs = 20.0, double sampleRate=44100.0)
            : base(attackMs, releaseMs, sampleRate)
        {
            Threshold = threshold;
            env = DC_OFFSET;

        }

        //public void Process( ref short in1, ref short in2 )
        //{
        //    // in/out pointers are assumed to reference stereo data

        //    // sidechain

        //    // rectify input
        //    short rect1 = Math.Abs( in1 );	// n.b. was fabs
        //    short rect2 = Math.Abs( in2 ); // n.b. was fabs

        //    // if desired, one could use another EnvelopeDetector to smooth
        //    // the rectified signal.

        //    short key = Math.Max( rect1, rect2 );	// link channels with greater of 2

        //    // threshold
        //    double over = ( key > _thresh ) ? 1.0 : 0.0;	// key over threshold ( 0.0 or 1.0 )

        //    // attack/release
        //    over += DC_OFFSET;				// add DC offset to avoid denormal

        //    env = Run(over, env);	// run attack/release

        //    over = env - DC_OFFSET;		// subtract DC offset

        //    // Regarding the DC offset: In this case, since the offset is added before 
        //    // the attack/release processes, the envelope will never fall below the offset,
        //    // thereby avoiding denormals. However, to prevent the offset from causing
        //    // constant gain reduction, we must subtract it from the envelope, yielding
        //    // a minimum value of 0dB.

        //    // output gain
        //    in1 *= (short)over;	// apply gain reduction to input
        //    in2 *= (short)over;
        //}

        public bool Process(Span<short> input)
        {
            var aboveThreshold = false;
            for (var index = 0; index < input.Length; index++)
            {
                var in1 = input[index];
                // rectify input
                short key = Math.Abs(in1); // n.b. was fabs

                // every 1000th sample, print the key value
                if (index % 1000 == 0)
                {
                    System.Console.Write($"key: {key}");
                }


                // if desired, one could use another EnvelopeDetector to smooth
                // the rectified signal.

                // threshold
                if (key > _thresh) { aboveThreshold = true; }

                double over = (key > _thresh) ? 1.0 : 0.0; // key over threshold ( 0.0 or 1.0 )
                //double over = 1.0;
                // attack/release
                over += DC_OFFSET; // add DC offset to avoid denormal

                env = Run(over, env); // run attack/release

                over = env - DC_OFFSET; // subtract DC offset

                // Regarding the DC offset: In this case, since the offset is added before 
                // the attack/release processes, the envelope will never fall below the offset,
                // thereby avoiding denormals. However, to prevent the offset from causing
                // constant gain reduction, we must subtract it from the envelope, yielding
                // a minimum value of 0dB.

                // output gain
                in1 = (short)(in1*over); // apply gain reduction to input
                if (index % 1000 == 0)
                {
                    System.Console.WriteLine($" {in1}");
                }
            }
            return aboveThreshold;
        }


        public double Threshold 
        {
            get => _thresh;
            set
            {
                _thresh = value;
                _threshdB =Decibels.LinearToDecibels(value);
            }
        }
    }
}
