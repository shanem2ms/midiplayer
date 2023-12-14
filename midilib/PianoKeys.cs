using System;
using System.Collections.Generic;
using System.Text;

namespace midilib
{
    public class Piano
    {
        public class PianoKey
        {
            public float x;
            public float y;
            public float ys;
            public bool isBlack;
            public uint channelsOn = 0;
        }

        public PianoKey[] PianoKeys = new PianoKey[88];
        public float PianoTopY = 0;
        public float PianoWhiteXs = 0;
        public float PianoBlackXs = 0;

        public Piano(bool glScale)
        {
            BuildPianoKeys(glScale);
        }
        void BuildPianoKeys(bool glScale)
        {
            int nWhiteKeys = 52;
            float xscale = 1.0f / (float)(nWhiteKeys + 1);
            float xleft = 0;
            bool[] hasBlackKey = { true, false, true, true, false, true, true };

            PianoWhiteXs = xscale * 0.8f;
            PianoBlackXs = PianoWhiteXs * 0.75f;

            int keyIdx = 0;
            for (int i = 0; i < nWhiteKeys; i++)
            {
                float xval = xleft + (i + 0.5f) * xscale;

                PianoKeys[keyIdx++] = new PianoKey { isBlack = false, x = xval, y = 0.5f, ys = 1 };

                int note = i % 7;
                if (!hasBlackKey[note] || keyIdx >= 88)
                    continue;

                xval = xleft + (i + 1) * xscale;
                PianoKeys[keyIdx++] = new PianoKey { isBlack = true, x = xval, y = 0.3f, ys = 0.6f };
            }

            if (glScale)
            {
                float yscale = 0.1f;
                this.PianoTopY = 1 - yscale;
                for (int i = 0; i < PianoKeys.Length; ++i)
                {
                    PianoKeys[i].y = 1 - PianoKeys[i].y;
                    PianoKeys[i].y *= yscale;
                    PianoKeys[i].ys *= yscale;
                    PianoKeys[i].y = 1 - PianoKeys[i].y;
                }
            }
        }
    }
}
