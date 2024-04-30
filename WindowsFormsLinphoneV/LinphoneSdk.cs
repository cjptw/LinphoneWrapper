using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsLinphoneV
{
    public class LinphoneSdk
    {
        const  string MdcProxyDllImportName = "MdcProxy.dll";

        /// <summary>
        /// 初始化
        /// </summary>
        [DllImport(MdcProxyDllImportName, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitSdk();

        /// <summary>
        /// 注册状态变更动作
        /// return值为dynamic对象
        /// </summary>
        [DllImport(MdcProxyDllImportName, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ActionOnRegistrationStateChanged(object action);

        /// <summary>
        /// 呼叫状态变更动作
        /// return值为dynamic对象
        /// </summary>
        [DllImport(MdcProxyDllImportName, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ActionOnCallStateChanged(object action);

        /// <summary>
        /// 消息接收更动作
        /// return值为dynamic对象
        /// </summary>
        [DllImport(MdcProxyDllImportName, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ActionOnMessageReceived(object action);

        /// <summary>
        /// 注册
        /// </summary>
        [DllImport(MdcProxyDllImportName, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern string Register([MarshalAs(UnmanagedType.BStr)] string registerId,
            [MarshalAs(UnmanagedType.BStr)] string domain,
            [MarshalAs(UnmanagedType.BStr)] string username,
            [MarshalAs(UnmanagedType.BStr)] string password);

        /// <summary>
        /// 注销
        /// </summary>
        [DllImport(MdcProxyDllImportName, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Unregistration([MarshalAs(UnmanagedType.BStr)] string registerId = "");

        /// <summary>
        /// 呼叫
        /// </summary>
        [DllImport(MdcProxyDllImportName, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CallPhone([MarshalAs(UnmanagedType.BStr)] string sip,
                                         [MarshalAs(UnmanagedType.BStr)] string ipAddress,
                                         [MarshalAs(UnmanagedType.BStr)] string port,
                                         [MarshalAs(UnmanagedType.Bool)] bool videoEnabled);

        /// <summary>
        /// 挂断
        /// </summary>
        [DllImport(MdcProxyDllImportName, EntryPoint = "TerminateAllCalls")]
        public static extern void TerminateAllCalls();

    }
}
