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
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            SongDb sdb = new SongDb(Path.Combine(documents, "midifiles"));
            //await sdb.Md5();
            return await sdb.BuildDb();
        }
}
}