using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TagInventory
{
    public static class ImageHelper
    {
        public static void SetScaledBackground(Context context, Button button, int resourceId, int reqWidth, int reqHeight)
        {
            Bitmap bmp = LoadScaledBitmap(context.Resources, resourceId, reqWidth, reqHeight);
            button.SetBackgroundDrawable(new BitmapDrawable(context.Resources, bmp));
        }
        public static void SetScaledBackground(Context context, LinearLayout linearlayout, int resourceId, int reqWidth, int reqHeight)
        {
            Bitmap bmp = LoadScaledBitmap(context.Resources, resourceId, reqWidth, reqHeight);
            linearlayout.SetBackgroundDrawable(new BitmapDrawable(context.Resources, bmp));
        }
        private static Bitmap LoadScaledBitmap(Android.Content.Res.Resources res, int resId, int reqWidth, int reqHeight)
        {
            // 1. Obtener las dimensiones sin cargar el bitmap completo
            var options = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeResource(res, resId, options);

            // 2. Calcular el factor de escala
            options.InSampleSize = CalculateInSampleSize(options, reqWidth, reqHeight);

            // 3. Cargar el bitmap escalado
            options.InJustDecodeBounds = false;
            return BitmapFactory.DecodeResource(res, resId, options);
        }

        private static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
        {
            int height = options.OutHeight;
            int width = options.OutWidth;
            int inSampleSize = 1;

            if (height > reqHeight || width > reqWidth)
            {
                int halfHeight = height / 2;
                int halfWidth = width / 2;

                while ((halfHeight / inSampleSize) >= reqHeight &&
                       (halfWidth / inSampleSize) >= reqWidth)
                {
                    inSampleSize *= 2;
                }
            }
            return inSampleSize;
        }
    }
}