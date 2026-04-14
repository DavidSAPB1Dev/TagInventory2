using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text.Style;
using Android.Text;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TagInventory.Utilerias;
using TagInventory.Modelos;
using Symbol.XamarinEMDK.Barcode;

namespace TagInventory.Adaptadores
{
    public class BaseAdapter_Recuento_ListView : BaseAdapter<ItemRecuento>
    {
        public ItemRecuento ItemRecuento;
        Context context;
        private TextView textViewIDRecuento;
        private TextView textViewHexValue;
        private TextView textViewStringValue;
        private TextView textViewMemoryBank;
        private TextView textViewQty;
        private ImageView Imagen;
        private LinearLayout LineaLayOut;
        public List<ItemRecuento> RecuentoList;
        public bool IsToShow;
        public string Separador;
        public int Index;
        private int selected = -1;
        public Hardware HardWare { get; set; }
        public InventoryType IType { get; set; }
        public BaseAdapter_Recuento_ListView(Context context)
        {
            this.context = context;
            RecuentoList = new List<ItemRecuento>();
            IsToShow = false;
            Separador = "";
            Index = -1;
        }
        public override int Count
        {
            get
            {
                return RecuentoList.Count;
            }
        }

        public override ItemRecuento this[int position]
        {
            get
            {
                return RecuentoList[position];
            }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return position;
        }

        public override long GetItemId(int position)
        {
            return position;
        }
        public void setSelection(int position)
        {
            selected = (selected == position) ? -1 : position;
        }
        public void DeleteRecuentoItemByIndex(int index)
        {
            RecuentoList.RemoveAt(index);
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View v = convertView;

            v ??= ((LayoutInflater)context.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.ItemRecuentoRow2, null);

            //if (IsToShow)
            //    v = ((LayoutInflater)context.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.ItemRecuentoRowShow, null);

            textViewIDRecuento = v.FindViewById<TextView>(Resource.Id.textView_IdRecuento);
            textViewHexValue = v.FindViewById<TextView>(Resource.Id.textView_HexValue);
            textViewStringValue = v.FindViewById<TextView>(Resource.Id.textView_StringValue);
            textViewMemoryBank = v.FindViewById<TextView>(Resource.Id.textView_MemoryBank);
            textViewQty = v.FindViewById<TextView>(Resource.Id.textView_Qty);

            Imagen = v.FindViewById<ImageView>(Resource.Id.ImgLinea);

            LineaLayOut = v.FindViewById<LinearLayout>(Resource.Id.LinearLayoutLineaRecuento);

            ItemRecuento = this[position];
            if (ItemRecuento != null)
            {
                if (IsToShow)
                {
                    textViewIDRecuento.Text = (position + 1).ToString();
                    if (Separador != "" && Index >= 0)
                    {
                        string[] datos = ItemRecuento.StringValue.Split(Separador);
                        string final = "";
                        int inicio, fin;
                        inicio = 1;
                        fin = 1;
                        for (int i = 0; i < datos.Length; i++)
                        {
                            if (i == Index)
                            {
                                inicio = ItemRecuento.StringValue.IndexOf(datos[i]);
                                fin = inicio + datos[i].Length;
                            }
                            final += datos[i] + " ";
                        }
                        try
                        {
                            ItemRecuento.ItemCode = datos[Index];
                            //ItemRecuento.ItemCode = ItemRecuento.StringValue.Substring(inicio, fin);
                        }
                        catch { }

                        SpannableStringBuilder sp = new SpannableStringBuilder(final);
                        sp.SetSpan(new ForegroundColorSpan(Color.Red), inicio, fin, SpanTypes.ExclusiveExclusive);
                        sp.SetSpan(new StyleSpan(TypefaceStyle.Bold), inicio, fin, SpanTypes.ExclusiveExclusive);
                        textViewHexValue.SetText(sp, TextView.BufferType.Spannable);
                    }
                    else
                    {
                        textViewHexValue.Text = ItemRecuento.StringValue;
                    }

                    textViewStringValue.Text = ItemRecuento.HexValue;
                }
                else
                {
                    textViewIDRecuento.Text = (position + 1).ToString();
                    textViewHexValue.Text = ItemRecuento.HexValue;
                    textViewStringValue.Text = ItemRecuento.StringValue;
                    textViewMemoryBank.Text = ItemRecuento.MemoryBankString;
                    if (IType == InventoryType.Blind)
                        textViewQty.Text = ItemRecuento.Qty.ToString();
                    else
                        textViewQty.Text = string.Format("{0}/{1}", ItemRecuento.TagInfoList.Count, ItemRecuento.Qty.ToString());
                }
                if (HardWare == Hardware.HoneyWell)
                {
                    Imagen.SetImageResource(Resource.Drawable.DefaultBarCode);
                }
                else
                {
                    if (ItemRecuento.lType != null)
                    {
                        if (ItemRecuento.lType == ScanDataCollection.LabelType.Code128 || ItemRecuento.lType == ScanDataCollection.LabelType.Code32 || ItemRecuento.lType == ScanDataCollection.LabelType.Codabar)
                            Imagen.SetImageResource(Resource.Drawable.BarCode);
                        else if (ItemRecuento.lType == ScanDataCollection.LabelType.Ean13 || ItemRecuento.lType == ScanDataCollection.LabelType.Ean128)
                            Imagen.SetImageResource(Resource.Drawable.EAN13);
                        else if (ItemRecuento.lType == ScanDataCollection.LabelType.Datamatrix)
                            Imagen.SetImageResource(Resource.Drawable.DataMatrix);
                        else if (ItemRecuento.lType == ScanDataCollection.LabelType.Pdf417)
                            Imagen.SetImageResource(Resource.Drawable.Pdf417);
                        else if (ItemRecuento.lType == ScanDataCollection.LabelType.Qrcode)
                            Imagen.SetImageResource(Resource.Drawable.QR);
                        else if (ItemRecuento.lType == ScanDataCollection.LabelType.Upca)
                            Imagen.SetImageResource(Resource.Drawable.UPCA);
                        else
                            Imagen.SetImageResource(Resource.Drawable.DefaultBarCode);
                    }
                }

                if (IsToShow)
                    if (!(ItemRecuento.ExisteSAP))
                        textViewHexValue.SetBackgroundColor(Color.Yellow);

            }

            //Android.Graphics.Color color = Android.Graphics.Color.ParseColor("#e5e8e4");
            Color color = Color.Transparent;

            if (IType == InventoryType.Theoric)
            {
                if (ItemRecuento.InFile)
                {
                    if (ItemRecuento.TagInfoList.Count > 0)
                    {
                        if (ItemRecuento.TagInfoList.Count < ItemRecuento.Qty)
                            color = Color.Yellow;
                        else if (ItemRecuento.TagInfoList.Count >= ItemRecuento.Qty)
                            color = Color.ForestGreen;
                    }
                }
                else
                    color = Color.Red;
            }

            if (position == selected)
                color = Color.CadetBlue;

            LineaLayOut.SetBackgroundColor(color);
            //textViewIDRecuento.SetBackgroundColor(color);
            //textViewHexValue.SetBackgroundColor(color);
            //textViewStringValue.SetBackgroundColor(color);

            return v;
        }
        /// <summary>
        /// Agrega el elemento a la lista
        /// </summary>
        /// <param name="Item"></param>
        public void AddRecuentoItem(ItemRecuento Item)
        {
            if (Item.lType != null)
                RecuentoList.Add(Item);
            else
                if (RecuentoList.FindAll(t => t.HexValue == Item.HexValue).Count == 0)
                RecuentoList.Add(Item);
        }
        public void Clear()
        {
            RecuentoList.Clear();
        }
    }
}