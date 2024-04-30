using Linphone;
using LinPhoneSDK;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using static Linphone.AccountCreatorListener;
namespace LinPhoneSDK
{
    /// <summary>
    /// Linphone帮助类
    /// </summary>
    public class LinphoneHelper
    {
        public static Core core;
        public static Factory factory;
        public static Call call;
        public static System.Timers.Timer t;

        /// <summary>
        /// 注册状态变更动作
        /// return值为dynamic对象
        /// </summary>
        private static object actionOnRegistrationStateChanged;
        [DllExport("ActionOnRegistrationStateChanged", CallingConvention = CallingConvention.Cdecl)]
        public static void OnActionRegistrationStateChanged(object _actionRegistrationStateChanged)
        {
            actionOnRegistrationStateChanged = _actionRegistrationStateChanged;
        }

        /// <summary>
        /// 呼叫状态变更动作
        /// return值为dynamic对象
        /// </summary>
        private static object actionOnCallStateChanged;
        [DllExport("ActionOnCallStateChanged", CallingConvention = CallingConvention.Cdecl)]
        public static void ActionOnCallStateChanged(object _actionOnCallStateChanged)
        {
            actionOnCallStateChanged = _actionOnCallStateChanged;
        }

        /// <summary>
        /// 消息接收更动作
        /// return值为dynamic对象
        /// </summary>
        private static object actionOnMessageReceived;
        [DllExport("ActionOnMessageReceived", CallingConvention = CallingConvention.Cdecl)]
        public static void ActionOnMessageReceived(object _actionOnMessageReceived)
        {
            actionOnMessageReceived = _actionOnMessageReceived;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        [DllExport("InitSdk", CallingConvention = CallingConvention.Cdecl)]
        public static void InitSdk()
        {
            if (factory != null && core != null)
            {
                return;
            }
            factory = Factory.Instance;
            //C:\Users\lilin\AppData\Local\linphone 如果装过linphone win exe的请删除这里面的内容否则core 创建不成功
            //多线程core需要加线程锁
            core = factory.CreateCore(new CoreListener() { }, "linphonerc", "linphonerc-factory");
            core.AvpfMode = AVPFMode.Disabled;
            Core.EnableLogCollection(LogCollectionState.Disabled);
            core.Listener.OnRegistrationStateChanged = OnRegistrationStateChanged;
            core.Listener.OnCallStateChanged = OnCallStateChanged;
            core.Listener.OnMessageReceived = OnMessageReceived;
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
            //t = new System.Timers.Timer(20);   //实例化Timer类，设置间隔时间为10000毫秒；   
            //t.Elapsed += new System.Timers.ElapsedEventHandler(theout); //到达时间的时候执行事件；   
            //t.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；   
            //t.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；   
            core.Iterate();
        }
        private static void theout(object sender, ElapsedEventArgs e)
        {
            core.Iterate();
        }
        /// <summary>
        /// 呼叫状态变更事件
        /// </summary>
        private static void OnCallStateChanged(Core lc, Call call, CallState cstate, string message)
        {
            dynamic data = new ExpandoObject();
            data.CallState = cstate;
            data.Message = message;
            if (actionOnCallStateChanged != null)
            {
                Action<object> action = actionOnCallStateChanged as Action<object>;
                action(data);
            }
        }
        /// <summary>
        /// 注册状态变更事件
        /// </summary>
        /// <param name="lc"></param>
        /// <param name="cfg"></param>
        /// <param name="cstate"></param>
        /// <param name="message"></param>
        private static void OnRegistrationStateChanged(Core lc, ProxyConfig cfg, RegistrationState cstate, string message)
        {
            dynamic data = new ExpandoObject();
            data.RegistrationState = cstate;
            data.Message = message;
            if(actionOnRegistrationStateChanged != null)
            {
                Action<object> action = actionOnRegistrationStateChanged as Action<object>;
                action(data);
            }
        }
        /// <summary>
        /// 消息回调
        /// </summary>
        /// <param name="lc"></param>
        /// <param name="room"></param>
        /// <param name="message"></param>
        private static void OnMessageReceived(Core lc, ChatRoom room, ChatMessage message)
        {
            dynamic data = new ExpandoObject();
            data.ChatMessage = message;
            data.Message = message.Text;
            if (actionOnMessageReceived != null)
            {
                Action<object> action = actionOnMessageReceived as Action<object>;
                action(data);
            }
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="domain">ipAddress:port</param>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>返回注册Id，注册Id在注销时使用</returns>
        [DllExport("Register", CallingConvention = CallingConvention.Cdecl)]
        public static string Register([MarshalAs(UnmanagedType.BStr)] string registerId,
            [MarshalAs(UnmanagedType.BStr)] string domain,
            [MarshalAs(UnmanagedType.BStr)] string username,
            [MarshalAs(UnmanagedType.BStr)] string password)
        {
            if (string.IsNullOrEmpty(registerId))
            {
                registerId = Guid.NewGuid().ToString();
            }
            //如果存在，则注销
            //Unregistration(registerId);
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
            proxyConfig.Idkey = registerId; 
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
        public static void Unregistration([MarshalAs(UnmanagedType.BStr)] string registerId = null)
        {
            if (core == null)
            {
                return ;
            }
            var proxyConfigs = core.ProxyConfigList.ToList();
            if (!string.IsNullOrEmpty(registerId))
            {
                var proxyConfig = core.GetProxyConfigByIdkey(registerId);
                if(proxyConfig != null)
                {
                    proxyConfigs = new List<ProxyConfig>
                    {
                        proxyConfig
                    };
                }
            }
            proxyConfigs = proxyConfigs ?? new List<ProxyConfig>();
            foreach (var config in proxyConfigs)
            {
                core.RemoveProxyConfig(config);
                var authInfo = config.FindAuthInfo();
                if (authInfo != null)
                {
                    core.RemoveAuthInfo(authInfo);
                }
            }
        }
        /// <summary>
        /// 挂断
        /// </summary>
        [DllExport("TerminateAllCalls", CallingConvention = CallingConvention.Cdecl)]
        public static void TerminateAllCalls()
        {
            if (core != null && (core.CurrentCall.State == CallState.OutgoingProgress || core.CurrentCall.State == CallState.StreamsRunning))
            {
                try
                {
                    core.TerminateAllCalls();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
        /// <summary>
        /// CallPhone  呼叫
        /// </summary>
        /// <param name="Sip"></param>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        [DllExport("CallPhone", CallingConvention = CallingConvention.Cdecl)]
        public static void CallPhone([MarshalAs(UnmanagedType.BStr)] string sip,
                                     [MarshalAs(UnmanagedType.BStr)] string ipAddress,
                                     [MarshalAs(UnmanagedType.BStr)] string port,
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
        private static void CallInviteWidthAddress(Address address, bool enableVideo)
        {
            call = core.CurrentCall;
            if (call != null)
            {
                core.TerminateCall(call);
            }
            //目标地址是否与代理配置的身份地址相同，如果相同则直接返回。
            var proxyConfig = core.DefaultProxyConfig;
            if (proxyConfig == null || address.WeakEqual(proxyConfig.IdentityAddress))
            {
                return;
            }
            CallParams callParams = core.CreateCallParams(null);
            callParams.VideoEnabled = enableVideo; ;
            core.InviteAddressWithParams(address, callParams);
        }
    }

}
