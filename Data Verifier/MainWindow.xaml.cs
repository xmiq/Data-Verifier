using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MoreLinq.Extensions;
using Renci.SshNet;
using Renci.SshNet.Async;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DataVerifier
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadXML();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        #region Controls

        Button PEM20_Button;
        Button PEM21_Button;
        Button PEM22_Button;

        TextBlock PEM20_Path;
        TextBlock PEM21_Path;
        TextBlock PEM22_Path;

        ListBox PEM20_ListBox;
        ListBox PEM21_ListBox;
        ListBox PEM22_ListBox;

        DatePicker TimestampPicker;

        TextBlock UploadNotification;

        Button UploadDatabases;
        Button LoadAll;
        Button Analyse;

        #endregion

        #region Variables

        private readonly Dictionary<string, TimeSpan> Activities = new Dictionary<string, TimeSpan>();

        #endregion

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            #region Designer
            PEM20_Button = this.FindControl<Button>("PEM20_Button");
            PEM21_Button = this.FindControl<Button>("PEM21_Button");
            PEM22_Button = this.FindControl<Button>("PEM22_Button");
            PEM20_Path = this.FindControl<TextBlock>("PEM20_Path");
            PEM21_Path = this.FindControl<TextBlock>("PEM21_Path");
            PEM22_Path = this.FindControl<TextBlock>("PEM22_Path");
            PEM20_ListBox = this.FindControl<ListBox>("PEM20_ListBox");
            PEM21_ListBox = this.FindControl<ListBox>("PEM21_ListBox");
            PEM22_ListBox = this.FindControl<ListBox>("PEM22_ListBox");
            TimestampPicker = this.FindControl<DatePicker>("TimestampPicker");
            UploadNotification = this.FindControl<TextBlock>("UploadNotification");
            UploadDatabases = this.FindControl<Button>("UploadDatabases");
            LoadAll = this.FindControl<Button>("LoadAll");
            Analyse = this.FindControl<Button>("Analyse");
            #endregion
            PEM20_Button.Click += async (sender, e) =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Open PEM 20 database"
                };
                string[] files = await openFileDialog.ShowAsync(this);
                if (files.Any())
                {
                    PEM20_Path.Text = string.Join("; ", files);
                }
            };
            PEM21_Button.Click += async (sender, e) =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Open PEM 21 database"
                };
                string[] files = await openFileDialog.ShowAsync(this);
                if (files.Any())
                {
                    PEM21_Path.Text = string.Join("; ", files);
                }
            };
            PEM22_Button.Click += async (sender, e) =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Open PEM 22 database"
                };
                string[] files = await openFileDialog.ShowAsync(this);
                if (files.Any())
                {
                    PEM22_Path.Text = string.Join("; ", files);
                }
            };
            UploadDatabases.Click += async (sender, e) =>
            {
                Task t = new Task(async () =>
                {
                    string pickerTime = (TimestampPicker.SelectedDate ?? DateTime.Now).ToString("yyyyMMddhhmm");
                    TextWriter textWriter = new StreamWriter(File.OpenWrite("ssh.log"));
                    using (SshClient ssh = new SshClient(ConfigurationManager.AppSettings["Host"], ConfigurationManager.AppSettings["Username"], ConfigurationManager.AppSettings["Password"]))
                    {
                        ssh.Connect();

                        void RunCommand(string command)
                        {
                            textWriter.WriteLine(command);
                            SshCommand cmd = ssh.RunCommand(command);
                            if (string.IsNullOrWhiteSpace(cmd.Error))
                                textWriter.WriteLine(cmd.Result);
                            else
                                textWriter.WriteLine(cmd.Error);
                        }

                        RunCommand($"mkdir { pickerTime }");
                        RunCommand($"mkdir -p { pickerTime }/PEM\\ 20");
                        RunCommand($"mkdir -p { pickerTime }/PEM\\ 21");
                        RunCommand($"mkdir -p { pickerTime }/PEM\\ 22");
                        ssh.Disconnect();
                    }

                    await textWriter.FlushAsync();

                    using (SftpClient sftp = new SftpClient(ConfigurationManager.AppSettings["Host"], ConfigurationManager.AppSettings["Username"], ConfigurationManager.AppSettings["Password"]))
                    {
                        sftp.Connect();

                        void UploadFileAsync(TextBlock databasePath, string remotePath)
                        {
                            foreach (FileInfo file in databasePath.Text.Split(separator: ";".ToCharArray(), options: StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => File.Exists(x)).Select(x => new FileInfo(x)))
                            {
                                string remoteFile = $"{ pickerTime }/{ remotePath }/{ file.Name }";
                                textWriter.WriteLine($"sftp { file.FullName } {remoteFile}");
                                try
                                {
                                    sftp.UploadFile(file.OpenRead(), remoteFile);
                                }
                                catch (Exception ex)
                                {
                                    textWriter.WriteLine(ex.Message);
                                }
                            }
                        }
                        UploadFileAsync(PEM20_Path, "PEM 20");
                        UploadFileAsync(PEM21_Path, "PEM 21");
                        UploadFileAsync(PEM22_Path, "PEM 22");
                        sftp.Disconnect();
                    }

                    await textWriter.FlushAsync();
                    textWriter.Close();
                });
                t.Start();
                await t;

                UploadNotification.Text = "Uploaded";
            };
            LoadAll.Click += async (sender, e) =>
            {
                OpenFolderDialog openFolderDialog = new OpenFolderDialog
                {
                    Title = "Open databases folder"
                };
                string directory = await openFolderDialog.ShowAsync(this);
                if (Directory.Exists(directory))
                {
                    var files = Directory.EnumerateDirectories(directory).Select(x => new { PEM_No = Regex.Match(x, "PEM\\s\\d{2}").Value, Files = Directory.EnumerateFiles(x).Where(y => !y.Contains("_database") && !y.Contains("database-shm") && !y.Contains("database-wal"))  });
                    foreach (var folder in files)
                    {
                        if (folder.Files.Any())
                        {
                            switch (folder.PEM_No)
                            {
                                case "PEM 20":
                                    PEM20_Path.Text = string.Join("; ", folder.Files);
                                    break;
                                case "PEM 21":
                                    PEM21_Path.Text = string.Join("; ", folder.Files);
                                    break;
                                case "PEM 22":
                                    PEM22_Path.Text = string.Join("; ", folder.Files);
                                    break;
                            }
                        }
                    }
                }
            };
            Analyse.Click += (sender, e) =>
            {
                LoadDatabase(PEM20_Path, PEM20_ListBox);
                LoadDatabase(PEM21_Path, PEM21_ListBox);
                LoadDatabase(PEM22_Path, PEM22_ListBox);

                void LoadDatabase(TextBlock path, ListBox list)
                {
                    IEnumerable<string> databases = path.Text.Split(separator: ";".ToCharArray(), options: StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => File.Exists(x));
                    foreach (string pathT in databases)
                    {
                        SQLiteConnection connection = new SQLiteConnection($"Data Source={ pathT }");
                        connection.Open();

                        GetData(connection, list.Items as IList);
                    }
                }
                void GetData(SQLiteConnection connection, IList ListBoxItems)
                {
                    try
                    {
                        SQLiteCommand command = connection.CreateCommand();
                        command.CommandText = "SELECT key as \"Activity ID\", userId as \"User\", time(totalTime/1000, \"unixepoch\") AS \"Time\" FROM activity WHERE totalTime IS NOT NULL ORDER BY userId, id";
                        Activity[] data = command.ExecuteReader().OfType<IDataRecord>().Select(x => new Activity { ActivityID = x["Activity ID"].ToString(), User = x["User"].ToString(), Time = x["Time"].ToString() }).ToArray();
                        IEnumerable<CombinedActivity> combined = data.FullJoin(second: Activities,
                            firstKeySelector: x => x.ActivityID,
                            secondKeySelector: x => x.Key,
                            firstSelector: x => new CombinedActivity { ActivityID = x.ActivityID, Time = TimeSpan.Parse(x.Time), User = x.User, ProperTime = TimeSpan.FromSeconds(0) },
                            secondSelector: x => new CombinedActivity { ActivityID = x.Key, Time = TimeSpan.FromSeconds(0), User = "-1", ProperTime = x.Value},
                            bothSelector: (x, y) => new CombinedActivity{ ActivityID = x.ActivityID, Time = TimeSpan.Parse(x.Time), User = x.User, ProperTime = y.Value });

                        #region Label

                        {

                            ListBoxItem lbi = new ListBoxItem();

                            ListBoxItems.Add(lbi);

                            Grid grid = new Grid();

                            lbi.Content = grid;

                            grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(60, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(40, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(80, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));

                            grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));
                            grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                            grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));

                            TextBlock id = new TextBlock
                            {
                                Text = "ActivityID"
                            };
                            Grid.SetColumn(id, 1);
                            Grid.SetRow(id, 1);

                            grid.Children.Add(id);

                            TextBlock usr = new TextBlock
                            {
                                Text = "User"
                            };
                            Grid.SetColumn(usr, 3);
                            Grid.SetRow(usr, 1);

                            grid.Children.Add(usr);

                            TextBlock time = new TextBlock
                            {
                                Text = "Time"
                            };
                            Grid.SetColumn(time, 5);
                            Grid.SetRow(time, 1);

                            grid.Children.Add(time);

                            TextBlock proper = new TextBlock
                            {
                                Text = "ProperTime"
                            };
                            Grid.SetColumn(proper, 7);
                            Grid.SetRow(proper, 1);

                            grid.Children.Add(proper);

                            lbi.IsEnabled = false;

                        }

                        #endregion


                        foreach (var record in combined)
                        {

                            ListBoxItem lbi = new ListBoxItem();

                            ListBoxItems.Add(lbi);

                            Grid grid = new Grid();

                            lbi.Content = grid;

                            grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(60, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(40, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(80, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                            grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));

                            grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));
                            grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                            grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));

                            TextBlock id = new TextBlock
                            {
                                Text = record.ActivityID
                            };
                            Grid.SetColumn(id, 1);
                            Grid.SetRow(id, 1);

                            grid.Children.Add(id);

                            TextBlock usr = new TextBlock
                            {
                                Text = record.User
                            };
                            Grid.SetColumn(usr, 3);
                            Grid.SetRow(usr, 1);

                            grid.Children.Add(usr);

                            TextBlock time = new TextBlock
                            {
                                Text = record.Time.ToString()
                            };
                            Grid.SetColumn(time, 5);
                            Grid.SetRow(time, 1);

                            grid.Children.Add(time);

                            TextBlock proper = new TextBlock
                            {
                                Text = record.ProperTime.ToString()
                            };
                            Grid.SetColumn(proper, 7);
                            Grid.SetRow(proper, 1);

                            grid.Children.Add(proper);
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            };

            TimestampPicker.SelectedDate = DateTime.Now;
        }

        private void LoadXML()
        {
            if (File.Exists("Settings.xml"))
            {
                string text = File.ReadAllText("Settings.xml");
                XElement root = XElement.Parse(text);
                XElement activities = root.Element("Activities");

                foreach(XElement activity in activities.Elements("Activity"))
                {
                    string val = activity.Element("Number").Value;
                    TimeSpan time = DateTime.Parse(activity.Element("Time").Value).TimeOfDay;
                    Activities.Add(val, time);
                }
            }
            else
            {
                XElement root = new XElement("Settings");

                XElement activities = new XElement("Activities");

                root.Add(activities);

                XElement activity = new XElement("Activity");

                activities.Add(activity);

                activity.Add(new XElement("Number", "1a"));

                activity.Add(new XElement("Time", "00:01:00"));

                #region Save XML file
                FileStream settingsFile = File.OpenWrite("Settings.xml");
                XmlWriter xmlWriter = XmlWriter.Create(settingsFile, new XmlWriterSettings() { Indent = true });
                root.WriteTo(xmlWriter);
                xmlWriter.Flush();
                xmlWriter.Close();
                #endregion
            }
        }
    }
}
