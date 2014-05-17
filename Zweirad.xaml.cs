using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Data;
using System.IO;

namespace Zweiradverleih
{
    /// <summary>
    /// Interaktionslogik für Zweirad.xaml
    /// </summary>
    public partial class Zweirad : Window
    {
        private int id;
        private FileContents Data;
        private BaseImageHandler bih;
        private bool changes = false;
        private WPFcommunication wpfc;

        public int ID
        {
            get { return this.id; }
            private set { this.id = value; }
        }

        public Zweirad()
        {
            InitializeComponent();
        }

        public Zweirad(string file)
        {
            InitializeComponent();
            VersionStamp.Content = file;
            if (Regex.IsMatch(file, "Zweirad_([0-9]+)\\.bin", RegexOptions.IgnoreCase))
            {
                int id = Convert.ToInt32(Regex.Match(file, "Zweirad_([0-9]{1,})\\.bin").Groups[1].Value);
                this.ID = id;
            }
            this.Title = "Zweirad Eintrag ("+ID.ToString()+")";

            this.Data = new FileContents(file);
            this.Data.ReadFile();
            wpfc = new WPFcommunication();

            try
            {
                if (this.Data.Data.Bild != "")
                {
                    bih = new BaseImageHandler();
                    FahrzeugBild.Source = bih.Image2BitmapImage(bih.Base642Image(this.Data.Data.Bild));
                }
            }
            catch
            {
                this.Data.Data.Bild = "";
            }
            try
            {
                FahrzeugKurzname.Text = this.Data.Data.Bezeichnung;
            }
            catch { }
            ReloadLogbuch();
            try
            {
                Kilometer.Content = wpfc.ZKilometer(this.Data.Data.Logbuch);
            }
            catch
            {
                Kilometer.Content = "0";
                this.Data.Data.Kilometer = 0;
            }
            try
            {
                ar_date.SelectedDate = DateTime.Now;
                ar_hour.Text = DateTime.Now.Hour.ToString();
                ar_minute.Text = DateTime.Now.Minute.ToString();

                if (this.Data.Data.Verfügbar == true || this.Data.Data.Logbuch == null)
                {
                    ToggleVerleihVerfügbar(true);
                }
                else
                {
                    ToggleVerleihVerfügbar(false);
                }
            }
            catch { }
        }

        private void ReloadLogbuch()
        {
            try
            {
                if (this.Data.Data.Logbuch != null)
                    FahrzeugFahrtenbuch.DataContext = wpfc.Fahrtenbuch(this.Data.Data.Logbuch);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void ToggleVerleihVerfügbar(bool st)
        {
            if (st == true)
            {
                StatusL.Content = "Verfügbar";
            }
            else
            {
                StatusL.Content = "Ausgeborgt";
            }
            b_Ruecknahme.IsEnabled = !st;
            b_Ausleihen.IsEnabled = st;
            tb_zgk.IsEnabled = !st;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            this.Write();
            return;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (Merge() == true)
            {
                // Alert box file has been changed
                MessageBoxResult msr = MessageBox.Show("Es wurden einige Daten verändert die noch nicht gespeichert wurden.\nWollen Sie die Änderungen verwerfen?", "Zweiradverwaltung - Achtung", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (msr == MessageBoxResult.No)
                    return;
            }
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (Merge() == true)
            {
                MessageBoxResult mb = MessageBox.Show("Möchten Sie wirklich schließen?\nAlle ungespeicherten Änderungen gehen verloren!", "Zweiradverwaltung - Warnung", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (mb != MessageBoxResult.Yes)
                    e.Cancel = true;
            }
            base.OnClosed(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                bih = new BaseImageHandler();
                this.Data.Data.Bild = bih.Image2Base64(bih.Path2Image(dlg.FileName));
                FahrzeugBild.Source = bih.Image2BitmapImage(bih.Path2Image(dlg.FileName));
            }
        }

        private void FahrzeugServicestichtag_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            this.Data.Data.Servicetermin = (System.DateTime) FahrzeugServicestichtag.SelectedDate;
        }

        private bool Merge()
        {
            bool rtchanges = this.changes;

            if (this.Data.Data.Bezeichnung == null || this.Data.Data.Bezeichnung.Trim() != FahrzeugKurzname.Text.Trim())
            {
                this.Data.Data.Bezeichnung = FahrzeugKurzname.Text.Trim();
                rtchanges = true;
            }

            if (FahrzeugServicestichtag.SelectedDate != null && this.Data.Data.Servicetermin != FahrzeugServicestichtag.SelectedDate)
            {
                DateTime t;

                if (FahrzeugServicestichtag.SelectedDate == null)
                    t = DateTime.MinValue;
                else
                    t = (DateTime) FahrzeugServicestichtag.SelectedDate;

                this.Data.Data.Servicetermin = (DateTime) t;
                rtchanges = true;
            }

            this.changes = rtchanges;
            return rtchanges;
        }

        private void Write()
        {
            Merge();
            this.Data.WriteFile();
            this.changes = false;
            this.Data.Poke();
        }

        private void Speichern_Click(object sender, RoutedEventArgs e)
        {
            Write();
        }

        private void b_Ausleihen_Click(object sender, RoutedEventArgs e)
        {
            if (this.Data.Data.Logbuch == null)
                this.Data.Data.Logbuch = new Fahrten[0];

            DateTime dt;
            try
            {
                dt = (DateTime)ar_date.SelectedDate;
                dt = dt.AddHours(Convert.ToDouble(ar_hour.Text)).AddMinutes(Convert.ToDouble(ar_minute.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei der Verarbeitung des Zeitpunktes.\nBitte überprüfe Sie ihre Eingaben!\n" + ex.Message, "Zweiradverwaltung - Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Array.Resize(ref this.Data.Data.Logbuch, this.Data.Data.Logbuch.Length + 1);

            this.Data.Data.Logbuch[this.Data.Data.Logbuch.Length - 1].Von = dt;
            this.Data.Data.Verfügbar = false;
            ToggleVerleihVerfügbar(false);
            ReloadLogbuch();
        }

        private void b_Ruecknahme_Click(object sender, RoutedEventArgs e)
        {
            DateTime dt;
            try
            {
                dt = (DateTime)ar_date.SelectedDate;
                if (Convert.ToDouble(ar_hour.Text) < 0 || Convert.ToDouble(ar_hour.Text) > 23)
                    throw new Exception("Stundenangabe ist ungültig!");

                if (Convert.ToDouble(ar_minute.Text) < 0 || Convert.ToDouble(ar_minute.Text) > 59)
                    throw new Exception("Minutenangabe ist ungültig!");

                dt = dt.AddHours(Convert.ToDouble(ar_hour.Text)).AddMinutes(Convert.ToDouble(ar_minute.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei der Verarbeitung des Zeitpunktes.\nBitte überprüfe Sie ihre Eingaben!\n" + ex.Message, "Zweiradverwaltung - Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double km;
            try
            {
                km = Convert.ToDouble(tb_zgk.Text);
                if (km < 0)
                    throw new Exception("Kilometer muss größer als 0 sein");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei der Verarbeitung der Kilometer.\nBitte überprüfe Sie ihre Eingaben!\n" + ex.Message, "Zweiradverwaltung - Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.Data.Data.Logbuch[this.Data.Data.Logbuch.Length - 1].Bis = dt;
            this.Data.Data.Logbuch[this.Data.Data.Logbuch.Length - 1].kilometer = km;
            this.Data.Data.Verfügbar = true;
            ToggleVerleihVerfügbar(true);
            ReloadLogbuch();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog1.Filter = "Text file|*.txt";
            saveFileDialog1.Title = "Exportire Zweirad";
            saveFileDialog1.ShowDialog();

            bool save = false;
            try
            {
                if (this.Data.Data.Bild != null && this.Data.Data.Bild != "")
                {
                    MessageBoxResult sis = MessageBox.Show("Soll auch das Bild in Textform exportiert werden?", "Zweiradverwaltung - Export", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    save = sis == MessageBoxResult.Yes;
                }
            }
            catch { }

            try
            {
                this.Data.ExportFile(saveFileDialog1.FileName, save);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Export der Datenstruktur!\n" + ex.Message, "Zweiradverwaltung - Exportfehler", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }
    }
}
