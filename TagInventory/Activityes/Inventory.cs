using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Text.Style;
using Android.Text;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Com.Zebra.Rfid.Api3;
using TagInventory.Adaptadores;
using Symbol.XamarinEMDK.Barcode;
using Symbol.XamarinEMDK;
using TagInventory.Modelos;
using System.Text.Json;
using Android.Graphics;
using TagInventory.ScannerControllers;
using Android.Util;
using static TagInventory.Utilerias;
using Com.Rscja.Deviceapi.Entity;
using System.IO;
using System.Runtime.Remoting.Contexts;
using System.Reflection;

namespace TagInventory.Activityes
{
    [Activity(Label = "Inventory", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class Inventory : Activity, EMDKManager.IEMDKListener
    {
        //Scanner
        EMDKManager emdkManager = null;
        BarcodeManager barcodeManager = null;
        Symbol.XamarinEMDK.Barcode.Scanner scanner = null;
        bool Scanner;
        //End
        //Reader Ya se traslado el controlador de la app SS1 a este codigo, hace falta instanciar y probar 06/08/2025, en cuanto se valide, hay que eliminar esta parte de codigo
        private static Readers readers;
        private static IList<ReaderDevice> availableRFIDReaderList;
        private static ReaderDevice readerDevice;
        private static RFIDReader Reader;
        private EventHandler eventHandler;
        private static Activity A;
        private static ToneGenerator Tone;
        //Reader
        //Chainway reader
        public static ChainwayRFIDController CRFIDC;
        public static ENUM_TRIGGER_MODE TriggerMode;
        public static HorizontalScrollView HeaderHorizontalScrollView;
        public static HorizontalScrollView LinesHorizontalScrollView;
        public static List<MEMORY_BANK> memoryBanksToRead;

        public static Button btt_Borrar;
        public static Button btt_Subir;
        public static Button btt_Potencia;
        public static Button btt_RFIDOrBarCode;
        public static Button btt_Memorias;
        public static Button btt_Buscar;
        public static Button btt_LecturaStartStop;
        public static TextView txtViewAntenaPwr;
        public static TextView textViewtitle;
        public static TextView textViewStatus;
        private static Utilerias utilerias;
        ProgressDialog progress;

        static AppConfig configuraciones;
        static bool isRfidRunning = false;
        public short AntPwr = 300;
        ListView inventoryListView;
        //public static List<TagData> TagList;
        public static List<GenericTag> TagList { get; set; }
        public static List<ItemRecuento> ItmRecuentoList;
        public static Hardware Hardware { get; set; }
        public static InventoryType IType { get; set; }
        //public static BaseAdapter_Inventory_ListView baseAdapterInventoryListView;
        private static BaseAdapter_Recuento_ListView baseAdapterInventoryListView;
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
                SetContentView(Resource.Layout.Inventory);
                LinearLayout LLFondo = FindViewById<LinearLayout>(Resource.Id.RecuentoZebra);

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

                A = this;
                if (Hardware == Hardware.ZebraMC33)
                {
                    ////Readers ZEBRA
                    readers ??= new Readers(Application.Context, ENUM_TRANSPORT.ServiceSerial);
                    string conreaderresstring = GetAvailableReaders();
                    //Asignación de acceso a la memoria
                    memoryBanksToRead = new List<MEMORY_BANK>();
                    memoryBanksToRead.Add(MEMORY_BANK.MemoryBankTid);
                    //memoryBanksToRead.Add(MEMORY_BANK.MemoryBankEpc);
                    //memoryBanksToRead.Add(MEMORY_BANK.MemoryBankReserved);
                    //memoryBanksToRead.Add(MEMORY_BANK.MemoryBankUser);
                    if (conreaderresstring != "")
                        utilerias.ShowMessage(conreaderresstring, "Error conexión reader", this);
                }
                else
                {
                    //android: background = "@drawable/fondolista"
                    ImageHelper.SetScaledBackground(this, LLFondo, Resource.Drawable.FondoLista, 200, 200);
                    CRFIDC ??= new ChainwayRFIDController();
                    CRFIDC.Init();
                    CRFIDC.SetEPCAndTIDMode();
                }

                //App
                //inventoryListView = FindViewById<ListView>(Resource.Id.listview_inventory);

                utilerias = new Utilerias();

                //HeaderHorizontalScrollView = (HorizontalScrollView)FindViewById(Resource.Id.HorizontalScrollHeader);
                //HeaderHorizontalScrollView.ScrollBarSize = 0;

                //LinesHorizontalScrollView = (HorizontalScrollView)FindViewById(Resource.Id.HorizontalScrollLines);
                //LinesHorizontalScrollView.ScrollChange += LinesHorizontalScrollView_ScrollChange;

                inventoryListView = (ListView)FindViewById(Resource.Id.listview_inventory);

                inventoryListView.ItemLongClick += InventoryListView_ItemLongClick;
                inventoryListView.ItemClick += InventoryListView_ItemClick;

                baseAdapterInventoryListView = new BaseAdapter_Recuento_ListView(this);
                baseAdapterInventoryListView.HardWare = Utilerias.Hardware.ZebraMC33;

                textViewtitle = (TextView)FindViewById(Resource.Id.textView_title);

                //25-08_2025
                //Pruebas para poder cargar un archivo y que se puedan leer "inventario teorico"
                IType = InventoryType.Blind;
                string InvTeorico = Intent.GetStringExtra("TeoricoFName");
                if (!string.IsNullOrEmpty(InvTeorico))
                    await ReadFile(InvTeorico);

                baseAdapterInventoryListView.IType = IType;
                //Termino de pruebas.

                //TagList = new List<TagData>();
                TagList = new List<GenericTag>();
                inventoryListView.Adapter = baseAdapterInventoryListView;

                btt_Subir = (Button)FindViewById(Resource.Id.button_subir);
                btt_Subir.Enabled = false;
                EnableChange(btt_Subir);
                btt_Subir.Click += Btt_Subir_Click;

                btt_Borrar = (Button)FindViewById(Resource.Id.button_scan_delete);
                btt_Borrar.Enabled = false;
                EnableChange(btt_Borrar);
                btt_Borrar.Click += Btt_Borrar_Click;

                btt_RFIDOrBarCode = (Button)FindViewById(Resource.Id.button_BCoRFID);
                btt_RFIDOrBarCode.Click += Btt_RFIDOrBarCodeClick;

                btt_Potencia = (Button)FindViewById(Resource.Id.button_ConfigAntena);
                btt_Potencia.Click += Btt_Potencia_Click;

                btt_Memorias = (Button)FindViewById(Resource.Id.button_Memorias);
                btt_Memorias.Click += Btt_Memorias_Click;

                btt_Buscar = (Button)FindViewById(Resource.Id.button_Buscar);
                btt_Buscar.Click += Btt_Buscar_Click;

                btt_LecturaStartStop = (Button)FindViewById(Resource.Id.button_RFIDPlayOrStop);
                btt_LecturaStartStop.Click += Btt_Leer_Click;

                textViewtitle.Click += TextViewtitle_Click;

                textViewStatus = (TextView)FindViewById(Resource.Id.textViewStatus);

                Tone = new ToneGenerator(Android.Media.Stream.Dtmf, 75);
                configuraciones = new AppConfig();
                configuraciones.Load();
                AntPwr = configuraciones.ZebraAntenaPower;

                txtViewAntenaPwr = (TextView)FindViewById(Resource.Id.textView_AntenaPwr);
                txtViewAntenaPwr.Text = string.Format("Potencia: {0} dbM", configuraciones.ZebraAntenaPower.ToString());

                Scanner = false;
            }
            catch (Exception ex)
            {
                utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, this);
            }
        }

        private void Btt_Buscar_Click(object sender, EventArgs e)
        {
            if (btt_Buscar.Text == "Quitar")
            {
                btt_Buscar.Text = "Buscar";
                baseAdapterInventoryListView.RecuentoList = ItmRecuentoList;
                baseAdapterInventoryListView.NotifyDataSetChanged();
                return;
            }
            LayoutInflater layoutInflater = LayoutInflater.From(this);
            View view = layoutInflater.Inflate(Resource.Layout.InputSearch, null);
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            EditText ettItemCode = view.FindViewById<EditText>(Resource.Id.ett_Buscar);
            RadioButton rbSoloLeidos = view.FindViewById<RadioButton>(Resource.Id.rb_SoloLeidos);
            builder.SetView(view);
            builder.SetPositiveButton("Ok", delegate
            {
                List<ItemRecuento> searchList = null;
                string itmCode = ettItemCode.Text;

                if (rbSoloLeidos.Checked)
                    searchList = ItmRecuentoList.FindAll(it => it.TagInfoList.Count > 0);
                else if (!string.IsNullOrEmpty(itmCode))
                    searchList = ItmRecuentoList.FindAll(it => it.StringValue.ToUpper().Contains(itmCode.ToUpper()));

                if (searchList.Count > 0)
                {
                    baseAdapterInventoryListView.RecuentoList = searchList;
                    Toast.MakeText(this, string.Format("Registros coincidentes: {0}", searchList.Count), ToastLength.Long).Show();
                    btt_Buscar.Text = "Quitar";
                }
                else
                    baseAdapterInventoryListView.RecuentoList = ItmRecuentoList;
                baseAdapterInventoryListView.NotifyDataSetChanged();
            });
            builder.SetNegativeButton("Cancelar", delegate
            {

            });
            builder.Show();
        }

        public async Task ReadFile(string FPath)
        {
            //utilerias.ShowProgressDialog("Archivo", "Leyendo archivo", ref progress, this);
            ProgressDialog progress = new ProgressDialog(this);
            progress.SetTitle("Procesando");
            progress.SetMessage("Leyendo archivo...");
            progress.SetProgressStyle(ProgressDialogStyle.Horizontal);
            progress.SetCancelable(false);
            progress.Progress = 0;
            progress.Show();
            try
            {
                await Task.Run(() =>
                {
                    IType = InventoryType.Theoric;
                    string[] filas = File.ReadAllLines(FPath);
                    ItemRecuento itm;
                    int c = 0;
                    ItmRecuentoList = new List<ItemRecuento>();
                    progress.Max = filas.Count();
                    foreach (string fila in filas)
                    {
                        c++;
                        if (c == 1) continue; //Encabezados
                        progress.Progress = c;
                        itm = new ItemRecuento();
                        itm.InFile = true;
                        itm.HexValue = utilerias.GetStringToHex(fila.Split(',')[0]);
                        itm.TID = Guid.NewGuid().ToString();
                        itm.StringValue = utilerias.GetHexToString(itm.HexValue);
                        itm.Qty = int.Parse(fila.Split(',')[1]);
                        itm.MemoryBankString = "TID";
                        ItmRecuentoList.Add(itm);
                    }
                });

                //Se tuvo que implementar de esta manera, ya que se encontro que al leer muchos elementos, esto trababa la UI y tardaba mucho al momento de agregar uno por uno al adapter.
                //RunOnUiThread(() =>
                //{
                foreach (ItemRecuento item in ItmRecuentoList)
                    baseAdapterInventoryListView.RecuentoList = ItmRecuentoList;
                //});
                textViewtitle.Text = string.Format("Tags {0} Totales: 0/{1}", ItmRecuentoList.Count, ItmRecuentoList.Sum(t => t.Qty));
            }
            finally
            {
                progress?.Dismiss();
            }

        }
        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (e.KeyCode.GetHashCode() == 139 || e.KeyCode.GetHashCode() == 280 || e.KeyCode.GetHashCode() == 293)
            {
                if (e.RepeatCount == 0)
                {
                    ChainwayRFIDScanEvent();
                    return true;
                }
            }
            return base.OnKeyDown(keyCode, e);
        }
        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            if (CRFIDC != null)
                if (CRFIDC.Reading)
                    CRFIDC.StopInventory();
            return base.OnKeyUp(keyCode, e);
        }
        private static void ChainwayRFIDScanEvent()
        {
            if (CRFIDC == null)
                return;
            if (CRFIDC.Reading)
                CRFIDC.StopInventory();
            else
            {
                if (CRFIDC.StartInventory())
                {
                    Thread th = new Thread(new ThreadStart(delegate
                    {
                        while (CRFIDC.Reading)
                        {
                            UHFTAGInfo TagInf = CRFIDC.ReadTagsFromBuffer();
                            if (TagInf != null)
                                ChainwayRFIDController_OnTagsRead(TagInf);
                            else
                                Thread.Sleep(2);
                        }
                    }));
                    th.IsBackground = true;
                    th.Start();
                }

            }
        }
        private static void ChainwayRFIDController_OnTagsRead(UHFTAGInfo UHFTag)
        {
            GenericTag GT = CRFIDC.FromChainway(UHFTag);
            if (memoryBanksToRead != null)
                GT.MemoryBank = memoryBanksToRead[0];
            TagValidation(GT);
        }
        protected override void OnStop()
        {
            base.OnStop();
            try
            {
                if (Reader.IsConnected)
                    DisableScanners();
            }
            catch { }
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
                readers ??= new Readers(Application.Context, ENUM_TRANSPORT.ServiceSerial);
                GetAvailableReaders();
            }

            //bool Enable = utilerias.IsAutoTimeEnabled();
            //if (!Enable)
            //{
            //    Toast.MakeText(this, "Se desactivo la hr y fecha", ToastLength.Long).Show();
            //    if (Reader.IsConnected)
            //        DisableScanners();
            //}
            //else
            //{
            //    readers ??= new Readers(Android.App.Application.Context, ENUM_TRANSPORT.ServiceSerial);
            //    GetAvailableReaders();
            //}
            //btt_Subir.Enabled = Enable;
            //btt_RFIDOrBarCode.Enabled = Enable;
            //btt_Potencia.Enabled = Enable;
            //btt_Memorias.Enabled = Enable;
            //btt_LecturaStartStop.Enabled = Enable;
            //btt_Borrar.Enabled = Enable;
        }

        private void Btt_Leer_Click(object sender, EventArgs e)
        {
            StartStopRFID();
        }
        private static void StartStopRFID()
        {
            try
            {
                if (Hardware == Hardware.ZebraMC33)
                {
                    if (Reader != null)
                    {
                        if (Reader.IsConnected)
                        {
                            A.RunOnUiThread(() =>
                            {
                                if (isRfidRunning)
                                {
                                    isRfidRunning = false;
                                    Reader.Actions.Inventory.Stop();
                                }
                                else
                                {
                                    isRfidRunning = true;

                                    if (memoryBanksToRead != null)
                                    {
                                        foreach (MEMORY_BANK bank in memoryBanksToRead)
                                        {
                                            TagAccess ta = new TagAccess();
                                            TagAccess.Sequence sequence = new TagAccess.Sequence(ta, ta);
                                            TagAccess.Sequence.Operation op = new TagAccess.Sequence.Operation(sequence);
                                            op.AccessOperationCode = ACCESS_OPERATION_CODE.AccessOperationRead;
                                            op.ReadAccessParams.MemoryBank = bank ?? throw new ArgumentNullException(nameof(bank));
                                            Reader.Actions.TagAccess.OperationSequence.Add(op);
                                        }
                                        Reader.Actions.TagAccess.OperationSequence.PerformSequence();
                                    }
                                    else
                                        Reader.Actions.Inventory.Perform();
                                }
                                UpdateReadButton(isRfidRunning);
                            });
                        }
                    }
                }
                else
                {
                    ChainwayRFIDScanEvent();
                    UpdateReadButton(CRFIDC.Reading);
                }


            }
            catch { }
        }
        private static void UpdateReadButton(bool reading)
        {
            if (reading)
            {
                btt_LecturaStartStop.Text = "Detener";
                btt_LecturaStartStop.SetBackgroundColor(Color.ParseColor("#a93226"));
                btt_Subir.Visibility = ViewStates.Gone;
                btt_Borrar.Visibility = ViewStates.Gone;
                //btt_RFIDOrBarCode.Visibility = ViewStates.Gone;
            }
            else
            {
                btt_LecturaStartStop.Text = "Leer";
                btt_LecturaStartStop.SetBackgroundColor(Color.ParseColor("#1f618d"));
                btt_Subir.Visibility = ViewStates.Visible;
                btt_Borrar.Visibility = ViewStates.Visible;
                //btt_RFIDOrBarCode.Visibility = ViewStates.Visible;
            }
        }
        private void Btt_Memorias_Click(object sender, EventArgs e)
        {
            try
            {
                LayoutInflater layoutInflater = LayoutInflater.From(this);
                View view = layoutInflater.Inflate(Resource.Layout.InputMemoriesBank, null);
                AlertDialog.Builder builder = new AlertDialog.Builder(this);

                CheckBox chbEPCCount = view.FindViewById<CheckBox>(Resource.Id.checkbox_Count);
                Spinner spmemorias = view.FindViewById<Spinner>(Resource.Id.sp_MemoryBank);
                List<string> Memorias = new List<string>();

                chbEPCCount.Checked = configuraciones.CountEPC;
                Memorias.Add("Ninguna");
                Memorias.Add(MEMORY_BANK.MemoryBankEpc.ToString());
                Memorias.Add(MEMORY_BANK.MemoryBankReserved.ToString());
                Memorias.Add(MEMORY_BANK.MemoryBankTid.ToString());
                Memorias.Add(MEMORY_BANK.MemoryBankUser.ToString());

                ArrayAdapter MemoriasAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, Memorias);
                spmemorias.Adapter = MemoriasAdapter;

                if (memoryBanksToRead != null)
                {
                    string memsel = memoryBanksToRead[0].ToString();
                    spmemorias.SetSelection(Memorias.IndexOf(memsel));
                }

                spmemorias.ItemSelected += delegate
                {
                    string sel = spmemorias.SelectedItem.ToString();
                    chbEPCCount.Visibility = (sel != "MEMORY_BANK_TID") ? ViewStates.Gone : ViewStates.Visible;
                };

                builder.SetView(view);
                builder.SetCancelable(true);
                builder.SetPositiveButton("Aceptar", delegate
                {
                    ////Asignación de acceso a la memoria
                    memoryBanksToRead = new List<MEMORY_BANK>();
                    if (spmemorias.SelectedItem.ToString() == MEMORY_BANK.MemoryBankEpc.ToString())
                        memoryBanksToRead.Add(MEMORY_BANK.MemoryBankEpc);
                    else if (spmemorias.SelectedItem.ToString() == MEMORY_BANK.MemoryBankReserved.ToString())
                        memoryBanksToRead.Add(MEMORY_BANK.MemoryBankReserved);
                    else if (spmemorias.SelectedItem.ToString() == MEMORY_BANK.MemoryBankTid.ToString())
                        memoryBanksToRead.Add(MEMORY_BANK.MemoryBankTid);
                    else if (spmemorias.SelectedItem.ToString() == MEMORY_BANK.MemoryBankUser.ToString())
                        memoryBanksToRead.Add(MEMORY_BANK.MemoryBankUser);
                    else
                        memoryBanksToRead = null;
                    if (memoryBanksToRead != null)
                    {
                        string selmem = memoryBanksToRead[0].ToString().Replace("MEMORY_BANK_", "");
                        SpannableStringBuilder sp = new SpannableStringBuilder(string.Format("MEMORIA ({0})", selmem));
                        sp.SetSpan(new RelativeSizeSpan(0.5f), 8, ((8 + selmem.Length) + 2), 0);
                        btt_Memorias.SetText(sp, Button.BufferType.Spannable);
                    }
                    else
                        btt_Memorias.Text = "MEMORIA";

                    configuraciones.CountEPC = chbEPCCount.Checked;
                    configuraciones.Save();
                });
                builder.SetNegativeButton("Cancelar", delegate
                {
                    builder.Dispose();
                });
                builder.Show();
            }
            catch (System.Exception ex)
            {
                utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, this);
            }
        }

        private void Btt_RFIDOrBarCodeClick(object sender, EventArgs e)
        {
            try
            {
                Color color;
                if (!Scanner)
                {
                    InitScanner();
                    EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);
                    if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
                    {
                        textViewStatus.Text = "Status: EMDKManager object creation failed ...";
                        color = Color.ParseColor("#96b80e");
                        Scanner = false;
                    }
                    else
                    {
                        textViewStatus.Text = "Status: EMDKManager object creation succeeded ...";
                        color = Color.ParseColor("#96b80e");
                        Scanner = true;
                        TriggerMode = ENUM_TRIGGER_MODE.BarcodeMode;

                        if (Reader != null)
                        {
                            Reader.Events.RemoveEventsListener(eventHandler);
                            Reader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.RfidMode, false);
                        }

                        btt_RFIDOrBarCode.Text = "RFID";
                    }
                }
                else
                {
                    DeinitScanner();
                    if (barcodeManager == null && scanner == null)
                        textViewStatus.Text = "Scanner deshabilitado";
                    btt_RFIDOrBarCode.Text = "Código de Barras";
                    color = Color.ParseColor("#808080");
                    Scanner = false;
                    TriggerMode = ENUM_TRIGGER_MODE.RfidMode;
                    if (Reader != null)
                    {
                        Reader.Events.AddEventsListener(eventHandler);
                        Reader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.BarcodeMode, false);
                    }

                }
                btt_RFIDOrBarCode.SetBackgroundColor(color);
            }
            catch (System.Exception ex)
            {
                utilerias.ShowMessage(string.Format("Ocurrió un error   {0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, this);
            }
        }

        void EMDKManager.IEMDKListener.OnOpened(EMDKManager emdkManager)
        {
            textViewStatus.Text = "Status: EMDK Opened successfully ...";
            this.emdkManager = emdkManager;
            InitScanner();
        }
        void EMDKManager.IEMDKListener.OnClosed()
        {
            textViewStatus.Text = "Status: EMDK Open failed unexpectedly. ";
            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;
            }
        }
        /// <summary>
        /// Destruye el objeto barcode
        /// </summary>
        void DeinitScanner()
        {
            try
            {
                if (emdkManager != null)
                {
                    if (scanner != null)
                    {
                        try
                        {
                            scanner.Data -= scanner_Data;
                            scanner.Status -= scanner_Status;
                            scanner.Disable();
                        }
                        catch (ScannerException e)
                        {
                        }
                    }

                    if (barcodeManager != null)
                    {
                        emdkManager.Release(EMDKManager.FEATURE_TYPE.Barcode);
                        emdkManager = null; //Agregamos esta linea de código, para validar si ELIMINANDO EL OBJETO, este se puede volver a instancia.
                    }
                    barcodeManager = null;
                    scanner = null;
                }
            }
            catch (System.Exception ex)
            {
            }
        }
        void InitScanner()
        {
            try
            {
                if (emdkManager != null)
                {
                    if (barcodeManager == null)
                    {
                        try
                        {
                            //Get the feature object such as BarcodeManager object for accessing the feature.
                            barcodeManager = (BarcodeManager)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Barcode);
                            scanner = barcodeManager.GetDevice(BarcodeManager.DeviceIdentifier.Default);
                            if (scanner != null)
                            {
                                //Attach the Data Event handler to get the data callbacks.
                                scanner.Data += scanner_Data;
                                //Attach Scanner Status Event to get the status callbacks.
                                scanner.Status += scanner_Status;
                                if (!scanner.IsEnabled)
                                    scanner.Enable();
                                //EMDK: Configure the scanner settings
                                ScannerConfig config = scanner.GetConfig();
                                config.SkipOnUnsupported = ScannerConfig.SkipOnUnSupported.None;
                                config.ScanParams.DecodeLEDFeedback = true;
                                config.ReaderParams.ReaderSpecific.ImagerSpecific.PickList = ScannerConfig.PickList.Enabled;

                                config.DecoderParams.Code39.Enabled = true;
                                config.DecoderParams.Code128.Enabled = true;

                                scanner.SetConfig(config);
                            }
                            else
                            {
                                displayStatus("Failed to enable scanner.\n");
                            }
                        }
                        catch (ScannerException e)
                        {
                            textViewStatus.Text = e.Result.Description;
                        }
                        catch (System.Exception ex)
                        {
                            textViewStatus.Text = ex.Message;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }
        /// <summary>
        /// Método que devuelve el valor de datos del scanner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void scanner_Data(object sender, Symbol.XamarinEMDK.Barcode.Scanner.DataEventArgs e)
        {
            try
            {
                ScanDataCollection scanDataCollection = e.P0;
                if ((scanDataCollection != null) && (scanDataCollection.Result == ScannerResults.Success))
                {
                    IList<ScanDataCollection.ScanData> scanData = scanDataCollection.GetScanData();
                    foreach (ScanDataCollection.ScanData data in scanData)
                    {
                        displaydata(data.Data, data.LabelType);
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }
        /// <summary>
        /// Método que devuelve el valor del status del scanner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void scanner_Status(object sender, Symbol.XamarinEMDK.Barcode.Scanner.StatusEventArgs e)
        {
            try
            {
                string statusStr = "";
                //EMDK: The status will be returned on multiple cases. Check the state and take the action.
                StatusData.ScannerStates state = e.P0.State;
                if (state == StatusData.ScannerStates.Idle)
                {
                    statusStr = "Scanner is idle and ready to submit read.";
                    try
                    {
                        if (scanner.IsEnabled & !scanner.IsReadPending)
                        {
                            scanner.Read();
                        }
                    }
                    catch (ScannerException e1)
                    {
                        statusStr = e1.Message;
                    }
                }
                if (state == StatusData.ScannerStates.Waiting)
                {
                    statusStr = "Waiting for Trigger Press to scan";
                }
                if (state == StatusData.ScannerStates.Scanning)
                {
                    statusStr = "Scanning in progress...";
                }
                if (state == StatusData.ScannerStates.Disabled)
                {
                    statusStr = "Scanner disabled";
                }
                if (state == StatusData.ScannerStates.Error)
                {
                    statusStr = "Error occurred during scanning";
                }
                displayStatus(statusStr);
                //mUIContext.Post(_ =>
                //{
                //    UpdateInfoUI(statusStr, "status_scanner");
                //}
                //     , null);
            }
            catch (System.Exception ex)
            {
            }
        }
        void displaydata(string data, ScanDataCollection.LabelType labelType)
        {
            if (Looper.MainLooper.Thread == Java.Lang.Thread.CurrentThread())
                UpdateInfoUI(data, "data_scanner", labelType);
            else
                RunOnUiThread(() => UpdateInfoUI(data, "data_scanner", labelType));
        }
        /// <summary>
        /// Hilo que actualiza los objetos de la interfaz de usuario a partir de su contexto (Datos Scanner o Status Scanner)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contexto"></param>
        private void UpdateInfoUI(string data, string contexto, ScanDataCollection.LabelType labelType)
        {
            //ScannerLectura
            try
            {
                switch (contexto)
                {
                    case "data_scanner":
                        ItemRecuento item;
                        //item = new Item_Inventory(Código, LoteSerie, Cantidad, myTags[index].TagID, HexToString);
                        item = new ItemRecuento();
                        item.HexValue = labelType.ToString();
                        item.StringValue = data;
                        item.ReadTime = DateTime.Now;
                        item.lType = labelType;
                        item.ltypeName = "BarCode";
                        item.MemoryBankString = "";

                        A.RunOnUiThread(() =>
                        {
                            baseAdapterInventoryListView.AddRecuentoItem(item);
                            baseAdapterInventoryListView.NotifyDataSetChanged();
                            btt_Subir.Enabled = baseAdapterInventoryListView.RecuentoList.Count > 0;
                            btt_Borrar.Enabled = baseAdapterInventoryListView.RecuentoList.Count > 0; ;
                            textViewtitle.Text = string.Format("Tags {0}", baseAdapterInventoryListView.RecuentoList.Count);
                            EnableChange(btt_Subir);
                            EnableChange(btt_Borrar);
                        });
                        break;
                    case "status_scanner":
                        textViewStatus.Text = data;
                        break;
                }
            }
            catch (System.Exception ex)
            { }
        }
        void displayStatus(string status)
        {
            try
            {
                if (Looper.MainLooper.Thread == Java.Lang.Thread.CurrentThread())
                    textViewStatus.Text = status;
                else
                    RunOnUiThread(() => textViewStatus.Text = status);
            }
            catch (System.Exception ex)
            {
            }
        }

        private void Button_RFIDOrBarCode_Click(object sender, EventArgs e)
        {
            Color color;
            if (!Scanner)
            {
                InitScanner();
                EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);
                if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
                {
                    textViewStatus.Text = "Status: EMDKManager object creation failed ...";
                    color = Color.ParseColor("#96b80e");
                    Scanner = false;
                }
                else
                {
                    textViewStatus.Text = "Status: EMDKManager object creation succeeded ...";
                    color = Color.ParseColor("#96b80e");
                    Scanner = true;
                    TriggerMode = ENUM_TRIGGER_MODE.BarcodeMode;
                    Reader?.Config.SetTriggerMode(ENUM_TRIGGER_MODE.RfidMode, false);
                }
            }
            else
            {
                DeinitScanner();
                if (barcodeManager == null && scanner == null)
                    textViewStatus.Text = "Scanner deshabilitado";

                color = Color.ParseColor("#808080");
                Scanner = false;
                TriggerMode = ENUM_TRIGGER_MODE.RfidMode;
                Reader?.Config.SetTriggerMode(ENUM_TRIGGER_MODE.BarcodeMode, false);
            }
            btt_RFIDOrBarCode.SetBackgroundColor(color);
        }
        public void DeshabilitaBarCodeScanner()
        {
            //Muy importante este TASK, se utiliza ya que como no pudimos mandar a invocar la API de data wedge se uso la de EMDK, al disparar el trigger, existe problema entre el barcode y el RFID
            //lo mandamos a apagar cuando se lee una remisión correctamente, pero este al venir de un evento de lectura, manda el error ya que detecta que aun esta leyendo, este task permite el termino de la lectura y da tiempo de terminar el evento para poder quitar el Barcode Scanner 27/04/2023
            Task.Factory.StartNew(() =>
            {
                if (Scanner)
                {
                    System.Threading.Thread.Sleep(1000);
                    DeinitScanner();
                    if (barcodeManager == null && scanner == null)
                        textViewStatus.Text = "Scanner deshabilitado";
                    Scanner = false;
                    TriggerMode = ENUM_TRIGGER_MODE.RfidMode;
                    btt_RFIDOrBarCode.SetBackgroundColor(Color.ParseColor("#808080"));
                }
            });
        }
        //End BarcodeVariables
        protected override void OnPause()
        {
            base.OnPause();
        }
        private void TextViewtitle_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    Intent intent = new Intent(BaseContext, typeof(HelpWindow));
            //    StartActivity(intent);
            //}
            //catch (Exception ex)
            //{
            //    ShowMessage(string.Format("Ocurrió un error inesperado\n.{0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, this);
            //}
        }

        private void InventoryListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            try
            {
                baseAdapterInventoryListView.setSelection(e.Position);
                baseAdapterInventoryListView.NotifyDataSetChanged();

                //baseAdapterInventoryListView.inventoryList[0].Id = 1;

                //int basecount = 0;
                //int taglistcount = 0;

                //basecount = baseAdapterInventoryListView.RecuentoList.Count;
                //taglistcount = TagList.Count;

                //ItemRecuento itemRecuento = baseAdapterInventoryListView.RecuentoList[e.Position];
                //TagData TD = TagList[e.Position];

            }
            catch (System.Exception ex)
            {
                utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, this);
            }
        }
        private void InventoryListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            ItemRecuento itm = null;
            if (baseAdapterInventoryListView.RecuentoList != null)
                itm = baseAdapterInventoryListView.RecuentoList[e.Position];

            if (itm != null)
            {
                if (itm.Qty > 1)
                {
                    AlertDialog.Builder builder = new AlertDialog.Builder(this);
                    builder.SetTitle("Operación");
                    builder.SetMessage("¿Borrar o ver TAG's?");
                    builder.SetPositiveButton("Ver Tags", delegate
                    {
                        ShowTagInfoList(itm);
                    });
                    builder.SetNeutralButton("Borrar", delegate
                    {
                        BorrarLinea(e.Position);
                    });
                    builder.SetNegativeButton("Cancelar", delegate
                    {

                    });
                    builder.Show();
                }
                else
                    BorrarLinea(e.Position);
            }
            else
                BorrarLinea(e.Position);
        }
        private void OpenWriteTag(string EPC)
        {
            try
            {

                if (Hardware == Hardware.ChainwayC72)
                {
                    //Añadido 13-08-2025 FUNCIONA solo tenemos que analizar como poner para que el tag que grabe, lo elimine y lo coloque y los cálculos para que la función StringToHex de utilerias, le rellene el texto con los multiplos de 0
                    LayoutInflater layoutInflater = LayoutInflater.From(this);
                    View view = layoutInflater.Inflate(Resource.Layout.InputWriteTag, null);
                    AlertDialog.Builder adregrabado = new AlertDialog.Builder(this);

                    Spinner SpMemoeryWrite = view.FindViewById<Spinner>(Resource.Id.sp_MemoryBankToWrite);
                    EditText ett_MemBankValue = view.FindViewById<EditText>(Resource.Id.ett_MemoryBankValue);
                    EditText ett_Password = view.FindViewById<EditText>(Resource.Id.ett_Contrasena);
                    TextView txtviewStringToHex = view.FindViewById<TextView>(Resource.Id.txtviewStringToHex);
                    TextView txtviewEPCObj = view.FindViewById<TextView>(Resource.Id.txtviewEPCObjectValue);
                    txtviewEPCObj.Text = EPC;
                    adregrabado.SetView(view);

                    List<string> MemoryBanks = new List<string> { "EPC", "USER" };
                    ArrayAdapter adapt = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, MemoryBanks.ToList());
                    SpMemoeryWrite.Adapter = adapt;
                    Utilerias.AplicarAutoEscala(ett_MemBankValue);
                    ett_MemBankValue.TextChanged += delegate
                    {
                        if (ett_MemBankValue.Text == string.Empty)
                        {
                            txtviewStringToHex.Text = "00000000";
                            return;
                        }
                        int max = (int)Math.Ceiling((ett_MemBankValue.Text.Length * 2) / 4.00);
                        txtviewStringToHex.Text = utilerias.StringToHex(ett_MemBankValue.Text, max);
                    };
                    adregrabado.SetPositiveButton("Ok", delegate
                    {
                        string HexEPC = txtviewStringToHex.Text;
                        string Password = "00000000";
                        if (ett_Password.Text != string.Empty) Password = utilerias.StringToHex(ett_Password.Text, 2);
                        string WriteEPCresult = "";

                        if (Hardware == Hardware.ChainwayC72)
                            WriteEPCresult = CRFIDC.WriteTag(Password, HexEPC);
                        //else
                        //    WriteEPCresult = ElfdayRFIDController.WriteEpc(Password, HexEPC);

                        if (WriteEPCresult == "Tag grabado")
                            Toast.MakeText(this, WriteEPCresult, ToastLength.Long).Show();
                    });
                    adregrabado.SetNegativeButton("Cancelar", delegate
                    {
                        Toast.MakeText(this, "Grabado cancelado", ToastLength.Short).Show();
                    });
                    adregrabado.Show();
                }
            }
            catch (Exception ex)
            {
                utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), MethodBase.GetCurrentMethod().Name, A);
            }
        }
        /// <summary>
        /// Muestra la información del tag
        /// </summary>
        /// <param name="DLI">Linea del documento</param>
        private void ShowTagInfoList(ItemRecuento itm)
        {
            try
            {
                LayoutInflater layoutInflater = LayoutInflater.From(A);
                View view = layoutInflater.Inflate(Resource.Layout.InputTagInfo, null);
                ListView LVSeriesOrBatchesInfo = view.FindViewById<ListView>(Resource.Id.LVDetallesSeries);
                Button bttEliminar = view.FindViewById<Button>(Resource.Id.btt_DeleteTag);

                bttEliminar.Enabled = false;
                bttEliminar.Visibility = ViewStates.Gone;
                //bttEliminar.SetBackgroundResource(Resource.Drawable.bttstrokeWindowsDisable);

                //TextView txtviewHeaderSerialOrBatch = view.FindViewById<TextView>(Resource.Id.txtViewTitleSerialOrBatch);
                ////Cambio en el titulo de la ventana
                //if (DLI.ItemCodeInfo.ManBatchNum == BoYesNoEnum.tYES)
                //    txtviewHeaderSerialOrBatch.Text = "Lotes";

                HorizontalScrollView SeriesHeaderHorizontalScrollView = view.FindViewById<HorizontalScrollView>(Resource.Id.HorizontalScrollHeaderSeries);
                HorizontalScrollView SeriesLinesHorizontalScrollView = view.FindViewById<HorizontalScrollView>(Resource.Id.HorizontalScrollLVSeries);

                SeriesHeaderHorizontalScrollView.ScrollBarSize = 0;
                BaseAdapter_TagInfoListView adaptadorTagInfo = new BaseAdapter_TagInfoListView(A);

                SeriesHeaderHorizontalScrollView.ScrollChange += delegate (object sender, View.ScrollChangeEventArgs e)
                {
                    SeriesLinesHorizontalScrollView.ScrollX = SeriesHeaderHorizontalScrollView.ScrollX;
                };
                SeriesLinesHorizontalScrollView.ScrollChange += delegate (object sender, View.ScrollChangeEventArgs e)
                {
                    SeriesHeaderHorizontalScrollView.ScrollX = SeriesLinesHorizontalScrollView.ScrollX;
                };

                adaptadorTagInfo.TagInfoList = itm.TagInfoList;

                LVSeriesOrBatchesInfo.Adapter = adaptadorTagInfo;

                LVSeriesOrBatchesInfo.ItemClick += delegate (object send, AdapterView.ItemClickEventArgs a)
                {
                    int sel;
                    adaptadorTagInfo.setSelection(a.Position);
                    sel = adaptadorTagInfo.selected;
                    bttEliminar.Enabled = sel >= 0;

                    //if (bttEliminar.Enabled)
                    //    bttEliminar.SetBackgroundResource(Resource.Drawable.btt_strokeNeg);
                    //else
                    //    bttEliminar.SetBackgroundResource(Resource.Drawable.bttstrokeWindowsDisable);

                    adaptadorTagInfo.NotifyDataSetChanged();
                };
                bttEliminar.Click += delegate (object sender, EventArgs e)
                {
                    //AlertDialog.Builder TBorrarTag = new AlertDialog.Builder(A);
                    //TBorrarTag.SetTitle("Borrar");
                    ////TBorrarTag.SetIcon(Resource.Drawable.IcoQuestion);
                    //TBorrarTag.SetMessage("Se borrara el tag seleccionado.\n¿Deseas continuar?");
                    //TBorrarTag.SetPositiveButton("Si", delegate
                    //{
                    //    RFIDTag TI = adaptadorTagInfo.TagInfoList[adaptadorTagInfo.selected];
                    //    if (TI != null)
                    //    {
                    //        if (DLI.TagError)
                    //            DLI.ReadQty--;
                    //        else
                    //            DLI.ReadQty -= TI.Qty;
                    //        DLI.TagsInfoLineList.Remove(TI);
                    //        int i = 0;
                    //        DLI.TagsInfoLineList.ForEach(ti => ti.Id = ++i);
                    //        if (DLI.ReadQty == 0)
                    //            DocInfo.Lines.Remove(DLI);
                    //    }
                    //    adaptadorTagInfo.NotifyDataSetChanged();
                    //    baseAdapterInventoryListView.setSelection(-1);
                    //    baseAdapterInventoryListView.NotifyDataSetChanged();
                    //});
                    //TBorrarTag.SetNegativeButton("No", delegate
                    //{
                    //    TBorrarTag.Dispose();
                    //});
                    //TBorrarTag.Show();
                };

                AlertDialog.Builder AlertTIDBuilder = new AlertDialog.Builder(A);

                adaptadorTagInfo.TagInfoList = itm.TagInfoList;

                adaptadorTagInfo.NotifyDataSetChanged();

                AlertTIDBuilder.SetTitle(string.Format("{0} linea: {1}", "Tags", itm.Id + 1));
                AlertTIDBuilder.SetView(view);
                AlertTIDBuilder.SetPositiveButton("Ok", delegate
                {
                    AlertTIDBuilder.Dispose();
                    AlertTIDBuilder = null;
                });
                AlertTIDBuilder.Show();
            }
            catch (Exception ex)
            {
                //utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, A, true);
            }
        }
        /// <summary>
        /// Procedimiento que borra una linea dependiendo de la linea seleccionada.
        /// </summary>
        /// <param name="posicion">Linea seleccionada</param>
        private void BorrarLinea(int posicion)
        {
            AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
            alertDialog.SetTitle("Linea " + (posicion + 1).ToString());
            string itmCode = baseAdapterInventoryListView.RecuentoList[posicion].HexValue;
            string op = "Si";
            if (Hardware == Hardware.ChainwayC72)
            {
                alertDialog.SetMessage(string.Format("¿Operación con el tag '{0}'?", itmCode));
                op = "Borrar";
            }
            else
                alertDialog.SetMessage(string.Format("¿Deseas borrar el tag '{0}'?", itmCode));

            alertDialog.SetPositiveButton(op, delegate
            {
                try
                {
                    baseAdapterInventoryListView.DeleteRecuentoItemByIndex(posicion);
                    int i = 1;
                    foreach (ItemRecuento itm in baseAdapterInventoryListView.RecuentoList)
                    {
                        itm.Id = i;
                        i += 1;
                    }
                    UpdateUI();
                }
                catch (Exception ex)
                {
                    utilerias.ShowMessage(ex.Message, "ListView Item Click", this);
                }
            });
            if (Hardware == Hardware.ChainwayC72)
            {
                alertDialog.SetNeutralButton("Re-Grabar", delegate
                {
                    OpenWriteTag(itmCode);
                });
            }
            alertDialog.SetNegativeButton("Cancelar", delegate
            {
                alertDialog.Dispose();
            });
            alertDialog.Show();
        }
        private static void UpdateUI()
        {
            baseAdapterInventoryListView.NotifyDataSetChanged();
            btt_Subir.Enabled = baseAdapterInventoryListView.RecuentoList.Count > 0;
            btt_Borrar.Enabled = baseAdapterInventoryListView.RecuentoList.Count > 0;
            int totc = 0;
            baseAdapterInventoryListView.RecuentoList.ForEach(t => totc += t.TagInfoList.Count);
            if (IType == InventoryType.Blind)
                textViewtitle.Text = string.Format("Tags {0} Totales: {1}", baseAdapterInventoryListView.RecuentoList.Count, totc);
            else
            {
                textViewtitle.Text = string.Format("Tags {0} Totales: {1}/{2}", baseAdapterInventoryListView.RecuentoList.Count, totc, baseAdapterInventoryListView.RecuentoList.Sum(t => t.Qty));
            }

            EnableChange(btt_Subir);
            EnableChange(btt_Borrar);
        }
        private void Btt_Potencia_Click(object sender, EventArgs e)
        {
            try
            {
                Intent intent = new Intent(BaseContext, typeof(AntenaConfig));
                intent.PutExtra("ActualPower", AntPwr);
                intent.PutExtra("HardWare", 0);
                StartActivityForResult(intent, 0);
            }
            catch (System.Exception ex)
            {
                utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, this);
            }
        }
        private void SetAntenaPower(short Power)
        {
            try
            {
                if (Reader != null)
                {
                    Antennas.Config antenna = Reader.Config.Antennas.GetAntennaConfig(1);
                    antenna.TransmitPowerIndex = Power;
                    Reader.Config.Antennas.SetAntennaConfig(1, antenna);
                }
            }
            catch (System.Exception ex)
            {
                _ = ex.Message;
            }
        }
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            try
            {
                AntPwr = data.GetShortExtra("PowerResult", 0);
                if (Hardware == Hardware.ZebraMC33)
                    SetAntenaPower(AntPwr);
                else
                    CRFIDC.SetPower(AntPwr);

                txtViewAntenaPwr.Text = string.Format("Potencia: {0} dbM", AntPwr.ToString());
                //configuraciones.ZebraAntenaPower = data.GetShortExtra("PowerResult", 0);
                //configuraciones.Save();
                base.OnActivityResult(requestCode, resultCode, data);
            }
            catch (Exception ex)
            {
                utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, this);
            }
        }
        private static void EnableChange(Button btt)
        {
            if (btt.Enabled)
                btt.SetTextColor(Color.White);
            else
                btt.SetTextColor(Color.Gray);
        }
        private void Btt_Subir_Click(object sender, EventArgs e)
        {
            try
            {
                Intent intent = new Intent(BaseContext, typeof(MenuGuardar));
                //Si existen códigos de barras, se debe invalidar el objeto TIPO, ya que no se puede construir por JSON
                foreach (ItemRecuento IR in baseAdapterInventoryListView.RecuentoList)
                    if (IR.lType != null)
                        IR.lType = null;
                string JsonItms = JsonSerializer.Serialize<List<ItemRecuento>>(baseAdapterInventoryListView.RecuentoList);
                //La app se cerraba cuando se leian 200+- (Guillermo y Alexis encontraron este error en una visita con un prospecto) se investiga con ChatGPT y se encuentra lo siguente
                //20/08/2024
                //           Lo que describes es típico de Android / Xamarin cuando se intentan pasar objetos grandes como string en un Intent.
                //👉 Sí, existe un límite de tamaño para los extras de un Intent.
                //En Android, el límite está alrededor de 1 MB por transacción Binder(y no es fijo, depende de versión/ fabricante).
                //Si tu JSON es muy grande(ej: 200 + registros con listas y propiedades largas), estás superando ese límite.
                //El resultado es exactamente lo que mencionas: la Activity no abre, no lanza excepción en tu código, porque el fallo ocurre en el sistema Android al serializar/ deserializar el Intent, no en tu try/catch.
                //En Zebra encontramos que esto ocurre alrededor de los 200kb no 1 MB
                int sizeInBytes = System.Text.Encoding.UTF8.GetByteCount(JsonItms);
                // Para mostrar en KB
                double sizeInKB = sizeInBytes / 1024.0;
                intent.PutExtra("Size", sizeInKB);
                if (sizeInKB >= 200) // más de 500 KB
                {
                    // Aquí en vez de ponerlo en el intent,
                    // guardas en archivo o GlobalData
                    string filePath = System.IO.Path.Combine(CacheDir.AbsolutePath, "TagInventoryTags.json");
                    File.WriteAllText(filePath, JsonItms);
                    intent.PutExtra("TagsFile", filePath);
                }
                else
                    intent.PutExtra("Tags", JsonItms);
                StartActivity(intent);

                //AlertDialog.Builder Subir = new AlertDialog.Builder(this);
                //Subir.SetTitle("Carga");
                //Subir.SetMessage("La información sera cargada en");
                //Subir.SetPositiveButton("BD", delegate
                //{
                //    SubeABD();
                //});
                //Subir.SetNegativeButton("Cancelar", delegate
                //{
                //    Subir.Dispose();
                //});
                //Subir.SetNeutralButton("Archivo", delegate
                //{
                //    Archivo();
                //});
                //Subir.Show();
            }
            catch (System.Exception ex)
            {
                utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, this);
            }
        }
        private void Btt_Borrar_Click(object sender, EventArgs e)
        {
            ////Set Antena COnfig
            //Antennas.Config antenna = Reader.Config.Antennas.GetAntennaConfig(1);
            //antenna.TransmitPowerIndex = 240;

            //Reader.Config.Antennas.SetAntennaConfig(1, antenna);

            //foreach (ItemRecuento it in baseAdapterInventoryListView.RecuentoList)
            //    EscribeTag(it.HexValue);

            //antenna.TransmitPowerIndex = configuraciones.ZebraAntenaPower;

            //Reader.Config.Antennas.SetAntennaConfig(1, antenna);

            try
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("Borrar");
                builder.SetMessage("Se borrara la información ingresada.\n¿Deseas continuar?");
                builder.SetPositiveButton("Si", delegate
                {
                    RunOnUiThread(() =>
                    {
                        if (IType == InventoryType.Theoric)
                        {
                            baseAdapterInventoryListView.RecuentoList.RemoveAll(t => !t.InFile);
                            baseAdapterInventoryListView.RecuentoList.ForEach(t => t.TagInfoList.Clear());
                        }
                        else
                            baseAdapterInventoryListView.Clear();


                        baseAdapterInventoryListView.NotifyDataSetChanged();
                        textViewtitle.Text = string.Format("Tags {0}", baseAdapterInventoryListView.RecuentoList.Count);
                        btt_Subir.Enabled = false;
                        EnableChange(btt_Subir);
                        btt_Borrar.Enabled = false;
                        EnableChange(btt_Borrar);
                    });
                    TagList.Clear();
                });
                builder.SetNegativeButton("No", delegate { builder.Dispose(); });
                builder.Show();
            }
            catch (System.Exception ex)
            {
                utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, this);
            }
        }
        private void LinesHorizontalScrollView_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            HeaderHorizontalScrollView.ScrollX = LinesHorizontalScrollView.ScrollX;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            DisableScanners();
        }
        private void DisableScanners()
        {
            try
            {
                if (Reader != null)
                {
                    Reader.Events.RemoveEventsListener(eventHandler);
                    Reader.Disconnect();
                    Reader = null;
                    readers.Dispose();
                    readers = null;
                }
                DeinitScanner();
            }
            catch { }
        }
        private string GetAvailableReaders() //Se convierte a string debido a que encOntramos que puede que no este configurada, entonces devolvemos el error de conexión.
        {
            string res = "";
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    if (readers != null && readers.AvailableRFIDReaderList != null)
                    {
                        availableRFIDReaderList = readers.AvailableRFIDReaderList;
                        if (availableRFIDReaderList.Count > 0)
                        {
                            if (Reader == null)
                            {
                                readerDevice = availableRFIDReaderList[0];
                                Reader = readerDevice.RFIDReader;

                                Reader.Connect();
                                if (Reader.IsConnected)
                                {
                                    ConfigureReader();
                                }
                            }
                        }
                    }

                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                    res = e.VendorMessage;
                }
                catch (OperationFailureException e)
                {
                    e.PrintStackTrace();
                    res = e.VendorMessage;
                }
            });
            return res;
        }
        private void ConfigureReader()
        {
            if (Reader.IsConnected)
            {
                TriggerInfo triggerInfo = new TriggerInfo();

                triggerInfo.StartTrigger.TriggerType = START_TRIGGER_TYPE.StartTriggerTypeImmediate;
                triggerInfo.StopTrigger.TriggerType = STOP_TRIGGER_TYPE.StopTriggerTypeImmediate;
                try
                {
                    eventHandler ??= new EventHandler(Reader);

                    Reader.Events.AddEventsListener(eventHandler);
                    Reader.Events.SetHandheldEvent(true);

                    Reader.Events.SetTagReadEvent(true);
                    Reader.Events.SetAttachTagDataWithReadEvent(false);

                    Reader.Events.SetInventoryStartEvent(true);
                    Reader.Events.SetInventoryStopEvent(true);
                    Reader.Events.SetOperationEndSummaryEvent(true);
                    Reader.Events.SetReaderDisconnectEvent(true);
                    Reader.Events.SetBatteryEvent(true);
                    Reader.Events.SetPowerEvent(true);
                    Reader.Events.SetTemperatureAlarmEvent(true);
                    Reader.Events.SetBufferFullEvent(true);

                    Antennas.Config antenna = Reader.Config.Antennas.GetAntennaConfig(1);
                    antenna.TransmitPowerIndex = AntPwr;

                    Reader.Config.Antennas.SetAntennaConfig(1, antenna);

                    Reader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.RfidMode, true);
                    TriggerMode = ENUM_TRIGGER_MODE.RfidMode;
                    ////Para cambiar a código de barras
                    //Reader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.BarcodeMode, true);

                    Reader.Config.StartTrigger = triggerInfo.StartTrigger;
                    Reader.Config.StopTrigger = triggerInfo.StopTrigger;

                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                }
                catch (OperationFailureException e)
                {
                    e.PrintStackTrace();
                }
            }
        }
        public static void beep()
        {
            try
            {
                Tone.StartTone(Android.Media.Tone.PropBeep);
            }
            catch { }
        }
        //public static void TagValidation(TagData tagData)
        public static void TagValidation(GenericTag tagData)
        {
            try
            {
                string HexToString = utilerias.GetHexToString(tagData.TagID);
                //Se ocupo para filtrar solo los de herstyler, pero en la configuracion tambien se tiene esto
                //if (!HexToString.Contains("EV56")) return;

                if (configuraciones.SrchPattern.Active)
                    if (!utilerias.TagPattern(HexToString, configuraciones.SrchPattern.Pattern))
                        return;
                ItemRecuento item;
                //item = new Item_Inventory(Código, LoteSerie, Cantidad, myTags[index].TagID, HexToString);
                item = new ItemRecuento();
                item.Id = baseAdapterInventoryListView.RecuentoList.Count + 1;
                item.HexValue = tagData.TagID;

                item.StringValue = HexToString;

                item.ReadTime = DateTime.Now;
                item.lType = null; //La colocamos en NULL para poder validarla en el adaptador
                item.ltypeName = "RFID";
                item.MemoryBankString = "";
                item.ReadTime = DateTime.Now;
                item.Qty = 1;

                if (tagData.MemoryBank == MEMORY_BANK.MemoryBankTid && !configuraciones.CountEPC) item.HexValue = tagData.MemoryBankData;
                TagData t;

                if (memoryBanksToRead != null)
                    item.MemoryBankString = memoryBanksToRead[0].ToString().Replace("MEMORY_BANK_", "");
                if (tagData.OpCode == ACCESS_OPERATION_CODE.AccessOperationRead && tagData.OpStatus == ACCESS_OPERATION_STATUS.AccessSuccess)
                    if (tagData.MemoryBankData.Length > 0)
                    {
                        //if (configuraciones.ConvertirMemoryHexAString)
                        //    item.HexValue = utilerias.GetHexToString(tagData.MemoryBankData);
                        //else
                        //    item.HexValue = tagData.MemoryBankData;
                        //if (tagData.MemoryBank == MEMORY_BANK.MemoryBankTid) item.TID = tagData.MemoryBankData;
                        if (tagData.MemoryBank == MEMORY_BANK.MemoryBankTid && !configuraciones.CountEPC) item.HexValue = tagData.MemoryBankData;

                        if (tagData.MemoryBank == MEMORY_BANK.MemoryBankTid && configuraciones.CountEPC)
                        {
                            RFIDTag TagInfo = new RFIDTag();
                            TagInfo.MEMORY_BANK = tagData.TagID;
                            TagInfo.MemmoryBankValue = item.HexValue;
                            TagInfo.MemmoryBankHexToString = item.StringValue;
                            TagInfo.TID = tagData.MemoryBankData;

                            if (configuraciones.TrimTag > 0)
                                TagInfo.TID = TagInfo.TID[..configuraciones.TrimTag];

                            TagInfo.Qty = 1;
                            TagInfo.ReadDate = DateTime.Now;
                            item.TID = tagData.MemoryBankData;

                            if (configuraciones.TrimTag > 0)
                            {
                                item.TID = item.TID[..configuraciones.TrimTag];
                                item.HexValue = item.HexValue[..configuraciones.TrimTag];
                            }
                            //else
                            //{
                            //    item.TID = item.TID;
                            //    item.HexValue = item.HexValue;
                            //}

                            ItemRecuento itm = baseAdapterInventoryListView.RecuentoList.Find(t => t.HexValue == item.HexValue);
                            //Si el EPC ya fue encontrado entonces debe buscarlo en la lista de tags
                            if (itm != null)
                            {
                                TagInfo.Id = itm.TagInfoList.Count + 1;
                                string TIDF = itm.TIDList.Find(t => t == item.TID);
                                if (TIDF == null)
                                {
                                    itm.TIDList.Add(item.TID);
                                    itm.TagInfoList.Add(TagInfo);
                                    if (IType == InventoryType.Blind)
                                        itm.Qty++; //En el inventario teorico esto no debe de sumarse
                                }
                            }
                            else
                            {
                                //item es de la variable de creación
                                if ((IType == InventoryType.Blind) || (IType == InventoryType.Theoric && configuraciones.AgregarNoIdent))
                                {
                                    item.TIDList.Add(item.TID);
                                    item.TagInfoList.Add(TagInfo);
                                    item.Qty = 1;
                                }
                            }
                        }
                    }
                if (item.HexValue != null)
                {
                    A.RunOnUiThread(() =>
                    {
                        //HerStyler, se encontró que estos tags al leer el TID, en ocasiones manda un TID distinto (caso playerytees), para evitar esto se recorta el tag a 24 caracteres 07/02/2025
                        if (configuraciones.TrimTag > 0) item.HexValue = item.HexValue[..configuraciones.TrimTag];
                        if ((IType == InventoryType.Blind) || (IType == InventoryType.Theoric && configuraciones.AgregarNoIdent))
                            baseAdapterInventoryListView.AddRecuentoItem(item);
                        UpdateUI();
                    });
                }
                try
                {
                    if ((baseAdapterInventoryListView.RecuentoList.FindAll(t => t.HexValue == item.HexValue).Count == 0))
                        TagList.Add(tagData);
                }
                catch (Exception ex) { string res = ex.Message; }
            }
            catch (Exception ex)
            {

            }
        }
        public class EventHandler : Java.Lang.Object, IRfidEventsListener
        {
            public EventHandler(RFIDReader Reader)
            {

            }
            public void EventReadNotify(RfidReadEvents e)
            {
                try
                {
                    //TagDataArray tagDataArray= Reader.Actions.GetReadTagsEx(10);
                    TagData[] myTags = Reader.Actions.GetReadTags(1500);
                    myTags = myTags.GroupBy(t => t.TagID).Select(grp => grp.First()).ToArray(); //Filtrado de tags para evitar duplicados en el set de lecturas
                    beep();
                    if (myTags != null)
                        foreach (TagData tagData in myTags)
                            TagValidation(FromZebra(tagData));
                }
                catch (Exception ex)
                {
                    utilerias.ShowMessage(string.Format("Ocurrió un error inesperado.\n{0}", ex.Message), System.Reflection.MethodBase.GetCurrentMethod().Name, A);
                }
            }
            public static GenericTag FromZebra(TagData zebraTag)
            {
                return new GenericTag
                {
                    TagID = zebraTag.TagID,
                    MemoryBankData = zebraTag.MemoryBankData,
                    HexValue = zebraTag.MemoryBankData,
                    MemoryBank = zebraTag.MemoryBank,
                    IsReadSuccess = zebraTag.OpStatus == ACCESS_OPERATION_STATUS.AccessSuccess,
                    OpCode = zebraTag.OpCode,
                    OpStatus = zebraTag.OpStatus,
                    ReadTime = DateTime.Now
                };
            }
            public void EventStatusNotify(RfidStatusEvents rfidStatusEvents)
            {
                try
                {
                    if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.HandheldTriggerEvent)
                    {
                        if (TriggerMode != ENUM_TRIGGER_MODE.RfidMode) return; //Con esto evitamos que se ejecute el evento de disparo de RFID **Actualización: Le agregamos al boton de rfid o barcode que elimine o agregue todo este evento, con esto optimizamos la app 04/08/2023
                        if (rfidStatusEvents.StatusEventData.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerPressed)
                        {
                            ThreadPool.QueueUserWorkItem(o =>
                            {
                                try
                                {
                                    if (memoryBanksToRead != null)
                                    {
                                        foreach (MEMORY_BANK bank in memoryBanksToRead)
                                        {
                                            TagAccess ta = new TagAccess();
                                            TagAccess.Sequence sequence = new TagAccess.Sequence(ta, ta);
                                            TagAccess.Sequence.Operation op = new TagAccess.Sequence.Operation(sequence);
                                            op.AccessOperationCode = ACCESS_OPERATION_CODE.AccessOperationRead;
                                            op.ReadAccessParams.MemoryBank = bank ?? throw new ArgumentNullException(nameof(bank));
                                            Reader.Actions.TagAccess.OperationSequence.Add(op);
                                        }
                                        //Aun desconocemos por que si le indicas que puedes leer el banco de memoria no tiene que ir "Reader.Actions.Inventory.Perform();" DEBE IR LA LINEA DE ABAJO.
                                        Reader.Actions.TagAccess.OperationSequence.PerformSequence();
                                    }
                                    else
                                    {
                                        Reader.Actions.Inventory.Perform();
                                    }
                                }
                                catch (InvalidUsageException e)
                                {
                                    e.PrintStackTrace();
                                }
                                catch (OperationFailureException e)
                                {
                                    e.PrintStackTrace();
                                }
                            });
                        }
                        if (rfidStatusEvents.StatusEventData.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerReleased)
                        {
                            ThreadPool.QueueUserWorkItem(o =>
                            {
                                try
                                {
                                    Reader.Actions.Inventory.Stop();
                                }
                                catch (InvalidUsageException e)
                                {
                                    e.PrintStackTrace();
                                }
                                catch (OperationFailureException e)
                                {
                                    e.PrintStackTrace();
                                }
                            });
                        }
                    }
                }
                catch { }
            }
        }
    }
}