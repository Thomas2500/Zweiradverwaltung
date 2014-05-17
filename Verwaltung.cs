using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Data;
using System.Windows.Media.Imaging;


namespace Zweiradverleih
{

    public class FileContents
    {
        // Dateiname "Zweirad_[0-9].bin

        public FileStructure Data;
        private string file;
        private int id;

        public FileContents(string filer)
        {
            if (Regex.IsMatch(filer, "Zweirad_([0-9]+)\\.bin", RegexOptions.IgnoreCase))
            {
                id = Convert.ToInt32(Regex.Match(filer, "Zweirad_([0-9]{1,})\\.bin").Groups[1].Value);
                Data.ID = id;
            }
            else
            {
                id = -1;
                Data.ID = -1;
            }
            this.file = filer;
        }

        public void ReadFile()
        {
            FileStream fs = new FileStream(this.file, FileMode.Open);
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                this.Data = (FileStructure)formatter.Deserialize(fs);
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

        public void WriteFile()
        {
            FileStream fs = new FileStream(this.file, FileMode.Create);
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(fs, this.Data);
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

        public void Poke()
        {
            this.Data.LetzteBenutzung = DateTime.Now;
            this.WriteFile();
        }

        public void ExportFile(string path, bool image)
        {
            TextWriter tw = new StreamWriter(path);
            string Ausgabe = "";
            Ausgabe += this.Data.ID.ToString() + " - " + this.Data.Bezeichnung.ToString() + "\n";
            Ausgabe += "\n";
            Ausgabe += "Servicestichtag: " + String.Format("{0:d/M/yyyy}", this.Data.Servicetermin) + "\n";
            Ausgabe += "Kilometer: " + this.Data.Kilometer.ToString() + " km\n";
            Ausgabe += "Verfügbar: ";

            if (this.Data.Verfügbar == false)
                Ausgabe += "Nein";
            else
                Ausgabe += "Ja";

            Ausgabe += "\n\n";
            Ausgabe += "--- Logbuch ---\n";

            if (this.Data.Logbuch == null)
                Ausgabe += "Es wurden noch keine Fahrten aufgezeichnet\n";
            else
                foreach (Fahrten f in this.Data.Logbuch)
                    Ausgabe += f.kilometer.ToString() + " km | " + String.Format("{0:d/M/yyyy HH:mm}", f.Von) + " - " + String.Format("{0:d/M/yyyy HH:mm}", f.Bis) + "\n";

            Ausgabe += "\n";
            if (image)
                Ausgabe += "--- Bild ---\n";

            Ausgabe = Ausgabe.Replace("\n", Environment.NewLine);
            tw.WriteLine(Ausgabe);
            tw.Flush();

            if (image)
                for (int i = 0; i < this.Data.Bild.Length; i = i + 64)
                    if (i + 64 > this.Data.Bild.Length)
                        tw.WriteLine(this.Data.Bild.Substring(i));
                    else
                        tw.WriteLine(this.Data.Bild.Substring(i, 64));

            tw.Flush();
            tw.Close();
        }
    }

    public class BaseImageHandler
    {
        public Image Path2Image(string fileName)
        {
            return (Image.FromFile(fileName));
        }

        public string Image2Base64(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }

        public Image Base642Image(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);

            ms.Write(imageBytes, 0, imageBytes.Length);
            Image image = Image.FromStream(ms, true);
            return image;
        }

        public BitmapImage Image2BitmapImage (Image image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, image.RawFormat);
            ms.Seek(0, SeekOrigin.Begin);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            return bi; 
        }


    }

    [Serializable]
    public struct FileStructure
    {
        public string Bezeichnung;
        public int ID;

        public DateTime Servicetermin;
        public int Kilometer;
        public Fahrten[] Logbuch;

        public DateTime LetzteBenutzung;

        public string Bild;
        public bool Verfügbar;
    }

    [Serializable]
    public struct Fahrten
    {
        public DateTime Von;
        public DateTime Bis;
        public double kilometer;
    }
}
