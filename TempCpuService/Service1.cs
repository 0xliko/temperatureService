using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OpenHardwareMonitor.Hardware;
using System.Timers;
using System.Net;
using System.Net.Mail;
namespace TempCpuService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            this.ServiceName = "TempService";
            this.EventLog.Source = "TempService";
            this.EventLog.Log = "Application";

            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;
            if (!EventLog.SourceExists("TempService"))
                EventLog.CreateEventSource("TempService", "Application");

            InitializeComponent();
        }
        protected void Email(string htmlString)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress("from email");
                message.To.Add(new MailAddress("Jake10.97@hotmail.com")); //""
                message.Subject = "temperature exceed";
                message.IsBodyHtml = false; //to make message body as html  
                message.Body = htmlString;
                smtp.Port = 587;
                smtp.Host = "smtp.gmail.com"; //for gmail host  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential("from email", "password");
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
            }
            catch (Exception e) {
                File.AppendAllText("c:\\logs\\service-log.txt", $"mail send issue{e.Message}");
            }
        }
        protected override void OnStart(string[] args)
        {
            Computer computer = new Computer() { CPUEnabled = true, GPUEnabled = true };
            computer.Open();

            Timer timer = new Timer() { Enabled = true, Interval = 1000 };
            float[] limits = { 43,43 };
            //File.AppendAllText("c:\\logs\\service-log.txt", $"{args[0]}\n");
            float[][] curTemps = new float[2][];
            curTemps[0] = new float[8];
            curTemps[1] = new float[8];
            for(int i = 0; i < 8; i++)
            {
                curTemps[0][i] = limits[0];
                curTemps[1][i] = limits[1];
            }
            timer.Elapsed += delegate (object sender, ElapsedEventArgs e)
            {


                //File.AppendAllText("c:\\logs\\service-log.txt", $"{DateTime.Now}\n");
                int i = 0;
                foreach (IHardware hardware in computer.Hardware)
                {
                    hardware.Update();

                    int j = 0;
                    //File.AppendAllText("c:\\logs\\service-log.txt", $"{hardware.HardwareType}: {hardware.Name}\n");
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        // Celsius is default unit
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            if(curTemps[i][j] <= limits[i] &&  sensor.Value > limits[i])
                            {
                                // send mail
                                String message = $"current {sensor.Name}'s temperature {sensor.Value} exceeded than  {limits[i]}°C\n";
                                Email(message);
                                File.AppendAllText("c:\\logs\\service-log.txt",message );
                                 
                            }
                            curTemps[i][j] = (float)sensor.Value;
                            //File.AppendAllText("c:\\logs\\service-log.txt", $"{sensor.Name}: {sensor.Value}°C\n");
                            
                        }
                        j++;

                    }
                    i++;
                    
                }
               
            };
            File.AppendAllText("c:\\logs\\service-log.txt", "started\n");
        }

        protected override void OnStop()
        {
            File.AppendAllText("c:\\logs\\service-log.txt", "stop\n");
        }
    }


}
