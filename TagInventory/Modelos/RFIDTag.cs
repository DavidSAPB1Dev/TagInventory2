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
using System.Text;

namespace TagInventory.Modelos
{
    public class RFIDTag
    {
        public int Id { get; set; }
        /// <summary>
        /// Campo ocupado para saber si el tag ya tiene un ID en la base de datos
        /// </summary>
        public int DBId { get; set; }
        public string MEMORY_BANK { get; set; }
        public string MemmoryBankValue { get; set; }
        public string MemmoryBankHexToString { get; set; }
        public string TID { get; set; }
        public string ItemCode { get; set; }
        public DateTime ReadDate { get; set; }
        public int BaseLine { get; set; }
        public double Qty { get; set; }
        public bool ReaderRead { get; set; }
        public DateTime ReaderReadDate { get; set; }
        public string ReaderId { get; set; }
        public int AntennaId { get; set; }
        public string TryParse(DataRow row)
        {
            try
            {
                Id = Id;
                try
                {
                    DBId = int.Parse(row["Id1"].ToString());
                }
                catch
                {
                    DBId = int.Parse(row["Id"].ToString());
                }

                TID = row["TID"].ToString();
                MEMORY_BANK = row["MemoryBank"].ToString();
                MemmoryBankValue = row["MemoryBankData"].ToString();
                MemmoryBankHexToString = row["MemoryBankValue"].ToString();
                ItemCode = row["ItemCode"].ToString();
                //DistNumber = row["DistNumber"].ToString();
                //AbsEntry = int.Parse(row["AbsEntry"].ToString());
                Qty = double.Parse(row["Quantity"].ToString());
                ReadDate = DateTime.Parse(row["ReadDate"].ToString());
                //ObjType = int.Parse(row["ObjType"].ToString());
                //DocEntry = int.Parse(row["DocEntry"].ToString());
                BaseLine = int.Parse(row["LineNum"].ToString());
                //ManType = int.Parse(row["ManType"].ToString());
                //Assign = (row["Assign"].ToString() == "1") ? true : false;
                //UserId = int.Parse(row["UserId"].ToString());
                ReaderRead = (row["ReaderRead"].ToString() == "1") ? true : false;
                if (row["ReadDate"] != DBNull.Value)
                    ReadDate = DateTime.Parse(row["ReadDate"].ToString());
                if (row["ReaderId"] != DBNull.Value)
                    ReaderId = row["ReaderId"].ToString();
                if (row["AntennaId"] != DBNull.Value)
                    AntennaId = int.Parse(row["AntennaId"].ToString());
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}