using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Timers;
using TagInventory.Activityes;
using static TagInventory.Utilerias;

namespace TagInventory
{
    [Activity(Label = "@string/app_name", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : Activity
    {
        public Timer TimerDocWorking;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SplashScreen);
            //android: background = "@drawable/SplashScreen"
            // Asignar como background
            LinearLayout LL = FindViewById<LinearLayout>(Resource.Id.LLSplash);
            ImageHelper.SetScaledBackground(this, LL, Resource.Drawable.SplashScreen, 200, 200);
            //Bitmap bmp = LoadScaledBitmap(Resources, Resource.Drawable.SplashScreen, 200, 200);
            //LL.SetBackgroundDrawable(new BitmapDrawable(Resources, bmp));

            string manufacturer = Build.Manufacturer;
            string model = Build.Model;

            TimerDocWorking = new Timer();
            TimerDocWorking.Elapsed += TimerDocWorking_Elapsed;
            TimerDocWorking.Interval = 3000;
            TimerDocWorking.Enabled = true;
            TimerDocWorking.Start();
        }

        private void TimerDocWorking_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimerDocWorking.Stop();
            TimerDocWorking.Enabled = false;
            TimerDocWorking = null;
            Intent intent;
            intent = new Intent(BaseContext, typeof(MenuP));
            this.Finish();
            StartActivity(intent);
        }
        public Bitmap LoadScaledBitmap(Resources res, int resourceId, int reqWidth, int reqHeight)
        {
            // Solo obtener las dimensiones
            var options = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeResource(res, resourceId, options);

            // Calcular factor de escala
            options.InSampleSize = CalculateInSampleSize(options, reqWidth, reqHeight);

            // Cargar la imagen escalada
            options.InJustDecodeBounds = false;
            return BitmapFactory.DecodeResource(res, resourceId, options);
        }

        private int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
        {
            int height = options.OutHeight;
            int width = options.OutWidth;
            int inSampleSize = 1;

            if (height > reqHeight || width > reqWidth)
            {
                int halfHeight = height / 2;
                int halfWidth = width / 2;

                while ((halfHeight / inSampleSize) >= reqHeight && (halfWidth / inSampleSize) >= reqWidth)
                {
                    inSampleSize *= 2;
                }
            }

            return inSampleSize;
        }
    }
}