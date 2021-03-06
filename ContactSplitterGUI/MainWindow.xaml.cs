﻿using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using VCF;
using Ookii.Dialogs.Wpf;
using System.Threading.Tasks;
using System.Threading;

namespace ContactSplitMergeGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
            mergeRadioBtn.IsChecked = true;
        }


        private bool isRunning = false;
        private VCFExtractorLocations extractorLocations;
        private VCFMergerLocations mergerLocations;
        private CancellationTokenSource tokenSource;

        private void Button_Click_Input(object sender, RoutedEventArgs e)
        {

            if (GetSourceFiles() == true)
                sourceLbl.Content = spiltRadioBtn.IsChecked == true ? extractorLocations.SourceFile : mergerLocations.SourceFiles.Length + " Contacts selected";
        }


        private void Button_Click_Output(object sender, RoutedEventArgs e)
        {
            if (spiltRadioBtn.IsChecked == true)
            {
                if (GetDestFolder() == true)
                    destLbl.Content = extractorLocations.DestinationFolder;
            }
            else if (mergeRadioBtn.IsChecked == true)
            {
                if (GetSaveFile() == true)
                    destLbl.Content = mergerLocations.DestinationFile;
            }
        }

        private async void GoButton_ClickAsync(object sender, RoutedEventArgs e)
        {

            if (spiltRadioBtn.IsChecked == true)
            {
                VCFExtractor extractor;
                try
                {
                    extractor = new VCFExtractor(extractorLocations, new Progress<VCFProgressData>((progressData) =>
                     {
                         progressBar.Value = progressData.Number;
                         statsLbl.Content = $"{progressData.Number} Extracting {progressData.Name}";
                     }));

                }
                catch (InvalidDataException exception)
                {
                    MessageBox.Show(exception.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }


                progressBar.Maximum = extractor.TotalCardsInSource;
                contactsFoundLbl.Content = $"{extractor.TotalCardsInSource} Contacts found!";



                




                tokenSource = new CancellationTokenSource();
                btnCancel.Visibility = Visibility.Visible;
                isRunning = true;

                try
                {
                    var returnData = await Task.Run(() => extractor.ExtractToFiles(tokenSource.Token));

                    MessageBox.Show($"{returnData.Number} contacts succesfully extracted to {returnData.Name}", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
                    Clear();

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Cancelled!", MessageBoxButton.OK, MessageBoxImage.Information);
                    Clear();
                }


            }

            else if (mergeRadioBtn.IsChecked == true)
            {

                VCFMerger merger;
                try
                {
                    merger = new VCFMerger(mergerLocations, new Progress<VCFProgressData>((progressData) =>
                     {
                         progressBar.Value = progressData.Number;
                         statsLbl.Content = $"{progressData.Number} Merging {progressData.Name}";
                     }));
                }

                catch (InvalidDataException g)
                {
                    MessageBox.Show(g.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                progressBar.Maximum = merger.TotalCards;
                contactsFoundLbl.Content = $"{merger.TotalCards} contacts selected.";







                tokenSource = new CancellationTokenSource();
                btnCancel.Visibility = Visibility.Visible;
                isRunning = true;

                try
                {
                    var returnData = await Task.Run(() => merger.MergeContacts(tokenSource.Token));
                    MessageBox.Show($"{returnData.Number} contacts succesfully merged to {returnData.Name}", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
                    Clear();

                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Cancelled!", MessageBoxButton.OK, MessageBoxImage.Information);
                    Clear();
                }



            }

        }


        private bool? GetSourceFiles()
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Multiselect = spiltRadioBtn.IsChecked == true ? false : true,
                Filter = "Contact files (*.vcf, *.vcard)|*.vcf;*.vcard",
                Title = "Source:"
            };

            bool? result = fileDialog.ShowDialog();
            if (result == true)
            {
                if (mergeRadioBtn.IsChecked == true)
                    mergerLocations.SourceFiles = fileDialog.FileNames;
                else if (spiltRadioBtn.IsChecked == true)
                    extractorLocations.SourceFile = fileDialog.FileName;

            }
            return result;
        }


        private bool? GetSaveFile()
        {
            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                Filter = "Contact file (*.vcf, *.vcard)|*.vcf;*.vcard",
                OverwritePrompt = true,
                ValidateNames = true,
                FileName = "vcard.vcf",
                DefaultExt = ".vcf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };
            bool? result = dialog.ShowDialog();

            if (result == true)
                mergerLocations.DestinationFile = dialog.FileName;
            return result;
        }

        private bool? GetDestFolder()
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog
            {
                Description = "Please select a folder.",
                UseDescriptionForTitle = true, // This applies to the Vista style dialog only, not the old dialog.
                ShowNewFolderButton = false,
                RootFolder = Environment.SpecialFolder.Desktop
            };
            bool? result = dialog.ShowDialog();
            if (result == true)
                extractorLocations.DestinationFolder = dialog.SelectedPath;
            return result;
        }


        private void Clear()
        {
            sourceLbl.Content = statsLbl.Content = contactsFoundLbl.Content = destLbl.Content = null;
            progressBar.Value = 0; isRunning = false;
            btnCancel.Visibility = Visibility.Hidden;
        }

        private void SplitChckBox_Checked(object sender, RoutedEventArgs e)
        {
            Clear();
            sourceLbl.Content = "No file selected";
            goButton.Content = "Split";
            extractorLocations = new VCFExtractorLocations();
        }



        private void MergeChckBox_Checked(object sender, RoutedEventArgs e)
        {
            Clear();
            sourceLbl.Content = "0 Contacts selected";
            goButton.Content = "Merge";
            mergerLocations = new VCFMergerLocations();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isRunning)
            {
                MessageBoxResult result =
                            MessageBox.Show("Are you sure you wanna exit? this will abort all running tasks!", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }


        }
    }
}
