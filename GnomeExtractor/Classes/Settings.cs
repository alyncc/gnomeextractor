using System;
using System.Xml.Serialization;
using System.IO;
using System.Windows;

namespace GnomeExtractor
{
    class SettingsFields
    {
        public string XMLFileName = Environment.CurrentDirectory + "\\settings.xml";

        public bool LastRunCheatMode = false;
        public bool LastRunIsLablesVertical = true;
        public bool FastEditModeIsFixed = true;
        public bool IsAutoUpdateEnabled = true;
        public int FastEditValue = 30;
        public int TabItemSelected = 0;
        public string ProgramLanguage;
        public WindowState LastRunWindowState = WindowState.Normal;
        public Point LastRunLocation = new Point(0, 0);
        public Size LastRunSize = new Size(1000, 500);
    }

    class Settings
    {
        public SettingsFields Fields;

        public Settings()
        {
            Fields = new SettingsFields();
        }

        public void WriteXml()
        {
            XmlSerializer ser = new XmlSerializer(typeof(SettingsFields));
            TextWriter writer = new StreamWriter(Fields.XMLFileName);
            ser.Serialize(writer, Fields);
            writer.Close();
        }

        public void ReadXml()
        {
            if (File.Exists(Fields.XMLFileName))
            {
                XmlSerializer ser = new XmlSerializer(typeof(SettingsFields));
                TextReader reader = new StreamReader(Fields.XMLFileName);
                Fields = ser.Deserialize(reader) as SettingsFields;
                reader.Close();
            }
            else { }
        }
    }
}
