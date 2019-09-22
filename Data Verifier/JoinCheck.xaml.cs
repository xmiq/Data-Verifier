using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace DataVerifier
{
    public class JoinCheck : Window
    {
        public JoinCheck(SQLiteConnection connection, CombinedActivity record)
        {
            _connection = connection;
            _record = record;
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        NumericUpDown Page;
        TextBlock Max;
        Task Load;
        CancellationTokenSource cts;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Button>("FullCheck").Click += async (sender, e) =>
            {
                if ((Load?.Status ?? TaskStatus.Created) == TaskStatus.Running)
                {
                    cts.Cancel();
                }
                else
                {
                    cts = new CancellationTokenSource();

                    Button FC = sender as Button;
                    FC.Content = "Stop Full Check";
                    Load = Task.Run(LoadFullCheck, cts.Token);


                    Page.Value = 0;
                    Page.GetType()?.GetField("EventHandlers")?.SetValue(Page, null);
                    Page.ValueChanged += (sender2, e2) => ChangeList();

                    await Task.Delay(200);

                    ChangeList();

                    await Load;

                    FC.Content = "Full Check";
                }
            };
            Page = this.FindControl<NumericUpDown>("Page");
            Max = this.FindControl<TextBlock>("Max");
        }

        SQLiteConnection _connection;
        CombinedActivity _record;


        ArrayList FullCheckData = ArrayList.Synchronized(new ArrayList());

        private void LoadFullCheck()
        {
            _connection.Open();
            try
            {
                SQLiteCommand command = _connection.CreateCommand();

                command.CommandText = @"
                    SELECT a.id, a.key, datetime(a.startTime/1000, 'unixepoch') AS startTimeFormatted, datetime(a.endTime/1000, 'unixepoch') AS endTimeFormatted, time(a.totalTime/1000, 'unixepoch') AS totalTimeFormatted, f.name, f.macAddress, f.rssi, sd.x, sd.y, sd.z,  datetime(sd.timestamp/1000, 'unixepoch') AS sdStartTimeFormatted, 
                           zg.heartRate, zg.respirationRate, zg.skinTemperature, zg.posture, zg.activity, zg.peakAcceleration, zg.breathingWaveAmplitude, zg.ecgAmplitude, zg.ecgNoise, zg.verticalAxisAccelerationMin, zg.verticalAxisAccelerationPeak, zg.lateralAxisAccelerationMin, zg.lateralAxisAccelerationPeak, zg.sagittalAxisAccelerationMin, zg.sagittalAxisAccelerationPeak, zg.gsr, zg.rog,
	                       zr.rToRSample0, zr.rToRSample1, zr.rToRSample2, zr.rToRSample3, zr.rToRSample4, zr.rToRSample5, zr.rToRSample6, zr.rToRSample7, zr.rToRSample8, zr.rToRSample9, zr.rToRSample10, zr.rToRSample11, zr.rToRSample12, zr.rToRSample13, zr.rToRSample14, zr.rToRSample15, zr.rToRSample16, zr.rToRSample17, zr.finalRtoRSample,
	                       zs.heartRate, zs.respirationRate, zs.skinTemperature, zs.posture, zs.activity, zs.peakAcceleration, zs.batteryVoltage, zs.batteryLevel, zs.breathingWaveAmplitude, zs.breathingWaveNoise, zs.breathingRateConfidence, zs.ecgAmplitude, zs.ecgNoise, zs.heartRateConfidence, zs.heartRateVariability, zs.systemConfidence,
                           zs.gsr, zs.rog, zs.verticalAxisAccelerationMin, zs.verticalAxisAccelerationPeak, zs.lateralAxisAccelerationMin, zs.lateralAxisAccelerationPeak, zs.sagittalAxisAccelerationMin, zs.sagittalAxisAccelerationPeak, zs.deviceInternalTemp, zs.statusInfo, zs.linkQuality, zs.rssi, zs.txPower, zs.estimatedCoreTemperature,
                           zs.auxiliaryChannel1, zs.auxiliaryChannel2, zs.auxiliaryChannel3, zs.reserved
                    FROM activity a
                    LEFT JOIN fingerprint f ON a.id = f.activityId
                    LEFT JOIN sensor_data sd ON sd.timestamp = f.timestamp OR sd.timestamp BETWEEN a.startTime AND a.endTime
                    LEFT JOIN zephyr_general zg on datetime(zg.logged - 7, 'unixepoch') = datetime(sd.timestamp / 1000, 'unixepoch')
                    LEFT JOIN zephyr_rr zr on zg.logged = zr.logged
                    LEFT JOIN zephyr_summary zs on zg.logged = zs.logged
                    WHERE a.id = @id";

                command.Parameters.AddWithValue("@id", _record.ID);

                IEnumerable<IDataRecord> records = command.ExecuteReader().OfType<IDataRecord>();

                foreach (IDataRecord record in records)
                {
                    if (cts.Token.IsCancellationRequested)
                        break;
                    FullCheckData.Add(new FullCheck
                    {
                        A_ID = record["id"].ToString(),
                        A_Key = record["key"].ToString(),
                        StartTime = record["startTimeFormatted"].ToString(),
                        EndTime = record["endTimeFormatted"].ToString(),
                        TotalTime = record["totalTimeFormatted"].ToString(),
                        F_Name = record["name"].ToString(),
                        F_MacAddress = record["macAddress"].ToString(),
                        F_RSSI = record["rssi"].ToString(),
                        SD_X = record["x"].ToString(),
                        SD_Y = record["y"].ToString(),
                        SD_Z = record["z"].ToString(),
                        SD_StartTime = record["sdStartTimeFormatted"].ToString(),
                        ZG_HeartRate = record["heartRate"].ToString(),
                        ZG_RespirationRate = record["respirationRate"].ToString(),
                        ZG_SkinTemperature = record["skinTemperature"].ToString(),
                        ZG_Posture = record["posture"].ToString(),
                        ZG_Activity = record["activity"].ToString(),
                        ZG_PeakAcceleration = record["peakAcceleration"].ToString(),
                        ZG_BreathingWaveAmplitude = record["breathingWaveAmplitude"].ToString(),
                        ZG_EcgAmplitude = record["ecgAmplitude"].ToString(),
                        ZG_EcgNoise = record["ecgNoise"].ToString(),
                        ZG_VerticalAxisAccelerationMin = record["verticalAxisAccelerationMin"].ToString(),
                        ZG_VerticalAxisAccelerationPeak = record["verticalAxisAccelerationPeak"].ToString(),
                        ZG_LateralAxisAccelerationMin = record["lateralAxisAccelerationMin"].ToString(),
                        ZG_LateralAxisAccelerationPeak = record["lateralAxisAccelerationPeak"].ToString(),
                        ZG_SagittalAxisAccelerationMin = record["sagittalAxisAccelerationMin"].ToString(),
                        ZG_SagittalAxisAccelerationPeak = record["sagittalAxisAccelerationPeak"].ToString(),
                        ZG_Gsr = record["gsr"].ToString(),
                        ZG_Rog = record["rog"].ToString(),
                        ZR_RToRSample0 = record["rToRSample0"].ToString(),
                        ZR_RToRSample1 = record["rToRSample1"].ToString(),
                        ZR_RToRSample2 = record["rToRSample2"].ToString(),
                        ZR_RToRSample3 = record["rToRSample3"].ToString(),
                        ZR_RToRSample4 = record["rToRSample4"].ToString(),
                        ZR_RToRSample5 = record["rToRSample5"].ToString(),
                        ZR_RToRSample6 = record["rToRSample6"].ToString(),
                        ZR_RToRSample7 = record["rToRSample7"].ToString(),
                        ZR_RToRSample8 = record["rToRSample8"].ToString(),
                        ZR_RToRSample9 = record["rToRSample9"].ToString(),
                        ZR_RToRSample10 = record["rToRSample10"].ToString(),
                        ZR_RToRSample11 = record["rToRSample11"].ToString(),
                        ZR_RToRSample12 = record["rToRSample12"].ToString(),
                        ZR_RToRSample13 = record["rToRSample13"].ToString(),
                        ZR_RToRSample14 = record["rToRSample14"].ToString(),
                        ZR_RToRSample15 = record["rToRSample15"].ToString(),
                        ZR_RToRSample16 = record["rToRSample16"].ToString(),
                        ZR_RToRSample17 = record["rToRSample17"].ToString(),
                        ZR_FinalRtoRSample = record["finalRtoRSample"].ToString(),
                        ZS_HeartRate = record["heartRate"].ToString(),
                        ZS_RespirationRate = record["respirationRate"].ToString(),
                        ZS_SkinTemperature = record["skinTemperature"].ToString(),
                        ZS_Posture = record["posture"].ToString(),
                        ZS_Activity = record["activity"].ToString(),
                        ZS_PeakAcceleration = record["peakAcceleration"].ToString(),
                        ZS_BatteryVoltage = record["batteryVoltage"].ToString(),
                        ZS_BatteryLevel = record["batteryLevel"].ToString(),
                        ZS_BreathingWaveAmplitude = record["breathingWaveAmplitude"].ToString(),
                        ZS_BreathingWaveNoise = record["breathingWaveNoise"].ToString(),
                        ZS_BreathingRateConfidence = record["breathingRateConfidence"].ToString(),
                        ZS_EcgAmplitude = record["ecgAmplitude"].ToString(),
                        ZS_EcgNoise = record["ecgNoise"].ToString(),
                        ZS_HeartRateConfidence = record["heartRateConfidence"].ToString(),
                        ZS_HeartRateVariability = record["heartRateVariability"].ToString(),
                        ZS_SystemConfidence = record["systemConfidence"].ToString(),
                        ZS_Gsr = record["gsr"].ToString(),
                        ZS_Rog = record["rog"].ToString(),
                        ZS_VerticalAxisAccelerationMin = record["verticalAxisAccelerationMin"].ToString(),
                        ZS_VerticalAxisAccelerationPeak = record["verticalAxisAccelerationPeak"].ToString(),
                        ZS_LateralAxisAccelerationMin = record["lateralAxisAccelerationMin"].ToString(),
                        ZS_LateralAxisAccelerationPeak = record["lateralAxisAccelerationPeak"].ToString(),
                        ZS_SagittalAxisAccelerationMin = record["sagittalAxisAccelerationMin"].ToString(),
                        ZS_SagittalAxisAccelerationPeak = record["sagittalAxisAccelerationPeak"].ToString(),
                        ZS_DeviceInternalTemp = record["deviceInternalTemp"].ToString(),
                        ZS_StatusInfo = record["statusInfo"].ToString(),
                        ZS_LinkQuality = record["linkQuality"].ToString(),
                        ZS_Rssi = record["rssi"].ToString(),
                        ZS_TxPower = record["txPower"].ToString(),
                        ZS_EstimatedCoreTemperature = record["estimatedCoreTemperature"].ToString(),
                        ZS_AuxiliaryChannel1 = record["auxiliaryChannel1"].ToString(),
                        ZS_AuxiliaryChannel2 = record["auxiliaryChannel2"].ToString(),
                        ZS_AuxiliaryChannel3 = record["auxiliaryChannel3"].ToString(),
                        ZS_Reserved = record["reserved"].ToString()
                    });
                }
            }
            finally
            {
                _connection.Close();
            }
        }

        private void ChangeList()
        {
            IList resultList = this.FindControl<ListBox>("Results").Items as IList;
            resultList.Clear();
            GC.Collect();
            IEnumerable<FullCheck> ConvertedList = ((ArrayList)FullCheckData.Clone()).OfType<FullCheck>();
            Page.Maximum = (ConvertedList.Count() / 20);
            Max.Text = Page.Maximum.ToString();

            FullCheck header = new FullCheck();
            foreach (System.Reflection.PropertyInfo prop in header.GetType().GetProperties())
            {
                prop.SetValue(header, prop.Name);
            }

            foreach (FullCheck item in new[] { header }.Concat(ConvertedList.Skip((Convert.ToInt32(Page.Value) - 1) * 20).Take(20)))
            {
                ListBoxItem lbi = new ListBoxItem();

                resultList.Add(lbi);

                Grid grid = new Grid();

                lbi.Content = grid;

                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(30, GridUnitType.Pixel)); //1
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(30, GridUnitType.Pixel)); //3
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(120, GridUnitType.Pixel)); //5
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(120, GridUnitType.Pixel)); //7
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(120, GridUnitType.Pixel)); //9
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(120, GridUnitType.Pixel)); //11
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(120, GridUnitType.Pixel)); //13
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(30, GridUnitType.Pixel)); //15
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(120, GridUnitType.Pixel)); //17
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(120, GridUnitType.Pixel)); //19
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(120, GridUnitType.Pixel)); //21
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto)); //23
                grid.ColumnDefinitions.Add(new ColumnDefinition(12, GridUnitType.Pixel));

                grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));

                TextBlock id = new TextBlock
                {
                    Text = item.A_ID
                };
                Grid.SetColumn(id, 1);
                Grid.SetRow(id, 1);

                grid.Children.Add(id);

                TextBlock key = new TextBlock
                {
                    Text = item.A_Key
                };
                Grid.SetColumn(key, 3);
                Grid.SetRow(key, 1);

                grid.Children.Add(key);

                TextBlock start = new TextBlock
                {
                    Text = item.StartTime
                };
                Grid.SetColumn(start, 5);
                Grid.SetRow(start, 1);

                grid.Children.Add(start);

                TextBlock end = new TextBlock
                {
                    Text = item.EndTime
                };
                Grid.SetColumn(end, 7);
                Grid.SetRow(end, 1);

                grid.Children.Add(end);

                TextBlock total = new TextBlock
                {
                    Text = item.TotalTime
                };
                Grid.SetColumn(total, 9);
                Grid.SetRow(total, 1);

                grid.Children.Add(total);

                TextBlock name = new TextBlock
                {
                    Text = item.F_Name
                };
                Grid.SetColumn(name, 11);
                Grid.SetRow(name, 1);

                grid.Children.Add(name);

                TextBlock mac = new TextBlock
                {
                    Text = item.F_MacAddress
                };
                Grid.SetColumn(mac, 13);
                Grid.SetRow(mac, 1);

                grid.Children.Add(mac);

                TextBlock rssi = new TextBlock
                {
                    Text = item.F_RSSI
                };
                Grid.SetColumn(rssi, 15);
                Grid.SetRow(rssi, 1);

                grid.Children.Add(rssi);

                TextBlock x = new TextBlock
                {
                    Text = item.SD_X
                };
                Grid.SetColumn(x, 17);
                Grid.SetRow(x, 1);

                grid.Children.Add(x);

                TextBlock y = new TextBlock
                {
                    Text = item.SD_Y
                };
                Grid.SetColumn(y, 19);
                Grid.SetRow(y, 1);

                grid.Children.Add(y);

                TextBlock z = new TextBlock
                {
                    Text = item.SD_Z
                };
                Grid.SetColumn(z, 21);
                Grid.SetRow(z, 1);

                grid.Children.Add(z);

                TextBlock sdStart = new TextBlock
                {
                    Text = item.SD_StartTime
                };
                Grid.SetColumn(sdStart, 23);
                Grid.SetRow(sdStart, 1);

                grid.Children.Add(sdStart);

                TextBlock heart = new TextBlock
                {
                    Text = item.ZG_HeartRate
                };
                Grid.SetColumn(heart, 1);
                Grid.SetRow(heart, 3);

                grid.Children.Add(heart);
            }
        }
    }
}
