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
using TagInventory.Modelos;


namespace TagInventory.Adaptadores
{
    class BaseAdapter_TagInfoListView : BaseAdapter<RFIDTag>
    {
        //private SerialNumbersInfo SerieInfo;
        private RFIDTag tagInfo;
        Context context;

        TextView txtviewTagInfoId;
        TextView txtviewTagInfoTID;
        TextView txtviewTagInfoCantidad;
        TextView txtviewTagInfoFecha;
        TextView txtviewTagInfoEPC;
        TextView txtviewTagInfoHEX;
        TextView txtviewTagInfoString;

        public List<RFIDTag> TagInfoList;
        //public List<BatchNumberInfo> BatchesList;
        public int selected = -1;
        public BaseAdapter_TagInfoListView(Context context)
        {
            this.context = context;
            TagInfoList = new List<RFIDTag>();
            //BatchesList = new List<BatchNumberInfo>();
        }
        public override int Count
        {
            get
            {
                return TagInfoList.Count;
            }
        }
        public override RFIDTag this[int position]
        {
            get
            {
                return TagInfoList[position];
            }
        }
        public void setSelection(int position)
        {
            if (selected == position)
                selected = -1;
            else
                selected = position;
        }
        public override Java.Lang.Object GetItem(int position)
        {
            return position;
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null)
                view = ((LayoutInflater)context.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.TagInfoRow, null);

            txtviewTagInfoId = view.FindViewById<TextView>(Resource.Id.txtviewTagInfoId);
            txtviewTagInfoTID = view.FindViewById<TextView>(Resource.Id.txtviewTagInfoTID);
            txtviewTagInfoCantidad = view.FindViewById<TextView>(Resource.Id.txtviewTagInfoCantidad);
            txtviewTagInfoFecha = view.FindViewById<TextView>(Resource.Id.txtviewTagInfoFecha);
            txtviewTagInfoEPC = view.FindViewById<TextView>(Resource.Id.txtviewTagInfoEPC);
            txtviewTagInfoHEX = view.FindViewById<TextView>(Resource.Id.txtviewTagInfoHEX);
            txtviewTagInfoString = view.FindViewById<TextView>(Resource.Id.txtviewTagInfoString);


            tagInfo = this[position];
            if (tagInfo != null)
            {
                txtviewTagInfoId.Text = tagInfo.Id.ToString();
                txtviewTagInfoTID.Text = tagInfo.TID;
                txtviewTagInfoCantidad.Text = tagInfo.Qty.ToString();
                txtviewTagInfoFecha.Text = tagInfo.ReadDate.ToString();
                txtviewTagInfoEPC.Text = tagInfo.MemmoryBankValue;
                txtviewTagInfoHEX.Text = tagInfo.MemmoryBankHexToString;
                txtviewTagInfoString.Text = tagInfo.MemmoryBankHexToString;
            }

            Android.Graphics.Color color;

            if (tagInfo.DBId > 0 && tagInfo.ReaderRead)
                color = Android.Graphics.Color.GreenYellow;
            else
                color = Android.Graphics.Color.ParseColor("#b2babb");

            if (position == selected)
                color = Android.Graphics.Color.CadetBlue;

            txtviewTagInfoTID.SetBackgroundColor(color);
            txtviewTagInfoCantidad.SetBackgroundColor(color);
            txtviewTagInfoFecha.SetBackgroundColor(color);
            txtviewTagInfoEPC.SetBackgroundColor(color);
            txtviewTagInfoHEX.SetBackgroundColor(color);
            txtviewTagInfoString.SetBackgroundColor(color);
            return view;
        }
        public void Clear()
        {
            TagInfoList.Clear();
        }
    }
}