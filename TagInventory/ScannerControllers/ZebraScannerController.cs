using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Symbol.XamarinEMDK.Barcode;
using Symbol.XamarinEMDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TagInventory.ScannerControllers
{
    public class ZebraScannerController : Java.Lang.Object, EMDKManager.IEMDKListener
    {
        private EMDKManager emdkManager = null;
        private BarcodeManager barcodeManager = null;
        private Scanner scanner = null;
        // 🔹 Evento para enviar el código escaneado a otra clase
        public event Action<string> OnScanResult;
        public event Action<string> OnStatusChange;
        public string StatusScannerResult;
        public bool Scanner;
        public void InitializeScanner()
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
                                scanner.Data += Scanner_Data;
                                //Attach Scanner Status Event to get the status callbacks.
                                scanner.Status += scanner_Status;
                                if (!scanner.IsEnabled)
                                    scanner.Enable();
                            }
                        }
                        catch (ScannerException e)
                        {
                            StatusScannerResult = e.Result.Description;
                        }
                        catch (System.Exception ex)
                        {
                            StatusScannerResult = ex.Message;
                        }
                    }
                }
            }
            catch { }
        }
        void scanner_Status(object sender, Scanner.StatusEventArgs e)
        {
            try
            {
                string lStatusScannerResult = "";
                //EMDK: The status will be returned on multiple cases. Check the state and take the action.
                StatusData.ScannerStates state = e.P0.State;
                if (state == StatusData.ScannerStates.Idle)
                {
                    lStatusScannerResult = "Scanner is idle and ready to submit read.";
                    try
                    {
                        if (scanner.IsEnabled & !scanner.IsReadPending)
                            scanner.Read();
                    }
                    catch (ScannerException e1)
                    {
                        lStatusScannerResult = e1.Message;
                    }
                }
                if (state == StatusData.ScannerStates.Waiting)
                    lStatusScannerResult = "Waiting for Trigger Press to scan";
                if (state == StatusData.ScannerStates.Scanning)
                    lStatusScannerResult = "Scanning in progress...";
                if (state == StatusData.ScannerStates.Disabled)
                    lStatusScannerResult = "Scanner disabled";
                if (state == StatusData.ScannerStates.Error)
                    lStatusScannerResult = "Error occurred during scanning";

                OnStatusChange?.Invoke(lStatusScannerResult);
            }
            catch { }
        }
        public EMDKResults LoadZebraScanner()
        {
            try
            {
                EMDKResults results = EMDKManager.GetEMDKManager(Android.App.Application.Context, this);
                if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
                {
                    StatusScannerResult = "Status: EMDKManager object creation failed ...";
                    Scanner = false;
                }
                else
                {
                    StatusScannerResult = "Status: EMDKManager object creation succeeded ...";
                    Scanner = true;
                }
                return results;
            }
            catch (System.Exception ex)
            {
                StatusScannerResult = "Scanner error";
                return null;
            }
        }
        public void Dispose()
        {

        }
        public bool Destroyed()
        {
            return barcodeManager == null && scanner == null;
        }
        /// <summary>
        /// Destruye el objeto barcode
        /// </summary>
        public void DeinitZebraScanner()
        {
            try
            {
                if (emdkManager != null)
                {
                    if (scanner != null)
                    {
                        try
                        {
                            scanner.Data -= Scanner_Data;
                            scanner.Status -= scanner_Status;
                            scanner.Disable();
                        }
                        catch { }
                    }
                    if (barcodeManager != null)
                    {
                        emdkManager.Release(EMDKManager.FEATURE_TYPE.Barcode);
                        emdkManager = null; //Agregamos esta linea de código, para validar si ELIMINANDO EL OBJETO, este se puede volver a instanciar.
                    }
                    barcodeManager = null;
                    scanner = null;
                }
                Scanner = false;
            }
            catch { }
        }

        void EMDKManager.IEMDKListener.OnClosed()
        {
            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;
            }
        }
        void EMDKManager.IEMDKListener.OnOpened(EMDKManager emdkManager)
        {
            try
            {
                this.emdkManager = emdkManager;
                InitializeScanner();
                //ConfigureScanner();
            }
            catch (System.Exception e)
            {
            }
        }
        /// <summary>
        /// Énvia la configuracion de los codigos de barras
        /// </summary>
        public List<BarCodeConfig> GetZebraBarCodeConfigs()
        {
            ///Aun falta por pasar de la ventana de la UI a esta parte
            ScannerConfig config = scanner.GetConfig();
            Type tipo = config.DecoderParams.GetType();
            PropertyInfo[] propiedades = tipo.GetProperties();
            PropertyInfo eprop;
            BarCodeConfig BCC;
            List<BarCodeConfig> BCCList = new List<BarCodeConfig>();
            string pname;
            object subob;
            foreach (PropertyInfo propiedad in propiedades)
            {
                if (propiedad != null && propiedad.CanWrite)  // Verifica que la propiedad sea escribible
                {
                    try
                    {
                        subob = propiedad.GetValue(config.DecoderParams);
                        pname = propiedad.Name;
                        eprop = subob.GetType().GetProperty("Enabled");
                        //eprop.SetValue(subob, true);
                        BCC = new BarCodeConfig();
                        BCC.Id = BCCList.Count + 1;
                        BCC.Name = pname;
                        BCC.Enable = bool.Parse(eprop.GetValue(subob).ToString());
                        BCCList.Add(BCC);
                    }
                    catch { }
                }
            }
            return BCCList;
        }
        /// <summary>
        /// Coloca la configuracion de los codigos de barra para zebra a partir del JSON enviado
        /// </summary>
        /// <param name="JSON">JSON con la informcion</param>
        /// <returns>Vacio si logro colocar la configuacion o un dato con el error</returns>
        public string SetZebraBarCodeConfigs(string JSON)
        {
            try
            {
                ScannerConfig config = scanner.GetConfig();
                Type tipo = config.DecoderParams.GetType();
                PropertyInfo[] propiedades = tipo.GetProperties();
                PropertyInfo eprop;
                BarCodeConfig BCC;
                string pname;
                object subob;

                List<BarCodeConfig> BCCList = JsonConvert.DeserializeObject<List<BarCodeConfig>>(JSON);
                foreach (PropertyInfo propiedad in propiedades)
                {
                    if (propiedad != null && propiedad.CanWrite)  // Verifica que la propiedad sea escribible
                    {
                        try
                        {
                            subob = propiedad.GetValue(config.DecoderParams);
                            pname = propiedad.Name;
                            eprop = subob.GetType().GetProperty("Enabled");
                            BCC = BCCList.Find(b => b.Name == pname);
                            if (BCC != null)
                                eprop.SetValue(subob, BCC.Enable);
                        }
                        catch { }
                    }
                }
                //Seguimos con el problema que no permite asignar la configuración
                if (scanner.IsEnabled)
                    scanner.SetConfig(config);
                return "";
            }
            catch (System.Exception ex) { return ex.Message; }
            finally
            {

            }
        }
        private void Scanner_Data(object sender, Scanner.DataEventArgs e)
        {
            try
            {
                ScanDataCollection scanDataCollection = e.P0;
                if ((scanDataCollection != null) && (scanDataCollection.Result == ScannerResults.Success))
                {
                    IList<ScanDataCollection.ScanData> scanData = scanDataCollection.GetScanData();
                    foreach (ScanDataCollection.ScanData data in scanData)
                        OnScanResult?.Invoke(data.Data);
                    //displaydata(data.Data, data.LabelType);
                }
            }
            catch { }
        }

        public void ConfigureScanner()
        {
            if (scanner != null)
            {
                try
                {
                    ScannerConfig config = scanner.GetConfig();

                    config.ReaderParams.ReaderSpecific.ImagerSpecific.IlluminationMode = ScannerConfig.IlluminationMode.Off;    // Apagar luz
                    config.ReaderParams.ReaderSpecific.ImagerSpecific.PickList = ScannerConfig.PickList.Enabled;                //Habilitar escaneo en pantalla

                    scanner.SetConfig(config);
                    //Console.WriteLine("Configuración inicial aplicada.");
                }
                catch (System.Exception ex)
                {
                    StatusScannerResult = "Error en la configuración inicial: " + ex.Message;
                }
            }
        }
    }
    public class BarCodeConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Enable { get; set; }
    }

}