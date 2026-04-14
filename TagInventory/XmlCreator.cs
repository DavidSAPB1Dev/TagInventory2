using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using TagInventory.Modelos;

namespace TagInventory
{
    public class XmlCreator
    {
        public int FREntry { get; set; }
        public string Usuario { get; set; }
        public string Password { get; set; }
        public string Userid { get; set; }
        public string Serial { get; set; }
        public string XmlDocRecuentoSimple(List<ItemRecuento> RecuentoList)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;
                System.Text.StringBuilder XmlBuild = new System.Text.StringBuilder();

                using (XmlWriter Writer = XmlWriter.Create(XmlBuild, settings))
                {
                    Writer.WriteStartDocument();

                    Writer.WriteStartElement("RECUENTO");

                    Writer.WriteStartElement("RECUENTO_HEADER");
                    Writer.WriteElementString("FEntry", FREntry.ToString());
                    Writer.WriteElementString("UserId", Userid);
                    Writer.WriteElementString("Serial", Serial);
                    //Writer.WriteElementString("Date", DateTime.Today.ToString("yyyy-MM-dd"));
                    //Writer.WriteElementString("Hour", DateTime.Now.ToString("HH:mm:ss"));
                    //Writer.WriteElementString("Status", "P");
                    Writer.WriteEndElement();

                    Writer.WriteStartElement("RECUENTO_LINES");
                    foreach (ItemRecuento itm in RecuentoList)
                    {
                        Writer.WriteStartElement("row");
                        Writer.WriteElementString("HexValue", itm.HexValue);
                        Writer.WriteElementString("String", itm.StringValue);
                        if (itm.TID != "")
                            Writer.WriteElementString("TID", itm.TID);
                        Writer.WriteElementString("Qty", itm.Qty.ToString()); //Se agrega la cantidad a petición de Armando para proyecto de TAGInventory 07/10/2024
                        Writer.WriteElementString("Fecha", itm.ReadTime.ToString("yyyy-MM-dd HH:mm:ss:fff"));
                        Writer.WriteEndElement();
                    }
                    Writer.WriteEndElement();

                    Writer.WriteEndElement();
                    Writer.WriteEndDocument();
                }
                return XmlBuild.ToString();
            }
            catch (System.Exception ex)
            {
                return "Error XmlBuild" + ex.Message;
            }
        }
    }
}