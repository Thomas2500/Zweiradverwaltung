using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Zweiradverleih
{
    public class WPFcommunication
    {

        public string ConfigurationData()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            else if (result == DialogResult.Cancel)
            {
                return "abbr";
            }
            return null;
        }

        public DataTable Fahrtenbuch(Fahrten[] data)
        {
            // Initiiere Tabelle
            DataTable table = new DataTable();

            DataColumn column;

            column = table.Columns.Add();
            column.ColumnName = "Von";
            column.DataType = typeof(string);

            column = table.Columns.Add();
            column.ColumnName = "Bis";
            column.DataType = typeof(string);

            column = table.Columns.Add();
            column.ColumnName = "Kilometer";
            column.DataType = typeof(double);

            DataRow row;

            foreach (Fahrten f in data)
            {
                row = table.NewRow();

                row["Von"] = String.Format("{0:d/M/yyyy HH:mm}", f.Von);
                row["Bis"] = String.Format("{0:d/M/yyyy HH:mm}", f.Bis);
                row["Kilometer"] = f.kilometer;
                table.Rows.Add(row);
            }

            return table;
        }

        public double ZKilometer(Fahrten[] data)
        {
            double z = 0;

            foreach (Fahrten f in data)
            {
                z += f.kilometer;
            }

            return z;
        }

        public DataTable Fahrzeuge(string pfad)
        {
            String[] files = Directory.GetFiles(pfad, "*.bin");

            // Initiiere Tabelle
            DataTable table = new DataTable();

            DataColumn column;

            column = table.Columns.Add();
            column.ColumnName = "ID";
            column.DataType = typeof(int);

            column = table.Columns.Add();
            column.ColumnName = "Name";
            column.DataType = typeof(string);

            column = table.Columns.Add();
            column.ColumnName = "Letzte Änderung";
            column.DataType = typeof(DateTime);

            DataRow row;

            Regex reg = new Regex(@"Zweirad_([0-9]+)\.bin");

            foreach (string einzelpfad in files)
            {
                Match m = reg.Match(einzelpfad);
                if (m.Success == false)
                    continue;

                FileContents fc = new FileContents(einzelpfad);
                fc.ReadFile();

                row = table.NewRow();
                row["ID"] = fc.Data.ID;
                row["Name"] = fc.Data.Bezeichnung;
                row["Letzte Änderung"] = fc.Data.LetzteBenutzung;
                table.Rows.Add(row);
            }

            return table;
        }

    }

    public class Pfadauswahl : WPFcommunication
    {
        private string projektpfad;

        public string Projektpfad
        {
            get { return this.projektpfad; }
            private set { this.projektpfad = value; }
        }

        public Pfadauswahl()
        {
            Projektpfad = ConfigurationData();
            if (Projektpfad == "abbr")
            {
                Projektpfad = "";
            }
            else if (!Directory.Exists(Projektpfad))
            {
                MessageBox.Show("Der gewählte Pfad " + Projektpfad + " existiert nicht oder der Zugriff wurde verweigert.");
            }
            else
            {
                this.Projektpfad = Projektpfad;
            }
        }
    }


    public class Hauptkonfiguration : MainWindow
    {
        public HauptConfig Einstellung;

        private string configpath;
        private bool loaded = false;

        public bool Geladen
        {
            get { return this.loaded; }
            private set { this.loaded = value; }
        }

        public Hauptkonfiguration(string prpfad)
        {
            this.Geladen = true;
            this.configpath = prpfad + @"\configuration.bin";
            if (File.Exists(this.configpath))
            {
                FileStream fs = new FileStream(this.configpath, FileMode.Open);
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    this.Einstellung = (HauptConfig)formatter.Deserialize(fs);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }
                finally
                {
                    fs.Close();
                }
            }
            else
            {
                this.Einstellung = new HauptConfig();
                this.Einstellung.Einträge = 0;
            }
        }

        public void Write()
        {
            if (Geladen == false)
                return;

            FileStream fs = new FileStream(this.configpath, FileMode.Create);
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(fs, this.Einstellung);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }
    }

    [Serializable]
    public struct HauptConfig
    {
        public int Einträge;
        public DateTime LastUse;
    }
}
