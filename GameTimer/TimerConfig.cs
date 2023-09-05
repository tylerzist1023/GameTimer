using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace GameTimer
{
    class TimerConfig
    {
        public const string timeFormat = @"d\:hh\:mm\:ss\.ff";
        public const string outputFormat = @"\:mm\:ss";
        public const string fileName = "times.xml";

        private Dictionary<string, TimerEntry> gameEntries;

        public List<string> Names
        {
            get
            {
                return gameEntries.Keys.ToList<string>();
            }
        }

        public TimerConfig(string filename)
        {
            gameEntries = new Dictionary<string, TimerEntry>();

            LoadConfig(filename);
        }

        public void LoadConfig(string filename)
        {
            // TODO: handle errors in xml parsing
            if(!File.Exists(filename))
            {
                File.Create(filename);
                string template = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n";
                template += "<times>\n";
                template += "</times>";
                File.WriteAllText(filename, template);
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            gameEntries.Clear();
            var nodeTimes = doc.SelectNodes("//times/game");
            foreach(XmlNode node in nodeTimes)
            {
                string name = node.Attributes["name"].Value;
                string time = node.Attributes["time"].Value;
                bool complete;
                try
                {
                    if(node.Attributes["complete"] != null)
                        complete = true;
                    else
                        complete = false;
                }
                catch
                {
                    complete = false;
                }

                gameEntries[name] = new TimerEntry(name, time, complete);
            }
        }

        public void SaveConfig(string filename, string name)
        {
            // create a new blank xml file if it doesn't exist yet
            if(!File.Exists(filename))
            {
                File.Create(filename);
                string template = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n";
                template += "<times>\n";
                template += "</times>";
                File.WriteAllText(filename, template);
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            XmlNode node = doc.SelectSingleNode(String.Format("//times/game[@name=\"{0}\"]", name));
            if(node != null) // if the node already exists, just modify it
            {
                node.Attributes["time"].Value = TimeSpan.Parse(gameEntries[name].time).ToString(timeFormat);

                node.Attributes.Remove(node.Attributes["complete"]);
                if(gameEntries[name].complete)
                {
                    node.Attributes.Append(doc.CreateAttribute("complete"));
                }
            }
            else // if it doesn't exist, then create a new node
            {
                XmlNode newNode = doc.CreateElement("game");

                XmlAttribute attName = doc.CreateAttribute("name");
                XmlAttribute attTime = doc.CreateAttribute("time");
                XmlAttribute attComplete = doc.CreateAttribute("complete");

                attName.Value = name;
                attTime.Value = gameEntries[name].time;
                attComplete.Value = "";

                newNode.Attributes.Append(attName);
                newNode.Attributes.Append(attTime);
                if(gameEntries[name].complete) newNode.Attributes.Append(attComplete);

                doc.SelectSingleNode("//times").AppendChild(newNode);
            }

            // create backup
            if(File.Exists(filename + ".bk"))
            {
                File.Delete(filename + ".bk");
            }
            File.Copy(filename, filename + ".bk");

            doc.Save(filename);
        }

        public string GetEntryTime(string name)
        {
            if(!ContainsKey(name))
            {
                return new TimeSpan(0).ToString(timeFormat);
            }
            return gameEntries[name].time;
        }

        public void UpdateTime(string name, string time)
        {
            if(!ContainsKey(name))
            {
                return;
            }
            gameEntries[name] = new TimerEntry(name, time, gameEntries[name].complete);
        }

        public void AddEntry(TimerEntry entry)
        {
            if(ContainsKey(entry.name))
            {
                throw new Exception(entry.name + " already exists.");
            }
            gameEntries[entry.name] = entry;
        }

        public void RemoveEntry(string name)
        {
            if(!ContainsKey(name))
            {
                throw new Exception(name + " not found.");
            }
            gameEntries.Remove(name);
        }

        public bool ContainsKey(string name)
        {
            return gameEntries.ContainsKey(name);
        }

        public void SetComplete(string name, bool complete)
        {
            if(!ContainsKey(name))
            {
                throw new Exception(name + " not found.");
            }
            gameEntries[name] = new TimerEntry(name, gameEntries[name].time, complete);
        }

        public bool IsComplete(string name)
        {
            if(!ContainsKey(name))
            {
                return false;
            }
            return gameEntries[name].complete;
        }

        public void PrintEntries()
        {
            Console.WriteLine(string.Join(Environment.NewLine, gameEntries.Select(a => $"{a.Key}: {a.Value.time}")));
        }
    }

    struct TimerEntry
    {
        public string name;
        public string time;
        public bool complete;

        public TimerEntry(string name, string time, bool complete)
        {
            this.name = name;
            this.time = time;
            this.complete = complete;
        }
    }
}