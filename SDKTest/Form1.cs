using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SDKTest
{
    public partial class Form1 : Form
    {
    //    [DllImport("MdcProxy.dll", EntryPoint = "Set_Login_Info", CallingConvention = CallingConvention.Cdecl)]
    //    public static extern void Set_Login_Info([MarshalAs(UnmanagedType.LPStr)] string CSserverIP, [MarshalAs(UnmanagedType.LPStr)] string localIP, [MarshalAs(UnmanagedType.LPStr)] string userName, [MarshalAs(UnmanagedType.LPStr)] string passWord, [MarshalAs(UnmanagedType.LPStr)] string sipPort);

    //    [DllImport("MdcProxy.dll", EntryPoint = "SDK_START", CallingConvention = CallingConvention.Cdecl)]
    //    public static extern int SDK_START();

    //    [DllImport("MdcProxy.dll", EntryPoint = "CSharp_Call", CallingConvention = CallingConvention.Cdecl)]
    //    public static extern void CSharp_Call(
    //        [MarshalAs(UnmanagedType.LPStr)] string Sip,
    // [MarshalAs(UnmanagedType.LPStr)] string IP,
    //[MarshalAs(UnmanagedType.LPStr)] string Port);

    //    [DllImport("MdcProxy.dll", EntryPoint = "CSharp_TerminateAllCalls", CallingConvention = CallingConvention.Cdecl)]
    //    public static extern void CSharp_TerminateAllCalls();
    //    [DllImport("MdcProxy.dll", EntryPoint = "CSharp_Accept", CallingConvention = CallingConvention.Cdecl)]
    //    public static extern void CSharp_Accept();


        public Form1()
        {
           
            InitializeComponent();
            LinPhoneSDK.Class1. SDK_START();
        }


        private void button1_Click(object sender, EventArgs e)
        {
           //var result= LinPhoneSDK.Class1.query("1", "3", "6666", "http://192.168.250.120");

            LinPhoneSDK.Class1.Set_Login_Info(SserverIPEdit.Text, "", userNameEdit.Text, passWord.Text, sipPort.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
          //  LinPhoneSDK.Class1.CSharp_Call(Sip.Text, IP.Text, Port.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
           // LinPhoneSDK.Class1.CSharp_TerminateAllCalls();
        }

        private void button4_Click(object sender, EventArgs e)
        {
           // LinPhoneSDK.Class1.CSharp_Accept();
        }

        private void button5_Click(object sender, EventArgs e)
        {
          //  LinPhoneSDK.Class1.CSharp_VideoCall(Sip.Text, IP.Text, Port.Text, pictureBox1.Handle);
        }
    }
}
