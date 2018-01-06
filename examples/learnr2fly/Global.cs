using Learnr;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace learnr2fly
{
    public class Global
    {
        // Loaded from file vars
        public GlobalVars Learnr;


        public bool RenderFullScreenOnLoad { get => renderFullScreenOnLoad; set => renderFullScreenOnLoad = value; }

        public static Global R;
        public const string ConfigFileName = "NureOne.config";
        public const string ConfigFileFilter = "Config files (*.config)|*.config";
        public const string ConfigNetworkFileFilter = "Network file (*.xml)|*.xml";
        public const string LoadPathsEvent = "LoadPath";


        #region Game Physics Params
        // definition of vars below will be applied and/or saved to file if no config file/var exists
        public float BOUNCINESS = 1f;
        public float ACCELERATE = 0.8f;
        public readonly float RADIANCONVERSION = 0.01745329252f;
        public float TURNING_SPEED = 7; //degrees per frame?
        public float PTS_MISSED_SHOT_PEN = 0.25f;

        public float BULLET_SPEED = 20;
        public int BULLET_LIMIT = 8;
        public float RELOAD_SPEED = 1;
        public float RELOAD_RATE = 0.5f;
        public float BULLET_CALIBRE = 5;

        public float PTS_DEATH_PEN = 0.35f;
        public float AST_MAX_SPEED = 6;

        public float BOOSTER_ANIMATION_SPEED = 0.1f;
        public int BOOSTER_ANIM_LIMIT = 50;
        public float DELETE_DUST_SPEED = 2;
        public float BOOSTER_SMOKE_SIZE = 7;

        public int MAX_ASTEROIDS = 20;
        public float AST_HALTAFTER = 2;
        public bool AST_COLLISION_USE_TRIS = true;
        public bool SPACESHIP_BRAKING_ENABLED = false;
        public bool SPACESHIP_BIDIRECTION_ENABLED = false;
        public bool SPACESHIP_DIRECTIONAL_SIGHT_ENABLED = false;
        public float SPACESHIP_HEAD_ROT_RANGE = 270;
        public float SPACESHIP_HEAD_ROT_MAXRATE = 5;
        #endregion
        #region Simulation Render Params
        // definition of vars below will be applied and/or saved to file if no config file/var exists
        private bool renderFullScreenOnLoad = false;
        #endregion
        #region Simulation Scoring Params
        // definition of vars below will be applied and/or saved to file if no config file/var exists

        #endregion


        public static void Init()
        {
            R = new Global()
            {
                Learnr = GlobalVars.R = new GlobalVars(18, 4)
            };
        }

        #region IO Methods
        

        public static void LoadSettingsFromFile(string filename, out Global R)
        {
            object obj = XmlExtension.LoadSettingsFromFile<Global>(filename, out R);
            R.Learnr = GlobalVars.R = ((Global)obj).Learnr;
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
}
