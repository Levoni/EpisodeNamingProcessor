using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using static System.Net.Mime.MediaTypeNames;

namespace Naming_Processor
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      List<EpisodeInfo> GlobalInfoList = null;
      public MainWindow()
      {
         InitializeComponent();
         PBFiles.Minimum = 0;
         PBFiles.Maximum = 100;
         PBFiles.Value = 0;
      }

      private void Button_Click(object sender, RoutedEventArgs e)
      {
         var dialog = new CommonOpenFileDialog();
         dialog.IsFolderPicker = true;
         CommonFileDialogResult result = dialog.ShowDialog();

          sourceDirectory.Content = dialog.FileName;
      }

      private void Button_Click_1(object sender, RoutedEventArgs e)
      {
         var dialog = new CommonOpenFileDialog();
         dialog.IsFolderPicker = true;
         CommonFileDialogResult result = dialog.ShowDialog();

         destinationDirectory.Content = dialog.FileName;
      }
      
      //TODO: Add copy option,
      //TODO: Add Revert Functionallity
      //TODO: Add Progress Bar Visualizatoin
      private async void Button_Click_2(object sender, RoutedEventArgs e)
      {
         //setup UI
         bool isInt = int.TryParse(txtBoxSeason.Text, out int season);
         txtboxEpisodes.Text = "";
         PBFiles.Value = 0;
         var DestinationPath = destinationDirectory.Content.ToString();
         var SourcePath = sourceDirectory.Content.ToString();
         var isRegex = ckboxUseRegex.IsChecked.Value;
         GlobalInfoList = null;
         btnExecute.IsEnabled = false;

         if (isInt)
         {
             await Task.Run(() => Process(season, DestinationPath, SourcePath,isRegex));
            btnExecute.IsEnabled = true;
            btnRevert.IsEnabled = true;
         }
      }
      
      private async Task Process(int season, string destinationDirectory, string sourceDirectory, bool isRegex)
      {
         //setup variables
         List<FileChangeInfo> infoList = new List<FileChangeInfo>();
         var episodeInfos = new List<EpisodeInfo>();
         var seasonString = season.ToString();


         //Get Season and File Info
         seasonString = season.ToString().PadLeft(2, '0');
         var fullSeasonString = $"Season {seasonString}";
         var DestinationPathArray = destinationDirectory.Split(System.IO.Path.DirectorySeparatorChar);
         var showName = DestinationPathArray.Last();
         var fileList = Directory.GetFiles(sourceDirectory).OrderBy(x => x);

         var episodeNum = 0;
         //Create Ordered Episode Info
         foreach (var file in fileList)
         {
            episodeNum++;
            var ext = file.Split('.').Last();
            string newFilePath = $"{destinationDirectory}{System.IO.Path.DirectorySeparatorChar + fullSeasonString + System.IO.Path.DirectorySeparatorChar}";
            string newFileName = string.Empty;

            if (isRegex)
            {
               var episodeSubstring = Regex.Match(file, @"E[\d]{3}|E[\d]{2}|E[\d]{1}");
               episodeNum = int.Parse(episodeSubstring.Value.Substring(1));  
            }
            newFileName = $"{showName} - S{seasonString}E{episodeNum.ToString().PadLeft(2, '0')}.{ext}";

            episodeInfos.Add(new EpisodeInfo()
            {
               oldFilePath = file,
               newFilePath = newFilePath + newFileName,
               seasonNum = season,
               episodeNum = episodeNum
            });
         }
         episodeInfos = episodeInfos.OrderBy(x => x.episodeNum).ToList();

         //Create Directory
         var directoryExists = Directory.Exists(destinationDirectory + System.IO.Path.DirectorySeparatorChar + fullSeasonString);
         if (!directoryExists)
         {
            Directory.CreateDirectory(destinationDirectory + System.IO.Path.DirectorySeparatorChar + fullSeasonString);
         }

         //Move/Rename files
         foreach (var episode in episodeInfos)
         {
            System.IO.File.Move(episode.oldFilePath, episode.newFilePath);
            infoList.Add(new FileChangeInfo()
            {
               oldFileName = episode.oldFilePath,
               newFileName = episode.newFilePath
            });
            PBFiles.Dispatcher.Invoke(() => PBFiles.Value = 100 * episode.episodeNum / episodeInfos.Count());
            var changeString = $"{episode.oldFilePath.PadRight(100, ' ')} -> {episode.newFilePath}";
            txtboxEpisodes.Dispatcher.Invoke(() => {
               txtboxEpisodes.AppendText(changeString + "\n");
               txtboxEpisodes.UpdateLayout();
            });
         }
         GlobalInfoList = episodeInfos;

         //LogChanges
         directoryExists = Directory.Exists("C:" + System.IO.Path.DirectorySeparatorChar + "Logs");
         if (!directoryExists)
         {
            Directory.CreateDirectory("C:" + System.IO.Path.DirectorySeparatorChar + "Logs");
         }
         directoryExists = Directory.Exists("C:" + System.IO.Path.DirectorySeparatorChar + "Logs" + System.IO.Path.DirectorySeparatorChar + "Naming Processor");
         if (!directoryExists)
         {
            Directory.CreateDirectory("C:" + System.IO.Path.DirectorySeparatorChar + "Logs" + System.IO.Path.DirectorySeparatorChar + "Naming Processor");
         }

         using (StreamWriter sw = new StreamWriter("C:\\Logs\\Naming Processor\\" + DateTime.Now.ToFileTime()))
         {
            sw.WriteLine("Moved/Renamed below files:");
            foreach (var item in infoList)
            {
               var changeString = $"{item.oldFileName.PadRight(100, ' ')} -> {item.newFileName}";
               sw.WriteLine(changeString);
            }
         }
      }

      private async Task RevertFileNamesInEpisodeList(List<EpisodeInfo> episodeInfos)
      {
         foreach (var episode in episodeInfos)
         {
            System.IO.File.Move(episode.newFilePath, episode.oldFilePath);
            PBFiles.Dispatcher.Invoke(() => PBFiles.Value = 100 * episode.episodeNum / episodeInfos.Count());
            var changeString = $"{episode.oldFilePath.PadRight(100, ' ')} -> {episode.newFilePath}";
            txtboxEpisodes.Dispatcher.Invoke(() => {
               txtboxEpisodes.AppendText(changeString + "\n");
               txtboxEpisodes.UpdateLayout();
            });
         }
         GlobalInfoList = null;
      }
      private async void Button_Click_3(object sender, RoutedEventArgs e)
      {
         if(GlobalInfoList != null)
         {
            txtboxEpisodes.Text = "";
            btnRevert.IsEnabled = false;
            await Task.Run(() => RevertFileNamesInEpisodeList(GlobalInfoList));
         }
      }
   }


   public class FileChangeInfo
   {
      public string oldFileName;
      public string newFileName;
   }

   public class EpisodeInfo
   {
      public string oldFilePath;
      public string newFilePath;
      public int episodeNum;
      public int seasonNum;
   }
}
