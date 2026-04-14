using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Javax.Xml.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TagInventory.Modelos;
using static TagInventory.Utilerias;

namespace TagInventory.Activityes
{
    [Activity(Label = "MenuGuardar", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MenuGuardar : Activity
    {
        Button btt_GuardarCSV, btt_GuardarBDWS;
        List<ItemRecuento> TagList;
        ProgressDialog progress;
        Utilerias utilerias;
        AppConfig configuraciones;
        public SaveType saveType;
        public Hardware Hardware { get; set; }
        public enum SaveType
        {
            CSV = 0,
            WS = 1
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SeleccionGuardar);

            LinearLayout LLFondo = FindViewById<LinearLayout>(Resource.Id.MenuLayout);
            ImageHelper.SetScaledBackground(this, LLFondo, Resource.Drawable.FondoAlmacenamiento, 200, 200);

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

            utilerias = new Utilerias();
            configuraciones = new AppConfig();
            configuraciones.Load();
            btt_GuardarCSV = FindViewById<Button>(Resource.Id.btt_GuardarCSV);
            btt_GuardarCSV.Click += Btt_GuardarCSV_Click;

            btt_GuardarBDWS = FindViewById<Button>(Resource.Id.btt_GuardarBDWS);
            btt_GuardarBDWS.Click += Btt_GuardarBDWS_Click;
            string TagsInfo;
            string filePath = System.IO.Path.Combine(CacheDir.AbsolutePath, "TagInventoryTags.json");
            double Size = Intent.GetDoubleExtra("Size", 0);
            if (Size >= 200)
                TagsInfo = System.IO.File.ReadAllText(filePath);
            else
                TagsInfo = Intent.GetStringExtra("Tags");
            TagList = JsonSerializer.Deserialize<List<ItemRecuento>>(TagsInfo);
        }
        private void Btt_GuardarBDWS_Click(object sender, EventArgs e)
        {
            if (Hardware == Hardware.ZebraMC33)
            {
                ScanmexLicenseInfo scanmexLicenseInfo = new ScanmexLicenseInfo();
                string cargalicencias = scanmexLicenseInfo.GetLicenseInfo();
                if (cargalicencias != "")
                {
                    utilerias.ShowMessage(cargalicencias, "Licencia", this);
                    return;
                }
            }

            Intent intent = new Intent(BaseContext, typeof(GuardarWSOSAP));
            intent.PutExtra("SaveType", 1);
            //intent.PutExtra("Tags", JsonSerializer.Serialize<List<ItemRecuento>>(TagList));
            intent.PutExtra("Tags", JsonSerializer.Serialize(TagList));
            StartActivity(intent);
        }
        private void Btt_GuardarCSV_Click(object sender, EventArgs e)
        {
            if (Hardware == Hardware.ZebraMC33)
            {
                ScanmexLicenseInfo scanmexLicenseInfo = new ScanmexLicenseInfo();
                string cargalicencias = scanmexLicenseInfo.GetLicenseInfo();
                if (cargalicencias != "")
                {
                    scanmexLicenseInfo.CreateLicense();
                    utilerias.ShowMessage(cargalicencias, "Licencia", this);
                    return;
                }
            }
            Archivo();
        }
        protected override void OnResume()
        {
            base.OnResume();
            configuraciones?.Load();
            if (Hardware == Hardware.ZebraMC33)
            {
                ScanmexLicenseInfo scanmexLicenseInfo = new ScanmexLicenseInfo();
                string cargalicencias = scanmexLicenseInfo.GetLicenseInfo();
                if (cargalicencias != "")
                {
                    utilerias.ShowMessage(cargalicencias, "Licencia", this);
                    return;
                }
            }
        }
        private void Archivo()
        {
            try
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("Archivo");
                builder.SetMessage(string.Format("Se generará el archivo '{0}'.\n¿Deseas continuar?", configuraciones.FileExtension));
                builder.SetPositiveButton("Si", async delegate
                {
                    progress = new ProgressDialog(this);
                    progress.SetTitle("Cargando");
                    progress.Indeterminate = true;
                    progress.SetProgressStyle(ProgressDialogStyle.Spinner);
                    progress.SetMessage("Subiendo información...");
                    progress.SetCancelable(false);
                    progress.Show();

                    //string result = await Task.Run(async () => await CreateXMLDocRecuento());
                    string result = await Task.Run(async () => await utilerias.GeneraArchivo(TagList));

                    progress?.Dismiss();

                    if (!result.Contains("Error"))
                    {
                        Toast.MakeText(this, string.Format("Archivo generado exitosamente.\n{0}", result), ToastLength.Long).Show();
                        Thread.Sleep(500);
                        Java.IO.File file = new Java.IO.File(result);
                        file.SetReadable(true);

                        if (Hardware == Hardware.ZebraMC33)
                        {
                            ScanmexLicenseInfo scanmexLicenseInfo = new ScanmexLicenseInfo();
                            scanmexLicenseInfo.Load();
                            scanmexLicenseInfo.UsedQty++;
                            scanmexLicenseInfo.UpdateLicenseFile();
                        }

                        //configuraciones.UsedQty++;
                        //configuraciones.Save();
                        try
                        {
                            Intent intent = new Intent(Intent.ActionView);
                            Android.Net.Uri uri = Android.Net.Uri.FromFile(file);
                            intent.SetDataAndType(uri, "text/plain");
                            intent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
                            StartActivity(intent);
                        }
                        catch { }
                    }
                    else
                        utilerias.ShowMessage(result, "Error", this);

                });
                builder.SetNegativeButton("No", delegate { builder.Dispose(); });
                builder.Show();
            }
            catch (Exception ex)
            {
                utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), "Archivo", this);
            }
        }
    }
}
