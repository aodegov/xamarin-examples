﻿using System;
using Thewissen.PancakeViewSample.PageModels;
using Thewissen.PancakeViewSample.Pages;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: ExportFont("Poppins-Bold.ttf", Alias = "PoppinsBold")]
[assembly: ExportFont("Assistant-Regular.ttf", Alias = "Assistant")]
[assembly: ExportFont("Assistant-Bold.ttf", Alias = "AssistantBold")]
[assembly: ExportFont("fa-brands-400.ttf", Alias = "FA")]
[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Thewissen.PancakeViewSample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
