﻿using System;
using Silk.NET.OpenAL;
using MeltySynth;

class Program
{
    unsafe static void Main()
    {
        using (var alc = ALContext.GetApi(true))
        using (var al = AL.GetApi(true))
        {
            var device = alc.OpenDevice("");
            var context = alc.CreateContext(device, null);
            alc.MakeContextCurrent(context);
            al.GetError();

            using (var player = new MidiPlayer(al, "TimGM6mb.sf2"))
            {
                // Load the MIDI file.
                var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

                // Play the MIDI file.
                player.Play(midiFile, true);

                // Wait until any key is pressed.
                Console.ReadKey();
            }

            alc.DestroyContext(context);
            alc.CloseDevice(device);
        }
    }
}
