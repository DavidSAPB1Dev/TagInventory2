using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace TagInventory
{
    public class AppConfig
    {
        private Utilerias utilerias;

        public AppConfig()
        {
            SrchPattern = new SearchPattern();
            SrchPattern.Active = false;
            SrchPattern.Pattern = "";
        }
        public string WSRFDIDemo { get; set; }
        public string CSVSeparador { get; set; }
        public string FileExtension { get; set; }
        public short ZebraAntenaPower { get; set; }
        public bool ConvertirMemoryHexAString { get; set; }
        public bool AgregarNoIdent { get; set; }
        public int MaxUssageQty { get; set; }
        public int UsedQty { get; set; }
        public int TrimTag { get; set; }
        public bool FirstUse { get; set; }
        public bool CountEPC { get; set; }
        public SearchPattern SrchPattern { get; set; }
        public void Load()
        {
            utilerias = new Utilerias();
            XmlDocument XmlDoc = new XmlDocument();
            try
            {
                string XmlAppConfig = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "appconfiguration.xml");
                XmlDoc.Load(XmlAppConfig);
            }
            catch (Exception ex)
            {
                XmlDoc.Load(Application.Context.Assets.Open("appconfiguration.xml"));
            }
            try
            {
                WSRFDIDemo = XmlDoc.SelectSingleNode("/Configuracion/WebServices/WSRFDIDemo").InnerText;
                FirstUse = bool.Parse(XmlDoc.SelectSingleNode("/Configuracion/FirstUse").InnerText);
                CountEPC = bool.Parse(XmlDoc.SelectSingleNode("/Configuracion/ContarEPC").InnerText);

                MaxUssageQty = int.Parse(XmlDoc.SelectSingleNode("/Configuracion/License/MaxUssedQty").InnerText);
                UsedQty = int.Parse(XmlDoc.SelectSingleNode("/Configuracion/License/QtyReleased").InnerText);
                TrimTag = int.Parse(XmlDoc.SelectSingleNode("/Configuracion/TrimTag").InnerText);
                //LicenseId = XmlDoc.SelectSingleNode("/Configuracion/License/Id").InnerText;
                short ZPower = 0;
                if (!(short.TryParse(XmlDoc.SelectSingleNode("/Configuracion/ZebraAntenaPower").InnerText, out ZPower)))
                    ZPower = 300;
                ZebraAntenaPower = ZPower;

                CSVSeparador = XmlDoc.SelectSingleNode("/Configuracion/CSVSeparador").InnerText;
                FileExtension = XmlDoc.SelectSingleNode("/Configuracion/FileExtension").InnerText;
                ConvertirMemoryHexAString = bool.Parse(XmlDoc.SelectSingleNode("/Configuracion/ConvertirHexAString").InnerText);

                SrchPattern.Active = bool.Parse(XmlDoc.SelectSingleNode("/Configuracion/Patron").Attributes["Activo"].Value);
                SrchPattern.Pattern = XmlDoc.SelectSingleNode("/Configuracion/Patron").Attributes["Valor"].Value;

                AgregarNoIdent = bool.Parse(XmlDoc.SelectSingleNode("/Configuracion/AgregarNoIndentTeorico").InnerText);

                XmlDoc = null;
            }
            catch (System.Exception ex)
            {

            }
        }
        /// <summary>
        /// Agrega al archivo de configuraciones un nuevo dispositivo bluetooth
        /// </summary>
        /// <param name="MacAddress">MacAddress del equipo</param>
        /// <param name="DeviceName">Nobre del dispositivo</param>
        public void AgregaDispositivo(string MacAddress, string DeviceName)
        {
            try
            {
                XmlDocument XmlDoc = new XmlDocument();
                //
                try
                {
                    XmlDoc.Load(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "appconfiguration.xml"));
                }
                catch (Exception ex)
                {
                    XmlDoc.Load(Android.App.Application.Context.Assets.Open("appconfiguration.xml"));
                }

                XmlNodeList xmlNodeList = XmlDoc.SelectNodes("/Configuracion/BluetoothDevices/Device");

                foreach (XmlNode node in xmlNodeList)
                    if (node.Attributes["MacAddress"].Value == MacAddress && node.Attributes["Name"].Value == DeviceName)
                        return;

                XmlNode XmlN = XmlDoc.SelectSingleNode("/Configuracion/WebServices");
                XmlElement xmlDevice = XmlDoc.CreateElement("Device");
                XmlAttribute xmlAttribute;

                xmlAttribute = XmlDoc.CreateAttribute("MacAddress");
                xmlAttribute.Value = MacAddress;
                xmlDevice.Attributes.Append(xmlAttribute);

                xmlAttribute = XmlDoc.CreateAttribute("Name");
                xmlAttribute.Value = DeviceName;
                xmlDevice.Attributes.Append(xmlAttribute);

                XmlNode xmlB = XmlDoc.SelectSingleNode("/Configuracion/BluetoothDevices");
                xmlB.AppendChild(xmlDevice);

                XmlDoc.Save(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "appconfiguration.xml"));

                //Toast.MakeText(this, "Nueva conexión guardada", ToastLength.Short).Show();
                //MainActivity.ws.Url = URLWSScanmex.Text;
                //MainActivity.txtViewUrl.Text = URLWSScanmex.Text;

            }
            catch (System.Exception ex)
            {
                //Toast.MakeText(this, "Error " + ex.Message, ToastLength.Long).Show();
            }
        }
        /// <summary>
        /// Método que extrae la información del numero de serie de un documento almacenad en la carpeta root 'devinfo.html' PointMobile
        /// </summary>
        /// <returns>Numero de serie del equipo</returns>
        public string GetPointMobileSerialNumber()
        {
            try
            {
                string rootpath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                StreamReader lector = new StreamReader(@"/storage/emulated/0/devinfo.html");
                string datos = lector.ReadToEnd();
                int indx = datos.IndexOf("Serial number: ");
                string serial = datos.Substring(indx + (15), 11);
                serial = Regex.Replace(serial, @"[^\w\.@|-]", "",
                         RegexOptions.None, TimeSpan.FromSeconds(1.5));
                return serial;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        /// <summary>
        /// Guarda las configuraciones
        /// </summary>
        /// <returns>Devuelve un string si ocurrió un error al guardar</returns>
        public string Save()
        {
            try
            {
                XmlDocument XmlDoc = new XmlDocument();
                //
                try
                {
                    XmlDoc.Load(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "appconfiguration.xml"));
                }
                catch (Exception ex)
                {
                    XmlDoc.Load(Application.Context.Assets.Open("appconfiguration.xml"));
                }

                XmlDoc.SelectSingleNode("/Configuracion/WebServices/WSRFDIDemo").InnerText = WSRFDIDemo;
                XmlDoc.SelectSingleNode("/Configuracion/ZebraAntenaPower").InnerText = ZebraAntenaPower.ToString();

                XmlDoc.SelectSingleNode("/Configuracion/CSVSeparador").InnerText = CSVSeparador;
                XmlDoc.SelectSingleNode("/Configuracion/FileExtension").InnerText = FileExtension;
                XmlDoc.SelectSingleNode("/Configuracion/TrimTag").InnerText = TrimTag.ToString();
                XmlDoc.SelectSingleNode("/Configuracion/ConvertirHexAString").InnerText = ConvertirMemoryHexAString.ToString();
                XmlDoc.SelectSingleNode("/Configuracion/FirstUse").InnerText = FirstUse.ToString();
                XmlDoc.SelectSingleNode("/Configuracion/ContarEPC").InnerText = CountEPC.ToString();
                //XmlDoc.SelectSingleNode("/Configuracion/License/Id").InnerText = LicenseId;
                XmlDoc.SelectSingleNode("/Configuracion/License/MaxUssedQty").InnerText = MaxUssageQty.ToString();
                XmlDoc.SelectSingleNode("/Configuracion/License/QtyReleased").InnerText = UsedQty.ToString();
                XmlDoc.SelectSingleNode("/Configuracion/Patron").Attributes["Activo"].Value = SrchPattern.Active.ToString();
                XmlDoc.SelectSingleNode("/Configuracion/Patron").Attributes["Valor"].Value = SrchPattern.Pattern;
                XmlDoc.SelectSingleNode("/Configuracion/AgregarNoIndentTeorico").InnerText = AgregarNoIdent.ToString();
                XmlDoc.Save(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "appconfiguration.xml"));
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
    public class SearchPattern
    {
        public bool Active { get; set; }
        public string Pattern { get; set; }
    }
}