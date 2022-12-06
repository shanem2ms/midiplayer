using midilib;
using System.Diagnostics;

namespace midiprocess
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ThreadPool.SetMaxThreads(32, 32);
            Task.WaitAll(Process());
        }

        static async Task<bool> Process()
        {
            MidiDb db = new MidiDb();
            await db.Initialize();
            SongDb sdb = new SongDb(db);
            //await sdb.Md5();
            return await sdb.Build();
        }
}
}