using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml;
using System.Drawing;

namespace FirestormRLVLauncher {
    public class ObfuscatedInt {

        int _mask;
        int _masked;

        public ObfuscatedInt() {
            _mask = GetRandomInteger();
            _masked = ~_mask;
        }

        public int Value {
            get { return _masked ^ _mask; }
            set { _masked = value ^ _mask; }
        }

        private static int GetRandomInteger() {
            using (var rng = RandomNumberGenerator.Create()) {
                byte[] data = new byte[4];
                rng.GetBytes(data);
                return BitConverter.ToInt32(data, 0);
            }
        }
    }
    public class ObfuscatedByteArray {

        byte[] _mask;
        byte[] _masked;

        public ObfuscatedByteArray(int length = 32) {
            _mask = GetRandomByteArray(length);
            _masked = new byte[length];
            for (int i = 0; i < length; i++) {
                _masked[i] = 0;
            }
        }

        public byte[] Value {
            get { 
                byte[] result = new byte[_mask.Length];
                for (int i = 0; i < _mask.Length; i++) {
                    result[i] = (byte)(_masked[i] ^ _mask[i]);
                }
                return result;
            }
            set {
                if (value.Length != _mask.Length) {
                    _mask = GetRandomByteArray(value.Length);
                    _masked = new byte[value.Length];
                }
                for (int i = 0; i < _mask.Length; i++) {
                    _masked[i] = (byte)(value[i] ^ _mask[i]);
                }
            }
        }

        private static byte[] GetRandomByteArray(int length) {
            using (var rng = RandomNumberGenerator.Create()) {
                byte[] data = new byte[length];
                rng.GetBytes(data);
                return data;
            }
        }
    }
    public class LoginData {
        public string _filePath;
        public string _userName;
        private string _encryptedPassword;
        public ObfuscatedInt _elapsedMinutes;
        public ObfuscatedInt _totalMinutes;
        public ObfuscatedByteArray _exeHash;
        public ObfuscatedInt _locked;
        private ObfuscatedByteArray _key;
        public Action<string, Color>? _onLoadError;
        public string GetPassword() {
            return Decrypt(_encryptedPassword);
        }
        public void SetPassword(string password) {
            _encryptedPassword = Encrypt(password);
        }

        private void InitializeValues() {
            _elapsedMinutes = new ObfuscatedInt();
            _elapsedMinutes.Value = 0;
            _totalMinutes = new ObfuscatedInt();
            _locked = new ObfuscatedInt();
            _locked.Value = 0;
            _key = new ObfuscatedByteArray(28);
            _key.Value = Encoding.Unicode.GetBytes("^e&g~_]02;cg^uYl6*b6t&gjB]^A;,W124.123");
            _exeHash = new ObfuscatedByteArray(32);
            SHA256 sha = SHA256.Create();
            _exeHash.Value = sha.ComputeHash(File.ReadAllBytes(Settings._firestormExe));
        }

        public LoginData(string filePath, string userName, string password)
        {
            InitializeValues();
            _filePath = filePath;
            _userName = userName;
            _encryptedPassword = Encrypt(password);

        }

        public LoginData(string filePath) {
            InitializeValues();
            _filePath = filePath;
            LoadLogin();
        }



        public bool SaveLogin() {
            try {
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true, Encoding = System.Text.Encoding.UTF8, OmitXmlDeclaration = false };
                using StringWriter sw = new StringWriter();
                using XmlWriter writer = XmlWriter.Create(sw, settings);

                writer.WriteStartDocument();
                writer.WriteStartElement("Login");

                writer.WriteElementString("UserName", _userName);
                writer.WriteElementString("Password", Decrypt(_encryptedPassword));
                writer.WriteElementString("TotalMinutes", _totalMinutes.Value.ToString());
                writer.WriteElementString("MinutesSpent", _elapsedMinutes.Value.ToString());
                writer.WriteElementString("Locked", _locked.Value != 0 ? "true" : "false");
                writer.WriteElementString("ExeHash", Convert.ToBase64String(_exeHash.Value));

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();

                File.WriteAllText(_filePath, Encrypt(sw.ToString()), Encoding.UTF8);
                //File.WriteAllText(_filePath, sw.ToString(), Encoding.UTF8);
            }
            catch (Exception) {
                return false;
            }
            return true;
        }

        public void LoadLogin() {
            _userName = string.Empty; 
            _encryptedPassword = string.Empty;

            string xml = Decrypt(File.ReadAllText(_filePath, Encoding.UTF8));
            //string xml = File.ReadAllText(_filePath, Encoding.UTF8);
            try {
                using XmlReader reader = XmlReader.Create(new StringReader(xml));
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element) {
                        switch (reader.Name) {
                            case "UserName":
                                _userName = reader.ReadElementContentAsString();
                                break;
                            case "Password":
                                _encryptedPassword = Encrypt(reader.ReadElementContentAsString());
                                break;
                            case "TotalMinutes":
                                _totalMinutes.Value = reader.ReadElementContentAsInt();
                                break;
                            case "MinutesSpent":
                                _elapsedMinutes.Value = reader.ReadElementContentAsInt();
                                break;
                            case "Locked":
                                _locked.Value = reader.ReadElementContentAsBoolean() ? 1 : 0;
                                break;
                            case "ExeHash":
                                string exeHashBase64 = reader.ReadElementContentAsString();
                                _exeHash.Value = Convert.FromBase64String(exeHashBase64);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex) {
                _onLoadError?.Invoke("Failed to load login: " + ex.Message + "\nWrong file type or the encryption key changed.", Color.DarkRed);
            }
        }

        public string Encrypt(string text) {
            byte[] clearBytes = Encoding.Unicode.GetBytes(text);
            using (Aes encryptor = Aes.Create()) {

                encryptor.Key = Rfc2898DeriveBytes.Pbkdf2(_key.Value, Encoding.Unicode.GetBytes("salt"), 10, HashAlgorithmName.SHA512, 32);
                encryptor.IV = Rfc2898DeriveBytes.Pbkdf2(_key.Value, Encoding.Unicode.GetBytes("salt"), 10, HashAlgorithmName.SHA512, 16);
                using (MemoryStream ms = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write)) {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    text = Convert.ToBase64String(ms.ToArray());
                }
            }
            return text;
        }

        public string Decrypt(string text) {
            text = text.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(text);
            using (Aes encryptor = Aes.Create()) {
                encryptor.Key = Rfc2898DeriveBytes.Pbkdf2(_key.Value, Encoding.Unicode.GetBytes("salt"), 10, HashAlgorithmName.SHA512, 32);
                encryptor.IV = Rfc2898DeriveBytes.Pbkdf2(_key.Value, Encoding.Unicode.GetBytes("salt"), 10, HashAlgorithmName.SHA512, 16);
                using (MemoryStream ms = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write)) {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    text = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return text;
        }
    }
}
