﻿using System;
using System.Net;
using System.Net.Http;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AppCMOV2.Models;
using Newtonsoft.Json;
using SkiaSharp.Views.Forms;
using SkiaSharp;
using System.Collections.Generic;

namespace AppCMOV2.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class Stocks : ContentPage
	{
        List<double> stockPrices1 = new List<double>();
        List<double> stockPrices2 = new List<double>();
        List<string> companiesList = new List<string>();
        List<string> intervalList = new List<string>();
        string company1 = "none", company2 = "none", type = "week";

        public Stocks ()
		{
			InitializeComponent();
            btCall.Clicked += BtCall_Clicked;
            
            companiesList.Add("Apple");
            companiesList.Add("IBM");
            companiesList.Add("Hewlett Packard");
            companiesList.Add("Microsoft");
            companiesList.Add("Oracle");
            companiesList.Add("Google");
            companiesList.Add("Facebook");
            companiesList.Add("Twitter");
            companiesList.Add("Intel");
            companiesList.Add("AMD");
            picker1.ItemsSource = companiesList;
            picker1.SelectedIndexChanged += Picker1SelectedIndexChanged;
            picker2.ItemsSource = companiesList;
            picker2.SelectedIndexChanged += Picker2SelectedIndexChanged;
            intervalList.Add("week");
            intervalList.Add("month");
            pickerInterval.ItemsSource = intervalList;
            pickerInterval.SelectedIndex = 0;
            pickerInterval.SelectedIndexChanged += PickerIntervalSelectedIndexChanged;
        }


        public void Picker1SelectedIndexChanged(object sender, EventArgs e)
        {
            Picker picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;

            if (selectedIndex != -1)
            {
                btCall.IsEnabled = true;
                company1 = (string)picker.ItemsSource[selectedIndex];
            }
        }

        public void Picker2SelectedIndexChanged(object sender, EventArgs e)
        {
            Picker picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;

            if (selectedIndex != -1)
            {
                company2 = (string)picker.ItemsSource[selectedIndex];
                btCall.IsEnabled = true;
            }
        }

        public void PickerIntervalSelectedIndexChanged(object sender, EventArgs e)
        {
            Picker picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;

            if (selectedIndex != -1)
            {
                type = (string)picker.ItemsSource[selectedIndex];
            }
        }

        private async void BtCall_Clicked(object sender, EventArgs e)
        {
            stockPrices1.Clear();
            stockPrices2.Clear();
            using (HttpClient client = new HttpClient())
                try
                {
                    string url = App.base_url + "stocks?";
                    url += "company1=" + company1;
                    url += "&company2=" + company2;
                    url += "&type=" + type;
                    url += "&startDate=" + "20160701";
                    Loader.IsVisible = true;
                    Loader.IsRunning = true;
                    HttpResponseMessage message = await client.GetAsync(url);
                    Loader.IsRunning = false;
                    Loader.IsVisible = false;
                    if (message.StatusCode == HttpStatusCode.OK)
                    {
                        StockList r = JsonConvert.DeserializeObject<StockList>(await message.Content.ReadAsStringAsync());
                        string jsonParsed = r.results1[0].symbol + " - " + r.results1.Count + "\n";
                        foreach (var stock in r.results1)
                        {
                            jsonParsed += stock.tradingDay + " - " + stock.close + "\n";
                            stockPrices1.Add(stock.close);
                        }
                        foreach (var stock in r.results2)
                        {
                            jsonParsed += stock.tradingDay + " - " + stock.close + "\n";
                            stockPrices2.Add(stock.close);
                        }
                        chartCanvasView.InvalidateSurface();
                    } else
                    {
                        await DisplayAlert("Error", "Some error occurred.(" + message.StatusCode + ")", "Ok");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "Ok");
                }
        }

        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            if (stockPrices1.Count == 0)
            {
                return;
            }

            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            // AXES
            SKPath path = new SKPath();
            float paddingX = 0.1f * info.Width;
            float paddingY = 0.1f * info.Height;
            /*// X
            path.MoveTo(paddingX, info.Height - paddingY);
            path.LineTo(info.Width - paddingX, info.Height - paddingY);
            // Y
            path.MoveTo(paddingX, info.Height - paddingY);
            path.LineTo(paddingX, paddingY);*/

            SKPaint strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 2
            };
            canvas.DrawPath(path, strokePaint);

            // STOCK PRICES
            int numPoints = stockPrices1.Count;
            double minPrice = stockPrices1[0];
            double maxPrice = stockPrices1[0];
            List<double> allPrices = new List<double>();
            allPrices.AddRange(stockPrices1);
            allPrices.AddRange(stockPrices2);
            foreach (double price in allPrices)
            {
                minPrice = Math.Min(minPrice, price);
                maxPrice = Math.Max(maxPrice, price);
            }
            double diffPrices = maxPrice - minPrice;
            double initY = info.Height - paddingY - paddingY * 0.5;
            double endY = paddingY;
            double diffScaleY = - endY + initY;
            double initX = paddingX;
            double endX = info.Width - paddingX;
            double diffScaleX = endX - initX;

            //Axis
            double scale = Math.Pow(10, Math.Floor(Math.Log10(diffPrices)));
            double min = Math.Floor(minPrice / scale) * scale;
            double max = Math.Ceiling(maxPrice / scale) * scale;
            drawAxis(canvas, info.Width, 0, 0, 0, info.Height, min, max, scale, numPoints);

            if (stockPrices1.Count > 0)
            {
                drawOneStockCompany(canvas, initX, initY, min, max-min, diffScaleX, 
                    diffScaleY, numPoints, stockPrices1, SKColors.Red);
            }
            if (stockPrices2.Count > 0)
            {
                drawOneStockCompany(canvas, initX, initY, min, max-min, diffScaleX, 
                    diffScaleY, numPoints, stockPrices2, SKColors.Green);
            }
        }

        void drawAxis(SKCanvas canvas, float width, double minX, double maxX, double scaleX, float height, double minY, double maxY, double scaleY, int numberOfPoints)
        {
            SKPaint grey = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Gray,
                StrokeWidth = 1
            };

            SKPaint black = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1
            };

            //Calculate Padding
            float paddingX = 0.1f * width;
            float paddingY = 0.1f * height;

            //Vertical Axis
            canvas.DrawLine(paddingX, height - paddingY, paddingX, paddingY, black);

            double divs = maxY - minY;
            double d = divs / scaleY;
            double ydivs = Math.Round(d);
            int kas = (int)ydivs;
            int yDivs = (int)(Math.Round((maxY - minY) / scaleY));

            for (int i = 0; i <= yDivs; i++)
            {
                float h = 1 - (i * 1f / yDivs);
                h *= height * 0.8f;
                h += paddingY;
                canvas.DrawLine(paddingX, h, width - paddingX, h, grey);
                String text = "" + (i * scaleY + minY) + "€";
                float textX = paddingX - black.MeasureText(text);
                canvas.DrawText(text, paddingX - black.MeasureText(text) - 5, h + black.FontMetrics.CapHeight / 2, black);
            }

            //Horizontal Axis
            int step = 1;
            while ( black.MeasureText(".000") * (numberOfPoints / step) > width)
            {
                step++;
            }
            for (int i = 0; i < numberOfPoints; i+=step)
            {
                String text = "" + (i - numberOfPoints);
                float w = paddingX + width * 0.8f * i / (numberOfPoints-1);
                canvas.DrawLine(w, paddingY, w, height - paddingY, grey);
                canvas.DrawText(text, w - black.MeasureText(text) / 2, height - paddingY + black.FontMetrics.CapHeight + 5, black);
            }
        }

    void drawOneStockCompany(SKCanvas canvas, double initX, double initY, double minPrice, double diffPrices,
            double diffScaleX, double diffScaleY, int numPoints, List<double> stockPrices, SKColor color)
        {
            List<double> pricesY = new List<double>();
            List<double> pricesX = new List<double>();
            for (int i = 0; i < stockPrices.Count; i++)
            {
                double price = stockPrices[i];
                double y = initY - (price - minPrice) / diffPrices * diffScaleY;
                pricesY.Add(y);
                double x = initX + i / (double)(numPoints - 1) * diffScaleX;
                pricesX.Add(x);
            }


            SKPath pathStockPrices = new SKPath();
            pathStockPrices.MoveTo((float)pricesX[0], (float)pricesY[0]);
            for (int i = 1; i < stockPrices.Count; i++)
            {
                pathStockPrices.LineTo((float)pricesX[i], (float)pricesY[i]);
            }

            SKPaint strokePaintStockPrices = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = color,
                StrokeWidth = 2,
                IsAntialias=true
            };
            canvas.DrawPath(pathStockPrices, strokePaintStockPrices);
        }
    }
}