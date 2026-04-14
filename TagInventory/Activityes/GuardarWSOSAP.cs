using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using static Xamarin.Essentials.Permissions;
using System.Text.Json;
using System.Threading.Tasks;
using TagInventory.Adaptadores;
using TagInventory.Modelos;

namespace TagInventory.Activityes
{
    [Activity(Label = "GuardarWSOSAP", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class GuardarWSOSAP : Activity
    {
        EditText ettUrl, ettSeparador, ettIndex;
        Button btt_CargarTags;
        Utilerias utilerias;
        AppConfig configuraciones;
        List<ItemRecuento> TagList;
        ListView LVTagList;
        bool DispServ;
        public string URLServiceLayer, URLDIAPI, URLWS;
        List<string> Almacenes;
        ProgressDialog progress;
        private BaseAdapter_Recuento_ListView baseAdapterInventoryListView;
        public SaveType saveType;
        public enum SaveType
        {
            CSV = 0,
            WS = 1
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.GuardarWSoSAP);

            LinearLayout LLFondo = FindViewById<LinearLayout>(Resource.Id.LLGuardar);
            ImageHelper.SetScaledBackground(this, LLFondo, Resource.Drawable.FondoLista, 200, 200);

            utilerias = new Utilerias();

            configuraciones = new AppConfig();
            configuraciones.Load();

            ettUrl = FindViewById<EditText>(Resource.Id.ettUrlOWS);
            ettUrl.TextChanged += EttUrl_TextChanged;
            ettUrl.FocusChange += EttUrl_FocusChange;
            ettUrl.LongClick += EttUrl_LongClick;


            ettSeparador = FindViewById<EditText>(Resource.Id.ettSeparador);
            ettSeparador.TextChanged += EttSeparador_TextChanged;

            ettIndex = FindViewById<EditText>(Resource.Id.ettIndex);
            ettIndex.TextChanged += EttIndex_TextChanged;

            btt_CargarTags = FindViewById<Button>(Resource.Id.btt_CargarTags);
            btt_CargarTags.Click += Btt_CargarTags_Click;

            btt_CargarTags.Enabled = false;
            utilerias.EnableChange(btt_CargarTags);

            baseAdapterInventoryListView = new BaseAdapter_Recuento_ListView(this);

            LVTagList = FindViewById<ListView>(Resource.Id.listview_TagList);
            LVTagList.Adapter = baseAdapterInventoryListView;

            string TagsInfo = Intent.GetStringExtra("Tags");
            TagList = JsonSerializer.Deserialize<List<ItemRecuento>>(TagsInfo);

            int ST = Intent.GetIntExtra("SaveType", -1);
            saveType = (SaveType)ST;

            RunOnUiThread(() =>
            {
                foreach (ItemRecuento IT in TagList)
                    baseAdapterInventoryListView.AddRecuentoItem(IT);

                baseAdapterInventoryListView.NotifyDataSetChanged();
            });

            //Limpieza de tags (Caracteres Hexadecimales no se pueden imprimir, por lo tanto se tienen que cambiar
            int Limpiados = 0;
            foreach (ItemRecuento IR in baseAdapterInventoryListView.RecuentoList)
                foreach (char c in IR.StringValue)
                    if (!(Char.IsLetterOrDigit(c) || Char.IsPunctuation(c) || Char.IsNumber(c) || Char.IsSymbol(c) || Char.IsSeparator(c)))
                    {
                        IR.StringValue = IR.StringValue.Replace(c, '˟');
                        Limpiados++;
                    }
            if (Limpiados > 0)
                Toast.MakeText(this, string.Format("Se limpiaron {0} caracteres hexadecimal", Limpiados), ToastLength.Long).Show();

            if (saveType == SaveType.CSV || saveType == SaveType.WS)
            {
                ettSeparador.Visibility = ViewStates.Gone;
                ettIndex.Visibility = ViewStates.Gone;
            }
            else
            {
                baseAdapterInventoryListView.IsToShow = true;
                ettUrl.Hint = "IP o Servidor";
            }
            ettUrl.Text = configuraciones.WSRFDIDemo;
        }
        private void EttUrl_LongClick(object sender, View.LongClickEventArgs e)
        {
            if (saveType == SaveType.WS)
            {
                ettUrl.Text = configuraciones.WSRFDIDemo;
                WSRFIDDemoAvailable();
            }
        }
        private void EttUrl_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (!e.HasFocus)
                if (DispServ)
                    if (saveType == SaveType.WS)
                        WSRFIDDemoAvailable();
        }
        private void WSRFIDDemoAvailable()
        {
            bool WSDisp = false;
            try
            {
                WSRFID.WSRFIDDemo ws = new WSRFID.WSRFIDDemo();
                ws.Url = URLWS;
                WSDisp = ws.Available();
            }
            catch (Exception ex)
            {

                WSDisp = false;
            }
            btt_CargarTags.Enabled = WSDisp;
            utilerias.EnableChange(btt_CargarTags);
        }
        private void EttIndex_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            int ind = 0;
            if (ettIndex.Text != "")
                ind = int.Parse(ettIndex.Text);

            baseAdapterInventoryListView.Index = ind;
            baseAdapterInventoryListView.NotifyDataSetChanged();

        }
        private void EttSeparador_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            string sep = "";
            if (ettSeparador.Text != "")
                sep = ettSeparador.Text;

            baseAdapterInventoryListView.Separador = sep;
            baseAdapterInventoryListView.NotifyDataSetChanged();
        }
        private void EttUrl_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            ValidateUrl();
        }
        private void ValidateUrl()
        {
            DispServ = utilerias.PingHost(ettUrl.Text);

            if (saveType == SaveType.WS)
                URLWS = ettUrl.Text;

            if (DispServ)
                ettUrl.SetBackgroundResource(Resource.Drawable.ettStroke);
            else
                ettUrl.SetBackgroundResource(Resource.Drawable.ettStrokeError);
        }
        private void Btt_CargarTags_Click(object sender, EventArgs e)
        {
            if (saveType == SaveType.WS)
                SubeABD();
        }
        protected override void OnResume()
        {
            base.OnResume();
            configuraciones?.Load();
            ScanmexLicenseInfo scanmexLicenseInfo = new ScanmexLicenseInfo();
            string cargalicencias = scanmexLicenseInfo.GetLicenseInfo();
            if (cargalicencias != "")
            {
                utilerias.ShowMessage(cargalicencias, "Licencia", this);
                return;
            }
        }
        private void SubeABD()
        {
            try
            {
                ScanmexLicenseInfo scanmexLicenseInfo = new ScanmexLicenseInfo();
                string cargalicencias = scanmexLicenseInfo.GetLicenseInfo();
                if (cargalicencias != "")
                {
                    utilerias.ShowMessage(cargalicencias, "Licencia", this);
                    scanmexLicenseInfo.CreateLicense();
                    return;
                }
                if (!DispServ)
                {
                    utilerias.ShowMessage("No se pudo establecer conexión\nAsegúrate de estar en la red", "Sin Servicio de red", this);
                    return;
                }

                if ((scanmexLicenseInfo.UsedQty / scanmexLicenseInfo.MaxUssageQty) >= .90)
                    Toast.MakeText(this, string.Format("Cargas restantes: {0}", scanmexLicenseInfo.MaxUssageQty - scanmexLicenseInfo.UsedQty), ToastLength.Long).Show();

                AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
                alertDialog.SetTitle("Crear");
                alertDialog.SetMessage("Se subirá el recuento al sistema.\n¿Deseas continuar?");
                alertDialog.SetPositiveButton("Si", async delegate
                {
                    progress = new ProgressDialog(this);
                    progress.SetTitle("Cargando");
                    progress.Indeterminate = true;
                    progress.SetProgressStyle(ProgressDialogStyle.Spinner);
                    progress.SetMessage("Subiendo información...");
                    progress.SetCancelable(false);
                    progress.Show();

                    //string result = await Task.Run(async () => await CreateXMLDocRecuento());
                    string result = await Task.Run(async () => await utilerias.CreateXMLDocRecuento(configuraciones, baseAdapterInventoryListView.RecuentoList));

                    progress?.Dismiss();

                    if (!result.Contains("Error"))
                    {
                        utilerias.ShowMessage(string.Format("Filas agregadas: {0}", result), "Crear", this);
                        ScanmexLicenseInfo scanmexLicenseInfo = new ScanmexLicenseInfo();
                        scanmexLicenseInfo.Load();
                        scanmexLicenseInfo.UsedQty++;
                        scanmexLicenseInfo.UpdateLicenseFile();
                        //configuraciones.UsedQty++;
                        //configuraciones.Save();
                    }
                    else
                        utilerias.ShowMessage(result, "Error", this);
                });
                alertDialog.SetNegativeButton("No", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();

            }
            catch (System.Exception ex)
            {
                utilerias.ShowMessage(ex.Message, MethodBase.GetCurrentMethod().Name, this);
            }
            finally
            {
                if (progress != null)
                    progress.Dismiss();
            }
        }
    }
}