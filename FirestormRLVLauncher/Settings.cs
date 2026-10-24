using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace FirestormRLVLauncher {
    public static class Settings {

        public static string _firestormExe = "C:\\Program Files\\Firestorm-Releasex64\\Firestorm-Releasex64.exe";
        public static string? _lastLoginFile = null;

        private static string _ownSettingsFile = Path.Combine(Path.GetDirectoryName(System.Environment.ProcessPath), "settings.xml");

        // Used for XML serialization
        public class SettingsData {
            public string? FirestormExe { get; set; }
            public string? LastProfileFile { get; set; }
        }

        public static void LoadSettings()
        {
            try {
                if (!File.Exists(_ownSettingsFile))
                    return;

                var serializer = new XmlSerializer(typeof(SettingsData));
                using var stream = File.OpenRead(_ownSettingsFile);
                var data = (SettingsData?)serializer.Deserialize(stream);
                if (data != null) {
                    if (!string.IsNullOrWhiteSpace(data.FirestormExe))
                        _firestormExe = data.FirestormExe!;
                    _lastLoginFile = data.LastProfileFile;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Load fail");
                // Ignore errors - keep defaults
            }
        }

        public static void SaveSettings()
        {
            try {
                var dir = Path.GetDirectoryName(_ownSettingsFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);

                var data = new SettingsData {
                    FirestormExe = _firestormExe,
                    LastProfileFile = _lastLoginFile
                };

                var serializer = new XmlSerializer(typeof(SettingsData));
                var settings = new XmlWriterSettings { Indent = true, Encoding = System.Text.Encoding.UTF8 };
                using var stream = File.Create(_ownSettingsFile);
                using var writer = XmlWriter.Create(stream, settings);
                serializer.Serialize(writer, data);
            }
            catch (Exception)
            {
                Console.WriteLine("Save  fail" + _ownSettingsFile);
                // Ignore errors
            }
        }
    }
}
