using Learnr;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace learnr2drive
{
    public class Global
    {
        // Loaded from file vars
        public GlobalVars Learnr;


        public bool RenderFullScreenOnLoad { get => renderFullScreenOnLoad; set => renderFullScreenOnLoad = value; }

        public static Global R;
#if DEBUG
        public const string ConfigFileName = "SettingsDEBUG.config";
#else
        public const string ConfigFileName = "Settings.config";
#endif
        public const string ConfigFileFilter = "Config files (*.config)|*.config";
        public const string ConfigNetworkFileFilter = "Network file (*.xml)|*.xml";
        public const string LoadPathsEvent = "LoadPath";


        #region Game Physics Params
        // definition of vars below will be applied and/or saved to file if no config file/var exists
        public float CAR_INIT_VELOCITY = 10;
        public float CAR_INIT_ANGLE = -45;
        public float CAR_SPEED_DRAG = 0.07f;
        public float CAR_SPEED_TYREFRICTION = 0.20f;
        public float CAR_SPEED_BRAKESDRAG = 0.15f;
        public float CAR_SPEED_STEERDRAG = 0.03f;
        public float CAR_SPEED_MAXACCELERATION = 0.14f;
        public float CAR_SCORE_GOINGNOWHEREPENELTY = 2000f;
        public float CAR_REV_MAXACCELERATION = 0.112f;
        public float CAR_STEER_MAXRATE = 2.5f;
        public float CAR_RENDER_INDICATORANGLE = 30;
        public float CAR_RENDER_VISIONLINESANGLE = 30;
        public float CAR_RENDER_VISIONLINESANGLE1 = 15;
        public float CAR_RENDER_VISIONLINESRANGE = 150;
        public float CAR_RENDER_VISIONLINESRANGE1 = 400;
        public float CAR_RENDER_VISIONLINESRESOLUTION = 25f;
        public bool CAR_RENDER_VISIONLINESDISPLAYED = true;
        public bool CAR_DRIVE_EXPERIMENTAL = false;
        public bool CAR_SPAWN_RANDOMPOS_ENABLED = true;
        public bool CAR_SCORE_GOINGNOWHEREPENELTY_ENABLED = true;
        public int CAR_SPAWN_RANDOMPOS_MAXATTEMPTS = 20;
        #endregion

        #region Simulation Render Params
        // definition of vars below will be applied and/or saved to file if no config file/var exists
        public int ball_ghostCopiesCount = 35;
        public int ball_ghostCopiesInterCount = 15;
        public float ball_ghostbrightness = 0.8f;
        public float ball_ghostdim_factor = 0.92f;
        private bool renderFullScreenOnLoad = false;
        #endregion

        #region Simulation Scoring Params
        // definition of vars below will be applied and/or saved to file if no config file/var exists

        #endregion


        public static void Init()
        {
            R = new Global()
            {
                Learnr = GlobalVars.R = new GlobalVars(11, 2)
            };
        }

        #region IO Methods
        public static void LoadSettingsFromFile(string filename, out Global R)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(Global));
            using (TextReader reader = new StreamReader(filename))
            {
                object obj = deserializer.Deserialize(reader);
                R = (Global)obj;
                R.Learnr = GlobalVars.R = ((Global)obj).Learnr;
            }
        }

        public static void SaveSettingsToFile(string filename, Global R)
        {
            using (TextWriter reader = new StreamWriter(filename))
            {
                string s = XmlExtension.Serialize(R);
                reader.WriteLine(s);
            }
        }


        #endregion
    }
    public static class XmlExtension
    {
        public static string Serialize<T>(this T value)
        {
            if (value == null) return string.Empty;

            var xmlSerializer = new XmlSerializer(typeof(T));

            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true }))
                {
                    xmlSerializer.Serialize(xmlWriter, value);
                    return stringWriter.ToString();
                }
            }
        }
        public static object LoadSettingsFromFile<T>(string filename, out T R)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(T));
            using (TextReader reader = new StreamReader(filename))
            {
                object obj = deserializer.Deserialize(reader);
                R = (T)obj;
                return obj;
            }
        }

    }
}
