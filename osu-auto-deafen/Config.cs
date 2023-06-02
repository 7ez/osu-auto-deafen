using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using WindowsInput.Events;

namespace osu_auto_deafen
{
    public class Config
    {
        private Dictionary<string, string> _values;

        public Config(string configPath)
        {
            _values = new Dictionary<string, string>();
            
            if (!Load(configPath))
                throw new Exception("Config load error.");
        }

        public T Get<T>(string key)
        {
            string result;

            if (_values.TryGetValue(key, out result))
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T)converter.ConvertFromString(result);
            }

            return default;
        }

        public KeyCode[] GetKeyCodes()
        {
            var keys = Get<string>("Keys");

            if (keys == null)
                return null;
            
            var split = keys.Split('+');

            if (split.Length > 2)
                throw new Exception("More than 2 keys are not supported.");
            
            var keyCodes = new KeyCode[split.Length];

            for (int i = 0; i < split.Length; i++)
            {
                var keyCode = split[i];
                
                // TODO: Better error handling (maybe)
                if (!Enum.TryParse(keyCode, out keyCodes[i]))
                    throw new Exception("Config parse error."); 
            }

            return keyCodes;
        }

        public float GetDeafenPoint()
        {
            var deafenPoint = Get<int>("DeafenPoint");
            
            if (deafenPoint == 0)
                return 0.4f;
            
            if (0 > deafenPoint || deafenPoint > 95)
                throw new Exception("DeafenPoint must be between 0 and 95.");
            
            return deafenPoint / 100f;
        }
        
        private bool Load(string configPath)
        {
            if (!File.Exists(configPath))
                return false;

            var lines = new string[] {};
            
            try { lines = File.ReadAllLines(configPath); }
            catch { return false; }

            foreach (var line in lines)
            {
                if (line.StartsWith("#"))
                    continue;

                var split = line.Split('=');
                if (split.Length != 2)
                    continue;

                if (split.Length > 2) 
                    _values.Add(split[0], String.Join("=", split.Skip(1)));
                else 
                    _values.Add(split[0], split[1]);
            }

            return true;
        }
    }
}