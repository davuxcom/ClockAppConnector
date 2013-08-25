using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClockAppConnector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            new Thread(() =>
            {
                int coreCount = 0;
                foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
                {
                    coreCount += int.Parse(item["NumberOfLogicalProcessors"].ToString());
                }

                long totalMemory = 0;
                foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
                {
                    totalMemory += long.Parse(item["TotalPhysicalMemory"].ToString());
                }

                PerformanceCounter[] pc = new PerformanceCounter[coreCount];

                for (int i = 0; i < coreCount; i++)
                {
                    pc[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                    Console.WriteLine(pc[i].CounterName);
                }

                var allcpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");

                PerformanceCounter availmem = 
                   new PerformanceCounter("Memory", "Available MBytes");

                // Access is denied =>
                // netsh http add urlacl url=http://+:9494/ user=xeon\dave
                var listener = new HttpListener();
                listener.Prefixes.Add(@"http://+:9494/");
                listener.Start();

                while (listener.IsListening)
                {
                    try
                    {
                        var context = listener.GetContext();
                        var wr = new System.IO.StreamWriter(context.Response.OutputStream);



                        var objs = "";

                        foreach (var p in pc)
                        {
                            objs += "<cpu>" + (int)p.NextValue() + "%</cpu>";
                        }

                        objs += "<allcpu>" + (int)allcpu.NextValue() + "</allcpu>";
                        objs += "<memory total=\"" + totalMemory + "\">" + (int)availmem.NextValue() + "</memory>";

                        var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\" ?><x>" + objs + "</x>";

                        wr.WriteLine(xml);
                        wr.Close();
                    }
                    catch (Exception ex) { Debug.Write(ex); }
                }

            }).Start();
        }

        private void ListenerCallback(IAsyncResult ar)
        {
            var listener = ar.AsyncState as HttpListener;

            var context = listener.EndGetContext(ar);
            context.Response.StatusCode = 200;
            //do some stuff
        }
    }
}
