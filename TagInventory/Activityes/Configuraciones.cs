using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TagInventory.Utilerias;

namespace TagInventory.Activityes
{
    [Activity(Label = "Configuraciones", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class Configuraciones : Activity
    {
        AppConfig configuraciones;
        Utilerias utilerias;
        Spinner spPotencia, spFilesExt;
        Button bttGuardar;
        EditText ettURL, ettSeparador, ettTagTrim, ettPatron;
        CheckBox chbPatron, chbAgregaNoIdnt;
        TextView txtviewIdDev;
        public Hardware Hardware { get; set; }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Configuraciones);

            LinearLayout LLFondo = FindViewById<LinearLayout>(Resource.Id.LLConfigFndo);
            ImageHelper.SetScaledBackground(this, LLFondo, Resource.Drawable.FondoLista, 200, 200);

            DisplayMetrics DM = Resources.DisplayMetrics;
            if (DM.HeightPixels == 728 && DM.WidthPixels == 480)
                Hardware = Hardware.ZebraMC33;
            else if (DM.HeightPixels == 1344 && DM.WidthPixels == 720)
                Hardware = Hardware.Urovo;
            else if (DM.HeightPixels == 1450 && DM.WidthPixels == 720)
                Hardware = Hardware.ZebraTC15;
            else if (DM.HeightPixels == 1184 && DM.WidthPixels == 720)
                Hardware = Hardware.Keyence;
            else if (DM.HeightPixels == 1776 && DM.WidthPixels == 1080)
                Hardware = Hardware.ChainwayC72;
            else
                Hardware = Hardware.PointMobile;

            configuraciones = new AppConfig();
            configuraciones.Load();
            utilerias = new Utilerias();
            bttGuardar = FindViewById<Button>(Resource.Id.btt_GuardarConfigs);

            ettURL = FindViewById<EditText>(Resource.Id.ettURL);
            ettSeparador = FindViewById<EditText>(Resource.Id.ettSeparador);
            ettPatron = FindViewById<EditText>(Resource.Id.ettPatron);
            ettTagTrim = FindViewById<EditText>(Resource.Id.ettRecorteTAG);

            chbPatron = FindViewById<CheckBox>(Resource.Id.checkbox_Patron);
            chbAgregaNoIdnt = FindViewById<CheckBox>(Resource.Id.checkbox_TeoricoAgregarNoIdentificados);

            txtviewIdDev = FindViewById<TextView>(Resource.Id.txtviewIdDevice);
            txtviewIdDev.LongClick += TxtviewIdDev_LongClick;

            spPotencia = FindViewById<Spinner>(Resource.Id.spPotencia);
            spPotencia.ItemSelected += SpPotencia_ItemSelected;

            spFilesExt = FindViewById<Spinner>(Resource.Id.spExtension);
            spFilesExt.ItemSelected += SpFilesExt_ItemSelected;

            List<int> Potencia = new List<int>();
            int i;
            int Maxval = 30;
            if (Hardware == Hardware.ZebraMC33)
                Maxval = 300;

            for (i = 0; i <= Maxval; i++)
                Potencia.Add(i);

            ArrayAdapter adaptador = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, Potencia.ToArray());
            spPotencia.Adapter = adaptador;
            if(Hardware == Hardware.ZebraMC33)
                spPotencia.SetSelection((int)configuraciones.ZebraAntenaPower);
            else
                spPotencia.SetSelection(Maxval);

            List<string> FileExt = new List<string>() { ".txt", ".csv" };
            ArrayAdapter adaptr = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, FileExt.ToArray());
            spFilesExt.Adapter = adaptr;
            int IdInt = FileExt.IndexOf(configuraciones.FileExtension);
            spFilesExt.SetSelection(IdInt);

            ettURL.Text = configuraciones.WSRFDIDemo;
            ettSeparador.Text = configuraciones.CSVSeparador;
            txtviewIdDev.Text = utilerias.GetZebraSerialNumber(); /*Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);*/
            ettURL.TextChanged += EttURL_TextChanged;
            ettSeparador.TextChanged += EttSeparador_TextChanged;
            bttGuardar.Click += BttGuardar_Click;

            ettPatron.Enabled = configuraciones.SrchPattern.Active;

            chbPatron.Checked = configuraciones.SrchPattern.Active;
            chbAgregaNoIdnt.Checked = configuraciones.AgregarNoIdent;

            ettPatron.Text = configuraciones.SrchPattern.Pattern;
            ettTagTrim.Text = configuraciones.TrimTag.ToString();

            ettPatron.TextChanged += EttPatron_TextChanged;
            chbPatron.CheckedChange += ChbPatron_CheckedChange;
            chbAgregaNoIdnt.CheckedChange += ChbAgregaNoIdnt_CheckedChange;
            ettTagTrim.TextChanged += EttTagTrim_TextChanged;
        }
        private void ChbAgregaNoIdnt_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            configuraciones.AgregarNoIdent = chbAgregaNoIdnt.Checked;
        }
        private void EttTagTrim_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            int val = 0;
            if (ettTagTrim.Text == "")
                val = 0;
            else
                val = int.Parse(ettTagTrim.Text);
            configuraciones.TrimTag = val;
        }
        private void SpFilesExt_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            configuraciones.FileExtension = spFilesExt.SelectedItem.ToString();
            bttGuardar.Enabled = true;
        }
        private void ChbPatron_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            ettPatron.Enabled = chbPatron.Checked;
            configuraciones.SrchPattern.Active = chbPatron.Checked;
            if (chbPatron.Checked)
            {
                utilerias.ShowMessage("Activar esta opción requiere de conocimientos avanzados programación de la clase Regex.\nSi no conoces esto, deshabilita la opción.", "Patron", this);
            }
        }
        private void EttPatron_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            configuraciones.SrchPattern.Pattern = ettPatron.Text;
        }
        private void TxtviewIdDev_LongClick(object sender, View.LongClickEventArgs e)
        {
            ScanmexLicenseInfo scanmexLicenseInfo = new ScanmexLicenseInfo();
            scanmexLicenseInfo.Load();
            scanmexLicenseInfo.ShowLicenseInfo(this);
        }
        private void EttSeparador_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            configuraciones.CSVSeparador = ettSeparador.Text;
        }
        private void BttGuardar_Click(object sender, EventArgs e)
        {
            if (configuraciones.SrchPattern.Active)
                if (ettPatron.Text == "")
                {
                    Toast.MakeText(this, "Se tiene activa la opción de un patron de búsqueda debes escribir un patron!", ToastLength.Long).Show();
                    return;
                }
            string TrySave = "";
            configuraciones.ZebraAntenaPower = short.Parse(spPotencia.SelectedItem.ToString());
            TrySave = configuraciones.Save();
            if (TrySave != "")
                utilerias.ShowMessage(string.Format("Ocurrió un error al guardar las configuraciones.\n{0}", TrySave), "Guardar", this);
            else
            {
                Toast.MakeText(this, "Configuraciones guardadas exitosamente", ToastLength.Long).Show();
                this.Finish();
            }
        }
        private void EttURL_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            configuraciones.WSRFDIDemo = ettURL.Text;
        }
        private void SpPotencia_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            bttGuardar.Enabled = true;
        }
    }
}