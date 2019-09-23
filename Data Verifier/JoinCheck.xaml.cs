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
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));
                grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                grid.RowDefinitions.Add(new RowDefinition(12, GridUnitType.Pixel));

                int Column = 1;
                int Row = 1;
                int MaxColumn = 23;

                void AddText(string text)
                {
                    TextBlock txt = new TextBlock
                    {
                        Text = text
                    };
                    Grid.SetColumn(txt, Column);
                    Grid.SetRow(txt, Row);

                    grid.Children.Add(txt);

                    Column += 2;

                    if (Column > MaxColumn)
                    {
                        Row += 2;
                        Column = 1;
                    }
                }

                AddText(item.A_ID);
                AddText(item.A_Key);
                AddText(item.StartTime);
                AddText(item.EndTime);
                AddText(item.TotalTime);
                AddText(item.F_Name);
                AddText(item.F_MacAddress);
                AddText(item.F_RSSI);
                AddText(item.SD_X);
                AddText(item.SD_Y);
                AddText(item.SD_Z);
                AddText(item.SD_StartTime);
                AddText(item.ZG_HeartRate);
                AddText(item.ZG_RespirationRate);
                AddText(item.ZG_SkinTemperature);
                AddText(item.ZG_Posture);
                AddText(item.ZG_Activity);
                AddText(item.ZG_PeakAcceleration);
                AddText(item.ZG_BreathingWaveAmplitude);
                AddText(item.ZG_EcgAmplitude);
                AddText(item.ZG_EcgNoise);
                AddText(item.ZG_VerticalAxisAccelerationMin);
                AddText(item.ZG_VerticalAxisAccelerationPeak);
                AddText(item.ZG_LateralAxisAccelerationMin);
                AddText(item.ZG_LateralAxisAccelerationPeak);
                AddText(item.ZG_SagittalAxisAccelerationMin);
                AddText(item.ZG_SagittalAxisAccelerationPeak);
                AddText(item.ZG_Gsr);
                AddText(item.ZG_Rog);
                AddText(item.ZR_RToRSample0);
                AddText(item.ZR_RToRSample1);
                AddText(item.ZR_RToRSample2);
                AddText(item.ZR_RToRSample3);
                AddText(item.ZR_RToRSample4);
                AddText(item.ZR_RToRSample5);
                AddText(item.ZR_RToRSample6);
                AddText(item.ZR_RToRSample7);
                AddText(item.ZR_RToRSample8);
                AddText(item.ZR_RToRSample9);
                AddText(item.ZR_RToRSample10);
                AddText(item.ZR_RToRSample11);
                AddText(item.ZR_RToRSample12);
                AddText(item.ZR_RToRSample13);
                AddText(item.ZR_RToRSample14);
                AddText(item.ZR_RToRSample15);
                AddText(item.ZR_RToRSample16);
                AddText(item.ZR_RToRSample17);
                AddText(item.ZR_FinalRtoRSample);
                AddText(item.ZS_HeartRate);
                AddText(item.ZS_RespirationRate);
                AddText(item.ZS_SkinTemperature);
                AddText(item.ZS_Posture);
                AddText(item.ZS_Activity);
                AddText(item.ZS_PeakAcceleration);
                AddText(item.ZS_BatteryVoltage);
                AddText(item.ZS_BatteryLevel);
                AddText(item.ZS_BreathingWaveAmplitude);
                AddText(item.ZS_BreathingWaveNoise);
                AddText(item.ZS_BreathingRateConfidence);
                AddText(item.ZS_EcgAmplitude);
                AddText(item.ZS_EcgNoise);
                AddText(item.ZS_HeartRateConfidence);
                AddText(item.ZS_HeartRateVariability);
                AddText(item.ZS_SystemConfidence);
                AddText(item.ZS_Gsr);
                AddText(item.ZS_Rog);
                AddText(item.ZS_VerticalAxisAccelerationMin);
                AddText(item.ZS_VerticalAxisAccelerationPeak);
                AddText(item.ZS_LateralAxisAccelerationMin);
                AddText(item.ZS_LateralAxisAccelerationPeak);
                AddText(item.ZS_SagittalAxisAccelerationMin);
                AddText(item.ZS_SagittalAxisAccelerationPeak);
                AddText(item.ZS_DeviceInternalTemp);
                AddText(item.ZS_StatusInfo);
                AddText(item.ZS_LinkQuality);
                AddText(item.ZS_Rssi);
                AddText(item.ZS_TxPower);
                AddText(item.ZS_EstimatedCoreTemperature);
                AddText(item.ZS_AuxiliaryChannel1);
                AddText(item.ZS_AuxiliaryChannel2);
                AddText(item.ZS_AuxiliaryChannel3);
                AddText(item.ZS_Reserved);
            }
        }
    }
}
