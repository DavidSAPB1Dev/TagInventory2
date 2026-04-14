using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TagInventory.Utilerias;

namespace TagInventory.Activityes
{
    [Activity(Label = "AntenaConfig", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class AntenaConfig : Activity
    {
        private static SeekBar SB_AntenaPwr;
        public static TextView txtViewAntenaPwr;
        public static ImageView Imagen;
        public static Button buttonAceptar;
        public static short ActualPower;
        private Hardware Hardware { get; set; }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.PowerAntenaF3);
            LinearLayout LLFondo = FindViewById<LinearLayout>(Resource.Id.activity_antenapower);
            ImageHelper.SetScaledBackground(this, LLFondo, Resource.Drawable.FondoLista, 200, 200);
            Imagen = (ImageView)FindViewById(Resource.Id.AntenaPwrF3);

            SB_AntenaPwr = (SeekBar)FindViewById(Resource.Id.SB_AntenaPwr);
            txtViewAntenaPwr = (TextView)FindViewById(Resource.Id.textViewAntenaPowerDB);
            buttonAceptar = (Button)FindViewById(Resource.Id.button_Accept);
            buttonAceptar.Click += ButtonAceptar_Click;

            //int HW = Intent.GetIntExtra("HardWare", 0);
            //HardWare = (Hardware)HW;

            DisplayMetrics DM = Resources.DisplayMetrics;
            if (DM.HeightPixels == 728 && DM.WidthPixels == 480)
                Hardware = Hardware.ZebraMC33;
            else if (DM.HeightPixels == 1344 && DM.WidthPixels == 720)
                Hardware = Hardware.Urovo;
            else if (DM.HeightPixels == 1450 && DM.WidthPixels == 720)
                Hardware = Hardware.ZebraTC15;
            else if (DM.HeightPixels == 1184 && DM.WidthPixels == 720)
                Hardware = Hardware.Keyence;
            else if (DM.HeightPixels == 1776 && DM.WidthPixels == 1080)
                Hardware = Hardware.ChainwayC72;
            else
                Hardware = Hardware.PointMobile;

            SB_AntenaPwr.Min = 1;

            if (Hardware == Hardware.ZebraMC33)
            {
                SB_AntenaPwr.Max = 300;
                ActualPower = Intent.GetShortExtra("ActualPower", 0);
            }
            else
            {
                SB_AntenaPwr.Max = 30;
                ActualPower = Intent.GetShortExtra("ActualPower", 0);
            }

            UpdateUI();

            SB_AntenaPwr.Progress = ActualPower;
            SB_AntenaPwr.ProgressChanged += SB_AntenaPwr_ProgressChanged;
            txtViewAntenaPwr.Text = string.Format("{0} dB", SB_AntenaPwr.Progress);
        }
        public override void OnBackPressed()
        {
            CierraApp();
        }
        private void ButtonAceptar_Click(object sender, EventArgs e)
        {
            CierraApp();
        }
        private void CierraApp()
        {
            Intent returnintent = new Intent();
            if (Hardware == Hardware.ZebraMC33)
                returnintent.PutExtra("PowerResult", short.Parse(ActualPower.ToString()));
            else
                returnintent.PutExtra("PowerResult", ActualPower);

            SetResult(Result.Ok, returnintent);
            this.Finish();
        }
        private void SB_AntenaPwr_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            ActualPower = (short)SB_AntenaPwr.Progress;
            txtViewAntenaPwr.Text = string.Format("{0} dB", SB_AntenaPwr.Progress);
            UpdateUI();
        }
        private void UpdateUI()
        {
            try
            {
                if (Hardware == Hardware.ZebraMC33)
                {
                    if (ActualPower == 1)
                        Imagen.SetImageResource(Resource.Drawable.Potencia1);

                    if (ActualPower >= 2 && ActualPower <= 149)
                        Imagen.SetImageResource(Resource.Drawable.Potencia2);

                    if (ActualPower >= 150)
                        Imagen.SetImageResource(Resource.Drawable.Potencia3);

                    if (ActualPower == 300)
                        Imagen.SetImageResource(Resource.Drawable.Potencia4);
                }
                else
                {
                    if (ActualPower == 1)
                        Imagen.SetImageResource(Resource.Drawable.Potencia1);

                    if (ActualPower >= 2 && ActualPower <= 14)
                        Imagen.SetImageResource(Resource.Drawable.Potencia2);

                    if (ActualPower >= 15)
                        Imagen.SetImageResource(Resource.Drawable.Potencia3);

                    if (ActualPower == 30)
                        Imagen.SetImageResource(Resource.Drawable.Potencia4);
                }
            }
            catch { }

        }
    }
}