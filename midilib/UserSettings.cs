using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Runtime;

namespace midilib
{
    public class UserSettings
    {
        public string CurrentSoundFont { get; set; }
        public List<string> PlayHistory { get; set; } 

        string settingsFile;

        public static UserSettings FromFile(string file)
        {
            UserSettings settings;
            if (File.Exists(file))
            {
                string text = File.ReadAllText(file);
                settings = JsonConvert.DeserializeObject<UserSettings>(text);
            }
            else
            {
                settings = new UserSettings();
                settings.CurrentSoundFont = "TimGM6mb.sf2";
            }
            if (settings.PlayHistory == null)
                settings.PlayHistory = new List<string>();
            settings.settingsFile = file;
            return settings;
        }
        public UserSettings()
        {
        }
        public void Persist()
        {
            string jsonstr = JsonConvert.SerializeObject(this);
            File.WriteAllText(settingsFile, jsonstr);
        }

    }
}
