using Linphone;
using LinPhoneSDK;
using System;
using System.Dynamic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
namespace LinPhoneSDK
{
    /// <summary>
    /// LinPhone帮助类
    /// </summary>
    public class LinphoneHelper
    {
        public  Core core;
        public  Factory factory;
        public  Call call;

        /// <summary>
        /// 定义呼叫状态事件
        /// return sender类型为dynamic,调用 sender.CallState,sender.Message
        /// </summary>
        //public  event EventHandler EventOnCallStateChanged;

        public delegate void OnRegistrationStateChangedDelegate(Core lc, ProxyConfig cfg, Linphone.RegistrationState cstate, string message);
        public  OnRegistrationStateChangedDelegate OnRegistrationStateChanged { get; set; }




        /// <summary>
        /// 定义注册状态回调事件
        /// return sender类型为dynamic,调用 sender.RegistrationState,sender.Message
        /// </summary>
        //public static event EventHandler EventOnRegistrationStateChanged;

        [DllExport("InitSdk", CallingConvention = CallingConvention.Cdecl)]
        public  void InitSdk()
        {
            //if(factory != null && core != null)
            //{
            //    return;
            //}
            factory = Factory.Instance;
            //C:\Users\lilin\AppData\Local\linphone 如果装过linphone win exe的请删除这里面的内容否则core 创建不成功
            //多线程core需要加线程锁
            core = factory.CreateCore(new CoreListener() { }, "linphonerc", "linphonerc-factory");
            core.AvpfMode = AVPFMode.Disabled;
            Core.EnableLogCollection(LogCollectionState.Disabled);
            //core.Listener.OnRegistrationStateChanged = onRegistrationStateChanged;
            core.Listener.OnCallStateChanged = onCallStateChanged;
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

            //core.NativeVideoWindowId = pictureBox1.Handle;//video window set

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
        }
        /// <summary>
        /// 呼叫状态
        /// </summary>
        private  void onCallStateChanged(Core lc, Call call, CallState cstate, string message)
        {
            Console.WriteLine("收到消息>>" + message);
            //测试使用
            //if(EventOnCallStateChanged != null)
            //{
            //    dynamic sender = new ExpandoObject();
            //    sender.CallState = cstate;
            //    sender.Message = message;
            //    EventOnCallStateChanged(sender, null);
            //}
        }
        private  void onRegistrationStateChanged(Core lc, ProxyConfig cfg, RegistrationState cstate, string message)
        {
            if(OnRegistrationStateChanged != null)
            {
                OnRegistrationStateChanged(lc, cfg, cstate, message);
            }
        }
        private  void OnMessage(Core lc, ChatRoom room, ChatMessage message)
        {
          
        }


        [DllExport("Set_Login_Info", CallingConvention = CallingConvention.Cdecl)]
        public  void Set_Login_Info( [MarshalAs(UnmanagedType.LPStr)] string CSserverIP,
                                            [MarshalAs(UnmanagedType.LPStr)] string userName,
                                            [MarshalAs(UnmanagedType.LPStr)] string passWord,
                                            [MarshalAs(UnmanagedType.LPStr)] string sipPort
                                            )
        {
            Register(CSserverIP + ":" + sipPort, userName, passWord);
        }
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="domain">ipAddress:port</param>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>返回注册Id，注册Id在注销时使用</returns>
        [DllExport("Register", CallingConvention = CallingConvention.Cdecl)]
        public  string Register([MarshalAs(UnmanagedType.LPStr)] String domain, 
            [MarshalAs(UnmanagedType.LPStr)] String username, 
            [MarshalAs(UnmanagedType.LPStr)] String password)
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
            var proxyConfig = core.CreateProxyConfig();
            proxyConfig.Idkey = Guid.NewGuid().ToString(); 
            string sipurl = "sip:" + username + "@" + domain;
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
            return proxyConfig.Idkey;
            //core.RefreshRegisters();
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <param name="registerId">注册Id，注册时产生</param>
        [DllExport("Unregistration", CallingConvention = CallingConvention.Cdecl)]
        public  void Unregistration([MarshalAs(UnmanagedType.LPStr)] string registerId)
        {
            if (core == null || string.IsNullOrEmpty(registerId))
            {
                return ;
            }
            ProxyConfig proxyConfig = core.GetProxyConfigByIdkey(registerId);
            if (proxyConfig != null)
            {
                core.RemoveProxyConfig(proxyConfig);
                var authInfo = proxyConfig.FindAuthInfo();
                if (authInfo != null)
                {
                    core.RemoveAuthInfo(authInfo);
                }
            }
        }

        /// <summary>
        /// CSharp_Call-StartP2PCall  呼叫
        /// </summary>
        /// <param name="Sip"></param>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        [DllExport("SartP2PCall", CallingConvention = CallingConvention.Cdecl)]
        public  void SartP2PCall([MarshalAs(UnmanagedType.LPStr)] string sip,
                                        [MarshalAs(UnmanagedType.LPStr)] string ipAddress,
                                        [MarshalAs(UnmanagedType.LPStr)] string port,
                                        [MarshalAs(UnmanagedType.Bool)] bool videoEnabled)
        {
            var address = core.InterpretUrl($"sip:" + sip + "@" + ipAddress + ":" + port + "");
            address.DisplayName = "";
            if (core.IsNetworkReachable)
            {
                CallInviteWidthAddress(address, videoEnabled);
                return;
            }
            throw new Exception("Network instability!");
        }

        /// <summary>
        /// 呼叫邀请地址
        /// </summary>
        /// <param name="address"></param>
        /// <param name="enableVideo"></param>
        private  void CallInviteWidthAddress(Address address, bool enableVideo)
        {
            call = core.CurrentCall;
            if (call != null)
            {
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
    }

}
