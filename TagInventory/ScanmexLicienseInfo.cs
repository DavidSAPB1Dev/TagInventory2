using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Telephony;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using static Android.Renderscripts.Sampler;

namespace TagInventory
{
    public class ScanmexLicenseInfo
    {
        private string LicenseFile = @"/storage/emulated/0/SMXTIN.smxl";
        private string LicenseFile2 = @"/storage/emulated/0/Zebra/License.smxl";
        private string MxStatsPath = @"/storage/emulated/0/Stats/MxStats.xml";
        public int MaxUssageQty { get; set; }
        public int UsedQty { get; set; }
        public string Key { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime InstllDate { get; set; }
        public string GUID { get; set; }
        private List<string> GUIDList { get; set; }
        public void CreateLicense()
        {
            try
            {
                LoadGuids();
                string Dir = Path.GetDirectoryName(LicenseFile2);
                if (!Directory.Exists(Dir))
                    Directory.CreateDirectory(Dir);

                if (GUIDList != null)
                    if (GUIDList.Contains(GUID))
                        return;
                File.AppendAllText(LicenseFile2, string.Format("{0}\n", GUID));
            }
            catch (Exception ex)
            {

            }
        }
        public string UpdateLicenseFile()
        {
            //string Contraseña = GetPassword(pass);
            //if (Contraseña != GetOnlyNumbers(pass))
            //    return "Contraseña incorrecta";
            try
            {
                Utilerias utilerias = new Utilerias();
                string DeviceId = utilerias.GetZebraSerialNumber();
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;

                StringBuilder XmlBuild = new StringBuilder();
                using (XmlWriter Writer = XmlWriter.Create(XmlBuild, settings))
                {
                    Writer.WriteStartDocument();
                    Writer.WriteStartElement("SMX");
                    Writer.WriteStartElement("LicenseInfo");
                    Writer.WriteAttributeString("Key", Base64Encode(DeviceId));
                    Writer.WriteAttributeString("UsedQty", Base64Encode(UsedQty.ToString()));
                    Writer.WriteAttributeString("MaxQty", Base64Encode(MaxUssageQty.ToString()));
                    Writer.WriteAttributeString("DueDate", Base64Encode(DueDate.Ticks.ToString()));
                    Writer.WriteAttributeString("InstllDate", Base64Encode(InstllDate.Ticks.ToString()));
                    Writer.WriteAttributeString("GUID", Base64Encode(GUID));
                    Writer.WriteEndElement();
                    Writer.WriteEndDocument();
                }
                byte[] Licencia;
                Licencia = Encoding.ASCII.GetBytes(Base64Encode(XmlBuild.ToString()));
                //string bytsToString = Base64StringToBytes(Licencia);
                //File.WriteAllText(LicenseFile, bytsToString);
                File.WriteAllBytes(LicenseFile, Licencia);
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData)
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public static string Base64StringToBytes(byte[] bytes)
        {
            string LicNum = "";
            foreach (byte b in bytes)
                LicNum += b.ToString();
            return LicNum;
        }
        public void ShowLicenseInfo(Context contexto)
        {
            AlertDialog.Builder LicenseInfo = new AlertDialog.Builder(contexto);
            LicenseInfo.SetTitle("Licencia");
            LicenseInfo.SetMessage(string.Format("Cantidad licencia: {0}\nCantidad usada: {1}\nFecha: {2}\nGUID: {3}", this.MaxUssageQty, this.UsedQty, this.DueDate.ToString("dd-MM-yyyy"), this.GUID));
            LicenseInfo.SetPositiveButton("Ok", delegate
            {

            });
            LicenseInfo.Show();
        }
        public static string BytesToBase64String(string texto)
        {
            List<string> lista = Enumerable.Range(0, texto.Length / 2).Select(i => texto.Substring(2 * i, 2)).ToList();
            List<byte> bytList = new List<byte>();
            string bytestobase64string = "";
            byte b;
            foreach (string s in lista)
            {
                b = Convert.ToByte(s);
                bytList.Add(b);
            }
            bytestobase64string = Encoding.ASCII.GetString(bytList.ToArray());
            return bytestobase64string;
        }

        public string Load()
        {
            try
            {
                XmlDocument XmlDoc = new XmlDocument();

                string texto = File.ReadAllText(LicenseFile);

                texto = Base64Decode(texto);
                XmlDoc.LoadXml(texto);

                MaxUssageQty = int.Parse(Base64Decode(XmlDoc.SelectSingleNode("/SMX/LicenseInfo").Attributes["MaxQty"].Value));
                UsedQty = int.Parse(Base64Decode(XmlDoc.SelectSingleNode("/SMX/LicenseInfo").Attributes["UsedQty"].Value));
                GUID = Base64Decode(XmlDoc.SelectSingleNode("/SMX/LicenseInfo").Attributes["GUID"].Value);
                DueDate = new DateTime(long.Parse(Base64Decode(XmlDoc.SelectSingleNode("/SMX/LicenseInfo").Attributes["DueDate"].Value)));
                InstllDate = new DateTime(long.Parse(Base64Decode(XmlDoc.SelectSingleNode("/SMX/LicenseInfo").Attributes["InstllDate"].Value)));

                XmlDoc = null;
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        /// <summary>
        /// Obtiene la información de la licencia
        /// </summary>
        /// <returns>Devuelve el error en string, si no devuelve nada quiere decir que se cargo todo correctamente</returns>
        public string GetLicenseInfo()
        {
            try
            {
                if (!File.Exists(LicenseFile))
                    return "No hay licencia en el dispositivo.";

                XmlDocument XmlDoc = new XmlDocument();
                try
                {
                    string texto = File.ReadAllText(LicenseFile);
                    //texto = BytesToBase64String(texto);
                    texto = Base64Decode(texto);
                    XmlDoc.LoadXml(texto);
                }
                catch (Exception ex)
                {
                    return string.Format("Se encontró un error con el archivo de licencias:\n{0}", ex.Message);
                }
                this.Key = Base64Decode(XmlDoc.SelectSingleNode("/SMX/LicenseInfo").Attributes["Key"].Value);
                Utilerias utilerias = new Utilerias();
                //El archivo de smx stats en android 14 no se encuentra
                string DeviceId = utilerias.GetZebraSerialNumber();

                if (this.Key != DeviceId)
                    return "Este dispositivo no tiene licencia válida.";

                //if (Creator != Owner)
                //    return "El archivo de licencias es invalido";
                Load();

                if (UsedQty >= MaxUssageQty)
                    return "Se ha superado el número máximo de cargas.";

                if (DateTime.Today > DueDate)
                    return "Se ha terminado el periodo de la licencia.";

                if (InstllDate > DateTime.Today)
                    return "La fecha de instalación difiere de la fecha actual";

                LoadGuids();
                if (GUIDList != null)
                    if (GUIDList.Contains(GUID))
                        return "El archivo de licencias ya fue usado anteriormente.";

                return "";
            }
            catch (Exception ex)
            {
                return "Error " + ex.Message;
            }
        }
        public string GetDueDate()
        {
            XmlDocument XmlDoc = new XmlDocument();
            try
            {
                string texto = File.ReadAllText(LicenseFile);
                texto = Base64Decode(texto);
                XmlDoc.LoadXml(texto);
            }
            catch
            {
                return "El archivo de licencias no tiene el formato correcto.";
            }
            string EndLic = XmlDoc.SelectSingleNode("/SMX/LicenseInfo").Attributes["EndLicense"].Value;
            EndLic = Base64Decode(EndLic);
            DateTime dt = new DateTime(long.Parse(EndLic));
            return dt.ToString();
        }
        private void LoadGuids()
        {
            try
            {
                if (File.Exists(LicenseFile2))
                {
                    string[] Lineas = File.ReadAllLines(LicenseFile2);

                    GUIDList ??= new List<string>();

                    foreach (string Line in Lineas)
                        if (!GUIDList.Contains(Line))
                            GUIDList.Add(Line);
                }
            }
            catch (Exception ex)
            {

            }
        }
        ///// <summary>
        ///// Obtiene el DeviceId de la computadora, sin guiones
        ///// </summary>
        ///// <returns>string con el DeviceId</returns>
        //private string GetDeviceId()
        //{
        //    try
        //    {
        //        //string sn1 = Android.OS.Build.Id;
        //        //string IMEI = TelephonyManager.FromContext(Application.Context).DeviceId;
        //        //Encontramos que la siguiente linea de código manda un id distinto en cada reinstalan, intentamos con el código de arriba y manda errores de permiso a pesar de llevarlo en el manifest!
        //        XmlDocument XmlDoc = new XmlDocument();
        //        XmlDoc.Load(MxStatsPath);
        //        string sn = XmlDoc.SelectSingleNode("MXStats").Attributes["Serial"].Value;
        //        XmlDoc = null;
        //        //string sn = Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
        //        return sn;
        //    }
        //    catch (Exception ex)
        //    {
        //        return "Error " + ex.Message;
        //    }
        //}
    }
}