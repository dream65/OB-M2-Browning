using LiteDB;
using OpenBullet.ViewModels;
using RuriLib;
using RuriLib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace OpenBullet
{
    /// <summary>
    /// Logica di interazione per WordlistManager.xaml
    /// </summary>
    public partial class WordlistManager : Page
    {
        WordlistManagerViewModel vm = new WordlistManagerViewModel();
        private GridViewColumnHeader listViewSortCol = null;
        private SortAdorner listViewSortAdorner = null;

        public WordlistManager()
        {
            InitializeComponent();
            DataContext = vm;

            vm.RefreshList();
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            (new MainDialog(new DialogAddWordlist(this), "Add Wordlist")).ShowDialog();
        }

        
        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.LogInfo(Components.WordlistManager, $"Deleting {wordlistListView.SelectedItems.Count} references from the DB");
            using (var db = new LiteDatabase(Globals.dataBaseFile))
            {
                var list = wordlistListView.SelectedItems.Cast<Wordlist>();
                while (list.Count() > 0)
                {
                    db.GetCollection<Wordlist>("wordlists").Delete(list.First().Id);
                    vm.WordlistList.Remove(list.First());
                }   
            }
            Globals.LogInfo(Components.WordlistManager, "Successfully deleted the wordlist references from the DB");
        }

        
        private void deleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.LogWarning(Components.WordlistManager, "Purge selected, prompting warning");

            if (MessageBox.Show("This will purge the WHOLE Wordlists DB, are you sure you want to continue?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Globals.LogInfo(Components.WordlistManager, "Purge initiated");
                using (var db = new LiteDatabase(Globals.dataBaseFile))
                {
                    db.DropCollection("wordlists");
                }
                vm.WordlistList.Clear();
                Globals.LogInfo(Components.WordlistManager, "Purge finished");
            }
            else { Globals.LogInfo(Components.WordlistManager, "Purge dismissed"); }
        }

        private void deleteNotFoundWordlistsButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.LogWarning(Components.WordlistManager, "Rescan process of wordlists availability.");
            using (var db = new LiteDatabase(Globals.dataBaseFile))
            {
                foreach (var wordlist in wordlistListView.Items.Cast<Wordlist>().ToList())
                {
                    if (!File.Exists(wordlist.Path))
                    {
                        db.GetCollection<Wordlist>("wordlists").Delete(wordlist.Id);
                        vm.WordlistList.Remove(wordlist);
                    }
                }
            }
        }

        public void AddWordlist(Wordlist wordlist)
        {
            if (vm.WordlistList.Any(w => w.Path == wordlist.Path))
            {
                Globals.LogError(Components.WordlistManager, $"Wordlist already present: {wordlist.Path}");
                return;
            }

            vm.WordlistList.Add(wordlist);
            AddWordlistToDB(wordlist);
        }

        public void AddWordlistToDB(Wordlist wordlist)
        {
            // Add it to the DB
            Globals.LogInfo(Components.WordlistManager, "Adding Wordlist '" + wordlist.Name + " at position '" + wordlist.Path + "' with type '" + wordlist.Type + "' and purpose '" + wordlist.Purpose + "' to the DB");
            using (var db = new LiteDatabase(Globals.dataBaseFile))
            {
                db.GetCollection<Wordlist>("wordlists").Insert(wordlist);
            }
        }

        public List<Wordlist> GetLists()
        {
            return vm.WordlistList.ToList();
        }

        public Wordlist GetList(string name)
        {
            return vm.WordlistList.Where(x => x.Name == name).First();
        }

        private void listViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            string sortBy = column.Tag.ToString();
            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                wordlistListView.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            listViewSortCol = column;
            listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            wordlistListView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }

        private void wordlistListViewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach(var file in files.Where(x => x.EndsWith(".txt")).ToArray())
                {
                    try
                    {
                        // Build the wordlist object
                        var path = file;
                        var cwd = Directory.GetCurrentDirectory();
                        if (path.StartsWith(cwd)) path = path.Substring(cwd.Length + 1);
                        var wordlist = new Wordlist(Path.GetFileNameWithoutExtension(file), path, Globals.environment.WordlistTypes.First().Name, "");

                        // Get the first line
                        var first = File.ReadLines(wordlist.Path).First(l => !string.IsNullOrWhiteSpace(l));

                        // Set the correct wordlist type
                        wordlist.Type = Globals.environment.RecognizeWordlistType(first);

                        // Add the wordlist to the manager
                        AddWordlist(wordlist);
                    }
                    catch { }
                }
            }
        }

        //private void deleteDupeWordlistsButton_Click(object sender, RoutedEventArgs e)
        //{
        //    int lastWL = 0;
        //    Globals.LogWarning(Components.WordlistManager, "Deleting duplicates wordlists of same file path.");
        //    using (var db = new LiteDatabase(Globals.dataBaseFile))
        //    {
        //        foreach (var wordlist in wordlistListView.Items.Cast<Wordlist>().ToList())
        //        {
        //            if (wordlist.Total == lastWL)
        //            {
        //                db.GetCollection<Wordlist>("wordlists").Delete(wordlist.Id);
        //                vm.WordlistList.Remove(wordlist);
        //            }
        //            lastWL = wordlist.Total;
        //        }
        //    }
        //}
    }
}
