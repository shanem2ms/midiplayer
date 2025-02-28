﻿using System;
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
            public char KeyLetter;
            public int KeyLetterSharpFlat;
        }

        public PianoKey[] PianoKeys = new PianoKey[128];
        public float PianoTopY = 0;
        public float PianoWhiteXs = 0;
        public float PianoBlackXs = 0;

        public Piano()
        {
            BuildPianoKeys();
        }
        public Piano(float keyFixedWidth)
        {
            BuildPianoKeysFixedWidth(keyFixedWidth);
        }
        void BuildPianoKeys()
        {
            float xleft = 0;
            bool[] hasBlackKey = { true, true, false, true, true, true, false };
            char []keyletters = new char[] { 'C', 'D', 'E', 'F', 'G', 'A', 'B' };
            int nWhiteKeys = 85;
            float xscale = 1.0f / (float)(nWhiteKeys + 1);

            PianoWhiteXs = xscale;// * 0.8f;
            PianoBlackXs = PianoWhiteXs * 0.75f;

            int keyIdx = 0;
            for (int i = 0; keyIdx < PianoKeys.Length; i++)
            {
                float xval = xleft + (i + 0.5f) * xscale;

                int note = i % 7;
                PianoKeys[keyIdx++] = new PianoKey { isBlack = false, x = xval, y = 0.5f, ys = 1, 
                    KeyLetter = keyletters[note], KeyLetterSharpFlat = 0 };

                if (!hasBlackKey[note] || keyIdx >= PianoKeys.Length)
                    continue;

                xval = xleft + (i + 1) * xscale;
                PianoKeys[keyIdx++] = new PianoKey { isBlack = true, x = xval, y = 0.3f, ys = 0.6f, 
                    KeyLetter = keyletters[note + 1], KeyLetterSharpFlat = -1 };
            }           
        }

        void BuildPianoKeysFixedWidth(float width)
        {
            float xleft = 0;
            bool[] hasBlackKey = { true, true, false, true, true, true, false };
            char[] keyletters = new char[] { 'C', 'D', 'E', 'F', 'G', 'A', 'B' };
            int nWhiteKeys = 85;
            PianoWhiteXs = width;
            PianoBlackXs = width;

            int keyIdx = 0;
            float xval = width / 2;
            for (int i = 0; keyIdx < PianoKeys.Length; i++)
            {
                int note = i % 7;
                PianoKeys[keyIdx++] = new PianoKey { isBlack = false, x = xval, y = 0.5f, ys = 1, 
                    KeyLetter = keyletters[note], KeyLetterSharpFlat = 0 };

                xval += width;
                if (!hasBlackKey[note] || keyIdx >= PianoKeys.Length)
                    continue;

                PianoKeys[keyIdx++] = new PianoKey { isBlack = true, x = xval, y = 0.3f, ys = 0.6f, 
                    KeyLetter = keyletters[note + 1], KeyLetterSharpFlat = -1 };
                xval += width;
            }
        }
    }
}
