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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Data;
using System.Windows.Automation.Peers;
using System.Text.RegularExpressions;

namespace Zweiradverleih
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WPFcommunication wpfc;
        Hauptkonfiguration hc;

        public string Projektpfad;

        public MainWindow()
        {
            InitializeComponent();
            System.Version MyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            DateTime FCT = new DateTime(2000, 1, 1).AddDays(MyVersion.Build).AddSeconds(MyVersion.Revision * 2);
            VersionStamp.Content = "Build " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            VersionStamp.ToolTip = "Created on \n" + FCT.ToString("dd.MM.yyyy HH:mm:ss");

            EClose();

            wpfc = new WPFcommunication();
        }

        private void EClose()
        {
            OpenData.IsEnabled = false;
            AddData.IsEnabled = false;
            RemData.IsEnabled = false;
            RelMen.IsEnabled = false;
        }

        private void EOpen()
        {
            OpenData.IsEnabled = true;
            AddData.IsEnabled = true;
            RemData.IsEnabled = true;
            RelMen.IsEnabled = true;
        }

        public void ÖffneFahrzeugFenster(string file)
        {
            Zweirad ci = new Zweirad(file);
            ci.Show();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            Pfadauswahl st = new Pfadauswahl();
            if (st.Projektpfad == null || st.Projektpfad == "")
                return;

            this.Projektpfad = st.Projektpfad;
            this.hc = new Hauptkonfiguration(this.Projektpfad);
            Fahrzeuge.DataContext = wpfc.Fahrzeuge(this.Projektpfad);
            EOpen();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult mb = MessageBox.Show("Möchten Sie wirklich schließen?\nAlle ungespeicherten Änderungen gehen verloren!", "Zweiradverwaltung - Warnung", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (mb == MessageBoxResult.Yes)
            {
                if (this.hc != null)
                    this.hc.Write();

                foreach (Window win in App.Current.Windows)
                    if (win.Title != "Zweiradverwaltung")
                        win.Close();

                Fahrzeuge.DataContext = new DataGrid();

                EClose();
            }
        }

        private void OpenData_Click(object sender, RoutedEventArgs e)
        {
            if (Fahrzeuge.SelectedIndex == -1)
                MessageBox.Show("Es wurde kein Eintrag ausgewählt.", "Fehler - Zweiradverwaltung", MessageBoxButton.OK, MessageBoxImage.Information);
            else
            {
                int index = Fahrzeuge.SelectedIndex;
                DataRowView row = (DataRowView)Fahrzeuge.SelectedItems[0];
                int WindowID = -1;

                Regex reg = new Regex(@"Zweirad Eintrag \(([0-9]+)\)");

                foreach (Window win in App.Current.Windows)
                {
                    Match m = reg.Match(win.Title);
                    if (m.Success)
                    {
                        WindowID = Convert.ToInt32(m.Groups[1].Value);
                        if (Convert.ToInt32(row["ID"]) == WindowID)
                        {
                            // focus window
                            win.Activate();
                            win.Focus();
                            return;
                        }
                    }
                }
                ÖffneFahrzeugFenster(this.Projektpfad + @"\Zweirad_" + row["ID"] + ".bin");
            }
        }

        private void AddData_Click(object sender, RoutedEventArgs e)
        {
            // Prüfe Verzeichnis vorhanden
            // Lege neue Datei an
            // Dateien haben eine fixe Dateinamenstruktur
            // Zum Beispiel: Zweirad_%%i.bin
            if (this.hc.Geladen != true || this.Projektpfad == "")
            {
                //MSG error no project opened
                MessageBox.Show("Es wurde kein Projekt geöffnet.\nBitte öffne ein Projekt um einen neuen Eintrag hinzuzufügen", "Zweiradverleih - Fehler");
            }
            else
            {
                FileContents fc = new FileContents(this.Projektpfad + @"\Zweirad_" + this.hc.Einstellung.Einträge + ".bin");
                fc.Data.ID = this.hc.Einstellung.Einträge;
                fc.WriteFile();

                fc.Poke();

                // Display data window
                ÖffneFahrzeugFenster(this.Projektpfad + @"\Zweirad_" + this.hc.Einstellung.Einträge + ".bin");

                // increase data counter
                this.hc.Einstellung.Einträge++;

                Fahrzeuge.DataContext = wpfc.Fahrzeuge(this.Projektpfad);
            }
        }

        private void RemData_Click(object sender, RoutedEventArgs e)
        {
            // Lösche eine vorhandene, zuvor markierten Eintrag
            // Auswahl "Sind Sie sicher dass Sie ... löschen wollen?"
            if (Fahrzeuge.SelectedIndex == -1)
            {
                MessageBox.Show("Es wurde kein Eintrag ausgewählt.", "Fehler - Zweiradverwaltung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult mb = MessageBox.Show("Sind Sie sicher, dass Sie den ausgewählten Eintrag löschen möchten?", "Zweiradverwaltung - Löschen", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (mb == MessageBoxResult.Yes)
            {
                //Lösche ausgewählten Eintrag
                int index = Fahrzeuge.SelectedIndex;
                DataRowView row = (DataRowView)Fahrzeuge.SelectedItems[0];
                int WindowID = -1;

                Regex reg = new Regex(@"Zweirad Eintrag \(([0-9]+)\)");

                foreach (Window win in App.Current.Windows)
                {
                    Match m = reg.Match(win.Title);
                    if (m.Success)
                    {
                        WindowID = Convert.ToInt32(m.Groups[1].Value);
                        if (Convert.ToInt32(row["ID"]) == WindowID)
                            win.Close();
                    }
                }
                File.Delete(this.Projektpfad + @"\Zweirad_" + row["ID"] + ".bin");

                Fahrzeuge.DataContext = wpfc.Fahrzeuge(this.Projektpfad);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Überprüfe ob MainWindow noch offen ist
            // Bug #F7368A85
            bool isWindowOpen = false;
            foreach (Window win in App.Current.Windows)
                if (win.IsActive)
                {
                    isWindowOpen = true;
                    continue;
                }

            if (isWindowOpen && this.hc != null)
            {
                if (this.hc != null)
                    this.hc.Write();

                base.OnClosed(e);
                Application.Current.Shutdown();
                return;
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            // Neue Niederlassung
            Pfadauswahl pa = new Pfadauswahl();
            if (pa.Projektpfad == null || pa.Projektpfad == "")
                return;

            this.Projektpfad = pa.Projektpfad;

            this.hc = new Hauptkonfiguration(this.Projektpfad);
            EOpen();
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            if (this.Projektpfad != null && this.Projektpfad != "")
                Fahrzeuge.DataContext = wpfc.Fahrzeuge(this.Projektpfad);
        }

    }
}
