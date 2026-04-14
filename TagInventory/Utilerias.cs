using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Widget;
using Java.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using TagInventory.Modelos;

namespace TagInventory
{
    public class Utilerias
    {
        private string MxStatsPath = @"/storage/emulated/0/Stats/MxStats.xml";
        /// <summary>
        /// Convierte el valor de hexadecimal a string
        /// </summary>
        /// <param name="Value">Valor en hexadecimal</param>
        /// <returns>String con el contenido</returns>
        public string GetHexToString(string Value)
        {
            try
            {
                //string hexvalue = Value.Replace("null", "");
                string hexvalue = Value;
                byte[] data = Enumerable.Range(0, hexvalue.Length)
                 .Where(x => x % 2 == 0)
                 .Select(x => Convert.ToByte(hexvalue.Substring(x, 2), 16))
                 .ToArray();
                string taginfo = System.Text.Encoding.ASCII.GetString(data);
                taginfo = taginfo.Replace("\0", "");    //Se quitan espacios en blanco de información
                return taginfo;
            }
            catch (System.Exception ex) { return "Error " + ex.Message; }
        }
        public static void AplicarAutoEscala(EditText editText, float tamañoNormal = 16f, float tamañoReducido = 12f, int limite = 15)
        {
            // Verifica si ya tiene el tag del listener
            if (editText.Tag is ITextWatcher anteriorWatcher)
                editText.RemoveTextChangedListener(anteriorWatcher);

            AutoEscalaTextWatcher watcher = new AutoEscalaTextWatcher(editText, tamañoNormal, tamañoReducido, limite);
            editText.AddTextChangedListener(watcher);

            // Guarda el watcher en el Tag para control futuro
            editText.Tag = watcher;
        }
        /// <summary>
        /// Valida que el tag cumpla con el patron definido
        /// </summary>
        /// <param name="Value">Valor a validar/param>
        /// <returns>Verdadero o falso si el patron coincide</returns>
        public bool TagPattern(string Value, string pattern)
        {
            //string pattern = @"^[\x20-\x7E]{2}\d*$"; // cualquier carácter imprimirle ASCII 32 AL 126
            bool IsValid = Regex.IsMatch(Value, pattern);
            return IsValid;
        }
        public enum Hardware
        {
            ZebraMC33 = 0,
            ZebraTC15 = 1,
            PointMobile = 2,
            HoneyWell = 3,
            Urovo = 4,
            Keyence = 5,
            ChainwayC72 = 6,
        }
        public enum InventoryType
        {
            Blind = 0,
            Theoric = 1,
        }
        public string GetStringToHex(string value)
        {
            byte[] bytes = Encoding.Default.GetBytes(value);
            string hexString = BitConverter.ToString(bytes);
            hexString = hexString.Replace("-", "");
            return hexString;
        }
        public string StringToHex(string CodeBars, int Len)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                byte[] inputByte = Encoding.UTF8.GetBytes(CodeBars);
                foreach (byte b in inputByte)
                    sb.Append(string.Format("{0:x2}", b));
                Len *= 4;
                string Complete = sb.ToString().PadRight(Len, '0'); //Len =102 anterioremente
                return Complete;
                //return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Error " + ex.Message;
            }
        }
        public void ShowMessage(string mensaje, string Metodo, Context context)
        {
            try
            {
                AlertDialog.Builder alertDialog = new AlertDialog.Builder(context);
                alertDialog.SetTitle(Metodo);
                alertDialog.SetMessage(mensaje);
                alertDialog.SetNegativeButton("Ok", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
            }
            catch { }
        }
        /// <summary>
        /// Proceso que muestra el cuadro de dialogo de espera
        /// </summary>
        /// <param name="Title">Titulo a mostrar</param>
        /// <param name="message">Mensaje a mostrar</param>
        /// <param name="progress">Variable por referencia para usarla en el contexto</param>
        /// <param name="context">En que contexto sera usado (Activity)</param>
        public void ShowProgressDialog(string Title, string message, ref ProgressDialog progress, Context context)
        {
            try
            {
                progress = new ProgressDialog(context);
                progress.SetTitle(Title);
                progress.SetMessage(message);
                progress.SetProgressStyle(ProgressDialogStyle.Horizontal);
                progress.SetCancelable(false);
                progress.Max = 100;
                progress.Progress = 0;
                progress.Show();
            }
            catch { }
        }
        public string GetSerialNumber(Context context)
        {
            string serial = null;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                // Para Android 8 y superior, requiere permiso READ_PHONE_STATE
                try
                {
                    serial = Build.GetSerial();
                }
                catch
                {
                    serial = "UNKNOWN";
                }
            }
            else
            {
                // Para versiones anteriores a Android 8
                serial = Build.Serial;
            }

            // Si sigue siendo nulo o UNKNOWN, intentamos obtener el ANDROID_ID
            if (string.IsNullOrEmpty(serial) || serial == "UNKNOWN")
            {
                serial = Settings.Secure.GetString(context.ContentResolver, Settings.Secure.AndroidId);
            }

            return serial;
        }
        public async Task<string> CreateXMLDocRecuento(AppConfig configuraciones, List<ItemRecuento> baseAdapterRecuentoListView)
        {
            try
            {

                WSRFID.WSRFIDDemo WSScanmexRFID = new WSRFID.WSRFIDDemo();
                WSScanmexRFID.Url = configuraciones.WSRFDIDemo;

                XmlCreator xmlCreator = new XmlCreator();
                await Task.Delay(10);
                xmlCreator.FREntry = 1;
                xmlCreator.Userid = "1";
                xmlCreator.Serial = configuraciones.GetPointMobileSerialNumber();

                string xmlString = xmlCreator.XmlDocRecuentoSimple(baseAdapterRecuentoListView);
                string oRes = "";

                if (!xmlString.Contains("Error XmlBuild"))
                    oRes = WSScanmexRFID.LoadRecuentoSimple(xmlString);
                else
                    oRes = xmlString;
                return oRes;
            }
            catch (Exception ex)
            {
                return "Error " + ex.Message;
            }
        }
        /// <summary>
        /// Obtiene el numero de serie de Zebra, este se obtiene del archivo que zebra coloca en una carpeta
        /// </summary>
        /// <returns>Numero de serie</returns>
        public string GetZebraSerialNumber()
        {
            try
            {
                XmlDocument XmlDoc = new XmlDocument();
                XmlDoc.Load(MxStatsPath);
                string sn = XmlDoc.SelectSingleNode("MXStats").Attributes["Serial"].Value;
                XmlDoc = null;
                return sn;
            }
            catch (Exception ex)
            {
                return "Error " + ex.Message;
            }
        }
        /// <summary>
        /// Encripta en base64 la informacion ingresada en el parametro
        /// </summary>
        /// <param name="info">Cadena de texto a encriptar</param>
        /// <returns>Cadena encriptada</returns>
        public string Encriptar(string info)
        {
            Byte[] BytesEncriptar = Encoding.UTF8.GetBytes(info);
            return Convert.ToBase64String(BytesEncriptar);
        }
        /// <summary>
        /// Desencripta la información ingresada en el parametro
        /// </summary>
        /// <param name="info">Cadena de texto a desencriptar</param>
        /// <returns>Cadena desencriptada</returns>
        public string Desencriptar(string info)
        {
            Byte[] BytesDesencriptar = Convert.FromBase64String(info);
            return ASCIIEncoding.UTF8.GetString(BytesDesencriptar);
        }
        public async Task<string> GeneraArchivo(List<ItemRecuento> RecuentoList)
        {
            try
            {
                AppConfig config = new AppConfig();
                config.Load();
                string DirPath = @"/storage/emulated/0/Download";

                if (!Directory.Exists(DirPath))
                    Directory.CreateDirectory(DirPath);

                string FPath = string.Format(@"{0}/RecuentoScanmex{1}{2}", DirPath, DateTime.Now.ToString("yyMMddHHmmss"), config.FileExtension);
                StreamWriter escritor = new StreamWriter(FPath);
                string sep = config.CSVSeparador;
                escritor.WriteLine(string.Format("Id{0}Hexadecimal{0}StringValue{0}Cantidad{0}Fecha", sep));
                await Task.Delay(10);
                int i = 0;
                foreach (ItemRecuento itm in RecuentoList)
                    escritor.WriteLine(string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}", sep, ++i, itm.HexValue, itm.StringValue, itm.Qty, itm.ReadTime));
                escritor.Close();

                return FPath;
            }
            catch (UnauthorizedAccessException ua)
            {
                return "Error La aplicación no tiene permiso para generar archivos de texto, favor de habilitarla";
            }
            catch (Exception ex)
            {
                return "Error " + ex.Message;
            }
        }
        /// <summary>
        /// Obtiene el valor de "Usar la hora de la red" si esta habilitado o no
        /// </summary>
        /// <returns></returns>
        public bool IsAutoTimeEnabled()
        {
            ContentResolver contentResolver = Android.App.Application.Context.ContentResolver;
            try
            {
                int autoTime = Settings.Global.GetInt(contentResolver, Settings.Global.AutoTime);
                return autoTime == 1;
            }
            catch (Settings.SettingNotFoundException e)
            {
                // Manejar la excepción si la configuración no se encuentra
                return false;
            }
        }
        public bool PingHost(string nameOrAddress)
        {
            try
            {
                bool pingable = false;
                Ping pinger = null;
                try
                {
                    Regex r;
                    Match m;
                    string uri = "";
                    r = new Regex(@"(?<ip>\d+.\d+.\d+.\d+)", RegexOptions.None, TimeSpan.FromMilliseconds(150));
                    m = r.Match(nameOrAddress);

                    if (m.Success)
                    {
                        uri = m.Result("${ip}");
                        pinger = new Ping();
                        PingReply reply = pinger.Send(uri, 50);
                        pingable = reply.Status == IPStatus.Success;
                        return pingable;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (PingException)
                {
                    return false;
                    // Discard PingExceptions and return false;
                }
                catch (System.Exception ex)
                {
                    return false;
                }
                finally
                {
                    if (pinger != null)
                        pinger.Dispose();
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public void EnableChange(Button btt)
        {
            if (btt.Enabled)
                btt.SetTextColor(Android.Graphics.Color.White);
            else
                btt.SetTextColor(Android.Graphics.Color.Gray);
        }
        public void EnableChange(EditText ett)
        {
            if (ett.Enabled)
                ett.SetTextColor(Android.Graphics.Color.White);
            else
                ett.SetTextColor(Android.Graphics.Color.Gray);
        }
        private class AutoEscalaTextWatcher : Java.Lang.Object, ITextWatcher
        {
            private readonly EditText _editText;
            private readonly float _normal;
            private readonly float _reducido;
            private readonly int _limite;

            public AutoEscalaTextWatcher(EditText editText, float normal, float reducido, int limite)
            {
                _editText = editText;
                _normal = normal;
                _reducido = reducido;
                _limite = limite;
            }

            public void AfterTextChanged(IEditable s)
            {
                //if (_editText.Text.Length > _limite)
                //    _editText.SetTextSize(Android.Util.ComplexUnitType.Sp, _reducido);
                //else
                //    _editText.SetTextSize(Android.Util.ComplexUnitType.Sp, _normal);

                if (_editText.Text.Length > 25)
                    _editText.SetTextSize(Android.Util.ComplexUnitType.Sp, 10);
                else if (_editText.Text.Length > 15)
                    _editText.SetTextSize(Android.Util.ComplexUnitType.Sp, 11);
                else
                    _editText.SetTextSize(Android.Util.ComplexUnitType.Sp, 18);
            }

            public void BeforeTextChanged(Java.Lang.ICharSequence s, int start, int count, int after) { }

            public void OnTextChanged(Java.Lang.ICharSequence s, int start, int before, int count) { }
        }
    }
}