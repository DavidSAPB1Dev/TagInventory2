using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Zebra.Rfid.Api3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TagInventory.Modelos
{
    public class GenericTag
    {
        public string TagID { get; set; }           // EPC
        public string HexValue { get; set; }
        public string MemoryBankData { get; set; }  // Datos del banco de memoria
        public MEMORY_BANK MemoryBank { get; set; }      // Nombre del banco de memoria
        public bool IsReadSuccess { get; set; }
        public ACCESS_OPERATION_CODE OpCode { get; set; }
        public ACCESS_OPERATION_STATUS OpStatus { get; set; }
        public DateTime ReadTime { get; set; }
    }
}