﻿using Broadlink.NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Broadlink.WinApp.Forms
{
    public partial class MainForm : Form
    {
        #region Fields
        private RMDevice RMDevice;
        private List<Command> Commands;
        private Client DiscoverClient;
        string CommandFilePath => Path.GetFullPath("Komutlar.json");
        #endregion
        #region Main Form
        public MainForm()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            HelperMy.NotificationEvent += HelperMy_NotificationEvent;
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            RMDevice?.Dispose();
            DiscoverClient?.Dispose();
            base.OnClosing(e);
        }
        #endregion
        #region Forms Button Events
        private void btnLogClean_Click(object sender, EventArgs e)// => txtLog.Clear();
        {
            txtLog.Clear();
            //BroadlinkDevice.SetupAsync("modem_ssid", "modem_password", 4);
        }
        private async void btnConnect_Click(object senderBtn, EventArgs eBtn)
        {
            btnConnect.Enabled = false;
            if (cmbDevices.Items.Count > 0)
            {
                try
                {
                    DiscoverClient.Dispose();
                    DiscoverClient = null;
                }
                catch (Exception)
                {

                }
            }
            if (cmbDevices.Items.Count == 0)
            {
                if (DiscoverClient == null)
                {
                    DiscoverClient = new Client();
                    DiscoverClient.DeviceHandler += Client_DeviceHandler;
                }
                HelperMy.Notification(Color.Gray, "Cihazlar aranıyor...");
                await DiscoverClient.DiscoverAsync();
                btnConnect.Text = "Tarama Yap";
            }
            else if (cmbDevices.SelectedItem is RMDevice secim)
            {
                HelperMy.Notification(Color.Gray, "Bağlantı kuruluyor...");
                RMDevice = secim as RMDevice;
                if (!RMDevice.IsEventsReady)
                {
                    RMDevice.OnDeviceReady += RMDevice_OnDeviceReady;
                    RMDevice.OnTemperature += RMDevice_OnTemperature;
                    RMDevice.OnRawData += RMDevice_OnRawData;
                    RMDevice.OnRawRFDataFirst += RMDevice_OnRawRFDataFirst;
                    RMDevice.OnRawRFDataSecond += RMDevice_OnRawRFDataSecond;
                    RMDevice.OnSentDataCallback += RMDevice_OnSentDataCallback;
                    RMDevice.IsEventsReady = true;
                }
                await RMDevice.AuthorizeAsync();
                KomutYukle();
                btnMenuOgren.Enabled = cmbKomutListe.Enabled = btnIR_Learn.Enabled = btnRF_Learn.Enabled = btnLearnCancel.Enabled = btnKomutGonder.Enabled = btnKomutlariKaydet.Enabled = btnIceAktar.Enabled = true;
                btnConnect.Text = "Bağlanıldı";
            }
            else
                HelperMy.Notification(Color.Red, "Yazılıma uyumlu cihaz tespit edilemedi!");

            btnConnect.Enabled = true;
        }
        private async void btnIR_Learn_Click(object sender, EventArgs e)
        {
            if (RMDevice == null) return;
            btnIR_Learn.Enabled = false;
            await RMDevice.EnterIRLearningModeAsync();
            HelperMy.Notification(Color.RoyalBlue, "IR Kızılötesi Öğrenme modu etkinleştirildi.");
        }
        private async void btnRF_Learn_Click(object sender, EventArgs e)
        {
            if (RMDevice == null) return;
            btnRF_Learn.Enabled = false;
            await RMDevice.EnterRFLearningModeAsync();
            HelperMy.Notification(Color.RoyalBlue, "RF Frekans Öğrenme modu etkinleştirildi.");
            HelperMy.Notification(Color.Yellow, "RF Frekans Tarama [1/2]");
            HelperMy.Notification(Color.Yellow, "[Düğmeye basılı tutunuz!]");
        }
        private async void btnLearnCancel_Click(object sender, EventArgs e)
        {
            if (RMDevice == null) return;
            btnIR_Learn.Enabled = btnRF_Learn.Enabled = true;
            await RMDevice.ExitLearningModeAsync();
            HelperMy.Notification(Color.RoyalBlue, "Öğrenme modundan çıkıldı.");
        }
        private async void btnKomutGonder_Click(object sender, EventArgs e)
        {
            var selected = cmbKomutListe.SelectedItem;
            if (selected == null || RMDevice == null) return;
            var command = (selected as Command).Code.HexToBytes();
            if (txtIRCount.Text.IsNumeric() && txtIRCount.Text != "1")
                command[1] = (byte)(Convert.ToByte(txtIRCount.Text) - 1);
            await RMDevice.SendRemoteCommandAsync(command);
            HelperMy.Notification(Color.White, "Komut gönderildi : {0}", selected);
        }
        private void btnKomutlariKaydet_Click(object sender, EventArgs e) => File.WriteAllText(CommandFilePath, Commands.ToJson(), new System.Text.UTF8Encoding(false));
        private async void timerSicaklik_Tick(object sender, EventArgs e) => await RMDevice?.GetTemperatureAsync();
        private void btnIceAktar_Click(object sender, EventArgs e)
        {
            HelperMy.Notification(Color.RoyalBlue, "Broadlink eControl uygulamasındaki verileri içe aktarma");
            HelperMy.Notification(Color.White, "1) Uygulamadaki menüden 'Paylaş' butonuna tıklayınız.");
            HelperMy.Notification(Color.White, "2) 'Ağdaki başka bir telefon ile paylaşın' butonuna tıklayınız.");
            HelperMy.Notification(Color.White, "3) 'İptal' butonuna tıklayınız.");
            HelperMy.Notification(Color.White, "4) Telefon hafızasında bulunan aşağıdaki dosyaları bilgisayarınıza kopyalayınız.");
            HelperMy.Notification(Color.White, "\tKlasör : /broadlink/newremote/SharedData");
            HelperMy.Notification(Color.White, "\t* jsonDevice");
            HelperMy.Notification(Color.White, "\t* jsonButton");
            HelperMy.Notification(Color.White, "\t* jsonIrCode");
            HelperMy.Notification(Color.White, "\t* jsonSubIr");
            var model = NET.SharedData.CodeInfo.GetSharedData();
            if (model == null || model.Length == 0)
            {
                HelperMy.Notification(Color.Red, "Veri bulunamadı!");
                return;
            }
            if (Commands == null)
                Commands = new List<Command>();
            foreach (var item in model)
                if (Commands.FirstOrDefault(c => c.ID == item.Id.ToString()) is Command cmd)
                {
                    cmd.Name = item.ToString();
                    cmd.Code = item.Code;
                }
                else
                    Commands.Add(new Command
                    {
                        ID = item.Id.ToString(),
                        Key = item.ToString().FriendlyUrl(),
                        Name = item.ToString(),
                        Code = item.Code
                    });
            RefreshCmbKomutListe();
        }
        #endregion
        #region Broadlink.NET Events
        private void HelperMy_NotificationEvent(object sender, (Color Color, string Message, object[] FormatStringArgs) e) => txtLog.MaybeInvoke(() => txtLog.AppendLine(e.Color, e.Message, e.FormatStringArgs));
        private void Client_DeviceHandler(object sender, BroadlinkDevice device)
        {
            this.MaybeInvoke(() =>
            {
                if (!cmbDevices.Items.Cast<object>().Any(i => (i is RMDevice secim && secim.EndPoint.Address == device.EndPoint.Address)))
                {
                    HelperMy.Notification(Color.Lime, "Cihaz bulundu : {0}", device);
                    cmbDevices.Items.Add(device);
                    cmbDevices.SelectedIndex = 0;
                }
                btnConnect.Text = "Bağlan";
                btnConnect_Click(null, null);
            });
        }
        private void RMDevice_OnRawData(object sender, byte[] data)
        {
            this.MaybeInvoke(() =>
            {
                txtLog.AppendLine(Color.Lime, "OnRawData : {0}", data.ByteToHex());
                btnIR_Learn.Enabled = btnRF_Learn.Enabled = true;
                KomutEkle(data.ByteToHex());
            });
        }
        private void RMDevice_OnRawRFDataFirst(object sender, byte[] data)
        {
            //HelperMy.Bildirim(Color.Blue, "OnRawRFData : {0}", Convert.ToBase64String(data));
        }
        private void RMDevice_OnRawRFDataSecond(object sender, byte[] data)
        {
            HelperMy.Notification(Color.Yellow, "RF Frekans Tarama [2/2]");
            HelperMy.Notification(Color.Yellow, "[Aralarında bir duraklama ile RF düğmesine birden çok kez basın]");
        }
        private void RMDevice_OnTemperature(object sender, float temperature)
        {
            this.MaybeInvoke(() =>
            {
                //HelperMy.Bildirim(Color.Yellow, "OnTemperature : {0}", temperature);
                if (Tag != null)
                    Text = string.Format(Tag.ToString(), temperature);
            });
        }
        private async void RMDevice_OnDeviceReady(object sender, EventArgs e)
        {
            this.MaybeInvoke(() =>
            {
                txtLog.AppendLine(Color.Lime, "Cihaz kullanıma hazır.");

                txtLog.AppendLine(Color.WhiteSmoke, new string('-', 34));
                txtLog.AppendText(Color.LightGreen, "IP Adresi\t: ");
                txtLog.AppendLine(Color.Aqua, RMDevice.EndPoint.Address.ToString());

                txtLog.AppendText(Color.LightGreen, "Port\t\t: ");
                txtLog.AppendLine(Color.Aqua, RMDevice.EndPoint.Port.ToString());

                txtLog.AppendText(Color.LightGreen, "MAC Adresi\t: ");
                txtLog.AppendLine(Color.Aqua, RMDevice.MacAddressStr);
                txtLog.AppendLine(Color.WhiteSmoke, new string('-', 34));
                timerSicaklik.Start();
            });
            await RMDevice.GetTemperatureAsync();
        }
        private void RMDevice_OnSentDataCallback(object sender, byte[] payload)
        {
            var cmd = Commands.FirstOrDefault(item => item.Code.HexToBytes().BytesContains(payload));
            if (cmd == null) return;
            HelperMy.Notification(Color.Lime, $"[Callback] {cmd.Name}");
        }
        #endregion
        #region Helper Methods
        private void KomutYukle()
        {
            Commands = File.Exists(CommandFilePath) ? File.ReadAllText(CommandFilePath, new System.Text.UTF8Encoding(false)).FromJson<List<Command>>().OrderBy(i => i.Name).ToList() ?? new List<Command>() : new List<Command>();
            RefreshCmbKomutListe();
        }
        private void KomutEkle(string value)
        {
            var inputBox = Microsoft.VisualBasic.Interaction.InputBox("Komut başlığı", "İsim giriniz :");
            if (inputBox.IsNullOrEmptyTrim()) return;
            var cmd = new Command
            {
                ID = Guid.NewGuid().ToString(),
                Key = inputBox.FriendlyUrl(),
                Name = inputBox,
                Code = value
            };
            Commands.Add(cmd);
            RefreshCmbKomutListe();
        }
        private void RefreshCmbKomutListe()
        {
            cmbKomutListe.Items.Clear();
            if (Commands == null || Commands.Count == 0) return;
            foreach (var item in Commands)
                cmbKomutListe.Items.Add(item);
            if (cmbKomutListe.Items.Count > 0)
                cmbKomutListe.SelectedIndex = 0;
        }
        #endregion
    }
}
