using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Symbol.XamarinEMDK.Barcode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TagInventory.Modelos
{
    public class ItemRecuento
    {
        public int Id { get; set; }
        public string HexValue { get; set; }
        public string StringValue { get; set; }
        public string MemoryBankString { get; set; }
        public string TID { get; set; }
        public string ItemCode { get; set; }
        public int Qty { get; set; }
        public bool ExisteSAP { get; set; }
        /// <summary>
        /// Valida que el item se encontro en un archivo (para recuento teorico)
        /// </summary>
        public bool InFile { get; set; }
        public ScanDataCollection.LabelType lType { get; set; }
        public string ltypeName { get; set; }
        public DateTime ReadTime { get; set; }
        public List<string> TIDList { get; set; }//Se agrega este dato para cuando se quiera contar por EPC aqui se guarden los tids
        public List<RFIDTag> TagInfoList { get; set; }//Se agrega este dato para cuando se quiera contar por EPC aqui se guarden los tids
        public ItemRecuento()
        {
            TIDList = new List<string>();
            TagInfoList = new List<RFIDTag>();
        }
    }
}