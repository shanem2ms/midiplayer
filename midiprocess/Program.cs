using midilib;
using System.Diagnostics;

namespace midiprocess
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Process().Wait();
        }

        static async Task<bool> Process()
        {
            MidiDb db = new MidiDb();
            await db.Initialize();
            SongDb sdb = new SongDb(db);
            return await sdb.Build();
        }
}
}