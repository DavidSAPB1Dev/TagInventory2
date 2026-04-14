using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static TagInventory.Utilerias;

namespace TagInventory.Activityes
{
    [Activity(Label = "MenuP", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MenuP : Activity
    {
        Button bttRecuento, bttRecuentoTeorico, bttConfig;
        int REQUEST_WRITE_EXTERNAL_STORAGE = 200;
        int REQUEST_INTERNET = 300;
        Utilerias utilerias;
        AppConfig configuraciones;
        public Hardware Hardware { get; set; }
        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Menu);
            configuraciones = new AppConfig();
            configuraciones.Load();
            utilerias = new Utilerias();

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

            bttRecuento = FindViewById<Button>(Resource.Id.btt_Recuento);
            bttRecuentoTeorico = FindViewById<Button>(Resource.Id.btt_RecuentoTeorico);

            bttConfig = FindViewById<Button>(Resource.Id.btt_Configs);
            //"android:background="@drawable/FondoMenu"
            LinearLayout LL = FindViewById<LinearLayout>(Resource.Id.MenuLayout);
            ImageHelper.SetScaledBackground(this, LL, Resource.Drawable.FondoMenu, 200, 200);
            //android: background = "@drawable/BtnRFIDRead"
            ImageHelper.SetScaledBackground(this, bttRecuento, Resource.Drawable.BtnInventarioCiego, 200, 200);
            ImageHelper.SetScaledBackground(this, bttRecuentoTeorico, Resource.Drawable.BtnInventarioDocumento, 200, 200);
            //android: background = "@drawable/btnconfig"
            ImageHelper.SetScaledBackground(this, bttConfig, Resource.Drawable.BtnConfig, 200, 200);

            //
            bttRecuento.Click += BttRecuento_Click;
            bttRecuentoTeorico.Click += BttRecuentoTeorico_Click;
            bttConfig.Click += BttConfig_Click;

            await ObtenPermisoDeEscritura();

            if (Hardware == Hardware.ZebraMC33)
            {
                ScanmexLicenseInfo scanmexLicienseInfo = new ScanmexLicenseInfo();
                string licenseInfo = scanmexLicienseInfo.GetLicenseInfo();
                //licenseInfo = utilerias.GetZebraSerialNumber();
                //licenseInfo = utilerias.GetSerialNumber(this);

                if (licenseInfo != "")
                {
                    if (configuraciones.FirstUse)
                    {
                        BloqueaoDesbloqueaBotones(false);
                        ShowLicense();
                        configuraciones.FirstUse = false;
                        configuraciones.Save();
                        return;
                    }
                    utilerias.ShowMessage(licenseInfo, "Licencia", this);
                    scanmexLicienseInfo.CreateLicense();
                    BloqueaoDesbloqueaBotones(false);
                    return;
                }
            }

            //ShowLicense();
            //if (utilerias.IsAutoTimeEnabled())
            //{
            //    if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            //    {
            //    }
            //    else
            //    {
            //        Toast.MakeText(this, "No se pudo obtener la fecha", ToastLength.Long).Show();
            //    }
            //}
            //else
            //{
            //    Toast.MakeText(this, "Deshabilitado", ToastLength.Long).Show();
            //}
        }

        private void BttRecuentoTeorico_Click(object sender, EventArgs e)
        {

            LayoutInflater layoutInflater = LayoutInflater.From(this);
            View view = layoutInflater.Inflate(Resource.Layout.InputFileSearch, null);
            Spinner spFiles = view.FindViewById<Spinner>(Resource.Id.sp_Archivos);

            string DirPath = @"/storage/emulated/0/Download";

            DirectoryInfo DI = new DirectoryInfo(DirPath);
            FileInfo[] files = DI.GetFiles("*.csv");

            List<Dictionary<string, string>> DFiles = new List<Dictionary<string, string>>();
            foreach (FileInfo file in files)
                if (!file.Name.Contains("RecuentoSc"))
                    DFiles.Add(new Dictionary<string, string>()
        {
            { "FileName", file.Name },
            { "FullPath", file.FullName }
        });

            ArrayAdapter adaptador = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, DFiles.Select(d => d["FileName"]).ToList());
            spFiles.Adapter = adaptador;

            AlertDialog.Builder ADFiles = new AlertDialog.Builder(this);
            ADFiles.SetView(view);
            ADFiles.SetPositiveButton("Ok", async delegate
            {
                Intent intent = new Intent(BaseContext, typeof(Inventory));
                int pos = spFiles.SelectedItemPosition;
                string fullPath = DFiles[pos]["FullPath"];
                intent.PutExtra("TeoricoFName", fullPath);
                StartActivity(intent);
            });
            ADFiles.SetNegativeButton("Cancelar", delegate
            {

            });
            ADFiles.Show();
        }

        private void BloqueaoDesbloqueaBotones(bool Enable)
        {
            bttRecuento.Enabled = Enable;
            if (Enable)
            {
                bttRecuento.SetBackgroundResource(Resource.Drawable.BtnRFIDRead);
                bttConfig.SetBackgroundResource(Resource.Drawable.BtnConfig);
            }
            else
            {
                bttRecuento.SetBackgroundResource(Resource.Drawable.BtnRFIDReadDisable);
                bttConfig.SetBackgroundResource(Resource.Drawable.BtnConfigDisable);
            }
        }
        private void ShowLicense()
        {
            LayoutInflater layoutInflater = LayoutInflater.From(this);
            View view = layoutInflater.Inflate(Resource.Layout.InputLicense, null);
            AlertDialog.Builder License = new AlertDialog.Builder(this);
            EditText ettId = view.FindViewById<EditText>(Resource.Id.ettId);

            ettId.Text = utilerias.GetZebraSerialNumber();/* Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);*/
            License.SetView(view);
            License.SetPositiveButton("Ok", delegate
            {
                //ScanmexLicenseInfo licienseInfo = new ScanmexLicenseInfo();
                //string LicRes;
                //LicRes = licienseInfo.CreateLicenseFile(ettPass.Text, NPUsage.Value);
                //if (LicRes == "")
                //{
                //    configuraciones.MaxUssageQty = NPUsage.Value;
                //    //configuraciones.LicenseId = sn;
                //    configuraciones.FirstUse = false;
                //    configuraciones.UsedQty++;
                //    configuraciones.Save();
                //    BloqueaoDesbloqueaBotones(true);
                //    ObtenPermisoDeEscritura();
                //}
                //else
                //{
                //    utilerias.ShowMessage(LicRes, "Licencia", this);
                //    BloqueaoDesbloqueaBotones(false);
                //}
            });
            //License.SetNegativeButton("Cancelar", delegate
            //{
            //    BloqueaoDesbloqueaBotones(false);
            //});
            License.Show();
        }
        private void BttConfig_Click(object sender, EventArgs e)
        {
            if (!bttRecuento.Enabled)
            {
                ShowLicense();
                return;
            }
            Intent intent = new Intent(BaseContext, typeof(Configuraciones));
            StartActivity(intent);
        }

        public async Task ObtenPermisoDeEscritura()
        {
            await Task.Delay(10);
            int hasPermission = (int)ContextCompat.CheckSelfPermission(this, "android.permission.WRITE_EXTERNAL_STORAGE");
            if (hasPermission != 0) // PackageManaer.PERMISSION_GRANTED 
            {
                ActivityCompat.RequestPermissions(this, new string[] { "android.permission.WRITE_EXTERNAL_STORAGE" }, REQUEST_WRITE_EXTERNAL_STORAGE);
                return;
            }
        }
        public void ObtenPermisoInternet()
        {
            int hasPermission = (int)ContextCompat.CheckSelfPermission(this, "android.permission.INTERNET");
            if (hasPermission != 0) // PackageManaer.PERMISSION_GRANTED 
            {
                ActivityCompat.RequestPermissions(this, new string[] { "android.permission.INTERNET" }, REQUEST_INTERNET);
                return;
            }
        }
        private void BttRecuento_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(BaseContext, typeof(Inventory));
            StartActivity(intent);
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
                    scanmexLicenseInfo.CreateLicense();
                    BloqueaoDesbloqueaBotones(false);
                    return;
                }
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == REQUEST_WRITE_EXTERNAL_STORAGE)
                if (!(grantResults.Length == 1 && grantResults[0] == 0))
                    Toast.MakeText(this, "Permiso denegado", ToastLength.Long).Show();
        }
    }
}