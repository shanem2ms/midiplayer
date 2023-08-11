using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Security.Cryptography;

namespace midilib
{
    internal class MidiFingerprint
    {
        class Hash : IEquatable<Hash>
        {
            byte[] array;

            public Hash(byte[] array)
            {
                this.array = array;
            }

            public bool Equals(Hash other)
            {
                if (this == other)
                {
                    return true;
                }
                if (this == null || other == null)
                {
                    return false;
                }
                if (array.Length != array.Length)
                {
                    return false;
                }
                for (int i = 0; i < this.array.Length; i++)
                {
                    if (this.array[i] != other.array[i])
                    {
                        return false;
                    }
                }
                return true;
            }


            public override int GetHashCode()
            {
                unchecked
                {
                    if (array == null)
                    {
                        return 0;
                    }
                    int hash = 17;
                    foreach (byte element in array)
                    {
                        hash = hash * 31 + element;
                    }
                    return hash;
                }
            }
        }
        public async Task<bool> Md5(MidiDb db)
        {
            List<MidiDb.Fi> allfiles = db.FilteredMidiFiles.ToList();
            Dictionary<Hash, List<MidiDb.Fi>> filesDicts = new Dictionary<Hash, List<MidiDb.Fi>>();
            foreach (MidiDb.Fi fi in allfiles)
            {
                string path = await db.GetLocalFile(fi, true);
                if (path == null)
                    continue;
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(path))
                    {
                        Hash h = new Hash(md5.ComputeHash(stream));
                        List<MidiDb.Fi> outList;
                        if (filesDicts.TryGetValue(h, out outList))
                        {
                            outList.Add(fi);
                        }
                        else
                        {
                            filesDicts.Add(h, new List<MidiDb.Fi>() { fi });
                        }
                    }
                }
            }

            MappingsFile mappings = db.Mappings;
            var dupes = filesDicts.Where(kv => kv.Value.Count > 1);
            int removed = 0;
            foreach (var dup in dupes)
            {
                List<MidiDb.Fi> files = dup.Value;
                files.RemoveAt(0);

                foreach (MidiDb.Fi fi in files)
                {
                    if (!mappings.midifiles.Remove(fi.Name))
                        Debugger.Break();
                    removed++;
                }
            }
            return true;
        }
    }

}
