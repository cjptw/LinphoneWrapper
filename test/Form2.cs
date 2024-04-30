using Linphone;
using LinPhoneSDK;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Forms;

namespace WindowsFormsLinphoneV
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        Core core; Factory factory; Call call;
        System.Timers.Timer t;
        private void Form1_Load(object sender, EventArgs e)
        {
            //日志开启
            // LoggingService LoggingService = LoggingService.Instance;
            // LoggingService.LogLevel = LogLevel.Debug;
            //LoggingService.Listener.OnLogMessageWritten = OnLog;

            factory = Factory.Instance;

            //C:\Users\lilin\AppData\Local\linphone 如果装过linphone win exe的请删除这里面的内容否则core 创建不成功
            //多线程core需要加线程锁
            core = factory.CreateCore(new CoreListener() { }, "linphonerc", "linphonerc-factory");

            core.AvpfMode = AVPFMode.Disabled;
            Core.EnableLogCollection(LogCollectionState.Disabled);
            core.Listener.OnRegistrationStateChanged = OnRegistration;
            core.Listener.OnCallStateChanged = OnCallState;
            core.Listener.OnMessageReceived = OnMessage;
            core.Transports.UdpPort = -1;

            string x = "";
            foreach (string s in core.VideoDevicesList)
            {
                Console.WriteLine("set=" + s);
                if (s.IndexOf("Camera") != -1)
                {// bctbx-error-bctbx_file_open: Error open Permission denied
                    x = s;
                }
            }
            if (x != "")
            {
                Console.WriteLine("set last=" + x);
                core.VideoDevice = x;//"Directshow capture: Integrated Camera"; //摄像头设置
            }
            Console.WriteLine(">>" + core.DefaultInputAudioDevice.DeviceName);
            Console.WriteLine(">>" + core.DefaultOutputAudioDevice.DeviceName);

            foreach (AudioDevice s in core.ExtendedAudioDevices)
            {
                Console.WriteLine(s.DeviceName + ">++>");
            }
            core.SetUserAgent("mylinphone", "1.0");
            Console.WriteLine(">>");
            // core.InputAudioDevice
            //core.OutputAudioDevice

            core.NativeVideoWindowId = pictureBox1.Handle;//video window set

            foreach (PayloadType p in core.AudioPayloadTypes)
            {
                if (p.MimeType == "opus") { p.Enable(false); }//
                Console.Write(p.MimeType + "(" + p.Enabled() + ")" + "=");
            }
            Console.WriteLine();
            foreach (PayloadType p in core.VideoPayloadTypes)
            {
                if (p.MimeType == "VP8") { p.Enable(false); }//
                Console.Write(p.MimeType + "(" + p.Enabled() + ")" + "=");
            }
            Console.WriteLine();


            t = new System.Timers.Timer(20);   //实例化Timer类，设置间隔时间为10000毫秒；   
            t.Elapsed += new System.Timers.ElapsedEventHandler(theout); //到达时间的时候执行事件；   
            t.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；   
            t.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；   

        }

        private void OnMessage(Core lc, ChatRoom room, ChatMessage message)
        {
            Console.WriteLine("收到消息>>" + message.TextContent); ;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // String host = "47.104.66.191:7062", username = "1013", password = "h1234";
            //String host = "192.168.60.120:5060", username = "1003", password = "1003";
            String host = "210.51.10.231:5060", username = "1004", password = "1004";
            Reg(factory, core, host, username, password);
        }

        private void button2_Click(object sender, EventArgs e)
        {

            call = core.CurrentCall; if (call != null) { core.TerminateCall(call); }
            CallParams callParams = core.CreateCallParams(null);
            callParams.VideoEnabled = false;
            //call = core.InviteAddressWithParams(core.InterpretUrl("sip:9196@192.168.1.118:1890"), callParams);
            call = core.InviteAddressWithParams(core.InterpretUrl("sip:9196@192.168.3.3:1890"), callParams);

        }

        private void button3_Click(object sender, EventArgs e)
        {
            core.TerminateAllCalls();

        }
        private void theout(object sender, ElapsedEventArgs e)
        {

            core.Iterate();

        }
        /// <summary>
        /// 呼叫状态变更回调
        /// </summary>
        /// <param name="lc"></param>
        /// <param name="call"></param>
        /// <param name="cstate"></param>
        /// <param name="message"></param>
        private void OnCallState(Core lc, Call call, CallState cstate, string message)
        {
            this.Invoke(new MethodInvoker(() =>
            {
                richTextBoxLinPhoneState.AppendText(cstate.ToString() + "\n");
            }));
            Console.WriteLine("OnCallState=" + message);
        }

        private void OnRegistration(Core lc, ProxyConfig cfg, RegistrationState cstate, string message)
        {
            this.Invoke(new MethodInvoker(() =>
            {
                richTextBoxLinPhoneState.AppendText(cstate.ToString() + "：" + message + "\n");
            }));
            Console.WriteLine("OnRegistration" + message);
        }
        private void Reg(Factory factory, Core core, String domain, String username, String password)
        {

            foreach (AuthInfo a in core.AuthInfoList)
            {
                core.RemoveAuthInfo(a);
            }
            foreach (ProxyConfig a in core.ProxyConfigList)
            {
                core.RemoveProxyConfig(a);
            }

            AuthInfo authInfo = factory.CreateAuthInfo(username, username, password, null, null, domain);
            core.AddAuthInfo(authInfo);

            /*
            NatPolicy nat= core.CreateNatPolicy();
            nat.IceEnabled = true;
            nat.TurnEnabled = true;
            nat.StunServer = "118.190.151.162:3478";
            string turnusername = "lilin",turnpwd= "lilinaini";

            AuthInfo xauthInfo = core.FindAuthInfo(null, turnusername, null);
            if (xauthInfo != null)
            { 
                AuthInfo cloneAuthInfo = xauthInfo.Clone();
                core.RemoveAuthInfo(xauthInfo);   
                cloneAuthInfo.Username = turnusername;
                cloneAuthInfo.Userid = turnusername;
                cloneAuthInfo.Passwd=turnpwd;
                core.AddAuthInfo(cloneAuthInfo);
            }
            else
            {
                AuthInfo cloneAuthInfo = core.CreateAuthInfo(turnusername, turnusername, turnpwd, null, null, null);
                core.AddAuthInfo(cloneAuthInfo);
            }
            //
            nat.StunServerUsername = turnusername;
            core.NatPolicy = nat;   */

            var proxyConfig = core.CreateProxyConfig();
            proxyConfig.Idkey = "proxyConfigIdkey";
            String sipurl = "sip:" + username + "@" + domain;
            var identity = factory.CreateAddress(sipurl);
            // identity.Username = username;
            //identity.Domain = domain;
            //identity.Transport = TransportType.Udp;
            proxyConfig.Edit();
            proxyConfig.IdentityAddress = identity;
            proxyConfig.ServerAddr = sipurl;
            //  proxyConfig.Route = domain;

            proxyConfig.RegisterEnabled = true;
            proxyConfig.PublishEnabled = true;
            proxyConfig.AvpfMode = AVPFMode.Disabled;
            proxyConfig.Done();
            core.AddProxyConfig(proxyConfig);
            core.DefaultProxyConfig = proxyConfig;

            core.RefreshRegisters();

        }
        private static void OnLog(LoggingService logService, string domain, LogLevel lev, string message)
        {
            string now = DateTime.Now.ToString("hh:mm:ss");
            string log = now + " [";
            switch (lev)
            {
                case LogLevel.Debug:
                    log += "DEBUG";
                    break;
                case LogLevel.Error:
                    log += "ERROR";
                    break;
                case LogLevel.Message:
                    log += "MESSAGE";
                    break;
                case LogLevel.Warning:
                    log += "WARNING";
                    break;
                case LogLevel.Fatal:
                    log += "FATAL";
                    break;
                default:
                    break;
            }
            log += "] (" + domain + ") " + message;
            Console.WriteLine(log);
            //   sw.Write(log + "\n");

            //清空缓冲区
            // sw.Flush();
            //关闭流
            // sw.Close();
            // fs.Close();

        }

        private void button4_Click(object sender, EventArgs e)
        {

            call = core.CurrentCall;
            if (call != null)
            {
                call.Accept();
            }

        }
        /// <summary>
        /// 视频呼叫
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            string[] info = HostInfos;
            var address = core.InterpretUrl($"sip:" + tbPhone.Text + "@" + info[0] + ":" + info[3] + "");
            address.DisplayName = "";
            if (core.IsNetworkReachable)
            {
                InviteAddressWithParams(address, true);
            }
            else
            {
                richTextBoxLinPhoneState.AppendText("Network instability\n");
            }
        }
        private void InviteAddressWithParams(Address address, bool enableVideo)
        {
            call = core.CurrentCall;
            if (call != null) { 
                core.TerminateCall(call); 
            }
            //目标地址是否与代理配置的身份地址相同，如果相同则直接返回。
            var proxyConfig = core.DefaultProxyConfig;
            if (address.WeakEqual(proxyConfig.IdentityAddress))
            {
                return;
            }
            CallParams callParams = core.CreateCallParams(null);
            callParams.VideoEnabled = enableVideo; ;
            core.InviteAddressWithParams(address, callParams);
        }
        private void button6_Click(object sender, EventArgs e)
        {

            ChatRoom x = core.GetChatRoomFromUri("sip:1005@192.168.1.118:1890");

            x.SendChatMessage(x.CreateMessage("你好"));

        }
        public string[] HostInfos
        {
            get
            {
                string phone = tbPhone.Text;
                string hostInfo = textBoxLoginInfo.Text;
                if (string.IsNullOrEmpty(hostInfo))
                {
                    MessageBox.Show("未配置注册信息，请填写：CSserverIP,userName,passWord,sipPort");
                    return null;
                }
                if (string.IsNullOrEmpty(phone))
                {
                    MessageBox.Show("未配置拨打电话号码");
                    return null;
                }
                string[] infos = hostInfo.Split(',');
                return infos;
            }
        }
        /// <summary>
        /// 获取呼叫状态
        /// </summary>
        public System.Timers.Timer timerCallState;
        /// <summary>
        /// 拨打电话
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCallPhone_Click(object sender, EventArgs e)
        {
            string[] info = HostInfos;
            var address = core.InterpretUrl($"sip:" + tbPhone.Text + "@" + info[0] + ":" + info[3] + "");
            address.DisplayName = "";
            if (core.IsNetworkReachable)
            {
                InviteAddressWithParams(address, false);
            }
            else
            {
                richTextBoxLinPhoneState.AppendText("Network instability\n");
            }

        }
        /// <summary>
        /// 注销
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUnregistration_Click(object sender, EventArgs e)
        {
            if (core == null)
            {
                return;
            }
            ProxyConfig proxyConfig = core.GetProxyConfigByIdkey("proxyConfigIdkey");
            if (proxyConfig != null)
            {
                core.RemoveProxyConfig(proxyConfig);
                var authInfo = proxyConfig.FindAuthInfo();
                if (authInfo != null)
                {
                    core.RemoveAuthInfo(authInfo);
                }
            }
            else
            {
                richTextBoxLinPhoneState.AppendText("未注册，不能注销");
            }
        }

    }
}
