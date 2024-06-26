/*
LinphoneWrapper.cs
Copyright (C) 2017 Belledonne Communications SARL

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/
#define WINDOWS_UWP

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
#if __IOS__
using ObjCRuntime;
#endif

namespace Linphone
{
#region Wrapper specifics
	/// <summary>
	/// Only contains the LIB_NAME value that represents the library in which all DllImport are made
	/// </summary>
	public class LinphoneWrapper
	{ 
		public const string VERSION = "4.4.0-alpha-265-ged342f3";
#if __IOS__ 
	//	public const string LIB_NAME = "linphone.framework/linphone";
#else
		public const string LIB_NAME = "liblinphone"; // With this, it automatically finds liblinphone.so
#endif

#if WINDOWS_UWP
		public const string BELLE_SIP_LIB_NAME = "bellesip";
		public const string BCTOOLBOX_LIB_NAME = "bctoolbox";
#else
		public const string BELLE_SIP_LIB_NAME = LIB_NAME;
		public const string BCTOOLBOX_LIB_NAME = LIB_NAME;
#endif
/// https://docs.microsoft.com/fr-fr/xamarin/cross-platform/app-fundamentals/building-cross-platform-applications/platform-divergence-abstraction-divergent-implementation#android
#if __ANDROID__
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void setAndroidLogHandler();
#endif
#if __IOS__
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_iphone_enable_logs();
#endif

		/// <summary>
		/// Registers the native log handler in Linphone.
		/// </summary>
		public static void setNativeLogHandler()
		{
#if __ANDROID__
			setAndroidLogHandler();
#elif __IOS__
			linphone_iphone_enable_logs();
#endif
		}
	}

	/// <summary>
	/// All methods that returns a LinphoneStatus with a value != 0 as an error code in C are translated in C# by throwing a LinphoneException
	/// </summary>
#if WINDOWS_UWP
    public class LinphoneException : System.Exception
    {
        public LinphoneException() : base() { }
        public LinphoneException(string message) : base(message) { }
        public LinphoneException(string message, System.Exception inner) : base(message, inner) { }
    }
#else
    [Serializable()]
	public class LinphoneException : System.Exception
	{
		public LinphoneException() : base() { }
		public LinphoneException(string message) : base(message) { }
		public LinphoneException(string message, System.Exception inner) : base(message, inner) { }
		protected LinphoneException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
	}
#endif

	[StructLayout(LayoutKind.Sequential)]
	/// <summary>
	/// Parent class for a Linphone public objects
	/// </summary>
	public class LinphoneObject
	{
		internal IntPtr nativePtr;

		internal GCHandle handle;

		internal List<IntPtr> string_ptr_list;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnLinphoneObjectDataDestroyed(IntPtr data);

		[DllImport(LinphoneWrapper.BELLE_SIP_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern int belle_sip_object_data_set(IntPtr ptr, string name, IntPtr data, IntPtr cb);
#else
		static extern int belle_sip_object_data_set(IntPtr ptr, string name, IntPtr data, OnLinphoneObjectDataDestroyed cb);
#endif

		[DllImport(LinphoneWrapper.BELLE_SIP_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr belle_sip_object_data_get(IntPtr ptr, string name);

		[DllImport(LinphoneWrapper.BELLE_SIP_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr belle_sip_object_ref(IntPtr ptr);

		[DllImport(LinphoneWrapper.BELLE_SIP_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void belle_sip_object_unref(IntPtr ptr);

		[DllImport(LinphoneWrapper.BELLE_SIP_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void belle_sip_object_data_remove(IntPtr ptr, string name);

		[DllImport(LinphoneWrapper.BCTOOLBOX_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr bctbx_list_next(IntPtr ptr);

		[DllImport(LinphoneWrapper.BCTOOLBOX_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr bctbx_list_get_data(IntPtr ptr);

		[DllImport(LinphoneWrapper.BCTOOLBOX_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr bctbx_list_append(IntPtr elem, IntPtr data);

		~LinphoneObject()
		{
			//Console.WriteLine("Destroying " + this.ToString());
			if (nativePtr != IntPtr.Zero) {
				//Console.WriteLine("Unreffing " + this.ToString());
				belle_sip_object_data_remove(nativePtr, "cs_obj");
				belle_sip_object_unref(nativePtr);
				handle.Free();
			}
		}

		public static T fromNativePtr<T>(IntPtr ptr, bool takeRef=true) where T : LinphoneObject, new()
		{
			if (ptr == IntPtr.Zero) return null;
			IntPtr objPtr = belle_sip_object_data_get(ptr, "cs_obj");
			if (objPtr != IntPtr.Zero)
			{
				T obj = null;
				GCHandle handle = GCHandle.FromIntPtr(objPtr);
				if (handle.IsAllocated)
				{
					obj = (T)handle.Target;
				}
				if (obj == null)
				{
					//Console.WriteLine("Handle target is null " + handle.Target);
					objPtr = IntPtr.Zero;
				}
				else
				{
					//Console.WriteLine("Using existing " + obj.ToString());
					return obj;
				}
			}
			if (objPtr == IntPtr.Zero)
			{
				T obj = new T();
				//Console.WriteLine("Creating " + obj.ToString());
				if (takeRef)
				{
					ptr = belle_sip_object_ref(ptr);
					//Console.WriteLine("Reffing " + obj.ToString());
				}
				obj.nativePtr = ptr;
				obj.handle = GCHandle.Alloc(obj, GCHandleType.WeakTrackResurrection);
				objPtr = GCHandle.ToIntPtr(obj.handle);
#if WINDOWS_UWP
				belle_sip_object_data_set(ptr, "cs_obj", objPtr, IntPtr.Zero);
#else
				belle_sip_object_data_set(ptr, "cs_obj", objPtr, null);
#endif

				return obj;
			}
			return null;
		}

		internal static IEnumerable<string> MarshalStringArray(IntPtr listPtr)
		{
			if (listPtr != IntPtr.Zero)
			{
				IntPtr ptr = listPtr;
				while (ptr != IntPtr.Zero)
				{
					IntPtr dataPtr = bctbx_list_get_data(ptr);
					if (dataPtr == IntPtr.Zero)
					{
						break;
					}
					string key = Marshal.PtrToStringAnsi(dataPtr);
					yield return key;
					ptr = bctbx_list_next(ptr);
				}
			}
		}

		internal static IEnumerable<T> MarshalBctbxList<T>(IntPtr listPtr) where T : LinphoneObject, new()
		{
			if (listPtr != IntPtr.Zero)
			{
				IntPtr ptr = listPtr;
				while (ptr != IntPtr.Zero)
				{
					IntPtr dataPtr = bctbx_list_get_data(ptr);
					if (dataPtr == IntPtr.Zero)
					{
						break;
					}
					T obj = fromNativePtr<T>(dataPtr);
					yield return obj;
					ptr = bctbx_list_next(ptr);
				}
			}
		}

		internal protected IntPtr StringArrayToBctbxList(IEnumerable<string> stringlist)
		{
			IntPtr bctbx_list = IntPtr.Zero;
			string_ptr_list = new List<IntPtr>();
			foreach (string s in stringlist)
			{
				IntPtr string_ptr = Marshal.StringToHGlobalAnsi(s);
				bctbx_list = bctbx_list_append(bctbx_list, string_ptr);
				string_ptr_list.Add(string_ptr);
			}
			return bctbx_list;
		}

		internal protected void CleanStringArrayPtrs()
		{
			foreach (IntPtr string_ptr in string_ptr_list)
			{
				Marshal.FreeHGlobal(string_ptr);
			}
		}

		internal static IntPtr ObjectArrayToBctbxList<T>(IEnumerable<T> objlist) where T : LinphoneObject, new()
		{
			IntPtr bctbx_list = IntPtr.Zero;
			foreach (T ptr in objlist)
			{
				bctbx_list = bctbx_list_append(bctbx_list, ptr.nativePtr);
			}
			return bctbx_list;
		}
	}

	public class MediastreamerFactory
	{
		public IntPtr nativePtr;

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int ms_factory_enable_filter_from_name(IntPtr nativePtr, string name, char enabled);

		public void enableFilterFromName(string name, bool enabled)
		{
			ms_factory_enable_filter_from_name(nativePtr, name, enabled ? (char)1 : (char)0);
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void ms_devices_info_add(IntPtr devices_info, string manufacturer, string model, string platform, uint flags, int delay, int recommended_rate);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr ms_factory_get_devices_info(IntPtr factory);

		public void addDevicesInfo(string manufacturer, string model, string platform, uint flags, int delay, int recommended_rate)
		{
				ms_devices_info_add(ms_factory_get_devices_info(nativePtr), manufacturer, model, platform, flags, delay, recommended_rate);
		}
	}
#endregion

#region Enums
	/// <summary>
	/// Enum describing RTP AVPF activation modes. 
	/// </summary>
	
	public enum AVPFMode
	{
		/// <summary>
		/// Use default value defined at upper level. 
		/// </summary>
		Default = -1,
		/// <summary>
		/// AVPF is disabled. 
		/// </summary>
		Disabled = 0,
		/// <summary>
		/// AVPF is enabled. 
		/// </summary>
		Enabled = 1,
	}

	/// <summary>
	/// Enum describing Activation code checking. 
	/// </summary>
	
	public enum AccountCreatorActivationCodeStatus
	{
		/// <summary>
		/// Activation code ok. 
		/// </summary>
		Ok = 0,
		/// <summary>
		/// Activation code too short. 
		/// </summary>
		TooShort = 1,
		/// <summary>
		/// Activation code too long. 
		/// </summary>
		TooLong = 2,
		/// <summary>
		/// Contain invalid characters. 
		/// </summary>
		InvalidCharacters = 3,
	}

	/// <summary>
	/// Enum algorithm checking. 
	/// </summary>
	
	public enum AccountCreatorAlgoStatus
	{
		/// <summary>
		/// Algorithm ok. 
		/// </summary>
		Ok = 0,
		/// <summary>
		/// Algorithm not supported. 
		/// </summary>
		NotSupported = 1,
	}

	/// <summary>
	/// Enum describing Domain checking. 
	/// </summary>
	
	public enum AccountCreatorDomainStatus
	{
		/// <summary>
		/// Domain ok. 
		/// </summary>
		Ok = 0,
		/// <summary>
		/// Domain invalid. 
		/// </summary>
		Invalid = 1,
	}

	/// <summary>
	/// Enum describing Email checking. 
	/// </summary>
	
	public enum AccountCreatorEmailStatus
	{
		/// <summary>
		/// Email ok. 
		/// </summary>
		Ok = 0,
		/// <summary>
		/// Email malformed. 
		/// </summary>
		Malformed = 1,
		/// <summary>
		/// Contain invalid characters. 
		/// </summary>
		InvalidCharacters = 2,
	}

	/// <summary>
	/// Enum describing language checking. 
	/// </summary>
	
	public enum AccountCreatorLanguageStatus
	{
		/// <summary>
		/// Language ok. 
		/// </summary>
		Ok = 0,
	}

	/// <summary>
	/// Enum describing Password checking. 
	/// </summary>
	
	public enum AccountCreatorPasswordStatus
	{
		/// <summary>
		/// Password ok. 
		/// </summary>
		Ok = 0,
		/// <summary>
		/// Password too short. 
		/// </summary>
		TooShort = 1,
		/// <summary>
		/// Password too long. 
		/// </summary>
		TooLong = 2,
		/// <summary>
		/// Contain invalid characters. 
		/// </summary>
		InvalidCharacters = 3,
		/// <summary>
		/// Missing specific characters. 
		/// </summary>
		MissingCharacters = 4,
	}

	/// <summary>
	/// Enum describing Phone number checking. 
	/// </summary>
	
	public enum AccountCreatorPhoneNumberStatus
	{
		/// <summary>
		/// Phone number ok. 
		/// </summary>
		Ok = 1,
		/// <summary>
		/// Phone number too short. 
		/// </summary>
		TooShort = 2,
		/// <summary>
		/// Phone number too long. 
		/// </summary>
		TooLong = 4,
		/// <summary>
		/// Country code invalid. 
		/// </summary>
		InvalidCountryCode = 8,
		/// <summary>
		/// Phone number invalid. 
		/// </summary>
		Invalid = 16,
	}

	/// <summary>
	/// Enum describing the status of server request. 
	/// </summary>
	
	public enum AccountCreatorStatus
	{
		/// <summary>
		/// Request status. 
		/// </summary>
		RequestOk = 0,
		/// <summary>
		/// Request failed. 
		/// </summary>
		RequestFailed = 1,
		/// <summary>
		/// Request failed due to missing argument(s) 
		/// </summary>
		MissingArguments = 2,
		/// <summary>
		/// Request failed due to missing callback(s) 
		/// </summary>
		MissingCallbacks = 3,
		/// <summary>
		/// Account status. 
		/// </summary>
		AccountCreated = 4,
		/// <summary>
		/// Account not created. 
		/// </summary>
		AccountNotCreated = 5,
		/// <summary>
		/// Account exist. 
		/// </summary>
		AccountExist = 6,
		/// <summary>
		/// Account exist with alias. 
		/// </summary>
		AccountExistWithAlias = 7,
		/// <summary>
		/// Account not exist. 
		/// </summary>
		AccountNotExist = 8,
		/// <summary>
		/// Account was created with Alias. 
		/// </summary>
		AliasIsAccount = 9,
		/// <summary>
		/// Alias exist. 
		/// </summary>
		AliasExist = 10,
		/// <summary>
		/// Alias not exist. 
		/// </summary>
		AliasNotExist = 11,
		/// <summary>
		/// Account activated. 
		/// </summary>
		AccountActivated = 12,
		/// <summary>
		/// Account already activated. 
		/// </summary>
		AccountAlreadyActivated = 13,
		/// <summary>
		/// Account not activated. 
		/// </summary>
		AccountNotActivated = 14,
		/// <summary>
		/// Account linked. 
		/// </summary>
		AccountLinked = 15,
		/// <summary>
		/// Account not linked. 
		/// </summary>
		AccountNotLinked = 16,
		/// <summary>
		/// Server. 
		/// </summary>
		ServerError = 17,
		/// <summary>
		/// Error cannot send SMS. 
		/// </summary>
		PhoneNumberInvalid = 18,
		/// <summary>
		/// Error key doesn't match. 
		/// </summary>
		WrongActivationCode = 19,
		/// <summary>
		/// Error too many SMS sent. 
		/// </summary>
		PhoneNumberOverused = 20,
		AlgoNotSupported = 21,
		/// <summary>
		/// < Error algo isn't MD5 or SHA-256 
		/// </summary>
		UnexpectedError = 22,
	}

	/// <summary>
	/// Enum describing Transport checking. 
	/// </summary>
	
	public enum AccountCreatorTransportStatus
	{
		/// <summary>
		/// Transport ok. 
		/// </summary>
		Ok = 0,
		/// <summary>
		/// Transport invalid. 
		/// </summary>
		Unsupported = 1,
	}

	/// <summary>
	/// Enum describing Username checking. 
	/// </summary>
	
	public enum AccountCreatorUsernameStatus
	{
		/// <summary>
		/// Username ok. 
		/// </summary>
		Ok = 0,
		/// <summary>
		/// Username too short. 
		/// </summary>
		TooShort = 1,
		/// <summary>
		/// Username too long. 
		/// </summary>
		TooLong = 2,
		/// <summary>
		/// Contain invalid characters. 
		/// </summary>
		InvalidCharacters = 3,
		/// <summary>
		/// Invalid username. 
		/// </summary>
		Invalid = 4,
	}

	/// <summary>
	/// Enum describing Ip family. 
	/// </summary>
	
	public enum AddressFamily
	{
		/// <summary>
		/// IpV4. 
		/// </summary>
		Inet = 0,
		/// <summary>
		/// IpV6. 
		/// </summary>
		Inet6 = 1,
		/// <summary>
		/// Unknown. 
		/// </summary>
		Unspec = 2,
	}

	/// <summary>
	/// LinphoneAudioDeviceCapabilities enum represents whether a device can record
	/// audio, play audio or both 
	/// </summary>
	[Flags]
	public enum AudioDeviceCapabilities
	{
		CapabilityRecord = 1<<0,
		/// <summary>
		/// Can record audio. 
		/// </summary>
		CapabilityPlay = 1<<1,
	}

	/// <summary>
	/// LinphoneAudioDeviceType enum represents the different types of an audio device. 
	/// </summary>
	
	public enum AudioDeviceType
	{
		Unknown = 0,
		/// <summary>
		/// Unknown. 
		/// </summary>
		Microphone = 1,
		/// <summary>
		/// Microphone. 
		/// </summary>
		Earpiece = 2,
		/// <summary>
		/// Earpiece. 
		/// </summary>
		Speaker = 3,
		/// <summary>
		/// Speaker. 
		/// </summary>
		Bluetooth = 4,
		/// <summary>
		/// Bluetooth. 
		/// </summary>
		BluetoothA2DP = 5,
		/// <summary>
		/// Bluetooth A2DP. 
		/// </summary>
		Telephony = 6,
		/// <summary>
		/// Telephony. 
		/// </summary>
		AuxLine = 7,
		/// <summary>
		/// AuxLine. 
		/// </summary>
		GenericUsb = 8,
		/// <summary>
		/// GenericUsb. 
		/// </summary>
		Headset = 9,
		/// <summary>
		/// Headset. 
		/// </summary>
		Headphones = 10,
	}

	/// <summary>
	/// Enum describing type of audio route. 
	/// </summary>
	
	public enum AudioRoute
	{
		Earpiece = 0,
		Speaker = 1,
	}

	/// <summary>
	/// Enum describing the authentication methods. 
	/// </summary>
	
	public enum AuthMethod
	{
		/// <summary>
		/// Digest authentication requested. 
		/// </summary>
		HttpDigest = 0,
		/// <summary>
		/// Client certificate requested. 
		/// </summary>
		Tls = 1,
	}

	/// <summary>
	/// Enum representing the direction of a call. 
	/// </summary>
	
	public enum CallDir
	{
		/// <summary>
		/// outgoing calls 
		/// </summary>
		Outgoing = 0,
		/// <summary>
		/// incoming calls 
		/// </summary>
		Incoming = 1,
	}

	/// <summary>
	/// LinphoneCallState enum represents the different states a call can reach into. 
	/// </summary>
	
	public enum CallState
	{
		/// <summary>
		/// Initial state. 
		/// </summary>
		Idle = 0,
		/// <summary>
		/// Incoming call received. 
		/// </summary>
		IncomingReceived = 1,
		/// <summary>
		/// Outgoing call initialized. 
		/// </summary>
		OutgoingInit = 2,
		/// <summary>
		/// Outgoing call in progress. 
		/// </summary>
		OutgoingProgress = 3,
		/// <summary>
		/// Outgoing call ringing. 
		/// </summary>
		OutgoingRinging = 4,
		/// <summary>
		/// Outgoing call early media. 
		/// </summary>
		OutgoingEarlyMedia = 5,
		/// <summary>
		/// Connected. 
		/// </summary>
		Connected = 6,
		/// <summary>
		/// Streams running. 
		/// </summary>
		StreamsRunning = 7,
		/// <summary>
		/// Pausing. 
		/// </summary>
		Pausing = 8,
		/// <summary>
		/// Paused. 
		/// </summary>
		Paused = 9,
		/// <summary>
		/// Resuming. 
		/// </summary>
		Resuming = 10,
		/// <summary>
		/// Referred. 
		/// </summary>
		Referred = 11,
		/// <summary>
		/// Error. 
		/// </summary>
		Error = 12,
		/// <summary>
		/// Call end. 
		/// </summary>
		End = 13,
		/// <summary>
		/// Paused by remote. 
		/// </summary>
		PausedByRemote = 14,
		/// <summary>
		/// The call's parameters are updated for example when video is asked by remote. 
		/// </summary>
		UpdatedByRemote = 15,
		/// <summary>
		/// We are proposing early media to an incoming call. 
		/// </summary>
		IncomingEarlyMedia = 16,
		/// <summary>
		/// We have initiated a call update. 
		/// </summary>
		Updating = 17,
		/// <summary>
		/// The call object is now released. 
		/// </summary>
		Released = 18,
		/// <summary>
		/// The call is updated by remote while not yet answered (SIP UPDATE in early
		/// dialog received) 
		/// </summary>
		EarlyUpdatedByRemote = 19,
		/// <summary>
		/// We are updating the call while not yet answered (SIP UPDATE in early dialog
		/// sent) 
		/// </summary>
		EarlyUpdating = 20,
	}

	/// <summary>
	/// Enum representing the status of a call. 
	/// </summary>
	
	public enum CallStatus
	{
		/// <summary>
		/// The call was sucessful. 
		/// </summary>
		Success = 0,
		/// <summary>
		/// The call was aborted. 
		/// </summary>
		Aborted = 1,
		/// <summary>
		/// The call was missed (unanswered) 
		/// </summary>
		Missed = 2,
		/// <summary>
		/// The call was declined, either locally or by remote end. 
		/// </summary>
		Declined = 3,
		/// <summary>
		/// The call was aborted before being advertised to the application - for protocol
		/// reasons. 
		/// </summary>
		EarlyAborted = 4,
		/// <summary>
		/// The call was answered on another device. 
		/// </summary>
		AcceptedElsewhere = 5,
		/// <summary>
		/// The call was declined on another device. 
		/// </summary>
		DeclinedElsewhere = 6,
	}

	/// <summary>
	/// LinphoneChatMessageDirection is used to indicate if a message is outgoing or
	/// incoming. 
	/// </summary>
	
	public enum ChatMessageDirection
	{
		/// <summary>
		/// Incoming message. 
		/// </summary>
		Incoming = 0,
		/// <summary>
		/// Outgoing message. 
		/// </summary>
		Outgoing = 1,
	}

	/// <summary>
	/// LinphoneChatMessageState is used to notify if messages have been successfully
	/// delivered or not. 
	/// </summary>
	
	public enum ChatMessageState
	{
		/// <summary>
		/// Initial state. 
		/// </summary>
		Idle = 0,
		/// <summary>
		/// Delivery in progress. 
		/// </summary>
		InProgress = 1,
		/// <summary>
		/// Message successfully delivered and acknowledged by the server. 
		/// </summary>
		Delivered = 2,
		/// <summary>
		/// Message was not delivered. 
		/// </summary>
		NotDelivered = 3,
		/// <summary>
		/// Message was received and acknowledged but cannot get file from server. 
		/// </summary>
		FileTransferError = 4,
		/// <summary>
		/// File transfer has been completed successfully. 
		/// </summary>
		FileTransferDone = 5,
		/// <summary>
		/// Message successfully delivered an acknowledged by the remote user. 
		/// </summary>
		DeliveredToUser = 6,
		/// <summary>
		/// Message successfully displayed to the remote user. 
		/// </summary>
		Displayed = 7,
		FileTransferInProgress = 8,
	}

	/// <summary>
	/// LinphoneChatRoomBackend is used to indicate the backend implementation of a
	/// chat room. 
	/// </summary>
	[Flags]
	public enum ChatRoomBackend
	{
		/// <summary>
		/// Basic (client-to-client) chat room. 
		/// </summary>
		Basic = 1<<0,
		/// <summary>
		/// Server-based chat room. 
		/// </summary>
		FlexisipChat = 1<<1,
	}

	/// <summary>
	/// LinphoneChatRoomCapabilities is used to indicate the capabilities of a chat
	/// room. 
	/// </summary>
	[Flags]
	public enum ChatRoomCapabilities
	{
		/// <summary>
		/// No capabilities. 
		/// </summary>
		None = 0,
		/// <summary>
		/// No server. 
		/// </summary>
		Basic = 1<<0,
		/// <summary>
		/// Supports RTT. 
		/// </summary>
		RealTimeText = 1<<1,
		/// <summary>
		/// Use server (supports group chat) 
		/// </summary>
		Conference = 1<<2,
		/// <summary>
		/// Special proxy chat room flag. 
		/// </summary>
		Proxy = 1<<3,
		/// <summary>
		/// Chat room migratable from Basic to Conference. 
		/// </summary>
		Migratable = 1<<4,
		/// <summary>
		/// A communication between two participants (can be Basic or Conference) 
		/// </summary>
		OneToOne = 1<<5,
		/// <summary>
		/// Chat room is encrypted. 
		/// </summary>
		Encrypted = 1<<6,
	}

	/// <summary>
	/// LinphoneChatRoomEncryptionBackend is used to indicate the encryption engine
	/// used by a chat room. 
	/// </summary>
	[Flags]
	public enum ChatRoomEncryptionBackend
	{
		/// <summary>
		/// No encryption. 
		/// </summary>
		None = 0,
		/// <summary>
		/// Lime x3dh encryption. 
		/// </summary>
		Lime = 1<<0,
	}

	/// <summary>
	/// TODO move to encryption engine object when available
	/// LinphoneChatRoomSecurityLevel is used to indicate the encryption security level
	/// of a chat room. 
	/// </summary>
	
	public enum ChatRoomSecurityLevel
	{
		/// <summary>
		/// Security failure. 
		/// </summary>
		Unsafe = 0,
		/// <summary>
		/// No encryption. 
		/// </summary>
		ClearText = 1,
		/// <summary>
		/// Encrypted. 
		/// </summary>
		Encrypted = 2,
		/// <summary>
		/// Encrypted and verified. 
		/// </summary>
		Safe = 3,
	}

	/// <summary>
	/// LinphoneChatRoomState is used to indicate the current state of a chat room. 
	/// </summary>
	
	public enum ChatRoomState
	{
		/// <summary>
		/// Initial state. 
		/// </summary>
		None = 0,
		/// <summary>
		/// Chat room is now instantiated on local. 
		/// </summary>
		Instantiated = 1,
		/// <summary>
		/// One creation request was sent to the server. 
		/// </summary>
		CreationPending = 2,
		/// <summary>
		/// Chat room was created on the server. 
		/// </summary>
		Created = 3,
		/// <summary>
		/// Chat room creation failed. 
		/// </summary>
		CreationFailed = 4,
		/// <summary>
		/// Wait for chat room termination. 
		/// </summary>
		TerminationPending = 5,
		/// <summary>
		/// Chat room exists on server but not in local. 
		/// </summary>
		Terminated = 6,
		/// <summary>
		/// The chat room termination failed. 
		/// </summary>
		TerminationFailed = 7,
		/// <summary>
		/// Chat room was deleted on the server. 
		/// </summary>
		Deleted = 8,
	}

	/// <summary>
	/// LinphoneGlobalState describes the global state of the <see cref="Linphone.Core"
	/// /> object. 
	/// </summary>
	
	public enum ConfiguringState
	{
		Successful = 0,
		Failed = 1,
		Skipped = 2,
	}

	/// <summary>
	/// Consolidated presence information: 'online' means the user is open for
	/// communication, 'busy' means the user is open for communication but involved in
	/// an other activity, 'do not disturb' means the user is not open for
	/// communication, and 'offline' means that no presence information is available. 
	/// </summary>
	
	public enum ConsolidatedPresence
	{
		Online = 0,
		Busy = 1,
		DoNotDisturb = 2,
		Offline = 3,
	}

	/// <summary>
	/// LinphoneCoreLogCollectionUploadState is used to notify if log collection upload
	/// have been succesfully delivered or not. 
	/// </summary>
	
	public enum CoreLogCollectionUploadState
	{
		/// <summary>
		/// Delivery in progress. 
		/// </summary>
		InProgress = 0,
		/// <summary>
		/// Log collection upload successfully delivered and acknowledged by remote end
		/// point. 
		/// </summary>
		Delivered = 1,
		/// <summary>
		/// Log collection upload was not delivered. 
		/// </summary>
		NotDelivered = 2,
	}

	/// <summary>
	/// Enum describing the result of the echo canceller calibration process. 
	/// </summary>
	
	public enum EcCalibratorStatus
	{
		/// <summary>
		/// The echo canceller calibration process is on going. 
		/// </summary>
		InProgress = 0,
		/// <summary>
		/// The echo canceller calibration has been performed and produced an echo delay
		/// measure. 
		/// </summary>
		Done = 1,
		/// <summary>
		/// The echo canceller calibration process has failed. 
		/// </summary>
		Failed = 2,
		/// <summary>
		/// The echo canceller calibration has been performed and no echo has been
		/// detected. 
		/// </summary>
		DoneNoEcho = 3,
	}

	/// <summary>
	/// LinphoneEventLogType is used to indicate the type of an event. 
	/// </summary>
	
	public enum EventLogType
	{
		/// <summary>
		/// No defined event. 
		/// </summary>
		None = 0,
		/// <summary>
		/// Conference (created) event. 
		/// </summary>
		ConferenceCreated = 1,
		/// <summary>
		/// Conference (terminated) event. 
		/// </summary>
		ConferenceTerminated = 2,
		/// <summary>
		/// Conference call (start) event. 
		/// </summary>
		ConferenceCallStart = 3,
		/// <summary>
		/// Conference call (end) event. 
		/// </summary>
		ConferenceCallEnd = 4,
		/// <summary>
		/// Conference chat message event. 
		/// </summary>
		ConferenceChatMessage = 5,
		/// <summary>
		/// Conference participant (added) event. 
		/// </summary>
		ConferenceParticipantAdded = 6,
		/// <summary>
		/// Conference participant (removed) event. 
		/// </summary>
		ConferenceParticipantRemoved = 7,
		/// <summary>
		/// Conference participant (set admin) event. 
		/// </summary>
		ConferenceParticipantSetAdmin = 8,
		/// <summary>
		/// Conference participant (unset admin) event. 
		/// </summary>
		ConferenceParticipantUnsetAdmin = 9,
		/// <summary>
		/// Conference participant device (added) event. 
		/// </summary>
		ConferenceParticipantDeviceAdded = 10,
		/// <summary>
		/// Conference participant device (removed) event. 
		/// </summary>
		ConferenceParticipantDeviceRemoved = 11,
		/// <summary>
		/// Conference subject event. 
		/// </summary>
		ConferenceSubjectChanged = 12,
		/// <summary>
		/// Conference encryption security event. 
		/// </summary>
		ConferenceSecurityEvent = 13,
		/// <summary>
		/// Conference ephemeral message (ephemeral message lifetime changed) event. 
		/// </summary>
		ConferenceEphemeralMessageLifetimeChanged = 14,
		/// <summary>
		/// Conference ephemeral message (ephemeral message enabled) event. 
		/// </summary>
		ConferenceEphemeralMessageEnabled = 15,
		/// <summary>
		/// Conference ephemeral message (ephemeral message disabled) event. 
		/// </summary>
		ConferenceEphemeralMessageDisabled = 16,
	}

	/// <summary>
	/// Enum describing the status of a LinphoneFriendList operation. 
	/// </summary>
	[Flags]
	public enum FriendCapability
	{
		None = 0,
		GroupChat = 1<<0,
		LimeX3Dh = 1<<1,
		EphemeralMessages = 1<<2,
	}

	/// <summary>
	/// Enum describing the status of a LinphoneFriendList operation. 
	/// </summary>
	
	public enum FriendListStatus
	{
		OK = 0,
		NonExistentFriend = 1,
		InvalidFriend = 2,
	}

	/// <summary>
	/// Enum describing the status of a CardDAV synchronization. 
	/// </summary>
	
	public enum FriendListSyncStatus
	{
		Started = 0,
		Successful = 1,
		Failure = 2,
	}

	/// <summary>
	/// LinphoneGlobalState describes the global state of the <see cref="Linphone.Core"
	/// /> object. 
	/// </summary>
	
	public enum GlobalState
	{
		/// <summary>
		/// State in which we're in after <see cref="Linphone.Core.Stop()" />. 
		/// </summary>
		Off = 0,
		/// <summary>
		/// Transient state for when we call <see cref="Linphone.Core.Start()" /> 
		/// </summary>
		Startup = 1,
		/// <summary>
		/// Indicates <see cref="Linphone.Core" /> has been started and is up and running. 
		/// </summary>
		On = 2,
		/// <summary>
		/// Transient state for when we call <see cref="Linphone.Core.Stop()" /> 
		/// </summary>
		Shutdown = 3,
		/// <summary>
		/// Transient state between Startup and On if there is a remote provisionning URI
		/// configured. 
		/// </summary>
		Configuring = 4,
		/// <summary>
		/// <see cref="Linphone.Core" /> state after being created by <see
		/// cref="Linphone.Factory.CreateCore()" />, generally followed by a call to <see
		/// cref="Linphone.Core.Start()" /> 
		/// </summary>
		Ready = 5,
	}

	/// <summary>
	/// Enum describing ICE states. 
	/// </summary>
	
	public enum IceState
	{
		/// <summary>
		/// ICE has not been activated for this call or stream. 
		/// </summary>
		NotActivated = 0,
		/// <summary>
		/// ICE processing has failed. 
		/// </summary>
		Failed = 1,
		/// <summary>
		/// ICE process is in progress. 
		/// </summary>
		InProgress = 2,
		/// <summary>
		/// ICE has established a direct connection to the remote host. 
		/// </summary>
		HostConnection = 3,
		/// <summary>
		/// ICE has established a connection to the remote host through one or several
		/// NATs. 
		/// </summary>
		ReflexiveConnection = 4,
		/// <summary>
		/// ICE has established a connection through a relay. 
		/// </summary>
		RelayConnection = 5,
	}

	
	public enum LimeState
	{
		/// <summary>
		/// Lime is not used at all. 
		/// </summary>
		Disabled = 0,
		/// <summary>
		/// Lime is always used. 
		/// </summary>
		Mandatory = 1,
		/// <summary>
		/// Lime is used only if we already shared a secret with remote. 
		/// </summary>
		Preferred = 2,
	}

	
	public enum LogCollectionState
	{
		Disabled = 0,
		Enabled = 1,
		EnabledWithoutPreviousLogHandler = 2,
	}

	/// <summary>
	/// Verbosity levels of log messages. 
	/// </summary>
	[Flags]
	public enum LogLevel
	{
		/// <summary>
		/// Level for debug messages. 
		/// </summary>
		Debug = 1,
		/// <summary>
		/// Level for traces. 
		/// </summary>
		Trace = 1<<1,
		/// <summary>
		/// Level for information messages. 
		/// </summary>
		Message = 1<<2,
		/// <summary>
		/// Level for warning messages. 
		/// </summary>
		Warning = 1<<3,
		/// <summary>
		/// Level for error messages. 
		/// </summary>
		Error = 1<<4,
		/// <summary>
		/// Level for fatal error messages. 
		/// </summary>
		Fatal = 1<<5,
	}

	/// <summary>
	/// Indicates for a given media the stream direction. 
	/// </summary>
	
	public enum MediaDirection
	{
		Invalid = -1,
		Inactive = 0,
		/// <summary>
		/// No active media not supported yet. 
		/// </summary>
		SendOnly = 1,
		/// <summary>
		/// Send only mode. 
		/// </summary>
		RecvOnly = 2,
		/// <summary>
		/// recv only mode 
		/// </summary>
		SendRecv = 3,
	}

	/// <summary>
	/// Enum describing type of media encryption types. 
	/// </summary>
	
	public enum MediaEncryption
	{
		/// <summary>
		/// No media encryption is used. 
		/// </summary>
		None = 0,
		/// <summary>
		/// Use SRTP media encryption. 
		/// </summary>
		SRTP = 1,
		/// <summary>
		/// Use ZRTP media encryption. 
		/// </summary>
		ZRTP = 2,
		/// <summary>
		/// Use DTLS media encryption. 
		/// </summary>
		DTLS = 3,
	}

	/// <summary>
	/// The state of a LinphonePlayer. 
	/// </summary>
	
	public enum PlayerState
	{
		/// <summary>
		/// No file is opened for playing. 
		/// </summary>
		Closed = 0,
		/// <summary>
		/// The player is paused. 
		/// </summary>
		Paused = 1,
		/// <summary>
		/// The player is playing. 
		/// </summary>
		Playing = 2,
	}

	/// <summary>
	/// Activities as defined in section 3.2 of RFC 4480. 
	/// </summary>
	
	public enum PresenceActivityType
	{
		/// <summary>
		/// The person has a calendar appointment, without specifying exactly of what type. 
		/// </summary>
		Appointment = 0,
		/// <summary>
		/// The person is physically away from all interactive communication devices. 
		/// </summary>
		Away = 1,
		/// <summary>
		/// The person is eating the first meal of the day, usually eaten in the morning. 
		/// </summary>
		Breakfast = 2,
		/// <summary>
		/// The person is busy, without further details. 
		/// </summary>
		Busy = 3,
		/// <summary>
		/// The person is having his or her main meal of the day, eaten in the evening or
		/// at midday. 
		/// </summary>
		Dinner = 4,
		/// <summary>
		/// This is a scheduled national or local holiday. 
		/// </summary>
		Holiday = 5,
		/// <summary>
		/// The person is riding in a vehicle, such as a car, but not steering. 
		/// </summary>
		InTransit = 6,
		/// <summary>
		/// The person is looking for (paid) work. 
		/// </summary>
		LookingForWork = 7,
		/// <summary>
		/// The person is eating his or her midday meal. 
		/// </summary>
		Lunch = 8,
		/// <summary>
		/// The person is scheduled for a meal, without specifying whether it is breakfast,
		/// lunch, or dinner, or some other meal. 
		/// </summary>
		Meal = 9,
		/// <summary>
		/// The person is in an assembly or gathering of people, as for a business, social,
		/// or religious purpose. 
		/// </summary>
		Meeting = 10,
		/// <summary>
		/// The person is talking on the telephone. 
		/// </summary>
		OnThePhone = 11,
		/// <summary>
		/// The person is engaged in an activity with no defined representation. 
		/// </summary>
		Other = 12,
		/// <summary>
		/// A performance is a sub-class of an appointment and includes musical,
		/// theatrical, and cinematic performances as well as lectures. 
		/// </summary>
		Performance = 13,
		/// <summary>
		/// The person will not return for the foreseeable future, e.g., because it is no
		/// longer working for the company. 
		/// </summary>
		PermanentAbsence = 14,
		/// <summary>
		/// The person is occupying himself or herself in amusement, sport, or other
		/// recreation. 
		/// </summary>
		Playing = 15,
		/// <summary>
		/// The person is giving a presentation, lecture, or participating in a formal
		/// round-table discussion. 
		/// </summary>
		Presentation = 16,
		/// <summary>
		/// The person is visiting stores in search of goods or services. 
		/// </summary>
		Shopping = 17,
		/// <summary>
		/// The person is sleeping. 
		/// </summary>
		Sleeping = 18,
		/// <summary>
		/// The person is observing an event, such as a sports event. 
		/// </summary>
		Spectator = 19,
		/// <summary>
		/// The person is controlling a vehicle, watercraft, or plane. 
		/// </summary>
		Steering = 20,
		/// <summary>
		/// The person is on a business or personal trip, but not necessarily in-transit. 
		/// </summary>
		Travel = 21,
		/// <summary>
		/// The person is watching television. 
		/// </summary>
		TV = 22,
		/// <summary>
		/// The activity of the person is unknown. 
		/// </summary>
		Unknown = 23,
		/// <summary>
		/// A period of time devoted to pleasure, rest, or relaxation. 
		/// </summary>
		Vacation = 24,
		/// <summary>
		/// The person is engaged in, typically paid, labor, as part of a profession or
		/// job. 
		/// </summary>
		Working = 25,
		/// <summary>
		/// The person is participating in religious rites. 
		/// </summary>
		Worship = 26,
	}

	/// <summary>
	/// Basic status as defined in section 4.1.4 of RFC 3863. 
	/// </summary>
	
	public enum PresenceBasicStatus
	{
		/// <summary>
		/// This value means that the associated contact element, if any, is ready to
		/// accept communication. 
		/// </summary>
		Open = 0,
		/// <summary>
		/// This value means that the associated contact element, if any, is unable to
		/// accept communication. 
		/// </summary>
		Closed = 1,
	}

	/// <summary>
	/// Defines privacy policy to apply as described by rfc3323. 
	/// </summary>
	
	public enum Privacy
	{
		/// <summary>
		/// Privacy services must not perform any privacy function. 
		/// </summary>
		None = 0,
		/// <summary>
		/// Request that privacy services provide a user-level privacy function. 
		/// </summary>
		User = 1,
		/// <summary>
		/// Request that privacy services modify headers that cannot be set arbitrarily by
		/// the user (Contact/Via). 
		/// </summary>
		Header = 2,
		/// <summary>
		/// Request that privacy services provide privacy for session media. 
		/// </summary>
		Session = 4,
		/// <summary>
		/// rfc3325 The presence of this privacy type in a Privacy header field indicates
		/// that the user would like the Network Asserted Identity to be kept private with
		/// respect to SIP entities outside the Trust Domain with which the user
		/// authenticated. 
		/// </summary>
		Id = 8,
		/// <summary>
		/// Privacy service must perform the specified services or fail the request. 
		/// </summary>
		Critical = 16,
		/// <summary>
		/// Special keyword to use privacy as defined either globally or by proxy using
		/// <see cref="Linphone.ProxyConfig.SetPrivacy()" /> 
		/// </summary>
		Default = 32768,
	}

	/// <summary>
	/// Enum for publish states. 
	/// </summary>
	
	public enum PublishState
	{
		/// <summary>
		/// Initial state, do not use. 
		/// </summary>
		None = 0,
		/// <summary>
		/// An outgoing publish was created and submitted. 
		/// </summary>
		Progress = 1,
		/// <summary>
		/// Publish is accepted. 
		/// </summary>
		Ok = 2,
		/// <summary>
		/// Publish encoutered an error, <see cref="Linphone.Event.GetReason()" /> gives
		/// reason code. 
		/// </summary>
		Error = 3,
		/// <summary>
		/// Publish is about to expire, only sent if [sip]->refresh_generic_publish
		/// property is set to 0. 
		/// </summary>
		Expiring = 4,
		/// <summary>
		/// Event has been un published. 
		/// </summary>
		Cleared = 5,
	}

	/// <summary>
	/// Enum describing various failure reasons or contextual information for some
	/// events. 
	/// </summary>
	
	public enum Reason
	{
		/// <summary>
		/// No reason has been set by the core. 
		/// </summary>
		None = 0,
		/// <summary>
		/// No response received from remote. 
		/// </summary>
		NoResponse = 1,
		/// <summary>
		/// Authentication failed due to bad credentials or resource forbidden. 
		/// </summary>
		Forbidden = 2,
		/// <summary>
		/// The call has been declined. 
		/// </summary>
		Declined = 3,
		/// <summary>
		/// Destination of the call was not found. 
		/// </summary>
		NotFound = 4,
		/// <summary>
		/// The call was not answered in time (request timeout) 
		/// </summary>
		NotAnswered = 5,
		/// <summary>
		/// Phone line was busy. 
		/// </summary>
		Busy = 6,
		/// <summary>
		/// Unsupported content. 
		/// </summary>
		UnsupportedContent = 7,
		/// <summary>
		/// Transport error: connection failures, disconnections etc... 
		/// </summary>
		IOError = 8,
		/// <summary>
		/// Do not disturb reason. 
		/// </summary>
		DoNotDisturb = 9,
		/// <summary>
		/// Operation is unauthorized because missing credential. 
		/// </summary>
		Unauthorized = 10,
		/// <summary>
		/// Operation is rejected due to incompatible or unsupported media parameters. 
		/// </summary>
		NotAcceptable = 11,
		/// <summary>
		/// Operation could not be executed by server or remote client because it didn't
		/// have any context for it. 
		/// </summary>
		NoMatch = 12,
		/// <summary>
		/// Resource moved permanently. 
		/// </summary>
		MovedPermanently = 13,
		/// <summary>
		/// Resource no longer exists. 
		/// </summary>
		Gone = 14,
		/// <summary>
		/// Temporarily unavailable. 
		/// </summary>
		TemporarilyUnavailable = 15,
		/// <summary>
		/// Address incomplete. 
		/// </summary>
		AddressIncomplete = 16,
		/// <summary>
		/// Not implemented. 
		/// </summary>
		NotImplemented = 17,
		/// <summary>
		/// Bad gateway. 
		/// </summary>
		BadGateway = 18,
		/// <summary>
		/// The received request contains a Session-Expires header field with a duration
		/// below the minimum timer. 
		/// </summary>
		SessionIntervalTooSmall = 19,
		/// <summary>
		/// Server timeout. 
		/// </summary>
		ServerTimeout = 20,
		/// <summary>
		/// Unknown reason. 
		/// </summary>
		Unknown = 21,
	}

	/// <summary>
	/// LinphoneRegistrationState describes proxy registration states. 
	/// </summary>
	
	public enum RegistrationState
	{
		/// <summary>
		/// Initial state for registrations. 
		/// </summary>
		None = 0,
		/// <summary>
		/// Registration is in progress. 
		/// </summary>
		Progress = 1,
		/// <summary>
		/// Registration is successful. 
		/// </summary>
		Ok = 2,
		/// <summary>
		/// Unregistration succeeded. 
		/// </summary>
		Cleared = 3,
		/// <summary>
		/// Registration failed. 
		/// </summary>
		Failed = 4,
	}

	/// <summary>
	/// LinphoneSecurityEventType is used to indicate the type of security event. 
	/// </summary>
	
	public enum SecurityEventType
	{
		/// <summary>
		/// Event is not a security event. 
		/// </summary>
		None = 0,
		/// <summary>
		/// Chatroom security level downgraded event. 
		/// </summary>
		SecurityLevelDowngraded = 1,
		/// <summary>
		/// Participant has exceeded the maximum number of device event. 
		/// </summary>
		ParticipantMaxDeviceCountExceeded = 2,
		/// <summary>
		/// Peer device instant messaging encryption identity key has changed event. 
		/// </summary>
		EncryptionIdentityKeyChanged = 3,
		/// <summary>
		/// Man in the middle detected event. 
		/// </summary>
		ManInTheMiddleDetected = 4,
	}

	/// <summary>
	/// Session Timers refresher. 
	/// </summary>
	
	public enum SessionExpiresRefresher
	{
		Unspecified = 0,
		UAS = 1,
		UAC = 2,
	}

	/// <summary>
	/// Enum describing the stream types. 
	/// </summary>
	
	public enum StreamType
	{
		Audio = 0,
		Video = 1,
		Text = 2,
		Unknown = 3,
	}

	/// <summary>
	/// Enum controlling behavior for incoming subscription request. 
	/// </summary>
	
	public enum SubscribePolicy
	{
		/// <summary>
		/// Does not automatically accept an incoming subscription request. 
		/// </summary>
		SPWait = 0,
		/// <summary>
		/// Rejects incoming subscription request. 
		/// </summary>
		SPDeny = 1,
		/// <summary>
		/// Automatically accepts a subscription request. 
		/// </summary>
		SPAccept = 2,
	}

	/// <summary>
	/// Enum for subscription direction (incoming or outgoing). 
	/// </summary>
	
	public enum SubscriptionDir
	{
		/// <summary>
		/// Incoming subscription. 
		/// </summary>
		Incoming = 0,
		/// <summary>
		/// Outgoing subscription. 
		/// </summary>
		Outgoing = 1,
		/// <summary>
		/// Invalid subscription direction. 
		/// </summary>
		InvalidDir = 2,
	}

	/// <summary>
	/// Enum for subscription states. 
	/// </summary>
	
	public enum SubscriptionState
	{
		/// <summary>
		/// Initial state, should not be used. 
		/// </summary>
		None = 0,
		/// <summary>
		/// An outgoing subcription was sent. 
		/// </summary>
		OutgoingProgress = 1,
		/// <summary>
		/// An incoming subcription is received. 
		/// </summary>
		IncomingReceived = 2,
		/// <summary>
		/// Subscription is pending, waiting for user approval. 
		/// </summary>
		Pending = 3,
		/// <summary>
		/// Subscription is accepted. 
		/// </summary>
		Active = 4,
		/// <summary>
		/// Subscription is terminated normally. 
		/// </summary>
		Terminated = 5,
		/// <summary>
		/// Subscription was terminated by an error, indicated by <see
		/// cref="Linphone.Event.GetReason()" /> 
		/// </summary>
		Error = 6,
		/// <summary>
		/// Subscription is about to expire, only sent if [sip]->refresh_generic_subscribe
		/// property is set to 0. 
		/// </summary>
		Expiring = 7,
	}

	/// <summary>
	/// Enum listing frequent telephony tones. 
	/// </summary>
	
	public enum ToneID
	{
		/// <summary>
		/// Not a tone. 
		/// </summary>
		Undefined = 0,
		/// <summary>
		/// Busy tone. 
		/// </summary>
		Busy = 1,
		CallWaiting = 2,
		/// <summary>
		/// Call waiting tone. 
		/// </summary>
		CallOnHold = 3,
		/// <summary>
		/// Call on hold tone. 
		/// </summary>
		CallLost = 4,
	}

	/// <summary>
	/// Enum describing transport type for LinphoneAddress. 
	/// </summary>
	
	public enum TransportType
	{
		Udp = 0,
		Tcp = 1,
		Tls = 2,
		Dtls = 3,
	}

	/// <summary>
	/// Enum describing the tunnel modes. 
	/// </summary>
	
	public enum TunnelMode
	{
		/// <summary>
		/// The tunnel is disabled. 
		/// </summary>
		Disable = 0,
		/// <summary>
		/// The tunnel is enabled. 
		/// </summary>
		Enable = 1,
		/// <summary>
		/// The tunnel is enabled automatically if it is required. 
		/// </summary>
		Auto = 2,
	}

	/// <summary>
	/// Enum describing uPnP states. 
	/// </summary>
	
	public enum UpnpState
	{
		/// <summary>
		/// uPnP is not activate 
		/// </summary>
		Idle = 0,
		/// <summary>
		/// uPnP process is in progress 
		/// </summary>
		Pending = 1,
		/// <summary>
		/// Internal use: Only used by port binding. 
		/// </summary>
		Adding = 2,
		/// <summary>
		/// Internal use: Only used by port binding. 
		/// </summary>
		Removing = 3,
		/// <summary>
		/// uPnP is not available 
		/// </summary>
		NotAvailable = 4,
		/// <summary>
		/// uPnP is enabled 
		/// </summary>
		Ok = 5,
		/// <summary>
		/// uPnP processing has failed 
		/// </summary>
		Ko = 6,
		/// <summary>
		/// IGD router is blacklisted. 
		/// </summary>
		Blacklisted = 7,
	}

	/// <summary>
	/// Enum describing the result of a version update check. 
	/// </summary>
	
	public enum VersionUpdateCheckResult
	{
		UpToDate = 0,
		NewVersionAvailable = 1,
		Error = 2,
	}

	/// <summary>
	/// Enum describing the types of argument for LinphoneXmlRpcRequest. 
	/// </summary>
	
	public enum XmlRpcArgType
	{
		None = 0,
		Int = 1,
		String = 2,
		StringStruct = 3,
	}

	/// <summary>
	/// Enum describing the status of a LinphoneXmlRpcRequest. 
	/// </summary>
	
	public enum XmlRpcStatus
	{
		Pending = 0,
		Ok = 1,
		Failed = 2,
	}

	/// <summary>
	/// Enum describing the ZRTP SAS validation status of a peer URI. 
	/// </summary>
	
	public enum ZrtpPeerStatus
	{
		/// <summary>
		/// Peer URI unkown or never validated/invalidated the SAS. 
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Peer URI SAS rejected in database. 
		/// </summary>
		Invalid = 1,
		/// <summary>
		/// Peer URI SAS validated in database. 
		/// </summary>
		Valid = 2,
	}

#endregion

#region Listeners
	[StructLayout(LayoutKind.Sequential)]
	public class AccountCreatorListener : LinphoneObject
	{
        ~AccountCreatorListener()
        {
            unregister();
        }

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_account_creator_cbs_set_user_data(IntPtr thiz, IntPtr listener);

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_account_creator_cbs_get_user_data(IntPtr thiz);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_activate_account(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_activate_account(IntPtr thiz, OnActivateAccountDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnActivateAccountDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnActivateAccountDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnActivateAccountDelegatePrivate on_activate_account_private;
		private OnActivateAccountDelegate on_activate_account_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnActivateAccountDelegatePrivate))]
#endif
		private static void on_activate_account(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_activate_account_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnActivateAccountDelegate OnActivateAccount
		{
			get
			{
				return on_activate_account_public;
			}
			set
			{
				on_activate_account_public = value;
#if WINDOWS_UWP
				on_activate_account_private = on_activate_account;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_activate_account_private);
				linphone_account_creator_cbs_set_activate_account(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_activate_account(nativePtr, on_activate_account);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_activate_alias(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_activate_alias(IntPtr thiz, OnActivateAliasDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnActivateAliasDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnActivateAliasDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnActivateAliasDelegatePrivate on_activate_alias_private;
		private OnActivateAliasDelegate on_activate_alias_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnActivateAliasDelegatePrivate))]
#endif
		private static void on_activate_alias(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_activate_alias_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnActivateAliasDelegate OnActivateAlias
		{
			get
			{
				return on_activate_alias_public;
			}
			set
			{
				on_activate_alias_public = value;
#if WINDOWS_UWP
				on_activate_alias_private = on_activate_alias;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_activate_alias_private);
				linphone_account_creator_cbs_set_activate_alias(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_activate_alias(nativePtr, on_activate_alias);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_is_account_linked(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_is_account_linked(IntPtr thiz, OnIsAccountLinkedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnIsAccountLinkedDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnIsAccountLinkedDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnIsAccountLinkedDelegatePrivate on_is_account_linked_private;
		private OnIsAccountLinkedDelegate on_is_account_linked_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnIsAccountLinkedDelegatePrivate))]
#endif
		private static void on_is_account_linked(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_is_account_linked_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnIsAccountLinkedDelegate OnIsAccountLinked
		{
			get
			{
				return on_is_account_linked_public;
			}
			set
			{
				on_is_account_linked_public = value;
#if WINDOWS_UWP
				on_is_account_linked_private = on_is_account_linked;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_is_account_linked_private);
				linphone_account_creator_cbs_set_is_account_linked(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_is_account_linked(nativePtr, on_is_account_linked);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_link_account(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_link_account(IntPtr thiz, OnLinkAccountDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnLinkAccountDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnLinkAccountDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnLinkAccountDelegatePrivate on_link_account_private;
		private OnLinkAccountDelegate on_link_account_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnLinkAccountDelegatePrivate))]
#endif
		private static void on_link_account(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_link_account_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnLinkAccountDelegate OnLinkAccount
		{
			get
			{
				return on_link_account_public;
			}
			set
			{
				on_link_account_public = value;
#if WINDOWS_UWP
				on_link_account_private = on_link_account;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_link_account_private);
				linphone_account_creator_cbs_set_link_account(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_link_account(nativePtr, on_link_account);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_is_alias_used(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_is_alias_used(IntPtr thiz, OnIsAliasUsedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnIsAliasUsedDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnIsAliasUsedDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnIsAliasUsedDelegatePrivate on_is_alias_used_private;
		private OnIsAliasUsedDelegate on_is_alias_used_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnIsAliasUsedDelegatePrivate))]
#endif
		private static void on_is_alias_used(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_is_alias_used_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnIsAliasUsedDelegate OnIsAliasUsed
		{
			get
			{
				return on_is_alias_used_public;
			}
			set
			{
				on_is_alias_used_public = value;
#if WINDOWS_UWP
				on_is_alias_used_private = on_is_alias_used;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_is_alias_used_private);
				linphone_account_creator_cbs_set_is_alias_used(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_is_alias_used(nativePtr, on_is_alias_used);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_is_account_activated(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_is_account_activated(IntPtr thiz, OnIsAccountActivatedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnIsAccountActivatedDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnIsAccountActivatedDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnIsAccountActivatedDelegatePrivate on_is_account_activated_private;
		private OnIsAccountActivatedDelegate on_is_account_activated_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnIsAccountActivatedDelegatePrivate))]
#endif
		private static void on_is_account_activated(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_is_account_activated_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnIsAccountActivatedDelegate OnIsAccountActivated
		{
			get
			{
				return on_is_account_activated_public;
			}
			set
			{
				on_is_account_activated_public = value;
#if WINDOWS_UWP
				on_is_account_activated_private = on_is_account_activated;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_is_account_activated_private);
				linphone_account_creator_cbs_set_is_account_activated(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_is_account_activated(nativePtr, on_is_account_activated);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_login_linphone_account(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_login_linphone_account(IntPtr thiz, OnLoginLinphoneAccountDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnLoginLinphoneAccountDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnLoginLinphoneAccountDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnLoginLinphoneAccountDelegatePrivate on_login_linphone_account_private;
		private OnLoginLinphoneAccountDelegate on_login_linphone_account_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnLoginLinphoneAccountDelegatePrivate))]
#endif
		private static void on_login_linphone_account(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_login_linphone_account_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnLoginLinphoneAccountDelegate OnLoginLinphoneAccount
		{
			get
			{
				return on_login_linphone_account_public;
			}
			set
			{
				on_login_linphone_account_public = value;
#if WINDOWS_UWP
				on_login_linphone_account_private = on_login_linphone_account;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_login_linphone_account_private);
				linphone_account_creator_cbs_set_login_linphone_account(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_login_linphone_account(nativePtr, on_login_linphone_account);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_is_account_exist(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_is_account_exist(IntPtr thiz, OnIsAccountExistDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnIsAccountExistDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnIsAccountExistDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnIsAccountExistDelegatePrivate on_is_account_exist_private;
		private OnIsAccountExistDelegate on_is_account_exist_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnIsAccountExistDelegatePrivate))]
#endif
		private static void on_is_account_exist(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_is_account_exist_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnIsAccountExistDelegate OnIsAccountExist
		{
			get
			{
				return on_is_account_exist_public;
			}
			set
			{
				on_is_account_exist_public = value;
#if WINDOWS_UWP
				on_is_account_exist_private = on_is_account_exist;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_is_account_exist_private);
				linphone_account_creator_cbs_set_is_account_exist(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_is_account_exist(nativePtr, on_is_account_exist);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_update_account(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_update_account(IntPtr thiz, OnUpdateAccountDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnUpdateAccountDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnUpdateAccountDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnUpdateAccountDelegatePrivate on_update_account_private;
		private OnUpdateAccountDelegate on_update_account_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnUpdateAccountDelegatePrivate))]
#endif
		private static void on_update_account(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_update_account_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnUpdateAccountDelegate OnUpdateAccount
		{
			get
			{
				return on_update_account_public;
			}
			set
			{
				on_update_account_public = value;
#if WINDOWS_UWP
				on_update_account_private = on_update_account;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_update_account_private);
				linphone_account_creator_cbs_set_update_account(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_update_account(nativePtr, on_update_account);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_recover_account(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_recover_account(IntPtr thiz, OnRecoverAccountDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnRecoverAccountDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnRecoverAccountDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnRecoverAccountDelegatePrivate on_recover_account_private;
		private OnRecoverAccountDelegate on_recover_account_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnRecoverAccountDelegatePrivate))]
#endif
		private static void on_recover_account(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_recover_account_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnRecoverAccountDelegate OnRecoverAccount
		{
			get
			{
				return on_recover_account_public;
			}
			set
			{
				on_recover_account_public = value;
#if WINDOWS_UWP
				on_recover_account_private = on_recover_account;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_recover_account_private);
				linphone_account_creator_cbs_set_recover_account(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_recover_account(nativePtr, on_recover_account);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_account_creator_cbs_set_create_account(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_account_creator_cbs_set_create_account(IntPtr thiz, OnCreateAccountDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnCreateAccountDelegatePrivate(IntPtr creator, int status, string resp);

		public delegate void OnCreateAccountDelegate(Linphone.AccountCreator creator, Linphone.AccountCreatorStatus status, string resp);
		private OnCreateAccountDelegatePrivate on_create_account_private;
		private OnCreateAccountDelegate on_create_account_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnCreateAccountDelegatePrivate))]
#endif
		private static void on_create_account(IntPtr creator, int status, string resp)
		{
			AccountCreator thiz = fromNativePtr<AccountCreator>(creator);
			AccountCreatorListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_create_account_public?.Invoke(thiz, (Linphone.AccountCreatorStatus)status, resp);
			}
		}

		public OnCreateAccountDelegate OnCreateAccount
		{
			get
			{
				return on_create_account_public;
			}
			set
			{
				on_create_account_public = value;
#if WINDOWS_UWP
				on_create_account_private = on_create_account;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_create_account_private);
				linphone_account_creator_cbs_set_create_account(nativePtr, cb);
#else
				linphone_account_creator_cbs_set_create_account(nativePtr, on_create_account);
#endif
			}
		}

		internal void register() {
			IntPtr listener = linphone_account_creator_cbs_get_user_data(nativePtr);
			if (listener == IntPtr.Zero)
			{
				GCHandle _handle = GCHandle.Alloc(this, GCHandleType.Normal);
				listener = GCHandle.ToIntPtr(_handle);
			} else
			{
				GCHandle _handle = GCHandle.FromIntPtr(listener);
				if (_handle.Target == this)
				{
					return;
				} else
				{
					_handle.Free();
					_handle = GCHandle.Alloc(this, GCHandleType.Normal);
					listener = GCHandle.ToIntPtr(_handle);
				}
			}
			linphone_account_creator_cbs_set_user_data(nativePtr, listener);
		}

		internal void unregister() {
			IntPtr listener = linphone_account_creator_cbs_get_user_data(nativePtr);
			linphone_account_creator_cbs_set_user_data(nativePtr, IntPtr.Zero);
			if (listener != IntPtr.Zero)
			{
				GCHandle.FromIntPtr(listener).Free();
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class CallListener : LinphoneObject
	{
        ~CallListener()
        {
            unregister();
        }

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_call_cbs_set_user_data(IntPtr thiz, IntPtr listener);

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_call_cbs_get_user_data(IntPtr thiz);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_camera_not_working(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_camera_not_working(IntPtr thiz, OnCameraNotWorkingDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnCameraNotWorkingDelegatePrivate(IntPtr call, string cameraName);

		public delegate void OnCameraNotWorkingDelegate(Linphone.Call call, string cameraName);
		private OnCameraNotWorkingDelegatePrivate on_camera_not_working_private;
		private OnCameraNotWorkingDelegate on_camera_not_working_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnCameraNotWorkingDelegatePrivate))]
#endif
		private static void on_camera_not_working(IntPtr call, string cameraName)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_camera_not_working_public?.Invoke(thiz, cameraName);
			}
		}

		public OnCameraNotWorkingDelegate OnCameraNotWorking
		{
			get
			{
				return on_camera_not_working_public;
			}
			set
			{
				on_camera_not_working_public = value;
#if WINDOWS_UWP
				on_camera_not_working_private = on_camera_not_working;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_camera_not_working_private);
				linphone_call_cbs_set_camera_not_working(nativePtr, cb);
#else
				linphone_call_cbs_set_camera_not_working(nativePtr, on_camera_not_working);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_snapshot_taken(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_snapshot_taken(IntPtr thiz, OnSnapshotTakenDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnSnapshotTakenDelegatePrivate(IntPtr call, string filepath);

		public delegate void OnSnapshotTakenDelegate(Linphone.Call call, string filepath);
		private OnSnapshotTakenDelegatePrivate on_snapshot_taken_private;
		private OnSnapshotTakenDelegate on_snapshot_taken_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnSnapshotTakenDelegatePrivate))]
#endif
		private static void on_snapshot_taken(IntPtr call, string filepath)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_snapshot_taken_public?.Invoke(thiz, filepath);
			}
		}

		public OnSnapshotTakenDelegate OnSnapshotTaken
		{
			get
			{
				return on_snapshot_taken_public;
			}
			set
			{
				on_snapshot_taken_public = value;
#if WINDOWS_UWP
				on_snapshot_taken_private = on_snapshot_taken;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_snapshot_taken_private);
				linphone_call_cbs_set_snapshot_taken(nativePtr, cb);
#else
				linphone_call_cbs_set_snapshot_taken(nativePtr, on_snapshot_taken);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_state_changed(IntPtr thiz, OnStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnStateChangedDelegatePrivate(IntPtr call, int cstate, string message);

		public delegate void OnStateChangedDelegate(Linphone.Call call, Linphone.CallState cstate, string message);
		private OnStateChangedDelegatePrivate on_state_changed_private;
		private OnStateChangedDelegate on_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnStateChangedDelegatePrivate))]
#endif
		private static void on_state_changed(IntPtr call, int cstate, string message)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_state_changed_public?.Invoke(thiz, (Linphone.CallState)cstate, message);
			}
		}

		public OnStateChangedDelegate OnStateChanged
		{
			get
			{
				return on_state_changed_public;
			}
			set
			{
				on_state_changed_public = value;
#if WINDOWS_UWP
				on_state_changed_private = on_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_state_changed_private);
				linphone_call_cbs_set_state_changed(nativePtr, cb);
#else
				linphone_call_cbs_set_state_changed(nativePtr, on_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_transfer_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_transfer_state_changed(IntPtr thiz, OnTransferStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnTransferStateChangedDelegatePrivate(IntPtr call, int cstate);

		public delegate void OnTransferStateChangedDelegate(Linphone.Call call, Linphone.CallState cstate);
		private OnTransferStateChangedDelegatePrivate on_transfer_state_changed_private;
		private OnTransferStateChangedDelegate on_transfer_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnTransferStateChangedDelegatePrivate))]
#endif
		private static void on_transfer_state_changed(IntPtr call, int cstate)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_transfer_state_changed_public?.Invoke(thiz, (Linphone.CallState)cstate);
			}
		}

		public OnTransferStateChangedDelegate OnTransferStateChanged
		{
			get
			{
				return on_transfer_state_changed_public;
			}
			set
			{
				on_transfer_state_changed_public = value;
#if WINDOWS_UWP
				on_transfer_state_changed_private = on_transfer_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_transfer_state_changed_private);
				linphone_call_cbs_set_transfer_state_changed(nativePtr, cb);
#else
				linphone_call_cbs_set_transfer_state_changed(nativePtr, on_transfer_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_tmmbr_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_tmmbr_received(IntPtr thiz, OnTmmbrReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnTmmbrReceivedDelegatePrivate(IntPtr call, int streamIndex, int tmmbr);

		public delegate void OnTmmbrReceivedDelegate(Linphone.Call call, int streamIndex, int tmmbr);
		private OnTmmbrReceivedDelegatePrivate on_tmmbr_received_private;
		private OnTmmbrReceivedDelegate on_tmmbr_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnTmmbrReceivedDelegatePrivate))]
#endif
		private static void on_tmmbr_received(IntPtr call, int streamIndex, int tmmbr)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_tmmbr_received_public?.Invoke(thiz, streamIndex, tmmbr);
			}
		}

		public OnTmmbrReceivedDelegate OnTmmbrReceived
		{
			get
			{
				return on_tmmbr_received_public;
			}
			set
			{
				on_tmmbr_received_public = value;
#if WINDOWS_UWP
				on_tmmbr_received_private = on_tmmbr_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_tmmbr_received_private);
				linphone_call_cbs_set_tmmbr_received(nativePtr, cb);
#else
				linphone_call_cbs_set_tmmbr_received(nativePtr, on_tmmbr_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_info_message_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_info_message_received(IntPtr thiz, OnInfoMessageReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnInfoMessageReceivedDelegatePrivate(IntPtr call, IntPtr msg);

		public delegate void OnInfoMessageReceivedDelegate(Linphone.Call call, Linphone.InfoMessage msg);
		private OnInfoMessageReceivedDelegatePrivate on_info_message_received_private;
		private OnInfoMessageReceivedDelegate on_info_message_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnInfoMessageReceivedDelegatePrivate))]
#endif
		private static void on_info_message_received(IntPtr call, IntPtr msg)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_info_message_received_public?.Invoke(thiz, fromNativePtr<Linphone.InfoMessage>(msg));
			}
		}

		public OnInfoMessageReceivedDelegate OnInfoMessageReceived
		{
			get
			{
				return on_info_message_received_public;
			}
			set
			{
				on_info_message_received_public = value;
#if WINDOWS_UWP
				on_info_message_received_private = on_info_message_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_info_message_received_private);
				linphone_call_cbs_set_info_message_received(nativePtr, cb);
#else
				linphone_call_cbs_set_info_message_received(nativePtr, on_info_message_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_encryption_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_encryption_changed(IntPtr thiz, OnEncryptionChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnEncryptionChangedDelegatePrivate(IntPtr call, char on, string authenticationToken);

		public delegate void OnEncryptionChangedDelegate(Linphone.Call call, bool on, string authenticationToken);
		private OnEncryptionChangedDelegatePrivate on_encryption_changed_private;
		private OnEncryptionChangedDelegate on_encryption_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnEncryptionChangedDelegatePrivate))]
#endif
		private static void on_encryption_changed(IntPtr call, char on, string authenticationToken)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_encryption_changed_public?.Invoke(thiz, on == 0, authenticationToken);
			}
		}

		public OnEncryptionChangedDelegate OnEncryptionChanged
		{
			get
			{
				return on_encryption_changed_public;
			}
			set
			{
				on_encryption_changed_public = value;
#if WINDOWS_UWP
				on_encryption_changed_private = on_encryption_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_encryption_changed_private);
				linphone_call_cbs_set_encryption_changed(nativePtr, cb);
#else
				linphone_call_cbs_set_encryption_changed(nativePtr, on_encryption_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_ack_processing(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_ack_processing(IntPtr thiz, OnAckProcessingDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnAckProcessingDelegatePrivate(IntPtr call, IntPtr ack, char isReceived);

		public delegate void OnAckProcessingDelegate(Linphone.Call call, Linphone.Headers ack, bool isReceived);
		private OnAckProcessingDelegatePrivate on_ack_processing_private;
		private OnAckProcessingDelegate on_ack_processing_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnAckProcessingDelegatePrivate))]
#endif
		private static void on_ack_processing(IntPtr call, IntPtr ack, char isReceived)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_ack_processing_public?.Invoke(thiz, fromNativePtr<Linphone.Headers>(ack), isReceived == 0);
			}
		}

		public OnAckProcessingDelegate OnAckProcessing
		{
			get
			{
				return on_ack_processing_public;
			}
			set
			{
				on_ack_processing_public = value;
#if WINDOWS_UWP
				on_ack_processing_private = on_ack_processing;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_ack_processing_private);
				linphone_call_cbs_set_ack_processing(nativePtr, cb);
#else
				linphone_call_cbs_set_ack_processing(nativePtr, on_ack_processing);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_dtmf_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_dtmf_received(IntPtr thiz, OnDtmfReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnDtmfReceivedDelegatePrivate(IntPtr call, int dtmf);

		public delegate void OnDtmfReceivedDelegate(Linphone.Call call, int dtmf);
		private OnDtmfReceivedDelegatePrivate on_dtmf_received_private;
		private OnDtmfReceivedDelegate on_dtmf_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnDtmfReceivedDelegatePrivate))]
#endif
		private static void on_dtmf_received(IntPtr call, int dtmf)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_dtmf_received_public?.Invoke(thiz, dtmf);
			}
		}

		public OnDtmfReceivedDelegate OnDtmfReceived
		{
			get
			{
				return on_dtmf_received_public;
			}
			set
			{
				on_dtmf_received_public = value;
#if WINDOWS_UWP
				on_dtmf_received_private = on_dtmf_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_dtmf_received_private);
				linphone_call_cbs_set_dtmf_received(nativePtr, cb);
#else
				linphone_call_cbs_set_dtmf_received(nativePtr, on_dtmf_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_next_video_frame_decoded(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_next_video_frame_decoded(IntPtr thiz, OnNextVideoFrameDecodedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnNextVideoFrameDecodedDelegatePrivate(IntPtr call);

		public delegate void OnNextVideoFrameDecodedDelegate(Linphone.Call call);
		private OnNextVideoFrameDecodedDelegatePrivate on_next_video_frame_decoded_private;
		private OnNextVideoFrameDecodedDelegate on_next_video_frame_decoded_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnNextVideoFrameDecodedDelegatePrivate))]
#endif
		private static void on_next_video_frame_decoded(IntPtr call)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_next_video_frame_decoded_public?.Invoke(thiz);
			}
		}

		public OnNextVideoFrameDecodedDelegate OnNextVideoFrameDecoded
		{
			get
			{
				return on_next_video_frame_decoded_public;
			}
			set
			{
				on_next_video_frame_decoded_public = value;
#if WINDOWS_UWP
				on_next_video_frame_decoded_private = on_next_video_frame_decoded;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_next_video_frame_decoded_private);
				linphone_call_cbs_set_next_video_frame_decoded(nativePtr, cb);
#else
				linphone_call_cbs_set_next_video_frame_decoded(nativePtr, on_next_video_frame_decoded);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_stats_updated(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_stats_updated(IntPtr thiz, OnStatsUpdatedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnStatsUpdatedDelegatePrivate(IntPtr call, IntPtr stats);

		public delegate void OnStatsUpdatedDelegate(Linphone.Call call, Linphone.CallStats stats);
		private OnStatsUpdatedDelegatePrivate on_stats_updated_private;
		private OnStatsUpdatedDelegate on_stats_updated_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnStatsUpdatedDelegatePrivate))]
#endif
		private static void on_stats_updated(IntPtr call, IntPtr stats)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_stats_updated_public?.Invoke(thiz, fromNativePtr<Linphone.CallStats>(stats));
			}
		}

		public OnStatsUpdatedDelegate OnStatsUpdated
		{
			get
			{
				return on_stats_updated_public;
			}
			set
			{
				on_stats_updated_public = value;
#if WINDOWS_UWP
				on_stats_updated_private = on_stats_updated;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_stats_updated_private);
				linphone_call_cbs_set_stats_updated(nativePtr, cb);
#else
				linphone_call_cbs_set_stats_updated(nativePtr, on_stats_updated);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_call_cbs_set_audio_device_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_call_cbs_set_audio_device_changed(IntPtr thiz, OnAudioDeviceChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnAudioDeviceChangedDelegatePrivate(IntPtr call, IntPtr audioDevice);

		public delegate void OnAudioDeviceChangedDelegate(Linphone.Call call, Linphone.AudioDevice audioDevice);
		private OnAudioDeviceChangedDelegatePrivate on_audio_device_changed_private;
		private OnAudioDeviceChangedDelegate on_audio_device_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnAudioDeviceChangedDelegatePrivate))]
#endif
		private static void on_audio_device_changed(IntPtr call, IntPtr audioDevice)
		{
			Call thiz = fromNativePtr<Call>(call);
			CallListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_audio_device_changed_public?.Invoke(thiz, fromNativePtr<Linphone.AudioDevice>(audioDevice));
			}
		}

		public OnAudioDeviceChangedDelegate OnAudioDeviceChanged
		{
			get
			{
				return on_audio_device_changed_public;
			}
			set
			{
				on_audio_device_changed_public = value;
#if WINDOWS_UWP
				on_audio_device_changed_private = on_audio_device_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_audio_device_changed_private);
				linphone_call_cbs_set_audio_device_changed(nativePtr, cb);
#else
				linphone_call_cbs_set_audio_device_changed(nativePtr, on_audio_device_changed);
#endif
			}
		}

		internal void register() {
			IntPtr listener = linphone_call_cbs_get_user_data(nativePtr);
			if (listener == IntPtr.Zero)
			{
				GCHandle _handle = GCHandle.Alloc(this, GCHandleType.Normal);
				listener = GCHandle.ToIntPtr(_handle);
			} else
			{
				GCHandle _handle = GCHandle.FromIntPtr(listener);
				if (_handle.Target == this)
				{
					return;
				} else
				{
					_handle.Free();
					_handle = GCHandle.Alloc(this, GCHandleType.Normal);
					listener = GCHandle.ToIntPtr(_handle);
				}
			}
			linphone_call_cbs_set_user_data(nativePtr, listener);
		}

		internal void unregister() {
			IntPtr listener = linphone_call_cbs_get_user_data(nativePtr);
			linphone_call_cbs_set_user_data(nativePtr, IntPtr.Zero);
			if (listener != IntPtr.Zero)
			{
				GCHandle.FromIntPtr(listener).Free();
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class ChatMessageListener : LinphoneObject
	{
        ~ChatMessageListener()
        {
            unregister();
        }

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_chat_message_cbs_set_user_data(IntPtr thiz, IntPtr listener);

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_chat_message_cbs_get_user_data(IntPtr thiz);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_message_cbs_set_participant_imdn_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_message_cbs_set_participant_imdn_state_changed(IntPtr thiz, OnParticipantImdnStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnParticipantImdnStateChangedDelegatePrivate(IntPtr msg, IntPtr state);

		public delegate void OnParticipantImdnStateChangedDelegate(Linphone.ChatMessage msg, Linphone.ParticipantImdnState state);
		private OnParticipantImdnStateChangedDelegatePrivate on_participant_imdn_state_changed_private;
		private OnParticipantImdnStateChangedDelegate on_participant_imdn_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnParticipantImdnStateChangedDelegatePrivate))]
#endif
		private static void on_participant_imdn_state_changed(IntPtr msg, IntPtr state)
		{
			ChatMessage thiz = fromNativePtr<ChatMessage>(msg);
			ChatMessageListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_participant_imdn_state_changed_public?.Invoke(thiz, fromNativePtr<Linphone.ParticipantImdnState>(state));
			}
		}

		public OnParticipantImdnStateChangedDelegate OnParticipantImdnStateChanged
		{
			get
			{
				return on_participant_imdn_state_changed_public;
			}
			set
			{
				on_participant_imdn_state_changed_public = value;
#if WINDOWS_UWP
				on_participant_imdn_state_changed_private = on_participant_imdn_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_participant_imdn_state_changed_private);
				linphone_chat_message_cbs_set_participant_imdn_state_changed(nativePtr, cb);
#else
				linphone_chat_message_cbs_set_participant_imdn_state_changed(nativePtr, on_participant_imdn_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_message_cbs_set_file_transfer_recv(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_message_cbs_set_file_transfer_recv(IntPtr thiz, OnFileTransferRecvDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnFileTransferRecvDelegatePrivate(IntPtr msg, IntPtr content, IntPtr buffer);

		public delegate void OnFileTransferRecvDelegate(Linphone.ChatMessage msg, Linphone.Content content, Linphone.Buffer buffer);
		private OnFileTransferRecvDelegatePrivate on_file_transfer_recv_private;
		private OnFileTransferRecvDelegate on_file_transfer_recv_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnFileTransferRecvDelegatePrivate))]
#endif
		private static void on_file_transfer_recv(IntPtr msg, IntPtr content, IntPtr buffer)
		{
			ChatMessage thiz = fromNativePtr<ChatMessage>(msg);
			ChatMessageListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_file_transfer_recv_public?.Invoke(thiz, fromNativePtr<Linphone.Content>(content), fromNativePtr<Linphone.Buffer>(buffer));
			}
		}

		public OnFileTransferRecvDelegate OnFileTransferRecv
		{
			get
			{
				return on_file_transfer_recv_public;
			}
			set
			{
				on_file_transfer_recv_public = value;
#if WINDOWS_UWP
				on_file_transfer_recv_private = on_file_transfer_recv;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_file_transfer_recv_private);
				linphone_chat_message_cbs_set_file_transfer_recv(nativePtr, cb);
#else
				linphone_chat_message_cbs_set_file_transfer_recv(nativePtr, on_file_transfer_recv);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_message_cbs_set_file_transfer_send(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_message_cbs_set_file_transfer_send(IntPtr thiz, OnFileTransferSendDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnFileTransferSendDelegatePrivate(IntPtr msg, IntPtr content, long offset, long size);

		public delegate void OnFileTransferSendDelegate(Linphone.ChatMessage msg, Linphone.Content content, long offset, long size);
		private OnFileTransferSendDelegatePrivate on_file_transfer_send_private;
		private OnFileTransferSendDelegate on_file_transfer_send_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnFileTransferSendDelegatePrivate))]
#endif
		private static void on_file_transfer_send(IntPtr msg, IntPtr content, long offset, long size)
		{
			ChatMessage thiz = fromNativePtr<ChatMessage>(msg);
			ChatMessageListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_file_transfer_send_public?.Invoke(thiz, fromNativePtr<Linphone.Content>(content), offset, size);
			}
		}

		public OnFileTransferSendDelegate OnFileTransferSend
		{
			get
			{
				return on_file_transfer_send_public;
			}
			set
			{
				on_file_transfer_send_public = value;
#if WINDOWS_UWP
				on_file_transfer_send_private = on_file_transfer_send;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_file_transfer_send_private);
				linphone_chat_message_cbs_set_file_transfer_send(nativePtr, cb);
#else
				linphone_chat_message_cbs_set_file_transfer_send(nativePtr, on_file_transfer_send);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_message_cbs_set_ephemeral_message_timer_started(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_message_cbs_set_ephemeral_message_timer_started(IntPtr thiz, OnEphemeralMessageTimerStartedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnEphemeralMessageTimerStartedDelegatePrivate(IntPtr msg);

		public delegate void OnEphemeralMessageTimerStartedDelegate(Linphone.ChatMessage msg);
		private OnEphemeralMessageTimerStartedDelegatePrivate on_ephemeral_message_timer_started_private;
		private OnEphemeralMessageTimerStartedDelegate on_ephemeral_message_timer_started_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnEphemeralMessageTimerStartedDelegatePrivate))]
#endif
		private static void on_ephemeral_message_timer_started(IntPtr msg)
		{
			ChatMessage thiz = fromNativePtr<ChatMessage>(msg);
			ChatMessageListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_ephemeral_message_timer_started_public?.Invoke(thiz);
			}
		}

		public OnEphemeralMessageTimerStartedDelegate OnEphemeralMessageTimerStarted
		{
			get
			{
				return on_ephemeral_message_timer_started_public;
			}
			set
			{
				on_ephemeral_message_timer_started_public = value;
#if WINDOWS_UWP
				on_ephemeral_message_timer_started_private = on_ephemeral_message_timer_started;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_ephemeral_message_timer_started_private);
				linphone_chat_message_cbs_set_ephemeral_message_timer_started(nativePtr, cb);
#else
				linphone_chat_message_cbs_set_ephemeral_message_timer_started(nativePtr, on_ephemeral_message_timer_started);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_message_cbs_set_file_transfer_progress_indication(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_message_cbs_set_file_transfer_progress_indication(IntPtr thiz, OnFileTransferProgressIndicationDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnFileTransferProgressIndicationDelegatePrivate(IntPtr msg, IntPtr content, long offset, long total);

		public delegate void OnFileTransferProgressIndicationDelegate(Linphone.ChatMessage msg, Linphone.Content content, long offset, long total);
		private OnFileTransferProgressIndicationDelegatePrivate on_file_transfer_progress_indication_private;
		private OnFileTransferProgressIndicationDelegate on_file_transfer_progress_indication_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnFileTransferProgressIndicationDelegatePrivate))]
#endif
		private static void on_file_transfer_progress_indication(IntPtr msg, IntPtr content, long offset, long total)
		{
			ChatMessage thiz = fromNativePtr<ChatMessage>(msg);
			ChatMessageListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_file_transfer_progress_indication_public?.Invoke(thiz, fromNativePtr<Linphone.Content>(content), offset, total);
			}
		}

		public OnFileTransferProgressIndicationDelegate OnFileTransferProgressIndication
		{
			get
			{
				return on_file_transfer_progress_indication_public;
			}
			set
			{
				on_file_transfer_progress_indication_public = value;
#if WINDOWS_UWP
				on_file_transfer_progress_indication_private = on_file_transfer_progress_indication;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_file_transfer_progress_indication_private);
				linphone_chat_message_cbs_set_file_transfer_progress_indication(nativePtr, cb);
#else
				linphone_chat_message_cbs_set_file_transfer_progress_indication(nativePtr, on_file_transfer_progress_indication);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_message_cbs_set_msg_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_message_cbs_set_msg_state_changed(IntPtr thiz, OnMsgStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnMsgStateChangedDelegatePrivate(IntPtr msg, int state);

		public delegate void OnMsgStateChangedDelegate(Linphone.ChatMessage msg, Linphone.ChatMessageState state);
		private OnMsgStateChangedDelegatePrivate on_msg_state_changed_private;
		private OnMsgStateChangedDelegate on_msg_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnMsgStateChangedDelegatePrivate))]
#endif
		private static void on_msg_state_changed(IntPtr msg, int state)
		{
			ChatMessage thiz = fromNativePtr<ChatMessage>(msg);
			ChatMessageListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_msg_state_changed_public?.Invoke(thiz, (Linphone.ChatMessageState)state);
			}
		}

		public OnMsgStateChangedDelegate OnMsgStateChanged
		{
			get
			{
				return on_msg_state_changed_public;
			}
			set
			{
				on_msg_state_changed_public = value;
#if WINDOWS_UWP
				on_msg_state_changed_private = on_msg_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_msg_state_changed_private);
				linphone_chat_message_cbs_set_msg_state_changed(nativePtr, cb);
#else
				linphone_chat_message_cbs_set_msg_state_changed(nativePtr, on_msg_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_message_cbs_set_ephemeral_message_deleted(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_message_cbs_set_ephemeral_message_deleted(IntPtr thiz, OnEphemeralMessageDeletedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnEphemeralMessageDeletedDelegatePrivate(IntPtr msg);

		public delegate void OnEphemeralMessageDeletedDelegate(Linphone.ChatMessage msg);
		private OnEphemeralMessageDeletedDelegatePrivate on_ephemeral_message_deleted_private;
		private OnEphemeralMessageDeletedDelegate on_ephemeral_message_deleted_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnEphemeralMessageDeletedDelegatePrivate))]
#endif
		private static void on_ephemeral_message_deleted(IntPtr msg)
		{
			ChatMessage thiz = fromNativePtr<ChatMessage>(msg);
			ChatMessageListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_ephemeral_message_deleted_public?.Invoke(thiz);
			}
		}

		public OnEphemeralMessageDeletedDelegate OnEphemeralMessageDeleted
		{
			get
			{
				return on_ephemeral_message_deleted_public;
			}
			set
			{
				on_ephemeral_message_deleted_public = value;
#if WINDOWS_UWP
				on_ephemeral_message_deleted_private = on_ephemeral_message_deleted;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_ephemeral_message_deleted_private);
				linphone_chat_message_cbs_set_ephemeral_message_deleted(nativePtr, cb);
#else
				linphone_chat_message_cbs_set_ephemeral_message_deleted(nativePtr, on_ephemeral_message_deleted);
#endif
			}
		}

		internal void register() {
			IntPtr listener = linphone_chat_message_cbs_get_user_data(nativePtr);
			if (listener == IntPtr.Zero)
			{
				GCHandle _handle = GCHandle.Alloc(this, GCHandleType.Normal);
				listener = GCHandle.ToIntPtr(_handle);
			} else
			{
				GCHandle _handle = GCHandle.FromIntPtr(listener);
				if (_handle.Target == this)
				{
					return;
				} else
				{
					_handle.Free();
					_handle = GCHandle.Alloc(this, GCHandleType.Normal);
					listener = GCHandle.ToIntPtr(_handle);
				}
			}
			linphone_chat_message_cbs_set_user_data(nativePtr, listener);
		}

		internal void unregister() {
			IntPtr listener = linphone_chat_message_cbs_get_user_data(nativePtr);
			linphone_chat_message_cbs_set_user_data(nativePtr, IntPtr.Zero);
			if (listener != IntPtr.Zero)
			{
				GCHandle.FromIntPtr(listener).Free();
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class ChatRoomListener : LinphoneObject
	{
        ~ChatRoomListener()
        {
            unregister();
        }

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_chat_room_cbs_set_user_data(IntPtr thiz, IntPtr listener);

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_chat_room_cbs_get_user_data(IntPtr thiz);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_state_changed(IntPtr thiz, OnStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnStateChangedDelegatePrivate(IntPtr cr, int newState);

		public delegate void OnStateChangedDelegate(Linphone.ChatRoom cr, Linphone.ChatRoomState newState);
		private OnStateChangedDelegatePrivate on_state_changed_private;
		private OnStateChangedDelegate on_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnStateChangedDelegatePrivate))]
#endif
		private static void on_state_changed(IntPtr cr, int newState)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_state_changed_public?.Invoke(thiz, (Linphone.ChatRoomState)newState);
			}
		}

		public OnStateChangedDelegate OnStateChanged
		{
			get
			{
				return on_state_changed_public;
			}
			set
			{
				on_state_changed_public = value;
#if WINDOWS_UWP
				on_state_changed_private = on_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_state_changed_private);
				linphone_chat_room_cbs_set_state_changed(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_state_changed(nativePtr, on_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_ephemeral_message_timer_started(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_ephemeral_message_timer_started(IntPtr thiz, OnEphemeralMessageTimerStartedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnEphemeralMessageTimerStartedDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnEphemeralMessageTimerStartedDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnEphemeralMessageTimerStartedDelegatePrivate on_ephemeral_message_timer_started_private;
		private OnEphemeralMessageTimerStartedDelegate on_ephemeral_message_timer_started_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnEphemeralMessageTimerStartedDelegatePrivate))]
#endif
		private static void on_ephemeral_message_timer_started(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_ephemeral_message_timer_started_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnEphemeralMessageTimerStartedDelegate OnEphemeralMessageTimerStarted
		{
			get
			{
				return on_ephemeral_message_timer_started_public;
			}
			set
			{
				on_ephemeral_message_timer_started_public = value;
#if WINDOWS_UWP
				on_ephemeral_message_timer_started_private = on_ephemeral_message_timer_started;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_ephemeral_message_timer_started_private);
				linphone_chat_room_cbs_set_ephemeral_message_timer_started(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_ephemeral_message_timer_started(nativePtr, on_ephemeral_message_timer_started);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_participant_admin_status_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_participant_admin_status_changed(IntPtr thiz, OnParticipantAdminStatusChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnParticipantAdminStatusChangedDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnParticipantAdminStatusChangedDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnParticipantAdminStatusChangedDelegatePrivate on_participant_admin_status_changed_private;
		private OnParticipantAdminStatusChangedDelegate on_participant_admin_status_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnParticipantAdminStatusChangedDelegatePrivate))]
#endif
		private static void on_participant_admin_status_changed(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_participant_admin_status_changed_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnParticipantAdminStatusChangedDelegate OnParticipantAdminStatusChanged
		{
			get
			{
				return on_participant_admin_status_changed_public;
			}
			set
			{
				on_participant_admin_status_changed_public = value;
#if WINDOWS_UWP
				on_participant_admin_status_changed_private = on_participant_admin_status_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_participant_admin_status_changed_private);
				linphone_chat_room_cbs_set_participant_admin_status_changed(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_participant_admin_status_changed(nativePtr, on_participant_admin_status_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_participant_removed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_participant_removed(IntPtr thiz, OnParticipantRemovedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnParticipantRemovedDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnParticipantRemovedDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnParticipantRemovedDelegatePrivate on_participant_removed_private;
		private OnParticipantRemovedDelegate on_participant_removed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnParticipantRemovedDelegatePrivate))]
#endif
		private static void on_participant_removed(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_participant_removed_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnParticipantRemovedDelegate OnParticipantRemoved
		{
			get
			{
				return on_participant_removed_public;
			}
			set
			{
				on_participant_removed_public = value;
#if WINDOWS_UWP
				on_participant_removed_private = on_participant_removed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_participant_removed_private);
				linphone_chat_room_cbs_set_participant_removed(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_participant_removed(nativePtr, on_participant_removed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_participant_registration_unsubscription_requested(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_participant_registration_unsubscription_requested(IntPtr thiz, OnParticipantRegistrationUnsubscriptionRequestedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnParticipantRegistrationUnsubscriptionRequestedDelegatePrivate(IntPtr cr, IntPtr participantAddr);

		public delegate void OnParticipantRegistrationUnsubscriptionRequestedDelegate(Linphone.ChatRoom cr, Linphone.Address participantAddr);
		private OnParticipantRegistrationUnsubscriptionRequestedDelegatePrivate on_participant_registration_unsubscription_requested_private;
		private OnParticipantRegistrationUnsubscriptionRequestedDelegate on_participant_registration_unsubscription_requested_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnParticipantRegistrationUnsubscriptionRequestedDelegatePrivate))]
#endif
		private static void on_participant_registration_unsubscription_requested(IntPtr cr, IntPtr participantAddr)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_participant_registration_unsubscription_requested_public?.Invoke(thiz, fromNativePtr<Linphone.Address>(participantAddr));
			}
		}

		public OnParticipantRegistrationUnsubscriptionRequestedDelegate OnParticipantRegistrationUnsubscriptionRequested
		{
			get
			{
				return on_participant_registration_unsubscription_requested_public;
			}
			set
			{
				on_participant_registration_unsubscription_requested_public = value;
#if WINDOWS_UWP
				on_participant_registration_unsubscription_requested_private = on_participant_registration_unsubscription_requested;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_participant_registration_unsubscription_requested_private);
				linphone_chat_room_cbs_set_participant_registration_unsubscription_requested(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_participant_registration_unsubscription_requested(nativePtr, on_participant_registration_unsubscription_requested);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_undecryptable_message_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_undecryptable_message_received(IntPtr thiz, OnUndecryptableMessageReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnUndecryptableMessageReceivedDelegatePrivate(IntPtr cr, IntPtr msg);

		public delegate void OnUndecryptableMessageReceivedDelegate(Linphone.ChatRoom cr, Linphone.ChatMessage msg);
		private OnUndecryptableMessageReceivedDelegatePrivate on_undecryptable_message_received_private;
		private OnUndecryptableMessageReceivedDelegate on_undecryptable_message_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnUndecryptableMessageReceivedDelegatePrivate))]
#endif
		private static void on_undecryptable_message_received(IntPtr cr, IntPtr msg)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_undecryptable_message_received_public?.Invoke(thiz, fromNativePtr<Linphone.ChatMessage>(msg));
			}
		}

		public OnUndecryptableMessageReceivedDelegate OnUndecryptableMessageReceived
		{
			get
			{
				return on_undecryptable_message_received_public;
			}
			set
			{
				on_undecryptable_message_received_public = value;
#if WINDOWS_UWP
				on_undecryptable_message_received_private = on_undecryptable_message_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_undecryptable_message_received_private);
				linphone_chat_room_cbs_set_undecryptable_message_received(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_undecryptable_message_received(nativePtr, on_undecryptable_message_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_participant_added(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_participant_added(IntPtr thiz, OnParticipantAddedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnParticipantAddedDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnParticipantAddedDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnParticipantAddedDelegatePrivate on_participant_added_private;
		private OnParticipantAddedDelegate on_participant_added_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnParticipantAddedDelegatePrivate))]
#endif
		private static void on_participant_added(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_participant_added_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnParticipantAddedDelegate OnParticipantAdded
		{
			get
			{
				return on_participant_added_public;
			}
			set
			{
				on_participant_added_public = value;
#if WINDOWS_UWP
				on_participant_added_private = on_participant_added;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_participant_added_private);
				linphone_chat_room_cbs_set_participant_added(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_participant_added(nativePtr, on_participant_added);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_ephemeral_event(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_ephemeral_event(IntPtr thiz, OnEphemeralEventDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnEphemeralEventDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnEphemeralEventDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnEphemeralEventDelegatePrivate on_ephemeral_event_private;
		private OnEphemeralEventDelegate on_ephemeral_event_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnEphemeralEventDelegatePrivate))]
#endif
		private static void on_ephemeral_event(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_ephemeral_event_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnEphemeralEventDelegate OnEphemeralEvent
		{
			get
			{
				return on_ephemeral_event_public;
			}
			set
			{
				on_ephemeral_event_public = value;
#if WINDOWS_UWP
				on_ephemeral_event_private = on_ephemeral_event;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_ephemeral_event_private);
				linphone_chat_room_cbs_set_ephemeral_event(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_ephemeral_event(nativePtr, on_ephemeral_event);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_chat_message_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_chat_message_received(IntPtr thiz, OnChatMessageReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnChatMessageReceivedDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnChatMessageReceivedDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnChatMessageReceivedDelegatePrivate on_chat_message_received_private;
		private OnChatMessageReceivedDelegate on_chat_message_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnChatMessageReceivedDelegatePrivate))]
#endif
		private static void on_chat_message_received(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_chat_message_received_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnChatMessageReceivedDelegate OnChatMessageReceived
		{
			get
			{
				return on_chat_message_received_public;
			}
			set
			{
				on_chat_message_received_public = value;
#if WINDOWS_UWP
				on_chat_message_received_private = on_chat_message_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_chat_message_received_private);
				linphone_chat_room_cbs_set_chat_message_received(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_chat_message_received(nativePtr, on_chat_message_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_conference_address_generation(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_conference_address_generation(IntPtr thiz, OnConferenceAddressGenerationDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnConferenceAddressGenerationDelegatePrivate(IntPtr cr);

		public delegate void OnConferenceAddressGenerationDelegate(Linphone.ChatRoom cr);
		private OnConferenceAddressGenerationDelegatePrivate on_conference_address_generation_private;
		private OnConferenceAddressGenerationDelegate on_conference_address_generation_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnConferenceAddressGenerationDelegatePrivate))]
#endif
		private static void on_conference_address_generation(IntPtr cr)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_conference_address_generation_public?.Invoke(thiz);
			}
		}

		public OnConferenceAddressGenerationDelegate OnConferenceAddressGeneration
		{
			get
			{
				return on_conference_address_generation_public;
			}
			set
			{
				on_conference_address_generation_public = value;
#if WINDOWS_UWP
				on_conference_address_generation_private = on_conference_address_generation;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_conference_address_generation_private);
				linphone_chat_room_cbs_set_conference_address_generation(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_conference_address_generation(nativePtr, on_conference_address_generation);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_participant_device_added(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_participant_device_added(IntPtr thiz, OnParticipantDeviceAddedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnParticipantDeviceAddedDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnParticipantDeviceAddedDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnParticipantDeviceAddedDelegatePrivate on_participant_device_added_private;
		private OnParticipantDeviceAddedDelegate on_participant_device_added_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnParticipantDeviceAddedDelegatePrivate))]
#endif
		private static void on_participant_device_added(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_participant_device_added_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnParticipantDeviceAddedDelegate OnParticipantDeviceAdded
		{
			get
			{
				return on_participant_device_added_public;
			}
			set
			{
				on_participant_device_added_public = value;
#if WINDOWS_UWP
				on_participant_device_added_private = on_participant_device_added;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_participant_device_added_private);
				linphone_chat_room_cbs_set_participant_device_added(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_participant_device_added(nativePtr, on_participant_device_added);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_security_event(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_security_event(IntPtr thiz, OnSecurityEventDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnSecurityEventDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnSecurityEventDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnSecurityEventDelegatePrivate on_security_event_private;
		private OnSecurityEventDelegate on_security_event_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnSecurityEventDelegatePrivate))]
#endif
		private static void on_security_event(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_security_event_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnSecurityEventDelegate OnSecurityEvent
		{
			get
			{
				return on_security_event_public;
			}
			set
			{
				on_security_event_public = value;
#if WINDOWS_UWP
				on_security_event_private = on_security_event;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_security_event_private);
				linphone_chat_room_cbs_set_security_event(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_security_event(nativePtr, on_security_event);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_conference_left(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_conference_left(IntPtr thiz, OnConferenceLeftDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnConferenceLeftDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnConferenceLeftDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnConferenceLeftDelegatePrivate on_conference_left_private;
		private OnConferenceLeftDelegate on_conference_left_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnConferenceLeftDelegatePrivate))]
#endif
		private static void on_conference_left(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_conference_left_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnConferenceLeftDelegate OnConferenceLeft
		{
			get
			{
				return on_conference_left_public;
			}
			set
			{
				on_conference_left_public = value;
#if WINDOWS_UWP
				on_conference_left_private = on_conference_left;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_conference_left_private);
				linphone_chat_room_cbs_set_conference_left(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_conference_left(nativePtr, on_conference_left);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_subject_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_subject_changed(IntPtr thiz, OnSubjectChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnSubjectChangedDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnSubjectChangedDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnSubjectChangedDelegatePrivate on_subject_changed_private;
		private OnSubjectChangedDelegate on_subject_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnSubjectChangedDelegatePrivate))]
#endif
		private static void on_subject_changed(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_subject_changed_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnSubjectChangedDelegate OnSubjectChanged
		{
			get
			{
				return on_subject_changed_public;
			}
			set
			{
				on_subject_changed_public = value;
#if WINDOWS_UWP
				on_subject_changed_private = on_subject_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_subject_changed_private);
				linphone_chat_room_cbs_set_subject_changed(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_subject_changed(nativePtr, on_subject_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_chat_message_sent(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_chat_message_sent(IntPtr thiz, OnChatMessageSentDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnChatMessageSentDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnChatMessageSentDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnChatMessageSentDelegatePrivate on_chat_message_sent_private;
		private OnChatMessageSentDelegate on_chat_message_sent_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnChatMessageSentDelegatePrivate))]
#endif
		private static void on_chat_message_sent(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_chat_message_sent_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnChatMessageSentDelegate OnChatMessageSent
		{
			get
			{
				return on_chat_message_sent_public;
			}
			set
			{
				on_chat_message_sent_public = value;
#if WINDOWS_UWP
				on_chat_message_sent_private = on_chat_message_sent;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_chat_message_sent_private);
				linphone_chat_room_cbs_set_chat_message_sent(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_chat_message_sent(nativePtr, on_chat_message_sent);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_conference_joined(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_conference_joined(IntPtr thiz, OnConferenceJoinedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnConferenceJoinedDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnConferenceJoinedDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnConferenceJoinedDelegatePrivate on_conference_joined_private;
		private OnConferenceJoinedDelegate on_conference_joined_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnConferenceJoinedDelegatePrivate))]
#endif
		private static void on_conference_joined(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_conference_joined_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnConferenceJoinedDelegate OnConferenceJoined
		{
			get
			{
				return on_conference_joined_public;
			}
			set
			{
				on_conference_joined_public = value;
#if WINDOWS_UWP
				on_conference_joined_private = on_conference_joined;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_conference_joined_private);
				linphone_chat_room_cbs_set_conference_joined(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_conference_joined(nativePtr, on_conference_joined);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_message_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_message_received(IntPtr thiz, OnMessageReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnMessageReceivedDelegatePrivate(IntPtr cr, IntPtr msg);

		public delegate void OnMessageReceivedDelegate(Linphone.ChatRoom cr, Linphone.ChatMessage msg);
		private OnMessageReceivedDelegatePrivate on_message_received_private;
		private OnMessageReceivedDelegate on_message_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnMessageReceivedDelegatePrivate))]
#endif
		private static void on_message_received(IntPtr cr, IntPtr msg)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_message_received_public?.Invoke(thiz, fromNativePtr<Linphone.ChatMessage>(msg));
			}
		}

		public OnMessageReceivedDelegate OnMessageReceived
		{
			get
			{
				return on_message_received_public;
			}
			set
			{
				on_message_received_public = value;
#if WINDOWS_UWP
				on_message_received_private = on_message_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_message_received_private);
				linphone_chat_room_cbs_set_message_received(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_message_received(nativePtr, on_message_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_ephemeral_message_deleted(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_ephemeral_message_deleted(IntPtr thiz, OnEphemeralMessageDeletedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnEphemeralMessageDeletedDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnEphemeralMessageDeletedDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnEphemeralMessageDeletedDelegatePrivate on_ephemeral_message_deleted_private;
		private OnEphemeralMessageDeletedDelegate on_ephemeral_message_deleted_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnEphemeralMessageDeletedDelegatePrivate))]
#endif
		private static void on_ephemeral_message_deleted(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_ephemeral_message_deleted_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnEphemeralMessageDeletedDelegate OnEphemeralMessageDeleted
		{
			get
			{
				return on_ephemeral_message_deleted_public;
			}
			set
			{
				on_ephemeral_message_deleted_public = value;
#if WINDOWS_UWP
				on_ephemeral_message_deleted_private = on_ephemeral_message_deleted;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_ephemeral_message_deleted_private);
				linphone_chat_room_cbs_set_ephemeral_message_deleted(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_ephemeral_message_deleted(nativePtr, on_ephemeral_message_deleted);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_participant_registration_subscription_requested(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_participant_registration_subscription_requested(IntPtr thiz, OnParticipantRegistrationSubscriptionRequestedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnParticipantRegistrationSubscriptionRequestedDelegatePrivate(IntPtr cr, IntPtr participantAddr);

		public delegate void OnParticipantRegistrationSubscriptionRequestedDelegate(Linphone.ChatRoom cr, Linphone.Address participantAddr);
		private OnParticipantRegistrationSubscriptionRequestedDelegatePrivate on_participant_registration_subscription_requested_private;
		private OnParticipantRegistrationSubscriptionRequestedDelegate on_participant_registration_subscription_requested_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnParticipantRegistrationSubscriptionRequestedDelegatePrivate))]
#endif
		private static void on_participant_registration_subscription_requested(IntPtr cr, IntPtr participantAddr)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_participant_registration_subscription_requested_public?.Invoke(thiz, fromNativePtr<Linphone.Address>(participantAddr));
			}
		}

		public OnParticipantRegistrationSubscriptionRequestedDelegate OnParticipantRegistrationSubscriptionRequested
		{
			get
			{
				return on_participant_registration_subscription_requested_public;
			}
			set
			{
				on_participant_registration_subscription_requested_public = value;
#if WINDOWS_UWP
				on_participant_registration_subscription_requested_private = on_participant_registration_subscription_requested;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_participant_registration_subscription_requested_private);
				linphone_chat_room_cbs_set_participant_registration_subscription_requested(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_participant_registration_subscription_requested(nativePtr, on_participant_registration_subscription_requested);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_participant_device_removed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_participant_device_removed(IntPtr thiz, OnParticipantDeviceRemovedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnParticipantDeviceRemovedDelegatePrivate(IntPtr cr, IntPtr eventLog);

		public delegate void OnParticipantDeviceRemovedDelegate(Linphone.ChatRoom cr, Linphone.EventLog eventLog);
		private OnParticipantDeviceRemovedDelegatePrivate on_participant_device_removed_private;
		private OnParticipantDeviceRemovedDelegate on_participant_device_removed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnParticipantDeviceRemovedDelegatePrivate))]
#endif
		private static void on_participant_device_removed(IntPtr cr, IntPtr eventLog)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_participant_device_removed_public?.Invoke(thiz, fromNativePtr<Linphone.EventLog>(eventLog));
			}
		}

		public OnParticipantDeviceRemovedDelegate OnParticipantDeviceRemoved
		{
			get
			{
				return on_participant_device_removed_public;
			}
			set
			{
				on_participant_device_removed_public = value;
#if WINDOWS_UWP
				on_participant_device_removed_private = on_participant_device_removed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_participant_device_removed_private);
				linphone_chat_room_cbs_set_participant_device_removed(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_participant_device_removed(nativePtr, on_participant_device_removed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_is_composing_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_is_composing_received(IntPtr thiz, OnIsComposingReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnIsComposingReceivedDelegatePrivate(IntPtr cr, IntPtr remoteAddr, char isComposing);

		public delegate void OnIsComposingReceivedDelegate(Linphone.ChatRoom cr, Linphone.Address remoteAddr, bool isComposing);
		private OnIsComposingReceivedDelegatePrivate on_is_composing_received_private;
		private OnIsComposingReceivedDelegate on_is_composing_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnIsComposingReceivedDelegatePrivate))]
#endif
		private static void on_is_composing_received(IntPtr cr, IntPtr remoteAddr, char isComposing)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_is_composing_received_public?.Invoke(thiz, fromNativePtr<Linphone.Address>(remoteAddr), isComposing == 0);
			}
		}

		public OnIsComposingReceivedDelegate OnIsComposingReceived
		{
			get
			{
				return on_is_composing_received_public;
			}
			set
			{
				on_is_composing_received_public = value;
#if WINDOWS_UWP
				on_is_composing_received_private = on_is_composing_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_is_composing_received_private);
				linphone_chat_room_cbs_set_is_composing_received(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_is_composing_received(nativePtr, on_is_composing_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_chat_room_cbs_set_chat_message_should_be_stored(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_chat_room_cbs_set_chat_message_should_be_stored(IntPtr thiz, OnChatMessageShouldBeStoredDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnChatMessageShouldBeStoredDelegatePrivate(IntPtr cr, IntPtr msg);

		public delegate void OnChatMessageShouldBeStoredDelegate(Linphone.ChatRoom cr, Linphone.ChatMessage msg);
		private OnChatMessageShouldBeStoredDelegatePrivate on_chat_message_should_be_stored_private;
		private OnChatMessageShouldBeStoredDelegate on_chat_message_should_be_stored_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnChatMessageShouldBeStoredDelegatePrivate))]
#endif
		private static void on_chat_message_should_be_stored(IntPtr cr, IntPtr msg)
		{
			ChatRoom thiz = fromNativePtr<ChatRoom>(cr);
			ChatRoomListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_chat_message_should_be_stored_public?.Invoke(thiz, fromNativePtr<Linphone.ChatMessage>(msg));
			}
		}

		public OnChatMessageShouldBeStoredDelegate OnChatMessageShouldBeStored
		{
			get
			{
				return on_chat_message_should_be_stored_public;
			}
			set
			{
				on_chat_message_should_be_stored_public = value;
#if WINDOWS_UWP
				on_chat_message_should_be_stored_private = on_chat_message_should_be_stored;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_chat_message_should_be_stored_private);
				linphone_chat_room_cbs_set_chat_message_should_be_stored(nativePtr, cb);
#else
				linphone_chat_room_cbs_set_chat_message_should_be_stored(nativePtr, on_chat_message_should_be_stored);
#endif
			}
		}

		internal void register() {
			IntPtr listener = linphone_chat_room_cbs_get_user_data(nativePtr);
			if (listener == IntPtr.Zero)
			{
				GCHandle _handle = GCHandle.Alloc(this, GCHandleType.Normal);
				listener = GCHandle.ToIntPtr(_handle);
			} else
			{
				GCHandle _handle = GCHandle.FromIntPtr(listener);
				if (_handle.Target == this)
				{
					return;
				} else
				{
					_handle.Free();
					_handle = GCHandle.Alloc(this, GCHandleType.Normal);
					listener = GCHandle.ToIntPtr(_handle);
				}
			}
			linphone_chat_room_cbs_set_user_data(nativePtr, listener);
		}

		internal void unregister() {
			IntPtr listener = linphone_chat_room_cbs_get_user_data(nativePtr);
			linphone_chat_room_cbs_set_user_data(nativePtr, IntPtr.Zero);
			if (listener != IntPtr.Zero)
			{
				GCHandle.FromIntPtr(listener).Free();
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class CoreListener : LinphoneObject
	{
        ~CoreListener()
        {
            unregister();
        }

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_core_cbs_set_user_data(IntPtr thiz, IntPtr listener);

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_core_cbs_get_user_data(IntPtr thiz);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_transfer_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_transfer_state_changed(IntPtr thiz, OnTransferStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnTransferStateChangedDelegatePrivate(IntPtr lc, IntPtr transfered, int newCallState);

		public delegate void OnTransferStateChangedDelegate(Linphone.Core lc, Linphone.Call transfered, Linphone.CallState newCallState);
		private OnTransferStateChangedDelegatePrivate on_transfer_state_changed_private;
		private OnTransferStateChangedDelegate on_transfer_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnTransferStateChangedDelegatePrivate))]
#endif
		private static void on_transfer_state_changed(IntPtr lc, IntPtr transfered, int newCallState)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_transfer_state_changed_public?.Invoke(thiz, fromNativePtr<Linphone.Call>(transfered), (Linphone.CallState)newCallState);
			}
		}

		public OnTransferStateChangedDelegate OnTransferStateChanged
		{
			get
			{
				return on_transfer_state_changed_public;
			}
			set
			{
				on_transfer_state_changed_public = value;
#if WINDOWS_UWP
				on_transfer_state_changed_private = on_transfer_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_transfer_state_changed_private);
				linphone_core_cbs_set_transfer_state_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_transfer_state_changed(nativePtr, on_transfer_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_friend_list_created(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_friend_list_created(IntPtr thiz, OnFriendListCreatedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnFriendListCreatedDelegatePrivate(IntPtr lc, IntPtr list);

		public delegate void OnFriendListCreatedDelegate(Linphone.Core lc, Linphone.FriendList list);
		private OnFriendListCreatedDelegatePrivate on_friend_list_created_private;
		private OnFriendListCreatedDelegate on_friend_list_created_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnFriendListCreatedDelegatePrivate))]
#endif
		private static void on_friend_list_created(IntPtr lc, IntPtr list)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_friend_list_created_public?.Invoke(thiz, fromNativePtr<Linphone.FriendList>(list));
			}
		}

		public OnFriendListCreatedDelegate OnFriendListCreated
		{
			get
			{
				return on_friend_list_created_public;
			}
			set
			{
				on_friend_list_created_public = value;
#if WINDOWS_UWP
				on_friend_list_created_private = on_friend_list_created;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_friend_list_created_private);
				linphone_core_cbs_set_friend_list_created(nativePtr, cb);
#else
				linphone_core_cbs_set_friend_list_created(nativePtr, on_friend_list_created);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_subscription_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_subscription_state_changed(IntPtr thiz, OnSubscriptionStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnSubscriptionStateChangedDelegatePrivate(IntPtr lc, IntPtr lev, int state);

		public delegate void OnSubscriptionStateChangedDelegate(Linphone.Core lc, Linphone.Event lev, Linphone.SubscriptionState state);
		private OnSubscriptionStateChangedDelegatePrivate on_subscription_state_changed_private;
		private OnSubscriptionStateChangedDelegate on_subscription_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnSubscriptionStateChangedDelegatePrivate))]
#endif
		private static void on_subscription_state_changed(IntPtr lc, IntPtr lev, int state)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_subscription_state_changed_public?.Invoke(thiz, fromNativePtr<Linphone.Event>(lev), (Linphone.SubscriptionState)state);
			}
		}

		public OnSubscriptionStateChangedDelegate OnSubscriptionStateChanged
		{
			get
			{
				return on_subscription_state_changed_public;
			}
			set
			{
				on_subscription_state_changed_public = value;
#if WINDOWS_UWP
				on_subscription_state_changed_private = on_subscription_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_subscription_state_changed_private);
				linphone_core_cbs_set_subscription_state_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_subscription_state_changed(nativePtr, on_subscription_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_call_log_updated(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_call_log_updated(IntPtr thiz, OnCallLogUpdatedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnCallLogUpdatedDelegatePrivate(IntPtr lc, IntPtr newcl);

		public delegate void OnCallLogUpdatedDelegate(Linphone.Core lc, Linphone.CallLog newcl);
		private OnCallLogUpdatedDelegatePrivate on_call_log_updated_private;
		private OnCallLogUpdatedDelegate on_call_log_updated_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnCallLogUpdatedDelegatePrivate))]
#endif
		private static void on_call_log_updated(IntPtr lc, IntPtr newcl)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_call_log_updated_public?.Invoke(thiz, fromNativePtr<Linphone.CallLog>(newcl));
			}
		}

		public OnCallLogUpdatedDelegate OnCallLogUpdated
		{
			get
			{
				return on_call_log_updated_public;
			}
			set
			{
				on_call_log_updated_public = value;
#if WINDOWS_UWP
				on_call_log_updated_private = on_call_log_updated;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_call_log_updated_private);
				linphone_core_cbs_set_call_log_updated(nativePtr, cb);
#else
				linphone_core_cbs_set_call_log_updated(nativePtr, on_call_log_updated);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_call_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_call_state_changed(IntPtr thiz, OnCallStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnCallStateChangedDelegatePrivate(IntPtr lc, IntPtr call, int cstate, string message);

		public delegate void OnCallStateChangedDelegate(Linphone.Core lc, Linphone.Call call, Linphone.CallState cstate, string message);
		private OnCallStateChangedDelegatePrivate on_call_state_changed_private;
		private OnCallStateChangedDelegate on_call_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnCallStateChangedDelegatePrivate))]
#endif
		private static void on_call_state_changed(IntPtr lc, IntPtr call, int cstate, string message)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_call_state_changed_public?.Invoke(thiz, fromNativePtr<Linphone.Call>(call), (Linphone.CallState)cstate, message);
			}
		}

		public OnCallStateChangedDelegate OnCallStateChanged
		{
			get
			{
				return on_call_state_changed_public;
			}
			set
			{
				on_call_state_changed_public = value;
#if WINDOWS_UWP
				on_call_state_changed_private = on_call_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_call_state_changed_private);
				linphone_core_cbs_set_call_state_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_call_state_changed(nativePtr, on_call_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_authentication_requested(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_authentication_requested(IntPtr thiz, OnAuthenticationRequestedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnAuthenticationRequestedDelegatePrivate(IntPtr lc, IntPtr authInfo, int method);

		public delegate void OnAuthenticationRequestedDelegate(Linphone.Core lc, Linphone.AuthInfo authInfo, Linphone.AuthMethod method);
		private OnAuthenticationRequestedDelegatePrivate on_authentication_requested_private;
		private OnAuthenticationRequestedDelegate on_authentication_requested_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnAuthenticationRequestedDelegatePrivate))]
#endif
		private static void on_authentication_requested(IntPtr lc, IntPtr authInfo, int method)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_authentication_requested_public?.Invoke(thiz, fromNativePtr<Linphone.AuthInfo>(authInfo), (Linphone.AuthMethod)method);
			}
		}

		public OnAuthenticationRequestedDelegate OnAuthenticationRequested
		{
			get
			{
				return on_authentication_requested_public;
			}
			set
			{
				on_authentication_requested_public = value;
#if WINDOWS_UWP
				on_authentication_requested_private = on_authentication_requested;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_authentication_requested_private);
				linphone_core_cbs_set_authentication_requested(nativePtr, cb);
#else
				linphone_core_cbs_set_authentication_requested(nativePtr, on_authentication_requested);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_notify_presence_received_for_uri_or_tel(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_notify_presence_received_for_uri_or_tel(IntPtr thiz, OnNotifyPresenceReceivedForUriOrTelDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnNotifyPresenceReceivedForUriOrTelDelegatePrivate(IntPtr lc, IntPtr lf, string uriOrTel, IntPtr presenceModel);

		public delegate void OnNotifyPresenceReceivedForUriOrTelDelegate(Linphone.Core lc, Linphone.Friend lf, string uriOrTel, Linphone.PresenceModel presenceModel);
		private OnNotifyPresenceReceivedForUriOrTelDelegatePrivate on_notify_presence_received_for_uri_or_tel_private;
		private OnNotifyPresenceReceivedForUriOrTelDelegate on_notify_presence_received_for_uri_or_tel_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnNotifyPresenceReceivedForUriOrTelDelegatePrivate))]
#endif
		private static void on_notify_presence_received_for_uri_or_tel(IntPtr lc, IntPtr lf, string uriOrTel, IntPtr presenceModel)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_notify_presence_received_for_uri_or_tel_public?.Invoke(thiz, fromNativePtr<Linphone.Friend>(lf), uriOrTel, fromNativePtr<Linphone.PresenceModel>(presenceModel));
			}
		}

		public OnNotifyPresenceReceivedForUriOrTelDelegate OnNotifyPresenceReceivedForUriOrTel
		{
			get
			{
				return on_notify_presence_received_for_uri_or_tel_public;
			}
			set
			{
				on_notify_presence_received_for_uri_or_tel_public = value;
#if WINDOWS_UWP
				on_notify_presence_received_for_uri_or_tel_private = on_notify_presence_received_for_uri_or_tel;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_notify_presence_received_for_uri_or_tel_private);
				linphone_core_cbs_set_notify_presence_received_for_uri_or_tel(nativePtr, cb);
#else
				linphone_core_cbs_set_notify_presence_received_for_uri_or_tel(nativePtr, on_notify_presence_received_for_uri_or_tel);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_chat_room_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_chat_room_state_changed(IntPtr thiz, OnChatRoomStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnChatRoomStateChangedDelegatePrivate(IntPtr lc, IntPtr cr, int state);

		public delegate void OnChatRoomStateChangedDelegate(Linphone.Core lc, Linphone.ChatRoom cr, Linphone.ChatRoomState state);
		private OnChatRoomStateChangedDelegatePrivate on_chat_room_state_changed_private;
		private OnChatRoomStateChangedDelegate on_chat_room_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnChatRoomStateChangedDelegatePrivate))]
#endif
		private static void on_chat_room_state_changed(IntPtr lc, IntPtr cr, int state)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_chat_room_state_changed_public?.Invoke(thiz, fromNativePtr<Linphone.ChatRoom>(cr), (Linphone.ChatRoomState)state);
			}
		}

		public OnChatRoomStateChangedDelegate OnChatRoomStateChanged
		{
			get
			{
				return on_chat_room_state_changed_public;
			}
			set
			{
				on_chat_room_state_changed_public = value;
#if WINDOWS_UWP
				on_chat_room_state_changed_private = on_chat_room_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_chat_room_state_changed_private);
				linphone_core_cbs_set_chat_room_state_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_chat_room_state_changed(nativePtr, on_chat_room_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_buddy_info_updated(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_buddy_info_updated(IntPtr thiz, OnBuddyInfoUpdatedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnBuddyInfoUpdatedDelegatePrivate(IntPtr lc, IntPtr lf);

		public delegate void OnBuddyInfoUpdatedDelegate(Linphone.Core lc, Linphone.Friend lf);
		private OnBuddyInfoUpdatedDelegatePrivate on_buddy_info_updated_private;
		private OnBuddyInfoUpdatedDelegate on_buddy_info_updated_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnBuddyInfoUpdatedDelegatePrivate))]
#endif
		private static void on_buddy_info_updated(IntPtr lc, IntPtr lf)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_buddy_info_updated_public?.Invoke(thiz, fromNativePtr<Linphone.Friend>(lf));
			}
		}

		public OnBuddyInfoUpdatedDelegate OnBuddyInfoUpdated
		{
			get
			{
				return on_buddy_info_updated_public;
			}
			set
			{
				on_buddy_info_updated_public = value;
#if WINDOWS_UWP
				on_buddy_info_updated_private = on_buddy_info_updated;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_buddy_info_updated_private);
				linphone_core_cbs_set_buddy_info_updated(nativePtr, cb);
#else
				linphone_core_cbs_set_buddy_info_updated(nativePtr, on_buddy_info_updated);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_network_reachable(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_network_reachable(IntPtr thiz, OnNetworkReachableDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnNetworkReachableDelegatePrivate(IntPtr lc, char reachable);

		public delegate void OnNetworkReachableDelegate(Linphone.Core lc, bool reachable);
		private OnNetworkReachableDelegatePrivate on_network_reachable_private;
		private OnNetworkReachableDelegate on_network_reachable_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnNetworkReachableDelegatePrivate))]
#endif
		private static void on_network_reachable(IntPtr lc, char reachable)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_network_reachable_public?.Invoke(thiz, reachable == 0);
			}
		}

		public OnNetworkReachableDelegate OnNetworkReachable
		{
			get
			{
				return on_network_reachable_public;
			}
			set
			{
				on_network_reachable_public = value;
#if WINDOWS_UWP
				on_network_reachable_private = on_network_reachable;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_network_reachable_private);
				linphone_core_cbs_set_network_reachable(nativePtr, cb);
#else
				linphone_core_cbs_set_network_reachable(nativePtr, on_network_reachable);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_notify_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_notify_received(IntPtr thiz, OnNotifyReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnNotifyReceivedDelegatePrivate(IntPtr lc, IntPtr lev, string notifiedEvent, IntPtr body);

		public delegate void OnNotifyReceivedDelegate(Linphone.Core lc, Linphone.Event lev, string notifiedEvent, Linphone.Content body);
		private OnNotifyReceivedDelegatePrivate on_notify_received_private;
		private OnNotifyReceivedDelegate on_notify_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnNotifyReceivedDelegatePrivate))]
#endif
		private static void on_notify_received(IntPtr lc, IntPtr lev, string notifiedEvent, IntPtr body)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_notify_received_public?.Invoke(thiz, fromNativePtr<Linphone.Event>(lev), notifiedEvent, fromNativePtr<Linphone.Content>(body));
			}
		}

		public OnNotifyReceivedDelegate OnNotifyReceived
		{
			get
			{
				return on_notify_received_public;
			}
			set
			{
				on_notify_received_public = value;
#if WINDOWS_UWP
				on_notify_received_private = on_notify_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_notify_received_private);
				linphone_core_cbs_set_notify_received(nativePtr, cb);
#else
				linphone_core_cbs_set_notify_received(nativePtr, on_notify_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_new_subscription_requested(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_new_subscription_requested(IntPtr thiz, OnNewSubscriptionRequestedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnNewSubscriptionRequestedDelegatePrivate(IntPtr lc, IntPtr lf, string url);

		public delegate void OnNewSubscriptionRequestedDelegate(Linphone.Core lc, Linphone.Friend lf, string url);
		private OnNewSubscriptionRequestedDelegatePrivate on_new_subscription_requested_private;
		private OnNewSubscriptionRequestedDelegate on_new_subscription_requested_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnNewSubscriptionRequestedDelegatePrivate))]
#endif
		private static void on_new_subscription_requested(IntPtr lc, IntPtr lf, string url)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_new_subscription_requested_public?.Invoke(thiz, fromNativePtr<Linphone.Friend>(lf), url);
			}
		}

		public OnNewSubscriptionRequestedDelegate OnNewSubscriptionRequested
		{
			get
			{
				return on_new_subscription_requested_public;
			}
			set
			{
				on_new_subscription_requested_public = value;
#if WINDOWS_UWP
				on_new_subscription_requested_private = on_new_subscription_requested;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_new_subscription_requested_private);
				linphone_core_cbs_set_new_subscription_requested(nativePtr, cb);
#else
				linphone_core_cbs_set_new_subscription_requested(nativePtr, on_new_subscription_requested);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_call_stats_updated(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_call_stats_updated(IntPtr thiz, OnCallStatsUpdatedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnCallStatsUpdatedDelegatePrivate(IntPtr lc, IntPtr call, IntPtr stats);

		public delegate void OnCallStatsUpdatedDelegate(Linphone.Core lc, Linphone.Call call, Linphone.CallStats stats);
		private OnCallStatsUpdatedDelegatePrivate on_call_stats_updated_private;
		private OnCallStatsUpdatedDelegate on_call_stats_updated_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnCallStatsUpdatedDelegatePrivate))]
#endif
		private static void on_call_stats_updated(IntPtr lc, IntPtr call, IntPtr stats)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_call_stats_updated_public?.Invoke(thiz, fromNativePtr<Linphone.Call>(call), fromNativePtr<Linphone.CallStats>(stats));
			}
		}

		public OnCallStatsUpdatedDelegate OnCallStatsUpdated
		{
			get
			{
				return on_call_stats_updated_public;
			}
			set
			{
				on_call_stats_updated_public = value;
#if WINDOWS_UWP
				on_call_stats_updated_private = on_call_stats_updated;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_call_stats_updated_private);
				linphone_core_cbs_set_call_stats_updated(nativePtr, cb);
#else
				linphone_core_cbs_set_call_stats_updated(nativePtr, on_call_stats_updated);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_notify_presence_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_notify_presence_received(IntPtr thiz, OnNotifyPresenceReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnNotifyPresenceReceivedDelegatePrivate(IntPtr lc, IntPtr lf);

		public delegate void OnNotifyPresenceReceivedDelegate(Linphone.Core lc, Linphone.Friend lf);
		private OnNotifyPresenceReceivedDelegatePrivate on_notify_presence_received_private;
		private OnNotifyPresenceReceivedDelegate on_notify_presence_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnNotifyPresenceReceivedDelegatePrivate))]
#endif
		private static void on_notify_presence_received(IntPtr lc, IntPtr lf)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_notify_presence_received_public?.Invoke(thiz, fromNativePtr<Linphone.Friend>(lf));
			}
		}

		public OnNotifyPresenceReceivedDelegate OnNotifyPresenceReceived
		{
			get
			{
				return on_notify_presence_received_public;
			}
			set
			{
				on_notify_presence_received_public = value;
#if WINDOWS_UWP
				on_notify_presence_received_private = on_notify_presence_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_notify_presence_received_private);
				linphone_core_cbs_set_notify_presence_received(nativePtr, cb);
#else
				linphone_core_cbs_set_notify_presence_received(nativePtr, on_notify_presence_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_audio_device_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_audio_device_changed(IntPtr thiz, OnAudioDeviceChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnAudioDeviceChangedDelegatePrivate(IntPtr lc, IntPtr audioDevice);

		public delegate void OnAudioDeviceChangedDelegate(Linphone.Core lc, Linphone.AudioDevice audioDevice);
		private OnAudioDeviceChangedDelegatePrivate on_audio_device_changed_private;
		private OnAudioDeviceChangedDelegate on_audio_device_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnAudioDeviceChangedDelegatePrivate))]
#endif
		private static void on_audio_device_changed(IntPtr lc, IntPtr audioDevice)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_audio_device_changed_public?.Invoke(thiz, fromNativePtr<Linphone.AudioDevice>(audioDevice));
			}
		}

		public OnAudioDeviceChangedDelegate OnAudioDeviceChanged
		{
			get
			{
				return on_audio_device_changed_public;
			}
			set
			{
				on_audio_device_changed_public = value;
#if WINDOWS_UWP
				on_audio_device_changed_private = on_audio_device_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_audio_device_changed_private);
				linphone_core_cbs_set_audio_device_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_audio_device_changed(nativePtr, on_audio_device_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_ec_calibration_audio_init(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_ec_calibration_audio_init(IntPtr thiz, OnEcCalibrationAudioInitDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnEcCalibrationAudioInitDelegatePrivate(IntPtr lc);

		public delegate void OnEcCalibrationAudioInitDelegate(Linphone.Core lc);
		private OnEcCalibrationAudioInitDelegatePrivate on_ec_calibration_audio_init_private;
		private OnEcCalibrationAudioInitDelegate on_ec_calibration_audio_init_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnEcCalibrationAudioInitDelegatePrivate))]
#endif
		private static void on_ec_calibration_audio_init(IntPtr lc)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_ec_calibration_audio_init_public?.Invoke(thiz);
			}
		}

		public OnEcCalibrationAudioInitDelegate OnEcCalibrationAudioInit
		{
			get
			{
				return on_ec_calibration_audio_init_public;
			}
			set
			{
				on_ec_calibration_audio_init_public = value;
#if WINDOWS_UWP
				on_ec_calibration_audio_init_private = on_ec_calibration_audio_init;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_ec_calibration_audio_init_private);
				linphone_core_cbs_set_ec_calibration_audio_init(nativePtr, cb);
#else
				linphone_core_cbs_set_ec_calibration_audio_init(nativePtr, on_ec_calibration_audio_init);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_message_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_message_received(IntPtr thiz, OnMessageReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnMessageReceivedDelegatePrivate(IntPtr lc, IntPtr room, IntPtr message);

		public delegate void OnMessageReceivedDelegate(Linphone.Core lc, Linphone.ChatRoom room, Linphone.ChatMessage message);
		private OnMessageReceivedDelegatePrivate on_message_received_private;
		private OnMessageReceivedDelegate on_message_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnMessageReceivedDelegatePrivate))]
#endif
		private static void on_message_received(IntPtr lc, IntPtr room, IntPtr message)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_message_received_public?.Invoke(thiz, fromNativePtr<Linphone.ChatRoom>(room), fromNativePtr<Linphone.ChatMessage>(message));
			}
		}

		public OnMessageReceivedDelegate OnMessageReceived
		{
			get
			{
				return on_message_received_public;
			}
			set
			{
				on_message_received_public = value;
#if WINDOWS_UWP
				on_message_received_private = on_message_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_message_received_private);
				linphone_core_cbs_set_message_received(nativePtr, cb);
#else
				linphone_core_cbs_set_message_received(nativePtr, on_message_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_ec_calibration_result(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_ec_calibration_result(IntPtr thiz, OnEcCalibrationResultDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnEcCalibrationResultDelegatePrivate(IntPtr lc, int status, int delayMs);

		public delegate void OnEcCalibrationResultDelegate(Linphone.Core lc, Linphone.EcCalibratorStatus status, int delayMs);
		private OnEcCalibrationResultDelegatePrivate on_ec_calibration_result_private;
		private OnEcCalibrationResultDelegate on_ec_calibration_result_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnEcCalibrationResultDelegatePrivate))]
#endif
		private static void on_ec_calibration_result(IntPtr lc, int status, int delayMs)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_ec_calibration_result_public?.Invoke(thiz, (Linphone.EcCalibratorStatus)status, delayMs);
			}
		}

		public OnEcCalibrationResultDelegate OnEcCalibrationResult
		{
			get
			{
				return on_ec_calibration_result_public;
			}
			set
			{
				on_ec_calibration_result_public = value;
#if WINDOWS_UWP
				on_ec_calibration_result_private = on_ec_calibration_result;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_ec_calibration_result_private);
				linphone_core_cbs_set_ec_calibration_result(nativePtr, cb);
#else
				linphone_core_cbs_set_ec_calibration_result(nativePtr, on_ec_calibration_result);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_subscribe_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_subscribe_received(IntPtr thiz, OnSubscribeReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnSubscribeReceivedDelegatePrivate(IntPtr lc, IntPtr lev, string subscribeEvent, IntPtr body);

		public delegate void OnSubscribeReceivedDelegate(Linphone.Core lc, Linphone.Event lev, string subscribeEvent, Linphone.Content body);
		private OnSubscribeReceivedDelegatePrivate on_subscribe_received_private;
		private OnSubscribeReceivedDelegate on_subscribe_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnSubscribeReceivedDelegatePrivate))]
#endif
		private static void on_subscribe_received(IntPtr lc, IntPtr lev, string subscribeEvent, IntPtr body)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_subscribe_received_public?.Invoke(thiz, fromNativePtr<Linphone.Event>(lev), subscribeEvent, fromNativePtr<Linphone.Content>(body));
			}
		}

		public OnSubscribeReceivedDelegate OnSubscribeReceived
		{
			get
			{
				return on_subscribe_received_public;
			}
			set
			{
				on_subscribe_received_public = value;
#if WINDOWS_UWP
				on_subscribe_received_private = on_subscribe_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_subscribe_received_private);
				linphone_core_cbs_set_subscribe_received(nativePtr, cb);
#else
				linphone_core_cbs_set_subscribe_received(nativePtr, on_subscribe_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_info_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_info_received(IntPtr thiz, OnInfoReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnInfoReceivedDelegatePrivate(IntPtr lc, IntPtr call, IntPtr msg);

		public delegate void OnInfoReceivedDelegate(Linphone.Core lc, Linphone.Call call, Linphone.InfoMessage msg);
		private OnInfoReceivedDelegatePrivate on_info_received_private;
		private OnInfoReceivedDelegate on_info_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnInfoReceivedDelegatePrivate))]
#endif
		private static void on_info_received(IntPtr lc, IntPtr call, IntPtr msg)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_info_received_public?.Invoke(thiz, fromNativePtr<Linphone.Call>(call), fromNativePtr<Linphone.InfoMessage>(msg));
			}
		}

		public OnInfoReceivedDelegate OnInfoReceived
		{
			get
			{
				return on_info_received_public;
			}
			set
			{
				on_info_received_public = value;
#if WINDOWS_UWP
				on_info_received_private = on_info_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_info_received_private);
				linphone_core_cbs_set_info_received(nativePtr, cb);
#else
				linphone_core_cbs_set_info_received(nativePtr, on_info_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_chat_room_read(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_chat_room_read(IntPtr thiz, OnChatRoomReadDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnChatRoomReadDelegatePrivate(IntPtr lc, IntPtr room);

		public delegate void OnChatRoomReadDelegate(Linphone.Core lc, Linphone.ChatRoom room);
		private OnChatRoomReadDelegatePrivate on_chat_room_read_private;
		private OnChatRoomReadDelegate on_chat_room_read_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnChatRoomReadDelegatePrivate))]
#endif
		private static void on_chat_room_read(IntPtr lc, IntPtr room)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_chat_room_read_public?.Invoke(thiz, fromNativePtr<Linphone.ChatRoom>(room));
			}
		}

		public OnChatRoomReadDelegate OnChatRoomRead
		{
			get
			{
				return on_chat_room_read_public;
			}
			set
			{
				on_chat_room_read_public = value;
#if WINDOWS_UWP
				on_chat_room_read_private = on_chat_room_read;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_chat_room_read_private);
				linphone_core_cbs_set_chat_room_read(nativePtr, cb);
#else
				linphone_core_cbs_set_chat_room_read(nativePtr, on_chat_room_read);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_registration_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_registration_state_changed(IntPtr thiz, OnRegistrationStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnRegistrationStateChangedDelegatePrivate(IntPtr lc, IntPtr cfg, int cstate, string message);

		public delegate void OnRegistrationStateChangedDelegate(Linphone.Core lc, Linphone.ProxyConfig cfg, Linphone.RegistrationState cstate, string message);
		private OnRegistrationStateChangedDelegatePrivate on_registration_state_changed_private;
		private OnRegistrationStateChangedDelegate on_registration_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnRegistrationStateChangedDelegatePrivate))]
#endif
		private static void on_registration_state_changed(IntPtr lc, IntPtr cfg, int cstate, string message)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_registration_state_changed_public?.Invoke(thiz, fromNativePtr<Linphone.ProxyConfig>(cfg), (Linphone.RegistrationState)cstate, message);
			}
		}

		public OnRegistrationStateChangedDelegate OnRegistrationStateChanged
		{
			get
			{
				return on_registration_state_changed_public;
			}
			set
			{
				on_registration_state_changed_public = value;
#if WINDOWS_UWP
				on_registration_state_changed_private = on_registration_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_registration_state_changed_private);
				linphone_core_cbs_set_registration_state_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_registration_state_changed(nativePtr, on_registration_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_imee_user_registration(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_imee_user_registration(IntPtr thiz, OnImeeUserRegistrationDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnImeeUserRegistrationDelegatePrivate(IntPtr lc, char status, string userId, string info);

		public delegate void OnImeeUserRegistrationDelegate(Linphone.Core lc, bool status, string userId, string info);
		private OnImeeUserRegistrationDelegatePrivate on_imee_user_registration_private;
		private OnImeeUserRegistrationDelegate on_imee_user_registration_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnImeeUserRegistrationDelegatePrivate))]
#endif
		private static void on_imee_user_registration(IntPtr lc, char status, string userId, string info)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_imee_user_registration_public?.Invoke(thiz, status == 0, userId, info);
			}
		}

		public OnImeeUserRegistrationDelegate OnImeeUserRegistration
		{
			get
			{
				return on_imee_user_registration_public;
			}
			set
			{
				on_imee_user_registration_public = value;
#if WINDOWS_UWP
				on_imee_user_registration_private = on_imee_user_registration;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_imee_user_registration_private);
				linphone_core_cbs_set_imee_user_registration(nativePtr, cb);
#else
				linphone_core_cbs_set_imee_user_registration(nativePtr, on_imee_user_registration);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_friend_list_removed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_friend_list_removed(IntPtr thiz, OnFriendListRemovedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnFriendListRemovedDelegatePrivate(IntPtr lc, IntPtr list);

		public delegate void OnFriendListRemovedDelegate(Linphone.Core lc, Linphone.FriendList list);
		private OnFriendListRemovedDelegatePrivate on_friend_list_removed_private;
		private OnFriendListRemovedDelegate on_friend_list_removed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnFriendListRemovedDelegatePrivate))]
#endif
		private static void on_friend_list_removed(IntPtr lc, IntPtr list)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_friend_list_removed_public?.Invoke(thiz, fromNativePtr<Linphone.FriendList>(list));
			}
		}

		public OnFriendListRemovedDelegate OnFriendListRemoved
		{
			get
			{
				return on_friend_list_removed_public;
			}
			set
			{
				on_friend_list_removed_public = value;
#if WINDOWS_UWP
				on_friend_list_removed_private = on_friend_list_removed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_friend_list_removed_private);
				linphone_core_cbs_set_friend_list_removed(nativePtr, cb);
#else
				linphone_core_cbs_set_friend_list_removed(nativePtr, on_friend_list_removed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_refer_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_refer_received(IntPtr thiz, OnReferReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnReferReceivedDelegatePrivate(IntPtr lc, string referTo);

		public delegate void OnReferReceivedDelegate(Linphone.Core lc, string referTo);
		private OnReferReceivedDelegatePrivate on_refer_received_private;
		private OnReferReceivedDelegate on_refer_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnReferReceivedDelegatePrivate))]
#endif
		private static void on_refer_received(IntPtr lc, string referTo)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_refer_received_public?.Invoke(thiz, referTo);
			}
		}

		public OnReferReceivedDelegate OnReferReceived
		{
			get
			{
				return on_refer_received_public;
			}
			set
			{
				on_refer_received_public = value;
#if WINDOWS_UWP
				on_refer_received_private = on_refer_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_refer_received_private);
				linphone_core_cbs_set_refer_received(nativePtr, cb);
#else
				linphone_core_cbs_set_refer_received(nativePtr, on_refer_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_qrcode_found(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_qrcode_found(IntPtr thiz, OnQrcodeFoundDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnQrcodeFoundDelegatePrivate(IntPtr lc, string result);

		public delegate void OnQrcodeFoundDelegate(Linphone.Core lc, string result);
		private OnQrcodeFoundDelegatePrivate on_qrcode_found_private;
		private OnQrcodeFoundDelegate on_qrcode_found_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnQrcodeFoundDelegatePrivate))]
#endif
		private static void on_qrcode_found(IntPtr lc, string result)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_qrcode_found_public?.Invoke(thiz, result);
			}
		}

		public OnQrcodeFoundDelegate OnQrcodeFound
		{
			get
			{
				return on_qrcode_found_public;
			}
			set
			{
				on_qrcode_found_public = value;
#if WINDOWS_UWP
				on_qrcode_found_private = on_qrcode_found;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_qrcode_found_private);
				linphone_core_cbs_set_qrcode_found(nativePtr, cb);
#else
				linphone_core_cbs_set_qrcode_found(nativePtr, on_qrcode_found);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_configuring_status(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_configuring_status(IntPtr thiz, OnConfiguringStatusDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnConfiguringStatusDelegatePrivate(IntPtr lc, int status, string message);

		public delegate void OnConfiguringStatusDelegate(Linphone.Core lc, Linphone.ConfiguringState status, string message);
		private OnConfiguringStatusDelegatePrivate on_configuring_status_private;
		private OnConfiguringStatusDelegate on_configuring_status_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnConfiguringStatusDelegatePrivate))]
#endif
		private static void on_configuring_status(IntPtr lc, int status, string message)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_configuring_status_public?.Invoke(thiz, (Linphone.ConfiguringState)status, message);
			}
		}

		public OnConfiguringStatusDelegate OnConfiguringStatus
		{
			get
			{
				return on_configuring_status_public;
			}
			set
			{
				on_configuring_status_public = value;
#if WINDOWS_UWP
				on_configuring_status_private = on_configuring_status;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_configuring_status_private);
				linphone_core_cbs_set_configuring_status(nativePtr, cb);
#else
				linphone_core_cbs_set_configuring_status(nativePtr, on_configuring_status);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_call_created(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_call_created(IntPtr thiz, OnCallCreatedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnCallCreatedDelegatePrivate(IntPtr lc, IntPtr call);

		public delegate void OnCallCreatedDelegate(Linphone.Core lc, Linphone.Call call);
		private OnCallCreatedDelegatePrivate on_call_created_private;
		private OnCallCreatedDelegate on_call_created_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnCallCreatedDelegatePrivate))]
#endif
		private static void on_call_created(IntPtr lc, IntPtr call)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_call_created_public?.Invoke(thiz, fromNativePtr<Linphone.Call>(call));
			}
		}

		public OnCallCreatedDelegate OnCallCreated
		{
			get
			{
				return on_call_created_public;
			}
			set
			{
				on_call_created_public = value;
#if WINDOWS_UWP
				on_call_created_private = on_call_created;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_call_created_private);
				linphone_core_cbs_set_call_created(nativePtr, cb);
#else
				linphone_core_cbs_set_call_created(nativePtr, on_call_created);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_publish_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_publish_state_changed(IntPtr thiz, OnPublishStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnPublishStateChangedDelegatePrivate(IntPtr lc, IntPtr lev, int state);

		public delegate void OnPublishStateChangedDelegate(Linphone.Core lc, Linphone.Event lev, Linphone.PublishState state);
		private OnPublishStateChangedDelegatePrivate on_publish_state_changed_private;
		private OnPublishStateChangedDelegate on_publish_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnPublishStateChangedDelegatePrivate))]
#endif
		private static void on_publish_state_changed(IntPtr lc, IntPtr lev, int state)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_publish_state_changed_public?.Invoke(thiz, fromNativePtr<Linphone.Event>(lev), (Linphone.PublishState)state);
			}
		}

		public OnPublishStateChangedDelegate OnPublishStateChanged
		{
			get
			{
				return on_publish_state_changed_public;
			}
			set
			{
				on_publish_state_changed_public = value;
#if WINDOWS_UWP
				on_publish_state_changed_private = on_publish_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_publish_state_changed_private);
				linphone_core_cbs_set_publish_state_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_publish_state_changed(nativePtr, on_publish_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_first_call_started(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_first_call_started(IntPtr thiz, OnFirstCallStartedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnFirstCallStartedDelegatePrivate(IntPtr lc);

		public delegate void OnFirstCallStartedDelegate(Linphone.Core lc);
		private OnFirstCallStartedDelegatePrivate on_first_call_started_private;
		private OnFirstCallStartedDelegate on_first_call_started_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnFirstCallStartedDelegatePrivate))]
#endif
		private static void on_first_call_started(IntPtr lc)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_first_call_started_public?.Invoke(thiz);
			}
		}

		public OnFirstCallStartedDelegate OnFirstCallStarted
		{
			get
			{
				return on_first_call_started_public;
			}
			set
			{
				on_first_call_started_public = value;
#if WINDOWS_UWP
				on_first_call_started_private = on_first_call_started;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_first_call_started_private);
				linphone_core_cbs_set_first_call_started(nativePtr, cb);
#else
				linphone_core_cbs_set_first_call_started(nativePtr, on_first_call_started);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_call_encryption_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_call_encryption_changed(IntPtr thiz, OnCallEncryptionChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnCallEncryptionChangedDelegatePrivate(IntPtr lc, IntPtr call, char on, string authenticationToken);

		public delegate void OnCallEncryptionChangedDelegate(Linphone.Core lc, Linphone.Call call, bool on, string authenticationToken);
		private OnCallEncryptionChangedDelegatePrivate on_call_encryption_changed_private;
		private OnCallEncryptionChangedDelegate on_call_encryption_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnCallEncryptionChangedDelegatePrivate))]
#endif
		private static void on_call_encryption_changed(IntPtr lc, IntPtr call, char on, string authenticationToken)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_call_encryption_changed_public?.Invoke(thiz, fromNativePtr<Linphone.Call>(call), on == 0, authenticationToken);
			}
		}

		public OnCallEncryptionChangedDelegate OnCallEncryptionChanged
		{
			get
			{
				return on_call_encryption_changed_public;
			}
			set
			{
				on_call_encryption_changed_public = value;
#if WINDOWS_UWP
				on_call_encryption_changed_private = on_call_encryption_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_call_encryption_changed_private);
				linphone_core_cbs_set_call_encryption_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_call_encryption_changed(nativePtr, on_call_encryption_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_is_composing_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_is_composing_received(IntPtr thiz, OnIsComposingReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnIsComposingReceivedDelegatePrivate(IntPtr lc, IntPtr room);

		public delegate void OnIsComposingReceivedDelegate(Linphone.Core lc, Linphone.ChatRoom room);
		private OnIsComposingReceivedDelegatePrivate on_is_composing_received_private;
		private OnIsComposingReceivedDelegate on_is_composing_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnIsComposingReceivedDelegatePrivate))]
#endif
		private static void on_is_composing_received(IntPtr lc, IntPtr room)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_is_composing_received_public?.Invoke(thiz, fromNativePtr<Linphone.ChatRoom>(room));
			}
		}

		public OnIsComposingReceivedDelegate OnIsComposingReceived
		{
			get
			{
				return on_is_composing_received_public;
			}
			set
			{
				on_is_composing_received_public = value;
#if WINDOWS_UWP
				on_is_composing_received_private = on_is_composing_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_is_composing_received_private);
				linphone_core_cbs_set_is_composing_received(nativePtr, cb);
#else
				linphone_core_cbs_set_is_composing_received(nativePtr, on_is_composing_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_message_received_unable_decrypt(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_message_received_unable_decrypt(IntPtr thiz, OnMessageReceivedUnableDecryptDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnMessageReceivedUnableDecryptDelegatePrivate(IntPtr lc, IntPtr room, IntPtr message);

		public delegate void OnMessageReceivedUnableDecryptDelegate(Linphone.Core lc, Linphone.ChatRoom room, Linphone.ChatMessage message);
		private OnMessageReceivedUnableDecryptDelegatePrivate on_message_received_unable_decrypt_private;
		private OnMessageReceivedUnableDecryptDelegate on_message_received_unable_decrypt_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnMessageReceivedUnableDecryptDelegatePrivate))]
#endif
		private static void on_message_received_unable_decrypt(IntPtr lc, IntPtr room, IntPtr message)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_message_received_unable_decrypt_public?.Invoke(thiz, fromNativePtr<Linphone.ChatRoom>(room), fromNativePtr<Linphone.ChatMessage>(message));
			}
		}

		public OnMessageReceivedUnableDecryptDelegate OnMessageReceivedUnableDecrypt
		{
			get
			{
				return on_message_received_unable_decrypt_public;
			}
			set
			{
				on_message_received_unable_decrypt_public = value;
#if WINDOWS_UWP
				on_message_received_unable_decrypt_private = on_message_received_unable_decrypt;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_message_received_unable_decrypt_private);
				linphone_core_cbs_set_message_received_unable_decrypt(nativePtr, cb);
#else
				linphone_core_cbs_set_message_received_unable_decrypt(nativePtr, on_message_received_unable_decrypt);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_log_collection_upload_progress_indication(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_log_collection_upload_progress_indication(IntPtr thiz, OnLogCollectionUploadProgressIndicationDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnLogCollectionUploadProgressIndicationDelegatePrivate(IntPtr lc, long offset, long total);

		public delegate void OnLogCollectionUploadProgressIndicationDelegate(Linphone.Core lc, long offset, long total);
		private OnLogCollectionUploadProgressIndicationDelegatePrivate on_log_collection_upload_progress_indication_private;
		private OnLogCollectionUploadProgressIndicationDelegate on_log_collection_upload_progress_indication_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnLogCollectionUploadProgressIndicationDelegatePrivate))]
#endif
		private static void on_log_collection_upload_progress_indication(IntPtr lc, long offset, long total)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_log_collection_upload_progress_indication_public?.Invoke(thiz, offset, total);
			}
		}

		public OnLogCollectionUploadProgressIndicationDelegate OnLogCollectionUploadProgressIndication
		{
			get
			{
				return on_log_collection_upload_progress_indication_public;
			}
			set
			{
				on_log_collection_upload_progress_indication_public = value;
#if WINDOWS_UWP
				on_log_collection_upload_progress_indication_private = on_log_collection_upload_progress_indication;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_log_collection_upload_progress_indication_private);
				linphone_core_cbs_set_log_collection_upload_progress_indication(nativePtr, cb);
#else
				linphone_core_cbs_set_log_collection_upload_progress_indication(nativePtr, on_log_collection_upload_progress_indication);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_chat_room_subject_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_chat_room_subject_changed(IntPtr thiz, OnChatRoomSubjectChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnChatRoomSubjectChangedDelegatePrivate(IntPtr lc, IntPtr cr);

		public delegate void OnChatRoomSubjectChangedDelegate(Linphone.Core lc, Linphone.ChatRoom cr);
		private OnChatRoomSubjectChangedDelegatePrivate on_chat_room_subject_changed_private;
		private OnChatRoomSubjectChangedDelegate on_chat_room_subject_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnChatRoomSubjectChangedDelegatePrivate))]
#endif
		private static void on_chat_room_subject_changed(IntPtr lc, IntPtr cr)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_chat_room_subject_changed_public?.Invoke(thiz, fromNativePtr<Linphone.ChatRoom>(cr));
			}
		}

		public OnChatRoomSubjectChangedDelegate OnChatRoomSubjectChanged
		{
			get
			{
				return on_chat_room_subject_changed_public;
			}
			set
			{
				on_chat_room_subject_changed_public = value;
#if WINDOWS_UWP
				on_chat_room_subject_changed_private = on_chat_room_subject_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_chat_room_subject_changed_private);
				linphone_core_cbs_set_chat_room_subject_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_chat_room_subject_changed(nativePtr, on_chat_room_subject_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_last_call_ended(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_last_call_ended(IntPtr thiz, OnLastCallEndedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnLastCallEndedDelegatePrivate(IntPtr lc);

		public delegate void OnLastCallEndedDelegate(Linphone.Core lc);
		private OnLastCallEndedDelegatePrivate on_last_call_ended_private;
		private OnLastCallEndedDelegate on_last_call_ended_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnLastCallEndedDelegatePrivate))]
#endif
		private static void on_last_call_ended(IntPtr lc)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_last_call_ended_public?.Invoke(thiz);
			}
		}

		public OnLastCallEndedDelegate OnLastCallEnded
		{
			get
			{
				return on_last_call_ended_public;
			}
			set
			{
				on_last_call_ended_public = value;
#if WINDOWS_UWP
				on_last_call_ended_private = on_last_call_ended;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_last_call_ended_private);
				linphone_core_cbs_set_last_call_ended(nativePtr, cb);
#else
				linphone_core_cbs_set_last_call_ended(nativePtr, on_last_call_ended);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_version_update_check_result_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_version_update_check_result_received(IntPtr thiz, OnVersionUpdateCheckResultReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnVersionUpdateCheckResultReceivedDelegatePrivate(IntPtr lc, int result, string version, string url);

		public delegate void OnVersionUpdateCheckResultReceivedDelegate(Linphone.Core lc, Linphone.VersionUpdateCheckResult result, string version, string url);
		private OnVersionUpdateCheckResultReceivedDelegatePrivate on_version_update_check_result_received_private;
		private OnVersionUpdateCheckResultReceivedDelegate on_version_update_check_result_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnVersionUpdateCheckResultReceivedDelegatePrivate))]
#endif
		private static void on_version_update_check_result_received(IntPtr lc, int result, string version, string url)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_version_update_check_result_received_public?.Invoke(thiz, (Linphone.VersionUpdateCheckResult)result, version, url);
			}
		}

		public OnVersionUpdateCheckResultReceivedDelegate OnVersionUpdateCheckResultReceived
		{
			get
			{
				return on_version_update_check_result_received_public;
			}
			set
			{
				on_version_update_check_result_received_public = value;
#if WINDOWS_UWP
				on_version_update_check_result_received_private = on_version_update_check_result_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_version_update_check_result_received_private);
				linphone_core_cbs_set_version_update_check_result_received(nativePtr, cb);
#else
				linphone_core_cbs_set_version_update_check_result_received(nativePtr, on_version_update_check_result_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_ec_calibration_audio_uninit(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_ec_calibration_audio_uninit(IntPtr thiz, OnEcCalibrationAudioUninitDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnEcCalibrationAudioUninitDelegatePrivate(IntPtr lc);

		public delegate void OnEcCalibrationAudioUninitDelegate(Linphone.Core lc);
		private OnEcCalibrationAudioUninitDelegatePrivate on_ec_calibration_audio_uninit_private;
		private OnEcCalibrationAudioUninitDelegate on_ec_calibration_audio_uninit_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnEcCalibrationAudioUninitDelegatePrivate))]
#endif
		private static void on_ec_calibration_audio_uninit(IntPtr lc)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_ec_calibration_audio_uninit_public?.Invoke(thiz);
			}
		}

		public OnEcCalibrationAudioUninitDelegate OnEcCalibrationAudioUninit
		{
			get
			{
				return on_ec_calibration_audio_uninit_public;
			}
			set
			{
				on_ec_calibration_audio_uninit_public = value;
#if WINDOWS_UWP
				on_ec_calibration_audio_uninit_private = on_ec_calibration_audio_uninit;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_ec_calibration_audio_uninit_private);
				linphone_core_cbs_set_ec_calibration_audio_uninit(nativePtr, cb);
#else
				linphone_core_cbs_set_ec_calibration_audio_uninit(nativePtr, on_ec_calibration_audio_uninit);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_global_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_global_state_changed(IntPtr thiz, OnGlobalStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnGlobalStateChangedDelegatePrivate(IntPtr lc, int gstate, string message);

		public delegate void OnGlobalStateChangedDelegate(Linphone.Core lc, Linphone.GlobalState gstate, string message);
		private OnGlobalStateChangedDelegatePrivate on_global_state_changed_private;
		private OnGlobalStateChangedDelegate on_global_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnGlobalStateChangedDelegatePrivate))]
#endif
		private static void on_global_state_changed(IntPtr lc, int gstate, string message)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_global_state_changed_public?.Invoke(thiz, (Linphone.GlobalState)gstate, message);
			}
		}

		public OnGlobalStateChangedDelegate OnGlobalStateChanged
		{
			get
			{
				return on_global_state_changed_public;
			}
			set
			{
				on_global_state_changed_public = value;
#if WINDOWS_UWP
				on_global_state_changed_private = on_global_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_global_state_changed_private);
				linphone_core_cbs_set_global_state_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_global_state_changed(nativePtr, on_global_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_log_collection_upload_state_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_log_collection_upload_state_changed(IntPtr thiz, OnLogCollectionUploadStateChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnLogCollectionUploadStateChangedDelegatePrivate(IntPtr lc, int state, string info);

		public delegate void OnLogCollectionUploadStateChangedDelegate(Linphone.Core lc, Linphone.CoreLogCollectionUploadState state, string info);
		private OnLogCollectionUploadStateChangedDelegatePrivate on_log_collection_upload_state_changed_private;
		private OnLogCollectionUploadStateChangedDelegate on_log_collection_upload_state_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnLogCollectionUploadStateChangedDelegatePrivate))]
#endif
		private static void on_log_collection_upload_state_changed(IntPtr lc, int state, string info)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_log_collection_upload_state_changed_public?.Invoke(thiz, (Linphone.CoreLogCollectionUploadState)state, info);
			}
		}

		public OnLogCollectionUploadStateChangedDelegate OnLogCollectionUploadStateChanged
		{
			get
			{
				return on_log_collection_upload_state_changed_public;
			}
			set
			{
				on_log_collection_upload_state_changed_public = value;
#if WINDOWS_UWP
				on_log_collection_upload_state_changed_private = on_log_collection_upload_state_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_log_collection_upload_state_changed_private);
				linphone_core_cbs_set_log_collection_upload_state_changed(nativePtr, cb);
#else
				linphone_core_cbs_set_log_collection_upload_state_changed(nativePtr, on_log_collection_upload_state_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_dtmf_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_dtmf_received(IntPtr thiz, OnDtmfReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnDtmfReceivedDelegatePrivate(IntPtr lc, IntPtr call, int dtmf);

		public delegate void OnDtmfReceivedDelegate(Linphone.Core lc, Linphone.Call call, int dtmf);
		private OnDtmfReceivedDelegatePrivate on_dtmf_received_private;
		private OnDtmfReceivedDelegate on_dtmf_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnDtmfReceivedDelegatePrivate))]
#endif
		private static void on_dtmf_received(IntPtr lc, IntPtr call, int dtmf)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_dtmf_received_public?.Invoke(thiz, fromNativePtr<Linphone.Call>(call), dtmf);
			}
		}

		public OnDtmfReceivedDelegate OnDtmfReceived
		{
			get
			{
				return on_dtmf_received_public;
			}
			set
			{
				on_dtmf_received_public = value;
#if WINDOWS_UWP
				on_dtmf_received_private = on_dtmf_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_dtmf_received_private);
				linphone_core_cbs_set_dtmf_received(nativePtr, cb);
#else
				linphone_core_cbs_set_dtmf_received(nativePtr, on_dtmf_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_chat_room_ephemeral_message_deleted(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_chat_room_ephemeral_message_deleted(IntPtr thiz, OnChatRoomEphemeralMessageDeletedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnChatRoomEphemeralMessageDeletedDelegatePrivate(IntPtr lc, IntPtr cr);

		public delegate void OnChatRoomEphemeralMessageDeletedDelegate(Linphone.Core lc, Linphone.ChatRoom cr);
		private OnChatRoomEphemeralMessageDeletedDelegatePrivate on_chat_room_ephemeral_message_deleted_private;
		private OnChatRoomEphemeralMessageDeletedDelegate on_chat_room_ephemeral_message_deleted_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnChatRoomEphemeralMessageDeletedDelegatePrivate))]
#endif
		private static void on_chat_room_ephemeral_message_deleted(IntPtr lc, IntPtr cr)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_chat_room_ephemeral_message_deleted_public?.Invoke(thiz, fromNativePtr<Linphone.ChatRoom>(cr));
			}
		}

		public OnChatRoomEphemeralMessageDeletedDelegate OnChatRoomEphemeralMessageDeleted
		{
			get
			{
				return on_chat_room_ephemeral_message_deleted_public;
			}
			set
			{
				on_chat_room_ephemeral_message_deleted_public = value;
#if WINDOWS_UWP
				on_chat_room_ephemeral_message_deleted_private = on_chat_room_ephemeral_message_deleted;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_chat_room_ephemeral_message_deleted_private);
				linphone_core_cbs_set_chat_room_ephemeral_message_deleted(nativePtr, cb);
#else
				linphone_core_cbs_set_chat_room_ephemeral_message_deleted(nativePtr, on_chat_room_ephemeral_message_deleted);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_message_sent(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_message_sent(IntPtr thiz, OnMessageSentDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnMessageSentDelegatePrivate(IntPtr lc, IntPtr room, IntPtr message);

		public delegate void OnMessageSentDelegate(Linphone.Core lc, Linphone.ChatRoom room, Linphone.ChatMessage message);
		private OnMessageSentDelegatePrivate on_message_sent_private;
		private OnMessageSentDelegate on_message_sent_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnMessageSentDelegatePrivate))]
#endif
		private static void on_message_sent(IntPtr lc, IntPtr room, IntPtr message)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_message_sent_public?.Invoke(thiz, fromNativePtr<Linphone.ChatRoom>(room), fromNativePtr<Linphone.ChatMessage>(message));
			}
		}

		public OnMessageSentDelegate OnMessageSent
		{
			get
			{
				return on_message_sent_public;
			}
			set
			{
				on_message_sent_public = value;
#if WINDOWS_UWP
				on_message_sent_private = on_message_sent;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_message_sent_private);
				linphone_core_cbs_set_message_sent(nativePtr, cb);
#else
				linphone_core_cbs_set_message_sent(nativePtr, on_message_sent);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_core_cbs_set_audio_devices_list_updated(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_core_cbs_set_audio_devices_list_updated(IntPtr thiz, OnAudioDevicesListUpdatedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnAudioDevicesListUpdatedDelegatePrivate(IntPtr lc);

		public delegate void OnAudioDevicesListUpdatedDelegate(Linphone.Core lc);
		private OnAudioDevicesListUpdatedDelegatePrivate on_audio_devices_list_updated_private;
		private OnAudioDevicesListUpdatedDelegate on_audio_devices_list_updated_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnAudioDevicesListUpdatedDelegatePrivate))]
#endif
		private static void on_audio_devices_list_updated(IntPtr lc)
		{
			Core thiz = fromNativePtr<Core>(lc);
			CoreListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_audio_devices_list_updated_public?.Invoke(thiz);
			}
		}

		public OnAudioDevicesListUpdatedDelegate OnAudioDevicesListUpdated
		{
			get
			{
				return on_audio_devices_list_updated_public;
			}
			set
			{
				on_audio_devices_list_updated_public = value;
#if WINDOWS_UWP
				on_audio_devices_list_updated_private = on_audio_devices_list_updated;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_audio_devices_list_updated_private);
				linphone_core_cbs_set_audio_devices_list_updated(nativePtr, cb);
#else
				linphone_core_cbs_set_audio_devices_list_updated(nativePtr, on_audio_devices_list_updated);
#endif
			}
		}

		internal void register() {
			IntPtr listener = linphone_core_cbs_get_user_data(nativePtr);
			if (listener == IntPtr.Zero)
			{
				GCHandle _handle = GCHandle.Alloc(this, GCHandleType.Normal);
				listener = GCHandle.ToIntPtr(_handle);
			} else
			{
				GCHandle _handle = GCHandle.FromIntPtr(listener);
				if (_handle.Target == this)
				{
					return;
				} else
				{
					_handle.Free();
					_handle = GCHandle.Alloc(this, GCHandleType.Normal);
					listener = GCHandle.ToIntPtr(_handle);
				}
			}
			linphone_core_cbs_set_user_data(nativePtr, listener);
		}

		internal void unregister() {
            IntPtr listener= IntPtr.Zero;
            if (nativePtr != IntPtr.Zero)
            {
                listener = linphone_core_cbs_get_user_data(nativePtr);
                linphone_core_cbs_set_user_data(nativePtr, IntPtr.Zero);
                if (listener != IntPtr.Zero)
                {
                    GCHandle.FromIntPtr(listener).Free();
                }
            }
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class EventListener : LinphoneObject
	{
        ~EventListener()
        {
            unregister();
        }

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_event_cbs_set_user_data(IntPtr thiz, IntPtr listener);

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_event_cbs_get_user_data(IntPtr thiz);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_event_cbs_set_notify_response(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_event_cbs_set_notify_response(IntPtr thiz, OnNotifyResponseDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnNotifyResponseDelegatePrivate(IntPtr ev);

		public delegate void OnNotifyResponseDelegate(Linphone.Event ev);
		private OnNotifyResponseDelegatePrivate on_notify_response_private;
		private OnNotifyResponseDelegate on_notify_response_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnNotifyResponseDelegatePrivate))]
#endif
		private static void on_notify_response(IntPtr ev)
		{
			Event thiz = fromNativePtr<Event>(ev);
			EventListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_notify_response_public?.Invoke(thiz);
			}
		}

		public OnNotifyResponseDelegate OnNotifyResponse
		{
			get
			{
				return on_notify_response_public;
			}
			set
			{
				on_notify_response_public = value;
#if WINDOWS_UWP
				on_notify_response_private = on_notify_response;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_notify_response_private);
				linphone_event_cbs_set_notify_response(nativePtr, cb);
#else
				linphone_event_cbs_set_notify_response(nativePtr, on_notify_response);
#endif
			}
		}

		internal void register() {
			IntPtr listener = linphone_event_cbs_get_user_data(nativePtr);
			if (listener == IntPtr.Zero)
			{
				GCHandle _handle = GCHandle.Alloc(this, GCHandleType.Normal);
				listener = GCHandle.ToIntPtr(_handle);
			} else
			{
				GCHandle _handle = GCHandle.FromIntPtr(listener);
				if (_handle.Target == this)
				{
					return;
				} else
				{
					_handle.Free();
					_handle = GCHandle.Alloc(this, GCHandleType.Normal);
					listener = GCHandle.ToIntPtr(_handle);
				}
			}
			linphone_event_cbs_set_user_data(nativePtr, listener);
		}

		internal void unregister() {
			IntPtr listener = linphone_event_cbs_get_user_data(nativePtr);
			linphone_event_cbs_set_user_data(nativePtr, IntPtr.Zero);
			if (listener != IntPtr.Zero)
			{
				GCHandle.FromIntPtr(listener).Free();
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class FriendListListener : LinphoneObject
	{
        ~FriendListListener()
        {
            unregister();
        }

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_friend_list_cbs_set_user_data(IntPtr thiz, IntPtr listener);

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_friend_list_cbs_get_user_data(IntPtr thiz);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_friend_list_cbs_set_contact_updated(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_friend_list_cbs_set_contact_updated(IntPtr thiz, OnContactUpdatedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnContactUpdatedDelegatePrivate(IntPtr list, IntPtr newFriend, IntPtr oldFriend);

		public delegate void OnContactUpdatedDelegate(Linphone.FriendList list, Linphone.Friend newFriend, Linphone.Friend oldFriend);
		private OnContactUpdatedDelegatePrivate on_contact_updated_private;
		private OnContactUpdatedDelegate on_contact_updated_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnContactUpdatedDelegatePrivate))]
#endif
		private static void on_contact_updated(IntPtr list, IntPtr newFriend, IntPtr oldFriend)
		{
			FriendList thiz = fromNativePtr<FriendList>(list);
			FriendListListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_contact_updated_public?.Invoke(thiz, fromNativePtr<Linphone.Friend>(newFriend), fromNativePtr<Linphone.Friend>(oldFriend));
			}
		}

		public OnContactUpdatedDelegate OnContactUpdated
		{
			get
			{
				return on_contact_updated_public;
			}
			set
			{
				on_contact_updated_public = value;
#if WINDOWS_UWP
				on_contact_updated_private = on_contact_updated;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_contact_updated_private);
				linphone_friend_list_cbs_set_contact_updated(nativePtr, cb);
#else
				linphone_friend_list_cbs_set_contact_updated(nativePtr, on_contact_updated);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_friend_list_cbs_set_presence_received(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_friend_list_cbs_set_presence_received(IntPtr thiz, OnPresenceReceivedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnPresenceReceivedDelegatePrivate(IntPtr list, IntPtr friends);

		public delegate void OnPresenceReceivedDelegate(Linphone.FriendList list, IEnumerable<Linphone.Friend> friends);
		private OnPresenceReceivedDelegatePrivate on_presence_received_private;
		private OnPresenceReceivedDelegate on_presence_received_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnPresenceReceivedDelegatePrivate))]
#endif
		private static void on_presence_received(IntPtr list, IntPtr friends)
		{
			FriendList thiz = fromNativePtr<FriendList>(list);
			FriendListListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_presence_received_public?.Invoke(thiz, MarshalBctbxList<Linphone.Friend>(friends));
			}
		}

		public OnPresenceReceivedDelegate OnPresenceReceived
		{
			get
			{
				return on_presence_received_public;
			}
			set
			{
				on_presence_received_public = value;
#if WINDOWS_UWP
				on_presence_received_private = on_presence_received;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_presence_received_private);
				linphone_friend_list_cbs_set_presence_received(nativePtr, cb);
#else
				linphone_friend_list_cbs_set_presence_received(nativePtr, on_presence_received);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_friend_list_cbs_set_sync_status_changed(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_friend_list_cbs_set_sync_status_changed(IntPtr thiz, OnSyncStatusChangedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnSyncStatusChangedDelegatePrivate(IntPtr list, int status, string msg);

		public delegate void OnSyncStatusChangedDelegate(Linphone.FriendList list, Linphone.FriendListSyncStatus status, string msg);
		private OnSyncStatusChangedDelegatePrivate on_sync_status_changed_private;
		private OnSyncStatusChangedDelegate on_sync_status_changed_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnSyncStatusChangedDelegatePrivate))]
#endif
		private static void on_sync_status_changed(IntPtr list, int status, string msg)
		{
			FriendList thiz = fromNativePtr<FriendList>(list);
			FriendListListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_sync_status_changed_public?.Invoke(thiz, (Linphone.FriendListSyncStatus)status, msg);
			}
		}

		public OnSyncStatusChangedDelegate OnSyncStatusChanged
		{
			get
			{
				return on_sync_status_changed_public;
			}
			set
			{
				on_sync_status_changed_public = value;
#if WINDOWS_UWP
				on_sync_status_changed_private = on_sync_status_changed;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_sync_status_changed_private);
				linphone_friend_list_cbs_set_sync_status_changed(nativePtr, cb);
#else
				linphone_friend_list_cbs_set_sync_status_changed(nativePtr, on_sync_status_changed);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_friend_list_cbs_set_contact_created(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_friend_list_cbs_set_contact_created(IntPtr thiz, OnContactCreatedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnContactCreatedDelegatePrivate(IntPtr list, IntPtr lf);

		public delegate void OnContactCreatedDelegate(Linphone.FriendList list, Linphone.Friend lf);
		private OnContactCreatedDelegatePrivate on_contact_created_private;
		private OnContactCreatedDelegate on_contact_created_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnContactCreatedDelegatePrivate))]
#endif
		private static void on_contact_created(IntPtr list, IntPtr lf)
		{
			FriendList thiz = fromNativePtr<FriendList>(list);
			FriendListListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_contact_created_public?.Invoke(thiz, fromNativePtr<Linphone.Friend>(lf));
			}
		}

		public OnContactCreatedDelegate OnContactCreated
		{
			get
			{
				return on_contact_created_public;
			}
			set
			{
				on_contact_created_public = value;
#if WINDOWS_UWP
				on_contact_created_private = on_contact_created;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_contact_created_private);
				linphone_friend_list_cbs_set_contact_created(nativePtr, cb);
#else
				linphone_friend_list_cbs_set_contact_created(nativePtr, on_contact_created);
#endif
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_friend_list_cbs_set_contact_deleted(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_friend_list_cbs_set_contact_deleted(IntPtr thiz, OnContactDeletedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnContactDeletedDelegatePrivate(IntPtr list, IntPtr lf);

		public delegate void OnContactDeletedDelegate(Linphone.FriendList list, Linphone.Friend lf);
		private OnContactDeletedDelegatePrivate on_contact_deleted_private;
		private OnContactDeletedDelegate on_contact_deleted_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnContactDeletedDelegatePrivate))]
#endif
		private static void on_contact_deleted(IntPtr list, IntPtr lf)
		{
			FriendList thiz = fromNativePtr<FriendList>(list);
			FriendListListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_contact_deleted_public?.Invoke(thiz, fromNativePtr<Linphone.Friend>(lf));
			}
		}

		public OnContactDeletedDelegate OnContactDeleted
		{
			get
			{
				return on_contact_deleted_public;
			}
			set
			{
				on_contact_deleted_public = value;
#if WINDOWS_UWP
				on_contact_deleted_private = on_contact_deleted;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_contact_deleted_private);
				linphone_friend_list_cbs_set_contact_deleted(nativePtr, cb);
#else
				linphone_friend_list_cbs_set_contact_deleted(nativePtr, on_contact_deleted);
#endif
			}
		}

		internal void register() {
			IntPtr listener = linphone_friend_list_cbs_get_user_data(nativePtr);
			if (listener == IntPtr.Zero)
			{
				GCHandle _handle = GCHandle.Alloc(this, GCHandleType.Normal);
				listener = GCHandle.ToIntPtr(_handle);
			} else
			{
				GCHandle _handle = GCHandle.FromIntPtr(listener);
				if (_handle.Target == this)
				{
					return;
				} else
				{
					_handle.Free();
					_handle = GCHandle.Alloc(this, GCHandleType.Normal);
					listener = GCHandle.ToIntPtr(_handle);
				}
			}
			linphone_friend_list_cbs_set_user_data(nativePtr, listener);
		}

		internal void unregister() {
			IntPtr listener = linphone_friend_list_cbs_get_user_data(nativePtr);
			linphone_friend_list_cbs_set_user_data(nativePtr, IntPtr.Zero);
			if (listener != IntPtr.Zero)
			{
				GCHandle.FromIntPtr(listener).Free();
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class LoggingServiceListener : LinphoneObject
	{
        ~LoggingServiceListener()
        {
            unregister();
        }

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_logging_service_cbs_set_user_data(IntPtr thiz, IntPtr listener);

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_logging_service_cbs_get_user_data(IntPtr thiz);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_logging_service_cbs_set_log_message_written(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_logging_service_cbs_set_log_message_written(IntPtr thiz, OnLogMessageWrittenDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnLogMessageWrittenDelegatePrivate(IntPtr logService, string domain, int lev, string message);

		public delegate void OnLogMessageWrittenDelegate(Linphone.LoggingService logService, string domain, Linphone.LogLevel lev, string message);
		private OnLogMessageWrittenDelegatePrivate on_log_message_written_private;
		private OnLogMessageWrittenDelegate on_log_message_written_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnLogMessageWrittenDelegatePrivate))]
#endif
		private static void on_log_message_written(IntPtr logService, string domain, int lev, string message)
		{
			LoggingService thiz = fromNativePtr<LoggingService>(logService);
			LoggingServiceListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_log_message_written_public?.Invoke(thiz, domain, (Linphone.LogLevel)lev, message);
			}
		}

		public OnLogMessageWrittenDelegate OnLogMessageWritten
		{
			get
			{
				return on_log_message_written_public;
			}
			set
			{
				on_log_message_written_public = value;
#if WINDOWS_UWP
				on_log_message_written_private = on_log_message_written;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_log_message_written_private);
				linphone_logging_service_cbs_set_log_message_written(nativePtr, cb);
#else
				linphone_logging_service_cbs_set_log_message_written(nativePtr, on_log_message_written);
#endif
			}
		}

		internal void register() {
			IntPtr listener = linphone_logging_service_cbs_get_user_data(nativePtr);
			if (listener == IntPtr.Zero)
			{
				GCHandle _handle = GCHandle.Alloc(this, GCHandleType.Normal);
				listener = GCHandle.ToIntPtr(_handle);
			} else
			{
				GCHandle _handle = GCHandle.FromIntPtr(listener);
				if (_handle.Target == this)
				{
					return;
				} else
				{
					_handle.Free();
					_handle = GCHandle.Alloc(this, GCHandleType.Normal);
					listener = GCHandle.ToIntPtr(_handle);
				}
			}
			linphone_logging_service_cbs_set_user_data(nativePtr, listener);
		}

		internal void unregister() {
			IntPtr listener = linphone_logging_service_cbs_get_user_data(nativePtr);
			linphone_logging_service_cbs_set_user_data(nativePtr, IntPtr.Zero);
			if (listener != IntPtr.Zero)
			{
				GCHandle.FromIntPtr(listener).Free();
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class PlayerListener : LinphoneObject
	{
        ~PlayerListener()
        {
            unregister();
        }

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_player_cbs_set_user_data(IntPtr thiz, IntPtr listener);

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_player_cbs_get_user_data(IntPtr thiz);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_player_cbs_set_eof_reached(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_player_cbs_set_eof_reached(IntPtr thiz, OnEofReachedDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnEofReachedDelegatePrivate(IntPtr obj);

		public delegate void OnEofReachedDelegate(Linphone.Player obj);
		private OnEofReachedDelegatePrivate on_eof_reached_private;
		private OnEofReachedDelegate on_eof_reached_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnEofReachedDelegatePrivate))]
#endif
		private static void on_eof_reached(IntPtr obj)
		{
			Player thiz = fromNativePtr<Player>(obj);
			PlayerListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_eof_reached_public?.Invoke(thiz);
			}
		}

		public OnEofReachedDelegate OnEofReached
		{
			get
			{
				return on_eof_reached_public;
			}
			set
			{
				on_eof_reached_public = value;
#if WINDOWS_UWP
				on_eof_reached_private = on_eof_reached;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_eof_reached_private);
				linphone_player_cbs_set_eof_reached(nativePtr, cb);
#else
				linphone_player_cbs_set_eof_reached(nativePtr, on_eof_reached);
#endif
			}
		}

		internal void register() {
			IntPtr listener = linphone_player_cbs_get_user_data(nativePtr);
			if (listener == IntPtr.Zero)
			{
				GCHandle _handle = GCHandle.Alloc(this, GCHandleType.Normal);
				listener = GCHandle.ToIntPtr(_handle);
			} else
			{
				GCHandle _handle = GCHandle.FromIntPtr(listener);
				if (_handle.Target == this)
				{
					return;
				} else
				{
					_handle.Free();
					_handle = GCHandle.Alloc(this, GCHandleType.Normal);
					listener = GCHandle.ToIntPtr(_handle);
				}
			}
			linphone_player_cbs_set_user_data(nativePtr, listener);
		}

		internal void unregister() {
			IntPtr listener = linphone_player_cbs_get_user_data(nativePtr);
			linphone_player_cbs_set_user_data(nativePtr, IntPtr.Zero);
			if (listener != IntPtr.Zero)
			{
				GCHandle.FromIntPtr(listener).Free();
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class XmlRpcRequestListener : LinphoneObject
	{
        ~XmlRpcRequestListener()
        {
            unregister();
        }

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_xml_rpc_request_cbs_set_user_data(IntPtr thiz, IntPtr listener);

        [DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_xml_rpc_request_cbs_get_user_data(IntPtr thiz);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
#if WINDOWS_UWP
		static extern void linphone_xml_rpc_request_cbs_set_response(IntPtr thiz, IntPtr cb);
#else
		static extern void linphone_xml_rpc_request_cbs_set_response(IntPtr thiz, OnResponseDelegatePrivate cb);
#endif

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void OnResponseDelegatePrivate(IntPtr request);

		public delegate void OnResponseDelegate(Linphone.XmlRpcRequest request);
		private OnResponseDelegatePrivate on_response_private;
		private OnResponseDelegate on_response_public;

#if __IOS__
		[MonoPInvokeCallback(typeof(OnResponseDelegatePrivate))]
#endif
		private static void on_response(IntPtr request)
		{
			XmlRpcRequest thiz = fromNativePtr<XmlRpcRequest>(request);
			XmlRpcRequestListener current_listener = thiz.CurrentCallbacks;
			if (current_listener != null)
			{
				current_listener.on_response_public?.Invoke(thiz);
			}
		}

		public OnResponseDelegate OnResponse
		{
			get
			{
				return on_response_public;
			}
			set
			{
				on_response_public = value;
#if WINDOWS_UWP
				on_response_private = on_response;
				IntPtr cb = Marshal.GetFunctionPointerForDelegate(on_response_private);
				linphone_xml_rpc_request_cbs_set_response(nativePtr, cb);
#else
				linphone_xml_rpc_request_cbs_set_response(nativePtr, on_response);
#endif
			}
		}

		internal void register() {
			IntPtr listener = linphone_xml_rpc_request_cbs_get_user_data(nativePtr);
			if (listener == IntPtr.Zero)
			{
				GCHandle _handle = GCHandle.Alloc(this, GCHandleType.Normal);
				listener = GCHandle.ToIntPtr(_handle);
			} else
			{
				GCHandle _handle = GCHandle.FromIntPtr(listener);
				if (_handle.Target == this)
				{
					return;
				} else
				{
					_handle.Free();
					_handle = GCHandle.Alloc(this, GCHandleType.Normal);
					listener = GCHandle.ToIntPtr(_handle);
				}
			}
			linphone_xml_rpc_request_cbs_set_user_data(nativePtr, listener);
		}

		internal void unregister() {
			IntPtr listener = linphone_xml_rpc_request_cbs_get_user_data(nativePtr);
			linphone_xml_rpc_request_cbs_set_user_data(nativePtr, IntPtr.Zero);
			if (listener != IntPtr.Zero)
			{
				GCHandle.FromIntPtr(listener).Free();
			}
		}
	}

	#endregion

	#region Classes
	/// <summary>
	/// The <see cref="Linphone.AccountCreator" /> object used to configure an account
	/// on a server via XML-RPC. 
	/// </summary>
	public enum Operation_t
	{
		DC_LOGIN, //调度员登入
		DC_LOGOUT, //调度员注销
		DC_EXIT, //调度台退出
		PTT_DIAL, //发起组呼/PTT抢权
		PTT_RELEASE, //发起PTT放权
		PTT_HANGUP, //组呼业务强拆
		SIP_INIT, //Sip初始化
		P2P_DIALOUT, //点对点拨打
		P2P_RECV, //点对点接听
		P2P_HANGUP, //点对点挂断
		P2P_REJECT, //点对点拒绝
		P2P_VIDEO_DIAL,//点对点视频拨打
		P2P_VIDEO_RECV,//点对点视频接听
		P2P_VIDEO_HANGUP,//点对点视频挂断
		P2P_VIDEO_MONITOR,//视频回传
		P2P_VIDEO_DISPATCH_DELETE,//视频分发挂断
		P2P_VIDEO_DISPATCH,//视频分发
		P2P_VIDEO_STARTCIRCDISPLAY,//发起视频轮询
		P2P_VIDEO_STOPCIRCDISPLAY,//停止视频轮询
		GRP_SUB, //群组订阅
		GRP_UNSUB, //群组去订阅
		GRP_JOIN, //加入群组
		GRP_LEAVE, //退出群组
		GRP_BREAKOFF, //强拆组呼
		GRP_QUERY, //用户当前群组查询
		P2P_BREAKOFF, //强拆点呼
		P2P_BREAKIN, //点呼抢权
		DC_PZT_CONCTROL, //PZT控制
		VOL_MUTE, //静音控制
		VOL_UNMUTE, //关闭静音控制
		BATCH_CFG,   //系统启动批配置操作
		SDS_SEND,   //发送短数据
		SDS_EXPORT,   //短数据导出
		REC_START,   //开始录音\录像
		REC_STOP,    //停止录音\录像
		VWALL_START,   //启动视频上墙
		VWALL_STOP,   //停止上墙
		DL_START,   //开始Discreet Listen
		DL_STOP,    //停止DL
		GIS_SUBSCRIBE, //GIS订阅/去订阅
		DGNA_CREATE,    //创建动态重组
		DGNA_EDIT,      //修改动态重组
		DGNA_CANCEL,     //取消动态重组
		TIC_DIALOUT,    //Telephone-Interconnect-Call dialout
		TIC_HANGUP,      //Telephone-Interconnect-Call hangup
		TEMPGRP_CREATE,  //创建临时组
		TEMP_PTT,        //发起临时组呼
		P2P_VIDEO_REJECT,//视频呼叫拒接
		P2P_VIDEO_PREVIEW, //视频预览，使用bypass模式
		P2P_VIDEO_SWITCHBACK, //视频切换，恢复non-bypass模式
		P2P_ENVIRONMENT_LISTEN, //个呼环境监听
		P2P_VIDEO_RECV_PARA, //点对点视频接听,带参数(fmt,mute),
		TEMPGRP_DELETE,
		PCHGRP_CREATE,//创建派接组
		PCHGRP_CANCEL,//删除派接组
		P2P_TRANSFER,//通话转接
		TIC_DIAL,//外部电话呼叫
		PCHGRP_ADD, //增加派接组成员
		PCHGRP_DEL, //删除派接组成员
		PTT_EMERGENCY,//紧急组呼
		VOLUME_CONTROL,//音量调节     
	}

	[StructLayout(LayoutKind.Sequential)]
	public class AccountCreator : LinphoneObject
	{

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_account_creator_cbs(IntPtr factory);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_add_callbacks(IntPtr thiz, IntPtr cbs);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_remove_callbacks(IntPtr thiz, IntPtr cbs);

		~AccountCreator() 
		{
			if (listener != null)
			{
				linphone_account_creator_remove_callbacks(nativePtr, listener.nativePtr);
			}
		}

		private AccountCreatorListener listener;

		public AccountCreatorListener Listener
		{
			get {
				if (listener == null)
				{
					IntPtr nativeListener = linphone_factory_create_account_creator_cbs(IntPtr.Zero);
					listener = fromNativePtr<AccountCreatorListener>(nativeListener, false);
					linphone_account_creator_add_callbacks(nativePtr, nativeListener);
					listener.register();
				}
				return listener;
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_activation_code(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorActivationCodeStatus linphone_account_creator_set_activation_code(IntPtr thiz, string activationCode);

		/// <summary>
		/// Get the activation code. 
		/// </summary>
		public string ActivationCode
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_activation_code(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_account_creator_set_activation_code(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_algorithm(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorAlgoStatus linphone_account_creator_set_algorithm(IntPtr thiz, string algorithm);

		/// <summary>
		/// Get the algorithm configured in the account creator. 
		/// </summary>
		public string Algorithm
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_algorithm(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_account_creator_set_algorithm(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_set_as_default(IntPtr thiz, char setAsDefault);

		/// <summary>
		/// Set the set_as_default property. 
		/// </summary>
		public bool AsDefault
		{
			set
			{
				linphone_account_creator_set_as_default(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_current_callbacks(IntPtr thiz);

		/// <summary>
		/// Get the current LinphoneAccountCreatorCbs object associated with a
		/// LinphoneAccountCreator. 
		/// </summary>
		public Linphone.AccountCreatorListener CurrentCallbacks
		{
			get
			{
				IntPtr ptr = linphone_account_creator_get_current_callbacks(nativePtr);
				Linphone.AccountCreatorListener obj = fromNativePtr<Linphone.AccountCreatorListener>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_display_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorUsernameStatus linphone_account_creator_set_display_name(IntPtr thiz, string displayName);

		/// <summary>
		/// Get the display name. 
		/// </summary>
		public string DisplayName
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_display_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_account_creator_set_display_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_domain(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorDomainStatus linphone_account_creator_set_domain(IntPtr thiz, string domain);

		/// <summary>
		/// Get the domain. 
		/// </summary>
		public string Domain
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_domain(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_account_creator_set_domain(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_email(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorEmailStatus linphone_account_creator_set_email(IntPtr thiz, string email);

		/// <summary>
		/// Get the email. 
		/// </summary>
		public string Email
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_email(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_account_creator_set_email(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_ha1(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorPasswordStatus linphone_account_creator_set_ha1(IntPtr thiz, string ha1);

		/// <summary>
		/// Get the ha1. 
		/// </summary>
		public string Ha1
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_ha1(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_account_creator_set_ha1(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_language(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorLanguageStatus linphone_account_creator_set_language(IntPtr thiz, string lang);

		/// <summary>
		/// Get the language use in email of SMS. 
		/// </summary>
		public string Language
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_language(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_account_creator_set_language(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_password(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorPasswordStatus linphone_account_creator_set_password(IntPtr thiz, string password);

		/// <summary>
		/// Get the password. 
		/// </summary>
		public string Password
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_password(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_account_creator_set_password(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_phone_number(IntPtr thiz);

		/// <summary>
		/// Get the RFC 3966 normalized phone number. 
		/// </summary>
		public string PhoneNumber
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_phone_number(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_account_creator_set_proxy_config(IntPtr thiz, IntPtr cfg);

		/// <summary>
		/// Assign a proxy config pointer to the LinphoneAccountCreator. 
		/// </summary>
		public Linphone.ProxyConfig ProxyConfig
		{
			set
			{
				linphone_account_creator_set_proxy_config(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_route(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_set_route(IntPtr thiz, string route);

		/// <summary>
		/// Get the route. 
		/// </summary>
		public string Route
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_route(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_account_creator_set_route(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_account_creator_get_set_as_default(IntPtr thiz);

		/// <summary>
		/// Get the set_as_default property. 
		/// </summary>
		public bool SetAsDefault
		{
			get
			{
				return linphone_account_creator_get_set_as_default(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.TransportType linphone_account_creator_get_transport(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorTransportStatus linphone_account_creator_set_transport(IntPtr thiz, int transport);

		/// <summary>
		/// get Transport 
		/// </summary>
		public Linphone.TransportType Transport
		{
			get
			{
				return linphone_account_creator_get_transport(nativePtr);
			}
			set
			{
				linphone_account_creator_set_transport(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_get_username(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorUsernameStatus linphone_account_creator_set_username(IntPtr thiz, string username);

		/// <summary>
		/// Get the username. 
		/// </summary>
		public string Username
		{
			get
			{
				IntPtr stringPtr = linphone_account_creator_get_username(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_account_creator_set_username(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_activate_account(IntPtr thiz);

		/// <summary>
		/// Send a request to activate an account on server. 
		/// </summary>
		public Linphone.AccountCreatorStatus ActivateAccount()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_activate_account(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_activate_alias(IntPtr thiz);

		/// <summary>
		/// Send a request to activate an alias. 
		/// </summary>
		public Linphone.AccountCreatorStatus ActivateAlias()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_activate_alias(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_configure(IntPtr thiz);

		/// <summary>
		/// Configure an account (create a proxy config and authentication info for it). 
		/// </summary>
		public Linphone.ProxyConfig Configure()
		{
			IntPtr ptr = linphone_account_creator_configure(nativePtr);
			Linphone.ProxyConfig returnVal = fromNativePtr<Linphone.ProxyConfig>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_create_account(IntPtr thiz);

		/// <summary>
		/// Send a request to create an account on server. 
		/// </summary>
		public Linphone.AccountCreatorStatus CreateAccount()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_create_account(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_account_creator_create_proxy_config(IntPtr thiz);

		/// <summary>
		/// Create and configure a proxy config and a authentication info for an account
		/// creator. 
		/// </summary>
		public Linphone.ProxyConfig CreateProxyConfig()
		{
			IntPtr ptr = linphone_account_creator_create_proxy_config(nativePtr);
			Linphone.ProxyConfig returnVal = fromNativePtr<Linphone.ProxyConfig>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_is_account_activated(IntPtr thiz);

		/// <summary>
		/// Send a request to know if an account is activated on server. 
		/// </summary>
		public Linphone.AccountCreatorStatus IsAccountActivated()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_is_account_activated(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_is_account_exist(IntPtr thiz);

		/// <summary>
		/// Send a request to know the existence of account on server. 
		/// </summary>
		public Linphone.AccountCreatorStatus IsAccountExist()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_is_account_exist(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_is_account_linked(IntPtr thiz);

		/// <summary>
		/// Send a request to know if an account is linked. 
		/// </summary>
		public Linphone.AccountCreatorStatus IsAccountLinked()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_is_account_linked(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_is_alias_used(IntPtr thiz);

		/// <summary>
		/// Send a request to know if an alias is used. 
		/// </summary>
		public Linphone.AccountCreatorStatus IsAliasUsed()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_is_alias_used(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_link_account(IntPtr thiz);

		/// <summary>
		/// Send a request to link an account to an alias. 
		/// </summary>
		public Linphone.AccountCreatorStatus LinkAccount()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_link_account(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_login_linphone_account(IntPtr thiz);

		/// <summary>
		/// Send a request to get the password & algorithm of an account using the
		/// confirmation key. 
		/// </summary>
		public Linphone.AccountCreatorStatus LoginLinphoneAccount()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_login_linphone_account(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_recover_account(IntPtr thiz);

		/// <summary>
		/// Send a request to recover an account. 
		/// </summary>
		public Linphone.AccountCreatorStatus RecoverAccount()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_recover_account(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_account_creator_reset(IntPtr thiz);

		/// <summary>
		/// Reset the account creator entries like username, password, phone number... 
		/// </summary>
		public void Reset()
		{
			linphone_account_creator_reset(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_account_creator_set_phone_number(IntPtr thiz, string phoneNumber, string countryCode);

		/// <summary>
		/// Set the phone number normalized. 
		/// </summary>
		public uint SetPhoneNumber(string phoneNumber, string countryCode)
		{
			uint returnVal = linphone_account_creator_set_phone_number(nativePtr, phoneNumber, countryCode);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AccountCreatorStatus linphone_account_creator_update_account(IntPtr thiz);

		/// <summary>
		/// Send a request to update an account. 
		/// </summary>
		public Linphone.AccountCreatorStatus UpdateAccount()
		{
			Linphone.AccountCreatorStatus returnVal = linphone_account_creator_update_account(nativePtr);
			
			
			return returnVal;
		}
	}
	/// <summary>
	/// Object that represents a SIP address. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Address : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_get_display_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_address_set_display_name(IntPtr thiz, string displayName);

		/// <summary>
		/// Returns the display name. 
		/// </summary>
		public string DisplayName
		{
			get
			{
				IntPtr stringPtr = linphone_address_get_display_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_address_set_display_name(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("DisplayName setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_get_domain(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_address_set_domain(IntPtr thiz, string domain);

		/// <summary>
		/// Returns the domain name. 
		/// </summary>
		public string Domain
		{
			get
			{
				IntPtr stringPtr = linphone_address_get_domain(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_address_set_domain(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Domain setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_address_is_sip(IntPtr thiz);

		/// <summary>
		/// returns true if address is a routable sip address 
		/// </summary>
		public bool IsSip
		{
			get
			{
				return linphone_address_is_sip(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_get_method_param(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_address_set_method_param(IntPtr thiz, string methodParam);

		/// <summary>
		/// Get the value of the method parameter. 
		/// </summary>
		public string MethodParam
		{
			get
			{
				IntPtr stringPtr = linphone_address_get_method_param(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_address_set_method_param(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_get_password(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_address_set_password(IntPtr thiz, string password);

		/// <summary>
		/// Get the password encoded in the address. 
		/// </summary>
		public string Password
		{
			get
			{
				IntPtr stringPtr = linphone_address_get_password(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_address_set_password(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_address_get_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_address_set_port(IntPtr thiz, int port);

		/// <summary>
		/// Get port number as an integer value, 0 if not present. 
		/// </summary>
		public int Port
		{
			get
			{
				return linphone_address_get_port(nativePtr);
			}
			set
			{
				int exception_result = linphone_address_set_port(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Port setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_get_scheme(IntPtr thiz);

		/// <summary>
		/// Returns the address scheme, normally "sip". 
		/// </summary>
		public string Scheme
		{
			get
			{
				IntPtr stringPtr = linphone_address_get_scheme(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_address_get_secure(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_address_set_secure(IntPtr thiz, char enabled);

		/// <summary>
		/// Returns true if address refers to a secure location (sips) 
		/// </summary>
		public bool Secure
		{
			get
			{
				return linphone_address_get_secure(nativePtr) != 0;
			}
			set
			{
				linphone_address_set_secure(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.TransportType linphone_address_get_transport(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_address_set_transport(IntPtr thiz, int transport);

		/// <summary>
		/// Get the transport. 
		/// </summary>
		public Linphone.TransportType Transport
		{
			get
			{
				return linphone_address_get_transport(nativePtr);
			}
			set
			{
				int exception_result = linphone_address_set_transport(nativePtr, (int)value);
				if (exception_result != 0) throw new LinphoneException("Transport setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_get_username(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_address_set_username(IntPtr thiz, string username);

		/// <summary>
		/// Returns the username. 
		/// </summary>
		public string Username
		{
			get
			{
				IntPtr stringPtr = linphone_address_get_username(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_address_set_username(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Username setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_as_string(IntPtr thiz);

		/// <summary>
		/// Returns the address as a string. 
		/// </summary>
		public string AsString()
		{
			IntPtr stringPtr = linphone_address_as_string(nativePtr);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_as_string_uri_only(IntPtr thiz);

		/// <summary>
		/// Returns the SIP uri only as a string, that is display name is removed. 
		/// </summary>
		public string AsStringUriOnly()
		{
			IntPtr stringPtr = linphone_address_as_string_uri_only(nativePtr);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_address_clean(IntPtr thiz);

		/// <summary>
		/// Removes address's tags and uri headers so that it is displayable to the user. 
		/// </summary>
		public void Clean()
		{
			linphone_address_clean(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_clone(IntPtr thiz);

		/// <summary>
		/// Clones a <see cref="Linphone.Address" /> object. 
		/// </summary>
		public Linphone.Address Clone()
		{
			IntPtr ptr = linphone_address_clone(nativePtr);
			Linphone.Address returnVal = fromNativePtr<Linphone.Address>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_address_equal(IntPtr thiz, IntPtr address2);

		/// <summary>
		/// Compare two <see cref="Linphone.Address" /> taking the tags and headers into
		/// account. 
		/// </summary>
		public bool Equal(Linphone.Address address2)
		{
			bool returnVal = linphone_address_equal(nativePtr, address2 != null ? address2.nativePtr : IntPtr.Zero) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_get_header(IntPtr thiz, string headerName);

		/// <summary>
		/// Get the header encoded in the address. 
		/// </summary>
		public string GetHeader(string headerName)
		{
			IntPtr stringPtr = linphone_address_get_header(nativePtr, headerName);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_get_param(IntPtr thiz, string paramName);

		/// <summary>
		/// Get the value of a parameter of the address. 
		/// </summary>
		public string GetParam(string paramName)
		{
			IntPtr stringPtr = linphone_address_get_param(nativePtr, paramName);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_address_get_uri_param(IntPtr thiz, string uriParamName);

		/// <summary>
		/// Get the value of a parameter of the URI of the address. 
		/// </summary>
		public string GetUriParam(string uriParamName)
		{
			IntPtr stringPtr = linphone_address_get_uri_param(nativePtr, uriParamName);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_address_has_param(IntPtr thiz, string paramName);

		/// <summary>
		/// Tell whether a parameter is present in the address. 
		/// </summary>
		public bool HasParam(string paramName)
		{
			bool returnVal = linphone_address_has_param(nativePtr, paramName) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_address_has_uri_param(IntPtr thiz, string uriParamName);

		/// <summary>
		/// Tell whether a parameter is present in the URI of the address. 
		/// </summary>
		public bool HasUriParam(string uriParamName)
		{
			bool returnVal = linphone_address_has_uri_param(nativePtr, uriParamName) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_address_remove_uri_param(IntPtr thiz, string uriParamName);

		/// <summary>
		/// Removes the value of a parameter of the URI of the address. 
		/// </summary>
		public void RemoveUriParam(string uriParamName)
		{
			linphone_address_remove_uri_param(nativePtr, uriParamName);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_address_set_header(IntPtr thiz, string headerName, string headerValue);

		/// <summary>
		/// Set a header into the address. 
		/// </summary>
		public void SetHeader(string headerName, string headerValue)
		{
			linphone_address_set_header(nativePtr, headerName, headerValue);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_address_set_param(IntPtr thiz, string paramName, string paramValue);

		/// <summary>
		/// Set the value of a parameter of the address. 
		/// </summary>
		public void SetParam(string paramName, string paramValue)
		{
			linphone_address_set_param(nativePtr, paramName, paramValue);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_address_set_uri_param(IntPtr thiz, string uriParamName, string uriParamValue);

		/// <summary>
		/// Set the value of a parameter of the URI of the address. 
		/// </summary>
		public void SetUriParam(string uriParamName, string uriParamValue)
		{
			linphone_address_set_uri_param(nativePtr, uriParamName, uriParamValue);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_address_weak_equal(IntPtr thiz, IntPtr address2);

		/// <summary>
		/// Compare two <see cref="Linphone.Address" /> ignoring tags and headers,
		/// basically just domain, username, and port. 
		/// </summary>
		public bool WeakEqual(Linphone.Address address2)
		{
			bool returnVal = linphone_address_weak_equal(nativePtr, address2 != null ? address2.nativePtr : IntPtr.Zero) == (char)0 ? false : true;
			
			return returnVal;
		}
	}
	/// <summary>
	/// Object holding audio device information. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class AudioDevice : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AudioDeviceCapabilities linphone_audio_device_get_capabilities(IntPtr thiz);

		/// <summary>
		/// Returns the capabilities of the device. 
		/// </summary>
		public Linphone.AudioDeviceCapabilities Capabilities
		{
			get
			{
				return linphone_audio_device_get_capabilities(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_audio_device_get_device_name(IntPtr thiz);

		/// <summary>
		/// Returns the name of the audio device. 
		/// </summary>
		public string DeviceName
		{
			get
			{
				IntPtr stringPtr = linphone_audio_device_get_device_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_audio_device_get_driver_name(IntPtr thiz);

		/// <summary>
		/// Returns the driver name used by the device. 
		/// </summary>
		public string DriverName
		{
			get
			{
				IntPtr stringPtr = linphone_audio_device_get_driver_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_audio_device_get_id(IntPtr thiz);

		/// <summary>
		/// Returns the id of the audio device. 
		/// </summary>
		public string Id
		{
			get
			{
				IntPtr stringPtr = linphone_audio_device_get_id(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AudioDeviceType linphone_audio_device_get_type(IntPtr thiz);

		/// <summary>
		/// Returns the type of the device. 
		/// </summary>
		public Linphone.AudioDeviceType Type
		{
			get
			{
				return linphone_audio_device_get_type(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_audio_device_has_capability(IntPtr thiz, int capability);

		/// <summary>
		/// Returns whether or not the audio device has the given capability. 
		/// </summary>
		public bool HasCapability(Linphone.AudioDeviceCapabilities capability)
		{
			bool returnVal = linphone_audio_device_has_capability(nativePtr, (int)capability) == (char)0 ? false : true;
			
			return returnVal;
		}
	}
	/// <summary>
	/// Object holding authentication information. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class AuthInfo : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_algorithm(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_algorithm(IntPtr thiz, string algorithm);

		/// <summary>
		/// Gets the algorithm. 
		/// </summary>
		public string Algorithm
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_algorithm(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_algorithm(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_domain(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_domain(IntPtr thiz, string domain);

		/// <summary>
		/// Gets the domain. 
		/// </summary>
		public string Domain
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_domain(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_domain(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_ha1(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_ha1(IntPtr thiz, string ha1);

		/// <summary>
		/// Gets the ha1. 
		/// </summary>
		public string Ha1
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_ha1(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_ha1(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_passwd(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_passwd(IntPtr thiz, string passwd);

		/// <summary>
		/// Gets the password. 
		/// </summary>
		public string Passwd
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_passwd(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_passwd(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_password(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_password(IntPtr thiz, string passwd);

		/// <summary>
		/// Gets the password. 
		/// </summary>
		public string Password
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_password(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_password(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_realm(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_realm(IntPtr thiz, string realm);

		/// <summary>
		/// Gets the realm. 
		/// </summary>
		public string Realm
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_realm(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_realm(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_tls_cert(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_tls_cert(IntPtr thiz, string tlsCert);

		/// <summary>
		/// Gets the TLS certificate. 
		/// </summary>
		public string TlsCert
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_tls_cert(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_tls_cert(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_tls_cert_path(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_tls_cert_path(IntPtr thiz, string tlsCertPath);

		/// <summary>
		/// Gets the TLS certificate path. 
		/// </summary>
		public string TlsCertPath
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_tls_cert_path(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_tls_cert_path(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_tls_key(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_tls_key(IntPtr thiz, string tlsKey);

		/// <summary>
		/// Gets the TLS key. 
		/// </summary>
		public string TlsKey
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_tls_key(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_tls_key(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_tls_key_path(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_tls_key_path(IntPtr thiz, string tlsKeyPath);

		/// <summary>
		/// Gets the TLS key path. 
		/// </summary>
		public string TlsKeyPath
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_tls_key_path(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_tls_key_path(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_userid(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_userid(IntPtr thiz, string userid);

		/// <summary>
		/// Gets the userid. 
		/// </summary>
		public string Userid
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_userid(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_userid(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_get_username(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_auth_info_set_username(IntPtr thiz, string username);

		/// <summary>
		/// Gets the username. 
		/// </summary>
		public string Username
		{
			get
			{
				IntPtr stringPtr = linphone_auth_info_get_username(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_auth_info_set_username(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_clone(IntPtr thiz);

		/// <summary>
		/// Instantiates a new auth info with values from source. 
		/// </summary>
		public Linphone.AuthInfo Clone()
		{
			IntPtr ptr = linphone_auth_info_clone(nativePtr);
			Linphone.AuthInfo returnVal = fromNativePtr<Linphone.AuthInfo>(ptr, true);
			
			return returnVal;
		}
	}
	/// <summary>
	/// The <see cref="Linphone.Content" /> object representing a data buffer. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Buffer : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_buffer_new_from_data(uint data, long size);

		/// <summary>
		/// Create a new <see cref="Linphone.Buffer" /> object from existing data. 
		/// </summary>
		public static Linphone.Buffer NewFromData(uint data, long size)
		{
			IntPtr ptr = linphone_buffer_new_from_data(data, size);
			Linphone.Buffer returnVal = fromNativePtr<Linphone.Buffer>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_buffer_new_from_string(string data);

		/// <summary>
		/// Create a new <see cref="Linphone.Buffer" /> object from a string. 
		/// </summary>
		public static Linphone.Buffer NewFromString(string data)
		{
			IntPtr ptr = linphone_buffer_new_from_string(data);
			Linphone.Buffer returnVal = fromNativePtr<Linphone.Buffer>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_buffer_get_content(IntPtr thiz);

		/// <summary>
		/// Get the content of the data buffer. 
		/// </summary>
		public uint Content
		{
			get
			{
				return linphone_buffer_get_content(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_buffer_is_empty(IntPtr thiz);

		/// <summary>
		/// Tell whether the <see cref="Linphone.Buffer" /> is empty. 
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				return linphone_buffer_is_empty(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_buffer_get_size(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_buffer_set_size(IntPtr thiz, long size);

		/// <summary>
		/// Get the size of the content of the data buffer. 
		/// </summary>
		public long Size
		{
			get
			{
				return linphone_buffer_get_size(nativePtr);
			}
			set
			{
				linphone_buffer_set_size(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_buffer_get_string_content(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_buffer_set_string_content(IntPtr thiz, string content);

		/// <summary>
		/// Get the string content of the data buffer. 
		/// </summary>
		public string StringContent
		{
			get
			{
				IntPtr stringPtr = linphone_buffer_get_string_content(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_buffer_set_string_content(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_buffer_set_content(IntPtr thiz, uint content, long size);

		/// <summary>
		/// Set the content of the data buffer. 
		/// </summary>
		public void SetContent(uint content, long size)
		{
			linphone_buffer_set_content(nativePtr, content, size);
			
			
			
		}
	}
	/// <summary>
	/// The <see cref="Linphone.Call" /> object represents a call issued or received by
	/// the <see cref="Linphone.Core" />. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Call : LinphoneObject
	{
		/// Get the native window handle of the video window, casted as an unsigned long.
		public string NativeVideoWindowIdString
		{
			get
			{
				return Marshal.PtrToStringUni(linphone_call_get_native_video_window_id(nativePtr));
			}
			set
			{
				IntPtr string_ptr = Marshal.StringToHGlobalUni(value);
				linphone_call_set_native_video_window_id(nativePtr, string_ptr);
				Marshal.FreeHGlobal(string_ptr);
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_call_cbs(IntPtr factory);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_add_callbacks(IntPtr thiz, IntPtr cbs);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_remove_callbacks(IntPtr thiz, IntPtr cbs);

		~Call() 
		{
			if (listener != null)
			{
				linphone_call_remove_callbacks(nativePtr, listener.nativePtr);
			}
		}

		private CallListener listener;

		public CallListener Listener
		{
			get {
				if (listener == null)
				{
					IntPtr nativeListener = linphone_factory_create_call_cbs(IntPtr.Zero);
					listener = fromNativePtr<CallListener>(nativeListener, false);
					linphone_call_add_callbacks(nativePtr, nativeListener);
					listener.register();
				}
				return listener;
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_authentication_token(IntPtr thiz);

		/// <summary>
		/// Returns the ZRTP authentication token to verify. 
		/// </summary>
		public string AuthenticationToken
		{
			get
			{
				IntPtr stringPtr = linphone_call_get_authentication_token(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_get_authentication_token_verified(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_set_authentication_token_verified(IntPtr thiz, char verified);

		/// <summary>
		/// Returns whether ZRTP authentication token is verified. 
		/// </summary>
		public bool AuthenticationTokenVerified
		{
			get
			{
				return linphone_call_get_authentication_token_verified(nativePtr) != 0;
			}
			set
			{
				linphone_call_set_authentication_token_verified(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_get_average_quality(IntPtr thiz);

		/// <summary>
		/// Returns call quality averaged over all the duration of the call. 
		/// </summary>
		public float AverageQuality
		{
			get
			{
				return linphone_call_get_average_quality(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_call_log(IntPtr thiz);

		/// <summary>
		/// Gets the call log associated to this call. 
		/// </summary>
		public Linphone.CallLog CallLog
		{
			get
			{
				IntPtr ptr = linphone_call_get_call_log(nativePtr);
				Linphone.CallLog obj = fromNativePtr<Linphone.CallLog>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_camera_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_enable_camera(IntPtr thiz, char enabled);

		/// <summary>
		/// Returns true if camera pictures are allowed to be sent to the remote party. 
		/// </summary>
		public bool CameraEnabled
		{
			get
			{
				return linphone_call_camera_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_call_enable_camera(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_chat_room(IntPtr thiz);

		/// <summary>
		/// Create a new chat room for messaging from a call if not already existing, else
		/// return existing one. 
		/// </summary>
		public Linphone.ChatRoom ChatRoom
		{
			get
			{
				IntPtr ptr = linphone_call_get_chat_room(nativePtr);
				Linphone.ChatRoom obj = fromNativePtr<Linphone.ChatRoom>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_conference(IntPtr thiz);

		/// <summary>
		/// Return the associated conference object. 
		/// </summary>
		public Linphone.Conference Conference
		{
			get
			{
				IntPtr ptr = linphone_call_get_conference(nativePtr);
				Linphone.Conference obj = fromNativePtr<Linphone.Conference>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_core(IntPtr thiz);

		/// <summary>
		/// Get the core that has created the specified call. 
		/// </summary>
		public Linphone.Core Core
		{
			get
			{
				IntPtr ptr = linphone_call_get_core(nativePtr);
				Linphone.Core obj = fromNativePtr<Linphone.Core>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_current_callbacks(IntPtr thiz);

		/// <summary>
		/// Gets the current LinphoneCallCbs. 
		/// </summary>
		public Linphone.CallListener CurrentCallbacks
		{
			get
			{
				IntPtr ptr = linphone_call_get_current_callbacks(nativePtr);
				Linphone.CallListener obj = fromNativePtr<Linphone.CallListener>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_current_params(IntPtr thiz);

		/// <summary>
		/// Returns current parameters associated to the call. 
		/// </summary>
		public Linphone.CallParams CurrentParams
		{
			get
			{
				IntPtr ptr = linphone_call_get_current_params(nativePtr);
				Linphone.CallParams obj = fromNativePtr<Linphone.CallParams>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_get_current_quality(IntPtr thiz);

		/// <summary>
		/// Obtain real-time quality rating of the call. 
		/// </summary>
		public float CurrentQuality
		{
			get
			{
				return linphone_call_get_current_quality(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.CallDir linphone_call_get_dir(IntPtr thiz);

		/// <summary>
		/// Returns direction of the call (incoming or outgoing). 
		/// </summary>
		public Linphone.CallDir Dir
		{
			get
			{
				return linphone_call_get_dir(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_diversion_address(IntPtr thiz);

		/// <summary>
		/// Returns the diversion address associated to this call. 
		/// </summary>
		public Linphone.Address DiversionAddress
		{
			get
			{
				IntPtr ptr = linphone_call_get_diversion_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_get_duration(IntPtr thiz);

		/// <summary>
		/// Returns call's duration in seconds. 
		/// </summary>
		public int Duration
		{
			get
			{
				return linphone_call_get_duration(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_echo_cancellation_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_enable_echo_cancellation(IntPtr thiz, char val);

		/// <summary>
		/// Returns true if echo cancellation is enabled. 
		/// </summary>
		public bool EchoCancellationEnabled
		{
			get
			{
				return linphone_call_echo_cancellation_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_call_enable_echo_cancellation(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_echo_limiter_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_enable_echo_limiter(IntPtr thiz, char val);

		/// <summary>
		/// Returns true if echo limiter is enabled. 
		/// </summary>
		public bool EchoLimiterEnabled
		{
			get
			{
				return linphone_call_echo_limiter_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_call_enable_echo_limiter(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_error_info(IntPtr thiz);

		/// <summary>
		/// Returns full details about call errors or termination reasons. 
		/// </summary>
		public Linphone.ErrorInfo ErrorInfo
		{
			get
			{
				IntPtr ptr = linphone_call_get_error_info(nativePtr);
				Linphone.ErrorInfo obj = fromNativePtr<Linphone.ErrorInfo>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_input_audio_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_set_input_audio_device(IntPtr thiz, IntPtr audioDevice);

		/// <summary>
		/// Gets the current input device for this call. 
		/// </summary>
		public Linphone.AudioDevice InputAudioDevice
		{
			get
			{
				IntPtr ptr = linphone_call_get_input_audio_device(nativePtr);
				Linphone.AudioDevice obj = fromNativePtr<Linphone.AudioDevice>(ptr, true);
				return obj;
			}
			set
			{
				linphone_call_set_input_audio_device(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_is_recording(IntPtr thiz);

		/// <summary>
		/// Returns whether or not the call is currently being recorded. 
		/// </summary>
		public bool IsRecording
		{
			get
			{
				return linphone_call_is_recording(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_get_microphone_muted(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_set_microphone_muted(IntPtr thiz, char muted);

		/// <summary>
		/// Get microphone muted state. 
		/// </summary>
		public bool MicrophoneMuted
		{
			get
			{
				return linphone_call_get_microphone_muted(nativePtr) != 0;
			}
			set
			{
				linphone_call_set_microphone_muted(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_get_microphone_volume_gain(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_set_microphone_volume_gain(IntPtr thiz, float volume);

		/// <summary>
		/// Get microphone volume gain. 
		/// </summary>
		public float MicrophoneVolumeGain
		{
			get
			{
				return linphone_call_get_microphone_volume_gain(nativePtr);
			}
			set
			{
				linphone_call_set_microphone_volume_gain(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_native_video_window_id(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_set_native_video_window_id(IntPtr thiz, IntPtr id);

		/// <summary>
		/// Get the native window handle of the video window, casted as an unsigned long. 
		/// </summary>
		public IntPtr NativeVideoWindowId
		{
			get
			{
				return linphone_call_get_native_video_window_id(nativePtr);
			}
			set
			{
				linphone_call_set_native_video_window_id(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_output_audio_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_set_output_audio_device(IntPtr thiz, IntPtr audioDevice);

		/// <summary>
		/// Gets the current output device for this call. 
		/// </summary>
		public Linphone.AudioDevice OutputAudioDevice
		{
			get
			{
				IntPtr ptr = linphone_call_get_output_audio_device(nativePtr);
				Linphone.AudioDevice obj = fromNativePtr<Linphone.AudioDevice>(ptr, true);
				return obj;
			}
			set
			{
				linphone_call_set_output_audio_device(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_params(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_set_params(IntPtr thiz, IntPtr parameters);

		/// <summary>
		/// Returns local parameters associated with the call. 
		/// </summary>
		public Linphone.CallParams Params
		{
			get
			{
				IntPtr ptr = linphone_call_get_params(nativePtr);
				Linphone.CallParams obj = fromNativePtr<Linphone.CallParams>(ptr, true);
				return obj;
			}
			set
			{
				linphone_call_set_params(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_get_play_volume(IntPtr thiz);

		/// <summary>
		/// Get the mesured playback volume level (received from remote) in dbm0. 
		/// </summary>
		public float PlayVolume
		{
			get
			{
				return linphone_call_get_play_volume(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_player(IntPtr thiz);

		/// <summary>
		/// Get a player associated with the call to play a local file and stream it to the
		/// remote peer. 
		/// </summary>
		public Linphone.Player Player
		{
			get
			{
				IntPtr ptr = linphone_call_get_player(nativePtr);
				Linphone.Player obj = fromNativePtr<Linphone.Player>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.Reason linphone_call_get_reason(IntPtr thiz);

		/// <summary>
		/// Returns the reason for a call termination (either error or normal termination) 
		/// </summary>
		public Linphone.Reason Reason
		{
			get
			{
				return linphone_call_get_reason(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_get_record_volume(IntPtr thiz);

		/// <summary>
		/// Get the mesured record volume level (sent to remote) in dbm0. 
		/// </summary>
		public float RecordVolume
		{
			get
			{
				return linphone_call_get_record_volume(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_refer_to(IntPtr thiz);

		/// <summary>
		/// Gets the refer-to uri (if the call was transfered). 
		/// </summary>
		public string ReferTo
		{
			get
			{
				IntPtr stringPtr = linphone_call_get_refer_to(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_remote_address(IntPtr thiz);

		/// <summary>
		/// Returns the remote address associated to this call. 
		/// </summary>
		public Linphone.Address RemoteAddress
		{
			get
			{
				IntPtr ptr = linphone_call_get_remote_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_remote_address_as_string(IntPtr thiz);

		/// <summary>
		/// Returns the remote address associated to this call as a string. 
		/// </summary>
		public string RemoteAddressAsString
		{
			get
			{
				IntPtr stringPtr = linphone_call_get_remote_address_as_string(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_remote_contact(IntPtr thiz);

		/// <summary>
		/// Returns the far end's sip contact as a string, if available. 
		/// </summary>
		public string RemoteContact
		{
			get
			{
				IntPtr stringPtr = linphone_call_get_remote_contact(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_remote_params(IntPtr thiz);

		/// <summary>
		/// Returns call parameters proposed by remote. 
		/// </summary>
		public Linphone.CallParams RemoteParams
		{
			get
			{
				IntPtr ptr = linphone_call_get_remote_params(nativePtr);
				Linphone.CallParams obj = fromNativePtr<Linphone.CallParams>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_remote_user_agent(IntPtr thiz);

		/// <summary>
		/// Returns the far end's user agent description string, if available. 
		/// </summary>
		public string RemoteUserAgent
		{
			get
			{
				IntPtr stringPtr = linphone_call_get_remote_user_agent(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_replaced_call(IntPtr thiz);

		/// <summary>
		/// Returns the call object this call is replacing, if any. 
		/// </summary>
		public Linphone.Call ReplacedCall
		{
			get
			{
				IntPtr ptr = linphone_call_get_replaced_call(nativePtr);
				Linphone.Call obj = fromNativePtr<Linphone.Call>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_get_speaker_muted(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_set_speaker_muted(IntPtr thiz, char muted);

		/// <summary>
		/// Get speaker muted state. 
		/// </summary>
		public bool SpeakerMuted
		{
			get
			{
				return linphone_call_get_speaker_muted(nativePtr) != 0;
			}
			set
			{
				linphone_call_set_speaker_muted(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_get_speaker_volume_gain(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_set_speaker_volume_gain(IntPtr thiz, float volume);

		/// <summary>
		/// Get speaker volume gain. 
		/// </summary>
		public float SpeakerVolumeGain
		{
			get
			{
				return linphone_call_get_speaker_volume_gain(nativePtr);
			}
			set
			{
				linphone_call_set_speaker_volume_gain(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.CallState linphone_call_get_state(IntPtr thiz);

		/// <summary>
		/// Retrieves the call's current state. 
		/// </summary>
		public Linphone.CallState State
		{
			get
			{
				return linphone_call_get_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_get_stream_count(IntPtr thiz);

		/// <summary>
		/// Returns the number of stream for the given call. 
		/// </summary>
		public int StreamCount
		{
			get
			{
				return linphone_call_get_stream_count(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_to_address(IntPtr thiz);

		/// <summary>
		/// Returns the to address with its headers associated to this call. 
		/// </summary>
		public Linphone.Address ToAddress
		{
			get
			{
				IntPtr ptr = linphone_call_get_to_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.CallState linphone_call_get_transfer_state(IntPtr thiz);

		/// <summary>
		/// Returns the current transfer state, if a transfer has been initiated from this
		/// call. 
		/// </summary>
		public Linphone.CallState TransferState
		{
			get
			{
				return linphone_call_get_transfer_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_transfer_target_call(IntPtr thiz);

		/// <summary>
		/// When this call has received a transfer request, returns the new call that was
		/// automatically created as a result of the transfer. 
		/// </summary>
		public Linphone.Call TransferTargetCall
		{
			get
			{
				IntPtr ptr = linphone_call_get_transfer_target_call(nativePtr);
				Linphone.Call obj = fromNativePtr<Linphone.Call>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_transferer_call(IntPtr thiz);

		/// <summary>
		/// Gets the transferer if this call was started automatically as a result of an
		/// incoming transfer request. 
		/// </summary>
		public Linphone.Call TransfererCall
		{
			get
			{
				IntPtr ptr = linphone_call_get_transferer_call(nativePtr);
				Linphone.Call obj = fromNativePtr<Linphone.Call>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_accept(IntPtr thiz);

		/// <summary>
		/// 接听
		/// Accept an incoming call. 
		/// </summary>
		public void Accept()
		{
			int exception_result = linphone_call_accept(nativePtr);
			if (exception_result != 0) throw new LinphoneException("Accept returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_accept_early_media(IntPtr thiz);

		/// <summary>
		/// Accept an early media session for an incoming call. 
		/// </summary>
		public void AcceptEarlyMedia()
		{
			int exception_result = linphone_call_accept_early_media(nativePtr);
			if (exception_result != 0) throw new LinphoneException("AcceptEarlyMedia returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_accept_early_media_with_params(IntPtr thiz, IntPtr parameters);

		/// <summary>
		/// When receiving an incoming, accept to start a media session as early-media. 
		/// </summary>
		public void AcceptEarlyMediaWithParams(Linphone.CallParams parameters)
		{
			int exception_result = linphone_call_accept_early_media_with_params(nativePtr, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AcceptEarlyMediaWithParams returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_accept_update(IntPtr thiz, IntPtr parameters);

		/// <summary>
		/// Accept call modifications initiated by other end. 
		/// </summary>
		public void AcceptUpdate(Linphone.CallParams parameters)
		{
			int exception_result = linphone_call_accept_update(nativePtr, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AcceptUpdate returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_accept_with_params(IntPtr thiz, IntPtr parameters);

		/// <summary>
		/// Accept an incoming call, with parameters. 
		/// </summary>
		public void AcceptWithParams(Linphone.CallParams parameters)
		{
			int exception_result = linphone_call_accept_with_params(nativePtr, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AcceptWithParams returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_asked_to_autoanswer(IntPtr thiz);

		/// <summary>
		/// Tell whether a call has been asked to autoanswer. 
		/// </summary>
		public bool AskedToAutoanswer()
		{
			bool returnVal = linphone_call_asked_to_autoanswer(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_cancel_dtmfs(IntPtr thiz);

		/// <summary>
		/// Stop current DTMF sequence sending. 
		/// </summary>
		public void CancelDtmfs()
		{
			linphone_call_cancel_dtmfs(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_decline(IntPtr thiz, int reason);

		/// <summary>
		/// Decline a pending incoming call, with a reason. 
		/// </summary>
		public void Decline(Linphone.Reason reason)
		{
			int exception_result = linphone_call_decline(nativePtr, (int)reason);
			if (exception_result != 0) throw new LinphoneException("Decline returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_decline_with_error_info(IntPtr thiz, IntPtr ei);

		/// <summary>
		/// Decline a pending incoming call, with a <see cref="Linphone.ErrorInfo" />
		/// object. 
		/// </summary>
		public int DeclineWithErrorInfo(Linphone.ErrorInfo ei)
		{
			int returnVal = linphone_call_decline_with_error_info(nativePtr, ei != null ? ei.nativePtr : IntPtr.Zero);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_defer_update(IntPtr thiz);

		/// <summary>
		/// When receiving a #LinphoneCallUpdatedByRemote state notification, prevent <see
		/// cref="Linphone.Core" /> from performing an automatic answer. 
		/// </summary>
		public void DeferUpdate()
		{
			int exception_result = linphone_call_defer_update(nativePtr);
			if (exception_result != 0) throw new LinphoneException("DeferUpdate returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_stats(IntPtr thiz, int type);

		/// <summary>
		/// Return a copy of the call statistics for a particular stream type. 
		/// </summary>
		public Linphone.CallStats GetStats(Linphone.StreamType type)
		{
			IntPtr ptr = linphone_call_get_stats(nativePtr, (int)type);
			Linphone.CallStats returnVal = fromNativePtr<Linphone.CallStats>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_get_to_header(IntPtr thiz, string name);

		/// <summary>
		/// Returns the value of the header name. 
		/// </summary>
		public string GetToHeader(string name)
		{
			IntPtr stringPtr = linphone_call_get_to_header(nativePtr, name);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_has_transfer_pending(IntPtr thiz);

		/// <summary>
		/// Returns true if this calls has received a transfer that has not been executed
		/// yet. 
		/// </summary>
		public bool HasTransferPending()
		{
			bool returnVal = linphone_call_has_transfer_pending(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_media_in_progress(IntPtr thiz);

		/// <summary>
		/// Indicates whether an operation is in progress at the media side. 
		/// </summary>
		public bool MediaInProgress()
		{
			bool returnVal = linphone_call_media_in_progress(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_ogl_render(IntPtr thiz);

		/// <summary>
		/// Call generic OpenGL render for a given call. 
		/// </summary>
		public void OglRender()
		{
			linphone_call_ogl_render(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_pause(IntPtr thiz);

		/// <summary>
		/// Pauses the call. 
		/// </summary>
		public void Pause()
		{
			int exception_result = linphone_call_pause(nativePtr);
			if (exception_result != 0) throw new LinphoneException("Pause returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_redirect(IntPtr thiz, string redirectUri);

		/// <summary>
		/// Redirect the specified call to the given redirect URI. 
		/// </summary>
		public void Redirect(string redirectUri)
		{
			int exception_result = linphone_call_redirect(nativePtr, redirectUri);
			if (exception_result != 0) throw new LinphoneException("Redirect returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_request_notify_next_video_frame_decoded(IntPtr thiz);

		/// <summary>
		/// Request the callback passed to linphone_call_cbs_set_next_video_frame_decoded
		/// to be called the next time the video decoder properly decodes a video frame. 
		/// </summary>
		public void RequestNotifyNextVideoFrameDecoded()
		{
			linphone_call_request_notify_next_video_frame_decoded(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_resume(IntPtr thiz);

		/// <summary>
		/// Resumes a call. 
		/// </summary>
		public void Resume()
		{
			int exception_result = linphone_call_resume(nativePtr);
			if (exception_result != 0) throw new LinphoneException("Resume returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_send_dtmf(IntPtr thiz, sbyte dtmf);

		/// <summary>
		/// Send the specified dtmf. 
		/// </summary>
		public void SendDtmf(sbyte dtmf)
		{
			int exception_result = linphone_call_send_dtmf(nativePtr, dtmf);
			if (exception_result != 0) throw new LinphoneException("SendDtmf returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_send_dtmfs(IntPtr thiz, string dtmfs);

		/// <summary>
		/// Send a list of dtmf. 
		/// </summary>
		public void SendDtmfs(string dtmfs)
		{
			int exception_result = linphone_call_send_dtmfs(nativePtr, dtmfs);
			if (exception_result != 0) throw new LinphoneException("SendDtmfs returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_send_info_message(IntPtr thiz, IntPtr info);

		/// <summary>
		/// Send a <see cref="Linphone.InfoMessage" /> through an established call. 
		/// </summary>
		public void SendInfoMessage(Linphone.InfoMessage info)
		{
			int exception_result = linphone_call_send_info_message(nativePtr, info != null ? info.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("SendInfoMessage returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_send_vfu_request(IntPtr thiz);

		/// <summary>
		/// Request remote side to send us a Video Fast Update. 
		/// </summary>
		public void SendVfuRequest()
		{
			linphone_call_send_vfu_request(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_start_recording(IntPtr thiz);

		/// <summary>
		/// Start call recording. 
		/// </summary>
		public void StartRecording()
		{
			linphone_call_start_recording(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_stop_recording(IntPtr thiz);

		/// <summary>
		/// Stop call recording. 
		/// </summary>
		public void StopRecording()
		{
			linphone_call_stop_recording(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_take_preview_snapshot(IntPtr thiz, string file);

		/// <summary>
		/// Take a photo of currently captured video and write it into a jpeg file. 
		/// </summary>
		public void TakePreviewSnapshot(string file)
		{
			int exception_result = linphone_call_take_preview_snapshot(nativePtr, file);
			if (exception_result != 0) throw new LinphoneException("TakePreviewSnapshot returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_take_video_snapshot(IntPtr thiz, string file);

		/// <summary>
		/// Take a photo of currently received video and write it into a jpeg file. 
		/// </summary>
		public void TakeVideoSnapshot(string file)
		{
			int exception_result = linphone_call_take_video_snapshot(nativePtr, file);
			if (exception_result != 0) throw new LinphoneException("TakeVideoSnapshot returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_terminate(IntPtr thiz);

		/// <summary>
		/// Terminates a call. 
		/// </summary>
		public void Terminate()
		{
			int exception_result = linphone_call_terminate(nativePtr);
			if (exception_result != 0) throw new LinphoneException("Terminate returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_terminate_with_error_info(IntPtr thiz, IntPtr ei);

		/// <summary>
		/// Terminates a call. 
		/// </summary>
		public void TerminateWithErrorInfo(Linphone.ErrorInfo ei)
		{
			int exception_result = linphone_call_terminate_with_error_info(nativePtr, ei != null ? ei.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("TerminateWithErrorInfo returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_transfer(IntPtr thiz, string referTo);

		/// <summary>
		/// Performs a simple call transfer to the specified destination. 
		/// </summary>
		public void Transfer(string referTo)
		{
			int exception_result = linphone_call_transfer(nativePtr, referTo);
			if (exception_result != 0) throw new LinphoneException("Transfer returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_transfer_to_another(IntPtr thiz, IntPtr dest);

		/// <summary>
		/// Transfers a call to destination of another running call. 
		/// </summary>
		public void TransferToAnother(Linphone.Call dest)
		{
			int exception_result = linphone_call_transfer_to_another(nativePtr, dest != null ? dest.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("TransferToAnother returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_update(IntPtr thiz, IntPtr parameters);

		/// <summary>
		/// Updates a running call according to supplied call parameters or parameters
		/// changed in the LinphoneCore. 
		/// </summary>
		public void Update(Linphone.CallParams parameters)
		{
			int exception_result = linphone_call_update(nativePtr, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("Update returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_zoom(IntPtr thiz, float zoomFactor, float cx, float cy);

		/// <summary>
		/// Perform a zoom of the video displayed during a call. 
		/// </summary>
		public void Zoom(float zoomFactor, float cx, float cy)
		{
			linphone_call_zoom(nativePtr, zoomFactor, cx, cy);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_zoom_video(IntPtr thiz, float zoomFactor, float cx, float cy);

		/// <summary>
		/// Perform a zoom of the video displayed during a call. 
		/// </summary>
		public void ZoomVideo(float zoomFactor, float cx, float cy)
		{
			linphone_call_zoom_video(nativePtr, zoomFactor, cx, cy);
			
			
			
		}
	}
	/// <summary>
	/// Structure representing a call log. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class CallLog : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_log_get_call_id(IntPtr thiz);

		/// <summary>
		/// Get the call ID used by the call. 
		/// </summary>
		public string CallId
		{
			get
			{
				IntPtr stringPtr = linphone_call_log_get_call_id(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.CallDir linphone_call_log_get_dir(IntPtr thiz);

		/// <summary>
		/// Get the direction of the call. 
		/// </summary>
		public Linphone.CallDir Dir
		{
			get
			{
				return linphone_call_log_get_dir(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_log_get_duration(IntPtr thiz);

		/// <summary>
		/// Get the duration of the call since connected. 
		/// </summary>
		public int Duration
		{
			get
			{
				return linphone_call_log_get_duration(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_log_get_error_info(IntPtr thiz);

		/// <summary>
		/// When the call was failed, return an object describing the failure. 
		/// </summary>
		public Linphone.ErrorInfo ErrorInfo
		{
			get
			{
				IntPtr ptr = linphone_call_log_get_error_info(nativePtr);
				Linphone.ErrorInfo obj = fromNativePtr<Linphone.ErrorInfo>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_log_get_from_address(IntPtr thiz);

		/// <summary>
		/// Get the origin address (ie from) of the call. 
		/// </summary>
		public Linphone.Address FromAddress
		{
			get
			{
				IntPtr ptr = linphone_call_log_get_from_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_log_get_local_address(IntPtr thiz);

		/// <summary>
		/// Get the local address (that is from or to depending on call direction) 
		/// </summary>
		public Linphone.Address LocalAddress
		{
			get
			{
				IntPtr ptr = linphone_call_log_get_local_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_log_get_quality(IntPtr thiz);

		/// <summary>
		/// Get the overall quality indication of the call. 
		/// </summary>
		public float Quality
		{
			get
			{
				return linphone_call_log_get_quality(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_log_get_ref_key(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_log_set_ref_key(IntPtr thiz, string refkey);

		/// <summary>
		/// Get the persistent reference key associated to the call log. 
		/// </summary>
		public string RefKey
		{
			get
			{
				IntPtr stringPtr = linphone_call_log_get_ref_key(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_call_log_set_ref_key(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_log_get_remote_address(IntPtr thiz);

		/// <summary>
		/// Get the remote address (that is from or to depending on call direction). 
		/// </summary>
		public Linphone.Address RemoteAddress
		{
			get
			{
				IntPtr ptr = linphone_call_log_get_remote_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_call_log_get_start_date(IntPtr thiz);

		/// <summary>
		/// Get the start date of the call. 
		/// </summary>
		public long StartDate
		{
			get
			{
				return linphone_call_log_get_start_date(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.CallStatus linphone_call_log_get_status(IntPtr thiz);

		/// <summary>
		/// Get the status of the call. 
		/// </summary>
		public Linphone.CallStatus Status
		{
			get
			{
				return linphone_call_log_get_status(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_log_get_to_address(IntPtr thiz);

		/// <summary>
		/// Get the destination address (ie to) of the call. 
		/// </summary>
		public Linphone.Address ToAddress
		{
			get
			{
				IntPtr ptr = linphone_call_log_get_to_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_log_video_enabled(IntPtr thiz);

		/// <summary>
		/// Tell whether video was enabled at the end of the call or not. 
		/// </summary>
		public bool VideoEnabled
		{
			get
			{
				return linphone_call_log_video_enabled(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_log_to_str(IntPtr thiz);

		/// <summary>
		/// Get a human readable string describing the call. 
		/// </summary>
		public string ToStr()
		{
			IntPtr stringPtr = linphone_call_log_to_str(nativePtr);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_log_was_conference(IntPtr thiz);

		/// <summary>
		/// Tells whether that call was a call to a conference server. 
		/// </summary>
		public bool WasConference()
		{
			bool returnVal = linphone_call_log_was_conference(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
	}
	/// <summary>
	/// The <see cref="Linphone.CallParams" /> is an object containing various call
	/// related parameters. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class CallParams : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_set_audio_bandwidth_limit(IntPtr thiz, int bw);

		/// <summary>
		/// Refine bandwidth settings for this call by setting a bandwidth limit for audio
		/// streams. 
		/// </summary>
		public int AudioBandwidthLimit
		{
			set
			{
				linphone_call_params_set_audio_bandwidth_limit(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.MediaDirection linphone_call_params_get_audio_direction(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_set_audio_direction(IntPtr thiz, int dir);

		/// <summary>
		/// Get the audio stream direction. 
		/// </summary>
		public Linphone.MediaDirection AudioDirection
		{
			get
			{
				return linphone_call_params_get_audio_direction(nativePtr);
			}
			set
			{
				linphone_call_params_set_audio_direction(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_params_audio_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_enable_audio(IntPtr thiz, char enabled);

		/// <summary>
		/// Tell whether audio is enabled or not. 
		/// </summary>
		public bool AudioEnabled
		{
			get
			{
				return linphone_call_params_audio_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_call_params_enable_audio(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_params_audio_multicast_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_enable_audio_multicast(IntPtr thiz, char yesno);

		/// <summary>
		/// Use to get multicast state of audio stream. 
		/// </summary>
		public bool AudioMulticastEnabled
		{
			get
			{
				return linphone_call_params_audio_multicast_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_call_params_enable_audio_multicast(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_custom_contents(IntPtr thiz);

		/// <summary>
		/// Gets a list of <see cref="Linphone.Content" /> set if exists. 
		/// </summary>
		public IEnumerable<Linphone.Content> CustomContents
		{
			get
			{
				return MarshalBctbxList<Linphone.Content>(linphone_call_params_get_custom_contents(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_params_early_media_sending_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_enable_early_media_sending(IntPtr thiz, char enabled);

		/// <summary>
		/// Indicate whether sending of early media was enabled. 
		/// </summary>
		public bool EarlyMediaSendingEnabled
		{
			get
			{
				return linphone_call_params_early_media_sending_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_call_params_enable_early_media_sending(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_params_get_local_conference_mode(IntPtr thiz);

		/// <summary>
		/// Tell whether the call is part of the locally managed conference. 
		/// </summary>
		public bool LocalConferenceMode
		{
			get
			{
				return linphone_call_params_get_local_conference_mode(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_params_low_bandwidth_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_enable_low_bandwidth(IntPtr thiz, char enabled);

		/// <summary>
		/// Tell whether the call has been configured in low bandwidth mode or not. 
		/// </summary>
		public bool LowBandwidthEnabled
		{
			get
			{
				return linphone_call_params_low_bandwidth_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_call_params_enable_low_bandwidth(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.MediaEncryption linphone_call_params_get_media_encryption(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_set_media_encryption(IntPtr thiz, int enc);

		/// <summary>
		/// Get the kind of media encryption selected for the call. 
		/// </summary>
		public Linphone.MediaEncryption MediaEncryption
		{
			get
			{
				return linphone_call_params_get_media_encryption(nativePtr);
			}
			set
			{
				linphone_call_params_set_media_encryption(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_call_params_get_privacy(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_set_privacy(IntPtr thiz, uint privacy);

		/// <summary>
		/// Get requested level of privacy for the call. 
		/// </summary>
		public uint Privacy
		{
			get
			{
				return linphone_call_params_get_privacy(nativePtr);
			}
			set
			{
				linphone_call_params_set_privacy(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_params_realtime_text_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_call_params_enable_realtime_text(IntPtr thiz, char yesno);

		/// <summary>
		/// Use to get real time text following rfc4103. 
		/// </summary>
		public bool RealtimeTextEnabled
		{
			get
			{
				return linphone_call_params_realtime_text_enabled(nativePtr) != 0;
			}
			set
			{
				int exception_result = linphone_call_params_enable_realtime_text(nativePtr, value ? (char)1 : (char)0);
				if (exception_result != 0) throw new LinphoneException("RealtimeTextEnabled setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_call_params_get_realtime_text_keepalive_interval(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_set_realtime_text_keepalive_interval(IntPtr thiz, uint interval);

		/// <summary>
		/// Use to get keep alive interval of real time text following rfc4103. 
		/// </summary>
		public uint RealtimeTextKeepaliveInterval
		{
			get
			{
				return linphone_call_params_get_realtime_text_keepalive_interval(nativePtr);
			}
			set
			{
				linphone_call_params_set_realtime_text_keepalive_interval(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_params_get_received_framerate(IntPtr thiz);

		/// <summary>
		/// Get the framerate of the video that is received. 
		/// </summary>
		public float ReceivedFramerate
		{
			get
			{
				return linphone_call_params_get_received_framerate(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_received_video_definition(IntPtr thiz);

		/// <summary>
		/// Get the definition of the received video. 
		/// </summary>
		public Linphone.VideoDefinition ReceivedVideoDefinition
		{
			get
			{
				IntPtr ptr = linphone_call_params_get_received_video_definition(nativePtr);
				Linphone.VideoDefinition obj = fromNativePtr<Linphone.VideoDefinition>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_record_file(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_set_record_file(IntPtr thiz, string path);

		/// <summary>
		/// Get the path for the audio recording of the call. 
		/// </summary>
		public string RecordFile
		{
			get
			{
				IntPtr stringPtr = linphone_call_params_get_record_file(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_call_params_set_record_file(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_params_rtp_bundle_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_enable_rtp_bundle(IntPtr thiz, char val);

		/// <summary>
		/// Indicates whether RTP bundle mode (also known as Media Multiplexing) is
		/// enabled. 
		/// </summary>
		public bool RtpBundleEnabled
		{
			get
			{
				return linphone_call_params_rtp_bundle_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_call_params_enable_rtp_bundle(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_rtp_profile(IntPtr thiz);

		/// <summary>
		/// Get the RTP profile being used. 
		/// </summary>
		public string RtpProfile
		{
			get
			{
				IntPtr stringPtr = linphone_call_params_get_rtp_profile(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_params_get_sent_framerate(IntPtr thiz);

		/// <summary>
		/// Get the framerate of the video that is sent. 
		/// </summary>
		public float SentFramerate
		{
			get
			{
				return linphone_call_params_get_sent_framerate(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_sent_video_definition(IntPtr thiz);

		/// <summary>
		/// Get the definition of the sent video. 
		/// </summary>
		public Linphone.VideoDefinition SentVideoDefinition
		{
			get
			{
				IntPtr ptr = linphone_call_params_get_sent_video_definition(nativePtr);
				Linphone.VideoDefinition obj = fromNativePtr<Linphone.VideoDefinition>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_session_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_set_session_name(IntPtr thiz, string name);

		/// <summary>
		/// Get the session name of the media session (ie in SDP). 
		/// </summary>
		public string SessionName
		{
			get
			{
				IntPtr stringPtr = linphone_call_params_get_session_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_call_params_set_session_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_used_audio_payload_type(IntPtr thiz);

		/// <summary>
		/// Get the audio payload type that has been selected by a call. 
		/// </summary>
		public Linphone.PayloadType UsedAudioPayloadType
		{
			get
			{
				IntPtr ptr = linphone_call_params_get_used_audio_payload_type(nativePtr);
				Linphone.PayloadType obj = fromNativePtr<Linphone.PayloadType>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_used_text_payload_type(IntPtr thiz);

		/// <summary>
		/// Get the text payload type that has been selected by a call. 
		/// </summary>
		public Linphone.PayloadType UsedTextPayloadType
		{
			get
			{
				IntPtr ptr = linphone_call_params_get_used_text_payload_type(nativePtr);
				Linphone.PayloadType obj = fromNativePtr<Linphone.PayloadType>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_used_video_payload_type(IntPtr thiz);

		/// <summary>
		/// Get the video payload type that has been selected by a call. 
		/// </summary>
		public Linphone.PayloadType UsedVideoPayloadType
		{
			get
			{
				IntPtr ptr = linphone_call_params_get_used_video_payload_type(nativePtr);
				Linphone.PayloadType obj = fromNativePtr<Linphone.PayloadType>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.MediaDirection linphone_call_params_get_video_direction(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_set_video_direction(IntPtr thiz, int dir);

		/// <summary>
		/// Get the video stream direction. 
		/// </summary>
		public Linphone.MediaDirection VideoDirection
		{
			get
			{
				return linphone_call_params_get_video_direction(nativePtr);
			}
			set
			{
				linphone_call_params_set_video_direction(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_params_video_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_enable_video(IntPtr thiz, char enabled);

		/// <summary>
		/// Tell whether video is enabled or not. 
		/// </summary>
		public bool VideoEnabled
		{
			get
			{
				return linphone_call_params_video_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_call_params_enable_video(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_call_params_video_multicast_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_enable_video_multicast(IntPtr thiz, char yesno);

		/// <summary>
		/// Use to get multicast state of video stream. 
		/// </summary>
		public bool VideoMulticastEnabled
		{
			get
			{
				return linphone_call_params_video_multicast_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_call_params_enable_video_multicast(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_add_custom_content(IntPtr thiz, IntPtr content);

		/// <summary>
		/// Adds a <see cref="Linphone.Content" /> to be added to the INVITE SDP. 
		/// </summary>
		public void AddCustomContent(Linphone.Content content)
		{
			linphone_call_params_add_custom_content(nativePtr, content != null ? content.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_add_custom_header(IntPtr thiz, string headerName, string headerValue);

		/// <summary>
		/// Add a custom SIP header in the INVITE for a call. 
		/// </summary>
		public void AddCustomHeader(string headerName, string headerValue)
		{
			linphone_call_params_add_custom_header(nativePtr, headerName, headerValue);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_add_custom_sdp_attribute(IntPtr thiz, string attributeName, string attributeValue);

		/// <summary>
		/// Add a custom attribute related to all the streams in the SDP exchanged within
		/// SIP messages during a call. 
		/// </summary>
		public void AddCustomSdpAttribute(string attributeName, string attributeValue)
		{
			linphone_call_params_add_custom_sdp_attribute(nativePtr, attributeName, attributeValue);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_add_custom_sdp_media_attribute(IntPtr thiz, int type, string attributeName, string attributeValue);

		/// <summary>
		/// Add a custom attribute related to a specific stream in the SDP exchanged within
		/// SIP messages during a call. 
		/// </summary>
		public void AddCustomSdpMediaAttribute(Linphone.StreamType type, string attributeName, string attributeValue)
		{
			linphone_call_params_add_custom_sdp_media_attribute(nativePtr, (int)type, attributeName, attributeValue);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_clear_custom_sdp_attributes(IntPtr thiz);

		/// <summary>
		/// Clear the custom SDP attributes related to all the streams in the SDP exchanged
		/// within SIP messages during a call. 
		/// </summary>
		public void ClearCustomSdpAttributes()
		{
			linphone_call_params_clear_custom_sdp_attributes(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_call_params_clear_custom_sdp_media_attributes(IntPtr thiz, int type);

		/// <summary>
		/// Clear the custom SDP attributes related to a specific stream in the SDP
		/// exchanged within SIP messages during a call. 
		/// </summary>
		public void ClearCustomSdpMediaAttributes(Linphone.StreamType type)
		{
			linphone_call_params_clear_custom_sdp_media_attributes(nativePtr, (int)type);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_copy(IntPtr thiz);

		/// <summary>
		/// Copy an existing <see cref="Linphone.CallParams" /> object to a new <see
		/// cref="Linphone.CallParams" /> object. 
		/// </summary>
		public Linphone.CallParams Copy()
		{
			IntPtr ptr = linphone_call_params_copy(nativePtr);
			Linphone.CallParams returnVal = fromNativePtr<Linphone.CallParams>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_custom_header(IntPtr thiz, string headerName);

		/// <summary>
		/// Get a custom SIP header. 
		/// </summary>
		public string GetCustomHeader(string headerName)
		{
			IntPtr stringPtr = linphone_call_params_get_custom_header(nativePtr, headerName);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_custom_sdp_attribute(IntPtr thiz, string attributeName);

		/// <summary>
		/// Get a custom SDP attribute that is related to all the streams. 
		/// </summary>
		public string GetCustomSdpAttribute(string attributeName)
		{
			IntPtr stringPtr = linphone_call_params_get_custom_sdp_attribute(nativePtr, attributeName);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_call_params_get_custom_sdp_media_attribute(IntPtr thiz, int type, string attributeName);

		/// <summary>
		/// Get a custom SDP attribute that is related to a specific stream. 
		/// </summary>
		public string GetCustomSdpMediaAttribute(Linphone.StreamType type, string attributeName)
		{
			IntPtr stringPtr = linphone_call_params_get_custom_sdp_media_attribute(nativePtr, (int)type, attributeName);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
	}
	/// <summary>
	/// The <see cref="Linphone.CallStats" /> objects carries various statistic
	/// informations regarding quality of audio or video streams. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class CallStats : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_download_bandwidth(IntPtr thiz);

		/// <summary>
		/// Get the bandwidth measurement of the received stream, expressed in kbit/s,
		/// including IP/UDP/RTP headers. 
		/// </summary>
		public float DownloadBandwidth
		{
			get
			{
				return linphone_call_stats_get_download_bandwidth(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_estimated_download_bandwidth(IntPtr thiz);

		/// <summary>
		/// Get the estimated bandwidth measurement of the received stream, expressed in
		/// kbit/s, including IP/UDP/RTP headers. 
		/// </summary>
		public float EstimatedDownloadBandwidth
		{
			get
			{
				return linphone_call_stats_get_estimated_download_bandwidth(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.IceState linphone_call_stats_get_ice_state(IntPtr thiz);

		/// <summary>
		/// Get the state of ICE processing. 
		/// </summary>
		public Linphone.IceState IceState
		{
			get
			{
				return linphone_call_stats_get_ice_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AddressFamily linphone_call_stats_get_ip_family_of_remote(IntPtr thiz);

		/// <summary>
		/// Get the IP address family of the remote peer. 
		/// </summary>
		public Linphone.AddressFamily IpFamilyOfRemote
		{
			get
			{
				return linphone_call_stats_get_ip_family_of_remote(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_jitter_buffer_size_ms(IntPtr thiz);

		/// <summary>
		/// Get the jitter buffer size in ms. 
		/// </summary>
		public float JitterBufferSizeMs
		{
			get
			{
				return linphone_call_stats_get_jitter_buffer_size_ms(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_call_stats_get_late_packets_cumulative_number(IntPtr thiz);

		/// <summary>
		/// Gets the cumulative number of late packets. 
		/// </summary>
		public uint LatePacketsCumulativeNumber
		{
			get
			{
				return linphone_call_stats_get_late_packets_cumulative_number(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_local_late_rate(IntPtr thiz);

		/// <summary>
		/// Gets the local late rate since last report. 
		/// </summary>
		public float LocalLateRate
		{
			get
			{
				return linphone_call_stats_get_local_late_rate(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_local_loss_rate(IntPtr thiz);

		/// <summary>
		/// Get the local loss rate since last report. 
		/// </summary>
		public float LocalLossRate
		{
			get
			{
				return linphone_call_stats_get_local_loss_rate(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_receiver_interarrival_jitter(IntPtr thiz);

		/// <summary>
		/// Gets the remote reported interarrival jitter. 
		/// </summary>
		public float ReceiverInterarrivalJitter
		{
			get
			{
				return linphone_call_stats_get_receiver_interarrival_jitter(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_receiver_loss_rate(IntPtr thiz);

		/// <summary>
		/// Gets the remote reported loss rate since last report. 
		/// </summary>
		public float ReceiverLossRate
		{
			get
			{
				return linphone_call_stats_get_receiver_loss_rate(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_round_trip_delay(IntPtr thiz);

		/// <summary>
		/// Get the round trip delay in s. 
		/// </summary>
		public float RoundTripDelay
		{
			get
			{
				return linphone_call_stats_get_round_trip_delay(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_rtcp_download_bandwidth(IntPtr thiz);

		/// <summary>
		/// Get the bandwidth measurement of the received RTCP, expressed in kbit/s,
		/// including IP/UDP/RTP headers. 
		/// </summary>
		public float RtcpDownloadBandwidth
		{
			get
			{
				return linphone_call_stats_get_rtcp_download_bandwidth(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_rtcp_upload_bandwidth(IntPtr thiz);

		/// <summary>
		/// Get the bandwidth measurement of the sent RTCP, expressed in kbit/s, including
		/// IP/UDP/RTP headers. 
		/// </summary>
		public float RtcpUploadBandwidth
		{
			get
			{
				return linphone_call_stats_get_rtcp_upload_bandwidth(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_sender_interarrival_jitter(IntPtr thiz);

		/// <summary>
		/// Gets the local interarrival jitter. 
		/// </summary>
		public float SenderInterarrivalJitter
		{
			get
			{
				return linphone_call_stats_get_sender_interarrival_jitter(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_sender_loss_rate(IntPtr thiz);

		/// <summary>
		/// Get the local loss rate since last report. 
		/// </summary>
		public float SenderLossRate
		{
			get
			{
				return linphone_call_stats_get_sender_loss_rate(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.StreamType linphone_call_stats_get_type(IntPtr thiz);

		/// <summary>
		/// Get the type of the stream the stats refer to. 
		/// </summary>
		public Linphone.StreamType Type
		{
			get
			{
				return linphone_call_stats_get_type(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_call_stats_get_upload_bandwidth(IntPtr thiz);

		/// <summary>
		/// Get the bandwidth measurement of the sent stream, expressed in kbit/s,
		/// including IP/UDP/RTP headers. 
		/// </summary>
		public float UploadBandwidth
		{
			get
			{
				return linphone_call_stats_get_upload_bandwidth(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.UpnpState linphone_call_stats_get_upnp_state(IntPtr thiz);

		/// <summary>
		/// Get the state of uPnP processing. 
		/// </summary>
		public Linphone.UpnpState UpnpState
		{
			get
			{
				return linphone_call_stats_get_upnp_state(nativePtr);
			}
		}
	}
	/// <summary>
	/// An chat message is the object that is sent and received through
	/// LinphoneChatRooms. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class ChatMessage : LinphoneObject
	{

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_chat_message_cbs(IntPtr factory);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_add_callbacks(IntPtr thiz, IntPtr cbs);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_remove_callbacks(IntPtr thiz, IntPtr cbs);

		~ChatMessage() 
		{
			if (listener != null)
			{
				linphone_chat_message_remove_callbacks(nativePtr, listener.nativePtr);
			}
		}

		private ChatMessageListener listener;

		public ChatMessageListener Listener
		{
			get {
				if (listener == null)
				{
					IntPtr nativeListener = linphone_factory_create_chat_message_cbs(IntPtr.Zero);
					listener = fromNativePtr<ChatMessageListener>(nativeListener, false);
					linphone_chat_message_add_callbacks(nativePtr, nativeListener);
					listener.register();
				}
				return listener;
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_appdata(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_set_appdata(IntPtr thiz, string data);

		/// <summary>
		/// Linphone message has an app-specific field that can store a text. 
		/// </summary>
		public string Appdata
		{
			get
			{
				IntPtr stringPtr = linphone_chat_message_get_appdata(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_chat_message_set_appdata(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_call_id(IntPtr thiz);

		/// <summary>
		/// Gets the callId accociated with the message. 
		/// </summary>
		public string CallId
		{
			get
			{
				IntPtr stringPtr = linphone_chat_message_get_call_id(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_chat_room(IntPtr thiz);

		/// <summary>
		/// Returns the chatroom this message belongs to. 
		/// </summary>
		public Linphone.ChatRoom ChatRoom
		{
			get
			{
				IntPtr ptr = linphone_chat_message_get_chat_room(nativePtr);
				Linphone.ChatRoom obj = fromNativePtr<Linphone.ChatRoom>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_content_type(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_set_content_type(IntPtr thiz, string contentType);

		/// <summary>
		/// Get the content type of a chat message. 
		/// </summary>
		public string ContentType
		{
			get
			{
				IntPtr stringPtr = linphone_chat_message_get_content_type(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_chat_message_set_content_type(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_contents(IntPtr thiz);

		/// <summary>
		/// Returns the list of contents in the message. 
		/// </summary>
		public IEnumerable<Linphone.Content> Contents
		{
			get
			{
				return MarshalBctbxList<Linphone.Content>(linphone_chat_message_get_contents(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_core(IntPtr thiz);

		/// <summary>
		/// Returns back pointer to <see cref="Linphone.Core" /> object. 
		/// </summary>
		public Linphone.Core Core
		{
			get
			{
				IntPtr ptr = linphone_chat_message_get_core(nativePtr);
				Linphone.Core obj = fromNativePtr<Linphone.Core>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_current_callbacks(IntPtr thiz);

		/// <summary>
		/// Gets the current LinphoneChatMessageCbs. 
		/// </summary>
		public Linphone.ChatMessageListener CurrentCallbacks
		{
			get
			{
				IntPtr ptr = linphone_chat_message_get_current_callbacks(nativePtr);
				Linphone.ChatMessageListener obj = fromNativePtr<Linphone.ChatMessageListener>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_chat_message_get_ephemeral_expire_time(IntPtr thiz);

		/// <summary>
		/// Returns the real time at which an ephemeral message expires and will be
		/// deleted. 
		/// </summary>
		public long EphemeralExpireTime
		{
			get
			{
				return linphone_chat_message_get_ephemeral_expire_time(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_chat_message_get_ephemeral_lifetime(IntPtr thiz);

		/// <summary>
		/// Returns lifetime of an ephemeral message. 
		/// </summary>
		public int EphemeralLifetime
		{
			get
			{
				return linphone_chat_message_get_ephemeral_lifetime(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_error_info(IntPtr thiz);

		/// <summary>
		/// Get full details about delivery error of a chat message. 
		/// </summary>
		public Linphone.ErrorInfo ErrorInfo
		{
			get
			{
				IntPtr ptr = linphone_chat_message_get_error_info(nativePtr);
				Linphone.ErrorInfo obj = fromNativePtr<Linphone.ErrorInfo>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_external_body_url(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_set_external_body_url(IntPtr thiz, string url);

		/// <summary>
		/// Linphone message can carry external body as defined by rfc2017. 
		/// </summary>
		public string ExternalBodyUrl
		{
			get
			{
				IntPtr stringPtr = linphone_chat_message_get_external_body_url(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_chat_message_set_external_body_url(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_file_transfer_filepath(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_set_file_transfer_filepath(IntPtr thiz, string filepath);

		/// <summary>
		/// Get the path to the file to read from or write to during the file transfer. 
		/// </summary>
		public string FileTransferFilepath
		{
			get
			{
				IntPtr stringPtr = linphone_chat_message_get_file_transfer_filepath(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_chat_message_set_file_transfer_filepath(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_file_transfer_information(IntPtr thiz);

		/// <summary>
		/// Get the file_transfer_information (used by call backs to recover informations
		/// during a rcs file transfer) 
		/// </summary>
		public Linphone.Content FileTransferInformation
		{
			get
			{
				IntPtr ptr = linphone_chat_message_get_file_transfer_information(nativePtr);
				Linphone.Content obj = fromNativePtr<Linphone.Content>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_forward_info(IntPtr thiz);

		/// <summary>
		/// Gets the forward info if available as a string. 
		/// </summary>
		public string ForwardInfo
		{
			get
			{
				IntPtr stringPtr = linphone_chat_message_get_forward_info(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_from_address(IntPtr thiz);

		/// <summary>
		/// Get origin of the message. 
		/// </summary>
		public Linphone.Address FromAddress
		{
			get
			{
				IntPtr ptr = linphone_chat_message_get_from_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_is_ephemeral(IntPtr thiz);

		/// <summary>
		/// Returns true if the chat message is an ephemeral message. 
		/// </summary>
		public bool IsEphemeral
		{
			get
			{
				return linphone_chat_message_is_ephemeral(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_is_file_transfer(IntPtr thiz);

		/// <summary>
		/// Return whether or not a chat message is a file transfer. 
		/// </summary>
		public bool IsFileTransfer
		{
			get
			{
				return linphone_chat_message_is_file_transfer(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_is_file_transfer_in_progress(IntPtr thiz);

		/// <summary>
		/// Gets whether or not a file is currently being downloaded or uploaded. 
		/// </summary>
		public bool IsFileTransferInProgress
		{
			get
			{
				return linphone_chat_message_is_file_transfer_in_progress(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_is_forward(IntPtr thiz);

		/// <summary>
		/// Returns true if the chat message is a forward message. 
		/// </summary>
		public bool IsForward
		{
			get
			{
				return linphone_chat_message_is_forward(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_is_outgoing(IntPtr thiz);

		/// <summary>
		/// Returns true if the message has been sent, returns true if the message has been
		/// received. 
		/// </summary>
		public bool IsOutgoing
		{
			get
			{
				return linphone_chat_message_is_outgoing(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_is_read(IntPtr thiz);

		/// <summary>
		/// Returns true if the message has been read, otherwise returns true. 
		/// </summary>
		public bool IsRead
		{
			get
			{
				return linphone_chat_message_is_read(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_is_secured(IntPtr thiz);

		/// <summary>
		/// Get if the message was encrypted when transfered. 
		/// </summary>
		public bool IsSecured
		{
			get
			{
				return linphone_chat_message_is_secured(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_is_text(IntPtr thiz);

		/// <summary>
		/// Return whether or not a chat message is a text. 
		/// </summary>
		public bool IsText
		{
			get
			{
				return linphone_chat_message_is_text(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_local_address(IntPtr thiz);

		/// <summary>
		/// Returns the origin address of a message if it was a outgoing message, or the
		/// destination address if it was an incoming message. 
		/// </summary>
		public Linphone.Address LocalAddress
		{
			get
			{
				IntPtr ptr = linphone_chat_message_get_local_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_message_id(IntPtr thiz);

		/// <summary>
		/// Get the message identifier. 
		/// </summary>
		public string MessageId
		{
			get
			{
				IntPtr stringPtr = linphone_chat_message_get_message_id(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ChatMessageState linphone_chat_message_get_state(IntPtr thiz);

		/// <summary>
		/// Get the state of the message. 
		/// </summary>
		public Linphone.ChatMessageState State
		{
			get
			{
				return linphone_chat_message_get_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_text(IntPtr thiz);

		/// <summary>
		/// Get text part of this message. 
		/// </summary>
		public string Text
		{
			get
			{
				IntPtr stringPtr = linphone_chat_message_get_text(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_text_content(IntPtr thiz);

		/// <summary>
		/// Gets the text content if available as a string. 
		/// </summary>
		public string TextContent
		{
			get
			{
				IntPtr stringPtr = linphone_chat_message_get_text_content(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_chat_message_get_time(IntPtr thiz);

		/// <summary>
		/// Get the time the message was sent. 
		/// </summary>
		public long Time
		{
			get
			{
				return linphone_chat_message_get_time(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_to_address(IntPtr thiz);

		/// <summary>
		/// Get destination of the message. 
		/// </summary>
		public Linphone.Address ToAddress
		{
			get
			{
				IntPtr ptr = linphone_chat_message_get_to_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_get_to_be_stored(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_set_to_be_stored(IntPtr thiz, char toBeStored);

		/// <summary>
		/// Get if a chat message is to be stored. 
		/// </summary>
		public bool ToBeStored
		{
			get
			{
				return linphone_chat_message_get_to_be_stored(nativePtr) != 0;
			}
			set
			{
				linphone_chat_message_set_to_be_stored(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_add_custom_header(IntPtr thiz, string headerName, string headerValue);

		/// <summary>
		/// Add custom headers to the message. 
		/// </summary>
		public void AddCustomHeader(string headerName, string headerValue)
		{
			linphone_chat_message_add_custom_header(nativePtr, headerName, headerValue);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_add_file_content(IntPtr thiz, IntPtr cContent);

		/// <summary>
		/// Adds a file content to the ChatMessage. 
		/// </summary>
		public void AddFileContent(Linphone.Content cContent)
		{
			linphone_chat_message_add_file_content(nativePtr, cContent != null ? cContent.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_add_text_content(IntPtr thiz, string text);

		/// <summary>
		/// Adds a text content to the ChatMessage. 
		/// </summary>
		public void AddTextContent(string text)
		{
			linphone_chat_message_add_text_content(nativePtr, text);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_cancel_file_transfer(IntPtr thiz);

		/// <summary>
		/// Cancel an ongoing file transfer attached to this message. 
		/// </summary>
		public void CancelFileTransfer()
		{
			linphone_chat_message_cancel_file_transfer(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_download_content(IntPtr thiz, IntPtr cContent);

		/// <summary>
		/// Start the download of the <see cref="Linphone.Content" /> referenced in the
		/// <see cref="Linphone.ChatMessage" /> from remote server. 
		/// </summary>
		public bool DownloadContent(Linphone.Content cContent)
		{
			bool returnVal = linphone_chat_message_download_content(nativePtr, cContent != null ? cContent.nativePtr : IntPtr.Zero) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_download_file(IntPtr thiz);

		/// <summary>
		/// Start the download of the file referenced in a <see cref="Linphone.ChatMessage"
		/// /> from remote server. 
		/// </summary>
		public bool DownloadFile()
		{
			bool returnVal = linphone_chat_message_download_file(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_custom_header(IntPtr thiz, string headerName);

		/// <summary>
		/// Retrieve a custom header value given its name. 
		/// </summary>
		public string GetCustomHeader(string headerName)
		{
			IntPtr stringPtr = linphone_chat_message_get_custom_header(nativePtr, headerName);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_message_get_participants_by_imdn_state(IntPtr thiz, int state);

		/// <summary>
		/// Gets the list of participants for which the imdn state has reached the
		/// specified state and the time at which they did. 
		/// </summary>
		public IEnumerable<Linphone.ParticipantImdnState> GetParticipantsByImdnState(Linphone.ChatMessageState state)
		{
			IEnumerable<Linphone.ParticipantImdnState> returnVal = MarshalBctbxList<Linphone.ParticipantImdnState>(linphone_chat_message_get_participants_by_imdn_state(nativePtr, (int)state));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_message_has_text_content(IntPtr thiz);

		/// <summary>
		/// Returns true if the chat message has a text content. 
		/// </summary>
		public bool HasTextContent()
		{
			bool returnVal = linphone_chat_message_has_text_content(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_chat_message_put_char(IntPtr thiz, uint character);

		/// <summary>
		/// Fulfill a chat message char by char. 
		/// </summary>
		public void PutChar(uint character)
		{
			int exception_result = linphone_chat_message_put_char(nativePtr, character);
			if (exception_result != 0) throw new LinphoneException("PutChar returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_remove_content(IntPtr thiz, IntPtr content);

		/// <summary>
		/// Removes a content from the ChatMessage. 
		/// </summary>
		public void RemoveContent(Linphone.Content content)
		{
			linphone_chat_message_remove_content(nativePtr, content != null ? content.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_remove_custom_header(IntPtr thiz, string headerName);

		/// <summary>
		/// Removes a custom header from the message. 
		/// </summary>
		public void RemoveCustomHeader(string headerName)
		{
			linphone_chat_message_remove_custom_header(nativePtr, headerName);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_resend_2(IntPtr thiz);

		/// <summary>
		/// Resend a chat message if it is in the 'not delivered' state for whatever
		/// reason. 
		/// </summary>
		public void Resend()
		{
			linphone_chat_message_resend_2(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_message_send(IntPtr thiz);

		/// <summary>
		/// Send a chat message. 
		/// </summary>
		public void Send()
		{
			linphone_chat_message_send(nativePtr);
			
			
			
		}
	}
	/// <summary>
	/// A chat room is the place where text messages are exchanged. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class ChatRoom : LinphoneObject
	{

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_chat_room_cbs(IntPtr factory);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_add_callbacks(IntPtr thiz, IntPtr cbs);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_remove_callbacks(IntPtr thiz, IntPtr cbs);

		~ChatRoom() 
		{
			if (listener != null)
			{
				linphone_chat_room_remove_callbacks(nativePtr, listener.nativePtr);
			}
		}

		private ChatRoomListener listener;

		public ChatRoomListener Listener
		{
			get {
				if (listener == null)
				{
					IntPtr nativeListener = linphone_factory_create_chat_room_cbs(IntPtr.Zero);
					listener = fromNativePtr<ChatRoomListener>(nativeListener, false);
					linphone_chat_room_add_callbacks(nativePtr, nativeListener);
					listener.register();
				}
				return listener;
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_call(IntPtr thiz);

		/// <summary>
		/// get Curent Call associated to this chatroom if any To commit a message, use
		/// linphone_chat_room_send_message 
		/// </summary>
		public Linphone.Call Call
		{
			get
			{
				IntPtr ptr = linphone_chat_room_get_call(nativePtr);
				Linphone.Call obj = fromNativePtr<Linphone.Call>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_chat_room_get_capabilities(IntPtr thiz);

		/// <summary>
		/// Get the capabilities of a chat room. 
		/// </summary>
		public uint Capabilities
		{
			get
			{
				return linphone_chat_room_get_capabilities(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_chat_room_get_char(IntPtr thiz);

		/// <summary>
		/// When realtime text is enabled linphone_call_params_realtime_text_enabled,
		/// LinphoneCoreIsComposingReceivedCb is call everytime a char is received from
		/// peer. 
		/// </summary>
		public uint Char
		{
			get
			{
				return linphone_chat_room_get_char(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_composing_addresses(IntPtr thiz);

		/// <summary>
		/// Gets the list of participants that are currently composing. 
		/// </summary>
		public IEnumerable<Linphone.Address> ComposingAddresses
		{
			get
			{
				return MarshalBctbxList<Linphone.Address>(linphone_chat_room_get_composing_addresses(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_conference_address(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_set_conference_address(IntPtr thiz, IntPtr confAddr);

		/// <summary>
		/// Get the conference address of the chat room. 
		/// </summary>
		public Linphone.Address ConferenceAddress
		{
			get
			{
				IntPtr ptr = linphone_chat_room_get_conference_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
			set
			{
				linphone_chat_room_set_conference_address(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_core(IntPtr thiz);

		/// <summary>
		/// Returns back pointer to <see cref="Linphone.Core" /> object. 
		/// </summary>
		public Linphone.Core Core
		{
			get
			{
				IntPtr ptr = linphone_chat_room_get_core(nativePtr);
				Linphone.Core obj = fromNativePtr<Linphone.Core>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_current_callbacks(IntPtr thiz);

		/// <summary>
		/// Gets the current LinphoneChatRoomCbs. 
		/// </summary>
		public Linphone.ChatRoomListener CurrentCallbacks
		{
			get
			{
				IntPtr ptr = linphone_chat_room_get_current_callbacks(nativePtr);
				Linphone.ChatRoomListener obj = fromNativePtr<Linphone.ChatRoomListener>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_current_params(IntPtr thiz);

		/// <summary>
		/// Returns current parameters associated with the chat room. 
		/// </summary>
		public Linphone.ChatRoomParams CurrentParams
		{
			get
			{
				IntPtr ptr = linphone_chat_room_get_current_params(nativePtr);
				Linphone.ChatRoomParams obj = fromNativePtr<Linphone.ChatRoomParams>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_ephemeral_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_enable_ephemeral(IntPtr thiz, char ephem);

		/// <summary>
		/// Returns whether or not the ephemeral message feature is enabled in the chat
		/// room. 
		/// </summary>
		public bool EphemeralEnabled
		{
			get
			{
				return linphone_chat_room_ephemeral_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_chat_room_enable_ephemeral(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_chat_room_get_ephemeral_lifetime(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_set_ephemeral_lifetime(IntPtr thiz, int time);

		/// <summary>
		/// Get lifetime (in seconds) for all new ephemeral messages in the chat room. 
		/// </summary>
		public int EphemeralLifetime
		{
			get
			{
				return linphone_chat_room_get_ephemeral_lifetime(nativePtr);
			}
			set
			{
				linphone_chat_room_set_ephemeral_lifetime(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_chat_room_get_history_events_size(IntPtr thiz);

		/// <summary>
		/// Gets the number of events in a chat room. 
		/// </summary>
		public int HistoryEventsSize
		{
			get
			{
				return linphone_chat_room_get_history_events_size(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_chat_room_get_history_size(IntPtr thiz);

		/// <summary>
		/// Gets the number of messages in a chat room. 
		/// </summary>
		public int HistorySize
		{
			get
			{
				return linphone_chat_room_get_history_size(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_is_empty(IntPtr thiz);

		/// <summary>
		/// Returns whether or not a <see cref="Linphone.ChatRoom" /> has at least one <see
		/// cref="Linphone.ChatMessage" /> or not. 
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				return linphone_chat_room_is_empty(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_is_remote_composing(IntPtr thiz);

		/// <summary>
		/// Tells whether the remote is currently composing a message. 
		/// </summary>
		public bool IsRemoteComposing
		{
			get
			{
				return linphone_chat_room_is_remote_composing(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_last_message_in_history(IntPtr thiz);

		/// <summary>
		/// Gets the last chat message sent or received in this chat room. 
		/// </summary>
		public Linphone.ChatMessage LastMessageInHistory
		{
			get
			{
				IntPtr ptr = linphone_chat_room_get_last_message_in_history(nativePtr);
				Linphone.ChatMessage obj = fromNativePtr<Linphone.ChatMessage>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_chat_room_get_last_update_time(IntPtr thiz);

		/// <summary>
		/// Return the last updated time for the chat room. 
		/// </summary>
		public long LastUpdateTime
		{
			get
			{
				return linphone_chat_room_get_last_update_time(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_local_address(IntPtr thiz);

		/// <summary>
		/// get local address associated to  this <see cref="Linphone.ChatRoom" /> 
		/// </summary>
		public Linphone.Address LocalAddress
		{
			get
			{
				IntPtr ptr = linphone_chat_room_get_local_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_me(IntPtr thiz);

		/// <summary>
		/// Get the participant representing myself in the chat room. 
		/// </summary>
		public Linphone.Participant Me
		{
			get
			{
				IntPtr ptr = linphone_chat_room_get_me(nativePtr);
				Linphone.Participant obj = fromNativePtr<Linphone.Participant>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_chat_room_get_nb_participants(IntPtr thiz);

		/// <summary>
		/// Get the number of participants in the chat room (that is without ourselves). 
		/// </summary>
		public int NbParticipants
		{
			get
			{
				return linphone_chat_room_get_nb_participants(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_participants(IntPtr thiz);

		/// <summary>
		/// Get the list of participants of a chat room. 
		/// </summary>
		public IEnumerable<Linphone.Participant> Participants
		{
			get
			{
				return MarshalBctbxList<Linphone.Participant>(linphone_chat_room_get_participants(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_peer_address(IntPtr thiz);

		/// <summary>
		/// get peer address associated to  this <see cref="Linphone.ChatRoom" /> 
		/// </summary>
		public Linphone.Address PeerAddress
		{
			get
			{
				IntPtr ptr = linphone_chat_room_get_peer_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ChatRoomSecurityLevel linphone_chat_room_get_security_level(IntPtr thiz);

		/// <summary>
		/// Get the security level of a chat room. 
		/// </summary>
		public Linphone.ChatRoomSecurityLevel SecurityLevel
		{
			get
			{
				return linphone_chat_room_get_security_level(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ChatRoomState linphone_chat_room_get_state(IntPtr thiz);

		/// <summary>
		/// Get the state of the chat room. 
		/// </summary>
		public Linphone.ChatRoomState State
		{
			get
			{
				return linphone_chat_room_get_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_subject(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_set_subject(IntPtr thiz, string subject);

		/// <summary>
		/// Get the subject of a chat room. 
		/// </summary>
		public string Subject
		{
			get
			{
				IntPtr stringPtr = linphone_chat_room_get_subject(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_chat_room_set_subject(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_chat_room_get_unread_messages_count(IntPtr thiz);

		/// <summary>
		/// Gets the number of unread messages in the chatroom. 
		/// </summary>
		public int UnreadMessagesCount
		{
			get
			{
				return linphone_chat_room_get_unread_messages_count(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_add_participant(IntPtr thiz, IntPtr addr);

		/// <summary>
		/// Add a participant to a chat room. 
		/// </summary>
		public void AddParticipant(Linphone.Address addr)
		{
			linphone_chat_room_add_participant(nativePtr, addr != null ? addr.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_add_participants(IntPtr thiz, IntPtr addresses);

		/// <summary>
		/// Add several participants to a chat room at once. 
		/// </summary>
		public bool AddParticipants(IEnumerable<Linphone.Address> addresses)
		{
			bool returnVal = linphone_chat_room_add_participants(nativePtr, ObjectArrayToBctbxList<Linphone.Address>(addresses)) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_allow_cpim(IntPtr thiz);

		/// <summary>
		/// Allow cpim on a basic chat room   . 
		/// </summary>
		public void AllowCpim()
		{
			linphone_chat_room_allow_cpim(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_allow_multipart(IntPtr thiz);

		/// <summary>
		/// Allow multipart on a basic chat room   . 
		/// </summary>
		public void AllowMultipart()
		{
			linphone_chat_room_allow_multipart(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_can_handle_participants(IntPtr thiz);

		/// <summary>
		/// Tells whether a chat room is able to handle participants. 
		/// </summary>
		public bool CanHandleParticipants()
		{
			bool returnVal = linphone_chat_room_can_handle_participants(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_compose(IntPtr thiz);

		/// <summary>
		/// Notifies the destination of the chat message being composed that the user is
		/// typing a new message. 
		/// </summary>
		public void Compose()
		{
			linphone_chat_room_compose(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_create_empty_message(IntPtr thiz);

		/// <summary>
		/// Creates an empty message attached to a dedicated chat room. 
		/// </summary>
		public Linphone.ChatMessage CreateEmptyMessage()
		{
			IntPtr ptr = linphone_chat_room_create_empty_message(nativePtr);
			Linphone.ChatMessage returnVal = fromNativePtr<Linphone.ChatMessage>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_create_file_transfer_message(IntPtr thiz, IntPtr initialContent);

		/// <summary>
		/// Creates a message attached to a dedicated chat room with a particular content. 
		/// </summary>
		public Linphone.ChatMessage CreateFileTransferMessage(Linphone.Content initialContent)
		{
			IntPtr ptr = linphone_chat_room_create_file_transfer_message(nativePtr, initialContent != null ? initialContent.nativePtr : IntPtr.Zero);
			Linphone.ChatMessage returnVal = fromNativePtr<Linphone.ChatMessage>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_create_forward_message(IntPtr thiz, IntPtr msg);

		/// <summary>
		/// Creates a forward message attached to a dedicated chat room with a particular
		/// message. 
		/// </summary>
		public Linphone.ChatMessage CreateForwardMessage(Linphone.ChatMessage msg)
		{
			IntPtr ptr = linphone_chat_room_create_forward_message(nativePtr, msg != null ? msg.nativePtr : IntPtr.Zero);
			Linphone.ChatMessage returnVal = fromNativePtr<Linphone.ChatMessage>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_create_message(IntPtr thiz, string message);

		/// <summary>
		/// Creates a message attached to a dedicated chat room. 
		/// </summary>
		public Linphone.ChatMessage CreateMessage(string message)
		{
			IntPtr ptr = linphone_chat_room_create_message(nativePtr, message);
			Linphone.ChatMessage returnVal = fromNativePtr<Linphone.ChatMessage>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_delete_history(IntPtr thiz);

		/// <summary>
		/// Delete all messages from the history. 
		/// </summary>
		public void DeleteHistory()
		{
			linphone_chat_room_delete_history(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_delete_message(IntPtr thiz, IntPtr msg);

		/// <summary>
		/// Delete a message from the chat room history. 
		/// </summary>
		public void DeleteMessage(Linphone.ChatMessage msg)
		{
			linphone_chat_room_delete_message(nativePtr, msg != null ? msg.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_ephemeral_supported_by_all_participants(IntPtr thiz);

		/// <summary>
		/// Uses linphone spec to check if all participants support ephemeral messages. 
		/// </summary>
		public bool EphemeralSupportedByAllParticipants()
		{
			bool returnVal = linphone_chat_room_ephemeral_supported_by_all_participants(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_find_message(IntPtr thiz, string messageId);

		/// <summary>
		/// Gets the chat message sent or received in this chat room that matches the
		/// message_id. 
		/// </summary>
		public Linphone.ChatMessage FindMessage(string messageId)
		{
			IntPtr ptr = linphone_chat_room_find_message(nativePtr, messageId);
			Linphone.ChatMessage returnVal = fromNativePtr<Linphone.ChatMessage>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_find_participant(IntPtr thiz, IntPtr addr);

		/// <summary>
		/// Find a participant of a chat room from its address. 
		/// </summary>
		public Linphone.Participant FindParticipant(Linphone.Address addr)
		{
			IntPtr ptr = linphone_chat_room_find_participant(nativePtr, addr != null ? addr.nativePtr : IntPtr.Zero);
			Linphone.Participant returnVal = fromNativePtr<Linphone.Participant>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_history(IntPtr thiz, int nbMessage);

		/// <summary>
		/// Gets nb_message most recent messages from cr chat room, sorted from oldest to
		/// most recent. 
		/// </summary>
		public IEnumerable<Linphone.ChatMessage> GetHistory(int nbMessage)
		{
			IEnumerable<Linphone.ChatMessage> returnVal = MarshalBctbxList<Linphone.ChatMessage>(linphone_chat_room_get_history(nativePtr, nbMessage));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_history_events(IntPtr thiz, int nbEvents);

		/// <summary>
		/// Gets nb_events most recent events from cr chat room, sorted from oldest to most
		/// recent. 
		/// </summary>
		public IEnumerable<Linphone.EventLog> GetHistoryEvents(int nbEvents)
		{
			IEnumerable<Linphone.EventLog> returnVal = MarshalBctbxList<Linphone.EventLog>(linphone_chat_room_get_history_events(nativePtr, nbEvents));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_history_message_events(IntPtr thiz, int nbEvents);

		/// <summary>
		/// Gets nb_events most recent chat message events from cr chat room, sorted from
		/// oldest to most recent. 
		/// </summary>
		public IEnumerable<Linphone.EventLog> GetHistoryMessageEvents(int nbEvents)
		{
			IEnumerable<Linphone.EventLog> returnVal = MarshalBctbxList<Linphone.EventLog>(linphone_chat_room_get_history_message_events(nativePtr, nbEvents));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_history_range(IntPtr thiz, int begin, int end);

		/// <summary>
		/// Gets the partial list of messages in the given range, sorted from oldest to
		/// most recent. 
		/// </summary>
		public IEnumerable<Linphone.ChatMessage> GetHistoryRange(int begin, int end)
		{
			IEnumerable<Linphone.ChatMessage> returnVal = MarshalBctbxList<Linphone.ChatMessage>(linphone_chat_room_get_history_range(nativePtr, begin, end));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_history_range_events(IntPtr thiz, int begin, int end);

		/// <summary>
		/// Gets the partial list of events in the given range, sorted from oldest to most
		/// recent. 
		/// </summary>
		public IEnumerable<Linphone.EventLog> GetHistoryRangeEvents(int begin, int end)
		{
			IEnumerable<Linphone.EventLog> returnVal = MarshalBctbxList<Linphone.EventLog>(linphone_chat_room_get_history_range_events(nativePtr, begin, end));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_chat_room_get_history_range_message_events(IntPtr thiz, int begin, int end);

		/// <summary>
		/// Gets the partial list of chat message events in the given range, sorted from
		/// oldest to most recent. 
		/// </summary>
		public IEnumerable<Linphone.EventLog> GetHistoryRangeMessageEvents(int begin, int end)
		{
			IEnumerable<Linphone.EventLog> returnVal = MarshalBctbxList<Linphone.EventLog>(linphone_chat_room_get_history_range_message_events(nativePtr, begin, end));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_has_been_left(IntPtr thiz);

		/// <summary>
		/// Return whether or not the chat room has been left. 
		/// </summary>
		public bool HasBeenLeft()
		{
			bool returnVal = linphone_chat_room_has_been_left(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_has_capability(IntPtr thiz, int mask);

		/// <summary>
		/// Check if a chat room has given capabilities. 
		/// </summary>
		public bool HasCapability(int mask)
		{
			bool returnVal = linphone_chat_room_has_capability(nativePtr, mask) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_leave(IntPtr thiz);

		/// <summary>
		/// Leave a chat room. 
		/// </summary>
		public void Leave()
		{
			linphone_chat_room_leave(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_lime_available(IntPtr thiz);

		/// <summary>
		/// Returns true if lime is available for given peer. 
		/// </summary>
		public bool LimeAvailable()
		{
			bool returnVal = linphone_chat_room_lime_available(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_mark_as_read(IntPtr thiz);

		/// <summary>
		/// Mark all messages of the conversation as read. 
		/// </summary>
		public void MarkAsRead()
		{
			linphone_chat_room_mark_as_read(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_notify_participant_device_registration(IntPtr thiz, IntPtr participantDevice);

		/// <summary>
		/// Notify the chatroom that a participant device has just registered. 
		/// </summary>
		public void NotifyParticipantDeviceRegistration(Linphone.Address participantDevice)
		{
			linphone_chat_room_notify_participant_device_registration(nativePtr, participantDevice != null ? participantDevice.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_receive_chat_message(IntPtr thiz, IntPtr msg);

		/// <summary>
		/// Used to receive a chat message when using async mechanism with IM encryption
		/// engine. 
		/// </summary>
		public void ReceiveChatMessage(Linphone.ChatMessage msg)
		{
			linphone_chat_room_receive_chat_message(nativePtr, msg != null ? msg.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_remove_participant(IntPtr thiz, IntPtr participant);

		/// <summary>
		/// Remove a participant of a chat room. 
		/// </summary>
		public void RemoveParticipant(Linphone.Participant participant)
		{
			linphone_chat_room_remove_participant(nativePtr, participant != null ? participant.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_remove_participants(IntPtr thiz, IntPtr participants);

		/// <summary>
		/// Remove several participants of a chat room at once. 
		/// </summary>
		public void RemoveParticipants(IEnumerable<Linphone.Participant> participants)
		{
			linphone_chat_room_remove_participants(nativePtr, ObjectArrayToBctbxList<Linphone.Participant>(participants));
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_send_chat_message_2(IntPtr thiz, IntPtr msg);

		/// <summary>
		/// Send a message to peer member of this chat room. 
		/// </summary>
		public void SendChatMessage(Linphone.ChatMessage msg)
		{
			linphone_chat_room_send_chat_message_2(nativePtr, msg != null ? msg.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_set_participant_admin_status(IntPtr thiz, IntPtr participant, char isAdmin);

		/// <summary>
		/// Change the admin status of a participant of a chat room (you need to be an
		/// admin yourself to do this). 
		/// </summary>
		public void SetParticipantAdminStatus(Linphone.Participant participant, bool isAdmin)
		{
			linphone_chat_room_set_participant_admin_status(nativePtr, participant != null ? participant.nativePtr : IntPtr.Zero, isAdmin ? (char)1 : (char)0);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_set_participant_devices(IntPtr thiz, IntPtr partAddr, IntPtr deviceIdentities);

		/// <summary>
		/// Set the list of participant devices in the form of SIP URIs with GRUUs for a
		/// given participant. 
		/// </summary>
		public void SetParticipantDevices(Linphone.Address partAddr, IEnumerable<Linphone.ParticipantDeviceIdentity> deviceIdentities)
		{
			linphone_chat_room_set_participant_devices(nativePtr, partAddr != null ? partAddr.nativePtr : IntPtr.Zero, ObjectArrayToBctbxList<Linphone.ParticipantDeviceIdentity>(deviceIdentities));
			
			
			
		}
	}
	/// <summary>
	/// An object to handle a chat room parameters. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class ChatRoomParams : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ChatRoomBackend linphone_chat_room_params_get_backend(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_params_set_backend(IntPtr thiz, int backend);

		/// <summary>
		/// Get the backend implementation of the chat room associated with the given
		/// parameters. 
		/// </summary>
		public Linphone.ChatRoomBackend Backend
		{
			get
			{
				return linphone_chat_room_params_get_backend(nativePtr);
			}
			set
			{
				linphone_chat_room_params_set_backend(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ChatRoomEncryptionBackend linphone_chat_room_params_get_encryption_backend(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_params_set_encryption_backend(IntPtr thiz, int backend);

		/// <summary>
		/// Get the encryption implementation of the chat room associated with the given
		/// parameters. 
		/// </summary>
		public Linphone.ChatRoomEncryptionBackend EncryptionBackend
		{
			get
			{
				return linphone_chat_room_params_get_encryption_backend(nativePtr);
			}
			set
			{
				linphone_chat_room_params_set_encryption_backend(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_params_encryption_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_params_enable_encryption(IntPtr thiz, char encrypted);

		/// <summary>
		/// Get the encryption status of the chat room associated with the given
		/// parameters. 
		/// </summary>
		public bool EncryptionEnabled
		{
			get
			{
				return linphone_chat_room_params_encryption_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_chat_room_params_enable_encryption(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_params_group_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_params_enable_group(IntPtr thiz, char group);

		/// <summary>
		/// Get the group chat status of the chat room associated with the given
		/// parameters. 
		/// </summary>
		public bool GroupEnabled
		{
			get
			{
				return linphone_chat_room_params_group_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_chat_room_params_enable_group(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_params_is_valid(IntPtr thiz);

		public bool IsValid
		{
			get
			{
				return linphone_chat_room_params_is_valid(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_chat_room_params_rtt_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_chat_room_params_enable_rtt(IntPtr thiz, char rtt);

		/// <summary>
		/// Get the real time text status of the chat room associated with the given
		/// parameters. 
		/// </summary>
		public bool RttEnabled
		{
			get
			{
				return linphone_chat_room_params_rtt_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_chat_room_params_enable_rtt(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
	}
	/// <summary>
	/// <see cref="Linphone.Conference" /> class The _LinphoneConference struct does
	/// not exists, it's the Conference C++ class that is used behind 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Conference : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_conference_get_current_params(IntPtr thiz);

		/// <summary>
		/// Get current parameters of the conference. 
		/// </summary>
		public Linphone.ConferenceParams CurrentParams
		{
			get
			{
				IntPtr ptr = linphone_conference_get_current_params(nativePtr);
				Linphone.ConferenceParams obj = fromNativePtr<Linphone.ConferenceParams>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_conference_get_ID(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_conference_set_ID(IntPtr thiz, string conferenceID);

		/// <summary>
		/// Get the conference id as string. 
		/// </summary>
		public string Id
		{
			get
			{
				IntPtr stringPtr = linphone_conference_get_ID(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_conference_set_ID(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_conference_get_participants(IntPtr thiz);

		/// <summary>
		/// Get URIs of all participants of one conference The returned bctbx_list_t
		/// contains URIs of all participant. 
		/// </summary>
		public IEnumerable<Linphone.Address> Participants
		{
			get
			{
				return MarshalBctbxList<Linphone.Address>(linphone_conference_get_participants(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_conference_add_participant(IntPtr thiz, IntPtr call);

		/// <summary>
		/// Join an existing call to the conference. 
		/// </summary>
		public int AddParticipant(Linphone.Call call)
		{
			int returnVal = linphone_conference_add_participant(nativePtr, call != null ? call.nativePtr : IntPtr.Zero);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_conference_invite_participants(IntPtr thiz, IntPtr addresses, IntPtr parameters);

		/// <summary>
		/// Invite participants to the conference, by supplying a list of <see
		/// cref="Linphone.Address" />. 
		/// </summary>
		public void InviteParticipants(IEnumerable<Linphone.Address> addresses, Linphone.CallParams parameters)
		{
			int exception_result = linphone_conference_invite_participants(nativePtr, ObjectArrayToBctbxList<Linphone.Address>(addresses), parameters != null ? parameters.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("InviteParticipants returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_conference_remove_participant(IntPtr thiz, IntPtr uri);

		/// <summary>
		/// Remove a participant from a conference. 
		/// </summary>
		public void RemoveParticipant(Linphone.Address uri)
		{
			int exception_result = linphone_conference_remove_participant(nativePtr, uri != null ? uri.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("RemoveParticipant returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_conference_update_params(IntPtr thiz, IntPtr parameters);

		/// <summary>
		/// Update parameters of the conference. 
		/// </summary>
		public int UpdateParams(Linphone.ConferenceParams parameters)
		{
			int returnVal = linphone_conference_update_params(nativePtr, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			
			
			return returnVal;
		}
	}
	/// <summary>
	/// Parameters for initialization of conferences The _LinphoneConferenceParams
	/// struct does not exists, it's the ConferenceParams C++ class that is used
	/// behind. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class ConferenceParams : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_conference_params_local_participant_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_conference_params_enable_local_participant(IntPtr thiz, char enable);

		/// <summary>
		/// Returns whether local participant has to enter the conference. 
		/// </summary>
		public bool LocalParticipantEnabled
		{
			get
			{
				return linphone_conference_params_local_participant_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_conference_params_enable_local_participant(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_conference_params_video_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_conference_params_enable_video(IntPtr thiz, char enable);

		/// <summary>
		/// Check whether video will be enable at conference starting. 
		/// </summary>
		public bool VideoEnabled
		{
			get
			{
				return linphone_conference_params_video_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_conference_params_enable_video(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_conference_params_clone(IntPtr thiz);

		/// <summary>
		/// Clone a <see cref="Linphone.ConferenceParams" />. 
		/// </summary>
		public Linphone.ConferenceParams Clone()
		{
			IntPtr ptr = linphone_conference_params_clone(nativePtr);
			Linphone.ConferenceParams returnVal = fromNativePtr<Linphone.ConferenceParams>(ptr, true);
			
			return returnVal;
		}
	}
	/// <summary>
	/// The <see cref="Linphone.Config" /> object is used to manipulate a configuration
	/// file. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Config : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_config_new_from_buffer(string buffer);

		/// <summary>
		/// Instantiates a <see cref="Linphone.Config" /> object from a user provided
		/// buffer. 
		/// </summary>
		public static Linphone.Config NewFromBuffer(string buffer)
		{
			IntPtr ptr = linphone_config_new_from_buffer(buffer);
			Linphone.Config returnVal = fromNativePtr<Linphone.Config>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_config_new_with_factory(string configFilename, string factoryConfigFilename);

		/// <summary>
		/// Instantiates a <see cref="Linphone.Config" /> object from a user config file
		/// and a factory config file. 
		/// </summary>
		public static Linphone.Config NewWithFactory(string configFilename, string factoryConfigFilename)
		{
			IntPtr ptr = linphone_config_new_with_factory(configFilename, factoryConfigFilename);
			Linphone.Config returnVal = fromNativePtr<Linphone.Config>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_config_get_sections_names_list(IntPtr thiz);

		/// <summary>
		/// Returns the list of sections' names in the LinphoneConfig. 
		/// </summary>
		public IEnumerable<string> SectionsNamesList
		{
			get
			{
				return MarshalStringArray(linphone_config_get_sections_names_list(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_clean_entry(IntPtr thiz, string section, string key);

		/// <summary>
		/// Removes entries for key,value in a section. 
		/// </summary>
		public void CleanEntry(string section, string key)
		{
			linphone_config_clean_entry(nativePtr, section, key);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_clean_section(IntPtr thiz, string section);

		/// <summary>
		/// Removes every pair of key,value in a section and remove the section. 
		/// </summary>
		public void CleanSection(string section)
		{
			linphone_config_clean_section(nativePtr, section);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_config_dump(IntPtr thiz);

		/// <summary>
		/// Dumps the <see cref="Linphone.Config" /> as INI into a buffer. 
		/// </summary>
		public string Dump()
		{
			IntPtr stringPtr = linphone_config_dump(nativePtr);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_config_dump_as_xml(IntPtr thiz);

		/// <summary>
		/// Dumps the <see cref="Linphone.Config" /> as XML into a buffer. 
		/// </summary>
		public string DumpAsXml()
		{
			IntPtr stringPtr = linphone_config_dump_as_xml(nativePtr);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_config_get_bool(IntPtr thiz, string section, string key, char defaultValue);

		/// <summary>
		/// Retrieves a configuration item as a boolean, given its section, key, and
		/// default value. 
		/// </summary>
		public bool GetBool(string section, string key, bool defaultValue)
		{
			bool returnVal = linphone_config_get_bool(nativePtr, section, key, defaultValue ? (char)1 : (char)0) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_config_get_default_float(IntPtr thiz, string section, string key, float defaultValue);

		/// <summary>
		/// Retrieves a default configuration item as a float, given its section, key, and
		/// default value. 
		/// </summary>
		public float GetDefaultFloat(string section, string key, float defaultValue)
		{
			float returnVal = linphone_config_get_default_float(nativePtr, section, key, defaultValue);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_config_get_default_int(IntPtr thiz, string section, string key, int defaultValue);

		/// <summary>
		/// Retrieves a default configuration item as an integer, given its section, key,
		/// and default value. 
		/// </summary>
		public int GetDefaultInt(string section, string key, int defaultValue)
		{
			int returnVal = linphone_config_get_default_int(nativePtr, section, key, defaultValue);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_config_get_default_int64(IntPtr thiz, string section, string key, int defaultValue);

		/// <summary>
		/// Retrieves a default configuration item as a 64 bit integer, given its section,
		/// key, and default value. 
		/// </summary>
		public int GetDefaultInt64(string section, string key, int defaultValue)
		{
			int returnVal = linphone_config_get_default_int64(nativePtr, section, key, defaultValue);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_config_get_default_string(IntPtr thiz, string section, string key, string defaultValue);

		/// <summary>
		/// Retrieves a default configuration item as a string, given its section, key, and
		/// default value. 
		/// </summary>
		public string GetDefaultString(string section, string key, string defaultValue)
		{
			IntPtr stringPtr = linphone_config_get_default_string(nativePtr, section, key, defaultValue);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_config_get_float(IntPtr thiz, string section, string key, float defaultValue);

		/// <summary>
		/// Retrieves a configuration item as a float, given its section, key, and default
		/// value. 
		/// </summary>
		public float GetFloat(string section, string key, float defaultValue)
		{
			float returnVal = linphone_config_get_float(nativePtr, section, key, defaultValue);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_config_get_int(IntPtr thiz, string section, string key, int defaultValue);

		/// <summary>
		/// Retrieves a configuration item as an integer, given its section, key, and
		/// default value. 
		/// </summary>
		public int GetInt(string section, string key, int defaultValue)
		{
			int returnVal = linphone_config_get_int(nativePtr, section, key, defaultValue);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_config_get_int64(IntPtr thiz, string section, string key, int defaultValue);

		/// <summary>
		/// Retrieves a configuration item as a 64 bit integer, given its section, key, and
		/// default value. 
		/// </summary>
		public int GetInt64(string section, string key, int defaultValue)
		{
			int returnVal = linphone_config_get_int64(nativePtr, section, key, defaultValue);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_config_get_overwrite_flag_for_entry(IntPtr thiz, string section, string key);

		/// <summary>
		/// Retrieves the overwrite flag for a config item. 
		/// </summary>
		public bool GetOverwriteFlagForEntry(string section, string key)
		{
			bool returnVal = linphone_config_get_overwrite_flag_for_entry(nativePtr, section, key) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_config_get_overwrite_flag_for_section(IntPtr thiz, string section);

		/// <summary>
		/// Retrieves the overwrite flag for a config section. 
		/// </summary>
		public bool GetOverwriteFlagForSection(string section)
		{
			bool returnVal = linphone_config_get_overwrite_flag_for_section(nativePtr, section) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_config_get_range(IntPtr thiz, string section, string key, int min, int max, int defaultMin, int defaultMax);

		/// <summary>
		/// Retrieves a configuration item as a range, given its section, key, and default
		/// min and max values. 
		/// </summary>
		public bool GetRange(string section, string key, int min, int max, int defaultMin, int defaultMax)
		{
			bool returnVal = linphone_config_get_range(nativePtr, section, key, min, max, defaultMin, defaultMax) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_config_get_section_param_string(IntPtr thiz, string section, string key, string defaultValue);

		/// <summary>
		/// Retrieves a section parameter item as a string, given its section and key. 
		/// </summary>
		public string GetSectionParamString(string section, string key, string defaultValue)
		{
			IntPtr stringPtr = linphone_config_get_section_param_string(nativePtr, section, key, defaultValue);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_config_get_skip_flag_for_entry(IntPtr thiz, string section, string key);

		/// <summary>
		/// Retrieves the skip flag for a config item. 
		/// </summary>
		public bool GetSkipFlagForEntry(string section, string key)
		{
			bool returnVal = linphone_config_get_skip_flag_for_entry(nativePtr, section, key) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_config_get_skip_flag_for_section(IntPtr thiz, string section);

		/// <summary>
		/// Retrieves the skip flag for a config section. 
		/// </summary>
		public bool GetSkipFlagForSection(string section)
		{
			bool returnVal = linphone_config_get_skip_flag_for_section(nativePtr, section) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_config_get_string(IntPtr thiz, string section, string key, string defaultString);

		/// <summary>
		/// Retrieves a configuration item as a string, given its section, key, and default
		/// value. 
		/// </summary>
		public string GetString(string section, string key, string defaultString)
		{
			IntPtr stringPtr = linphone_config_get_string(nativePtr, section, key, defaultString);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_config_get_string_list(IntPtr thiz, string section, string key, IntPtr defaultList);

		/// <summary>
		/// Retrieves a configuration item as a list of strings, given its section, key,
		/// and default value. 
		/// </summary>
		public IEnumerable<string> GetStringList(string section, string key, IEnumerable<string> defaultList)
		{
			IEnumerable<string> returnVal = MarshalStringArray(linphone_config_get_string_list(nativePtr, section, key, StringArrayToBctbxList(defaultList)));
			CleanStringArrayPtrs();
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_config_has_entry(IntPtr thiz, string section, string key);

		/// <summary>
		/// Returns 1 if a given section with a given key is present in the configuration. 
		/// </summary>
		public int HasEntry(string section, string key)
		{
			int returnVal = linphone_config_has_entry(nativePtr, section, key);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_config_has_section(IntPtr thiz, string section);

		/// <summary>
		/// Returns 1 if a given section is present in the configuration. 
		/// </summary>
		public int HasSection(string section)
		{
			int returnVal = linphone_config_has_section(nativePtr, section);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_config_load_from_xml_file(IntPtr thiz, string filename);

		/// <summary>
		/// Reads a xml config file and fill the <see cref="Linphone.Config" /> with the
		/// read config dynamic values. 
		/// </summary>
		public string LoadFromXmlFile(string filename)
		{
			IntPtr stringPtr = linphone_config_load_from_xml_file(nativePtr, filename);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_config_load_from_xml_string(IntPtr thiz, string buffer);

		/// <summary>
		/// Reads a xml config string and fill the <see cref="Linphone.Config" /> with the
		/// read config dynamic values. 
		/// </summary>
		public void LoadFromXmlString(string buffer)
		{
			int exception_result = linphone_config_load_from_xml_string(nativePtr, buffer);
			if (exception_result != 0) throw new LinphoneException("LoadFromXmlString returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_config_read_file(IntPtr thiz, string filename);

		/// <summary>
		/// Reads a user config file and fill the <see cref="Linphone.Config" /> with the
		/// read config values. 
		/// </summary>
		public void ReadFile(string filename)
		{
			int exception_result = linphone_config_read_file(nativePtr, filename);
			if (exception_result != 0) throw new LinphoneException("ReadFile returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_config_relative_file_exists(IntPtr thiz, string filename);

		public bool RelativeFileExists(string filename)
		{
			bool returnVal = linphone_config_relative_file_exists(nativePtr, filename) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_reload(IntPtr thiz);

		/// <summary>
		/// Reload the config from the file. 
		/// </summary>
		public void Reload()
		{
			linphone_config_reload(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_bool(IntPtr thiz, string section, string key, char val);

		/// <summary>
		/// Sets a boolean config item. 
		/// </summary>
		public void SetBool(string section, string key, bool val)
		{
			linphone_config_set_bool(nativePtr, section, key, val ? (char)1 : (char)0);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_float(IntPtr thiz, string section, string key, float val);

		/// <summary>
		/// Sets a float config item. 
		/// </summary>
		public void SetFloat(string section, string key, float val)
		{
			linphone_config_set_float(nativePtr, section, key, val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_int(IntPtr thiz, string section, string key, int val);

		/// <summary>
		/// Sets an integer config item. 
		/// </summary>
		public void SetInt(string section, string key, int val)
		{
			linphone_config_set_int(nativePtr, section, key, val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_int64(IntPtr thiz, string section, string key, int val);

		/// <summary>
		/// Sets a 64 bits integer config item. 
		/// </summary>
		public void SetInt64(string section, string key, int val)
		{
			linphone_config_set_int64(nativePtr, section, key, val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_int_hex(IntPtr thiz, string section, string key, int val);

		/// <summary>
		/// Sets an integer config item, but store it as hexadecimal. 
		/// </summary>
		public void SetIntHex(string section, string key, int val)
		{
			linphone_config_set_int_hex(nativePtr, section, key, val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_overwrite_flag_for_entry(IntPtr thiz, string section, string key, char val);

		/// <summary>
		/// Sets the overwrite flag for a config item (used when dumping config as xml) 
		/// </summary>
		public void SetOverwriteFlagForEntry(string section, string key, bool val)
		{
			linphone_config_set_overwrite_flag_for_entry(nativePtr, section, key, val ? (char)1 : (char)0);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_overwrite_flag_for_section(IntPtr thiz, string section, char val);

		/// <summary>
		/// Sets the overwrite flag for a config section (used when dumping config as xml) 
		/// </summary>
		public void SetOverwriteFlagForSection(string section, bool val)
		{
			linphone_config_set_overwrite_flag_for_section(nativePtr, section, val ? (char)1 : (char)0);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_range(IntPtr thiz, string section, string key, int minValue, int maxValue);

		/// <summary>
		/// Sets a range config item. 
		/// </summary>
		public void SetRange(string section, string key, int minValue, int maxValue)
		{
			linphone_config_set_range(nativePtr, section, key, minValue, maxValue);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_skip_flag_for_entry(IntPtr thiz, string section, string key, char val);

		/// <summary>
		/// Sets the skip flag for a config item (used when dumping config as xml) 
		/// </summary>
		public void SetSkipFlagForEntry(string section, string key, bool val)
		{
			linphone_config_set_skip_flag_for_entry(nativePtr, section, key, val ? (char)1 : (char)0);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_skip_flag_for_section(IntPtr thiz, string section, char val);

		/// <summary>
		/// Sets the skip flag for a config section (used when dumping config as xml) 
		/// </summary>
		public void SetSkipFlagForSection(string section, bool val)
		{
			linphone_config_set_skip_flag_for_section(nativePtr, section, val ? (char)1 : (char)0);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_string(IntPtr thiz, string section, string key, string val);

		/// <summary>
		/// Sets a string config item. 
		/// </summary>
		public void SetString(string section, string key, string val)
		{
			linphone_config_set_string(nativePtr, section, key, val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_set_string_list(IntPtr thiz, string section, string key, IntPtr val);

		/// <summary>
		/// Sets a string list config item. 
		/// </summary>
		public void SetStringList(string section, string key, IEnumerable<string> val)
		{
			linphone_config_set_string_list(nativePtr, section, key, StringArrayToBctbxList(val));
			
			CleanStringArrayPtrs();
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_config_sync(IntPtr thiz);

		/// <summary>
		/// Writes the config file to disk. 
		/// </summary>
		public void Sync()
		{
			int exception_result = linphone_config_sync(nativePtr);
			if (exception_result != 0) throw new LinphoneException("Sync returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_config_write_relative_file(IntPtr thiz, string filename, string data);

		/// <summary>
		/// Write a string in a file placed relatively with the Linphone configuration
		/// file. 
		/// </summary>
		public void WriteRelativeFile(string filename, string data)
		{
			linphone_config_write_relative_file(nativePtr, filename, data);
			
			
			
		}
	}
	/// <summary>
	/// The LinphoneContent object holds data that can be embedded in a signaling
	/// message. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Content : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_content_get_buffer(IntPtr thiz);

		/// <summary>
		/// Get the content data buffer, usually a string. 
		/// </summary>
		public uint Buffer
		{
			get
			{
				return linphone_content_get_buffer(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_get_encoding(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_content_set_encoding(IntPtr thiz, string encoding);

		/// <summary>
		/// Get the encoding of the data buffer, for example "gzip". 
		/// </summary>
		public string Encoding
		{
			get
			{
				IntPtr stringPtr = linphone_content_get_encoding(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_content_set_encoding(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_get_file_path(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_content_set_file_path(IntPtr thiz, string filePath);

		/// <summary>
		/// Get the file transfer filepath set for this content (replace
		/// linphone_chat_message_get_file_transfer_filepath). 
		/// </summary>
		public string FilePath
		{
			get
			{
				IntPtr stringPtr = linphone_content_get_file_path(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_content_set_file_path(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_content_get_file_size(IntPtr thiz);

		/// <summary>
		/// Get the file size if content is either a FileContent or a FileTransferContent. 
		/// </summary>
		public long FileSize
		{
			get
			{
				return linphone_content_get_file_size(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_content_is_file(IntPtr thiz);

		/// <summary>
		/// Tells whether or not this content contains a file. 
		/// </summary>
		public bool IsFile
		{
			get
			{
				return linphone_content_is_file(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_content_is_file_transfer(IntPtr thiz);

		/// <summary>
		/// Tells whether or not this content is a file transfer. 
		/// </summary>
		public bool IsFileTransfer
		{
			get
			{
				return linphone_content_is_file_transfer(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_content_is_multipart(IntPtr thiz);

		/// <summary>
		/// Tell whether a content is a multipart content. 
		/// </summary>
		public bool IsMultipart
		{
			get
			{
				return linphone_content_is_multipart(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_content_is_text(IntPtr thiz);

		/// <summary>
		/// Tells whether or not this content contains text. 
		/// </summary>
		public bool IsText
		{
			get
			{
				return linphone_content_is_text(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_get_key(IntPtr thiz);

		/// <summary>
		/// Get the key associated with a RCS file transfer message if encrypted. 
		/// </summary>
		public string Key
		{
			get
			{
				IntPtr stringPtr = linphone_content_get_key(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_content_get_key_size(IntPtr thiz);

		/// <summary>
		/// Get the size of key associated with a RCS file transfer message if encrypted. 
		/// </summary>
		public long KeySize
		{
			get
			{
				return linphone_content_get_key_size(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_get_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_content_set_name(IntPtr thiz, string name);

		/// <summary>
		/// Get the name associated with a RCS file transfer message. 
		/// </summary>
		public string Name
		{
			get
			{
				IntPtr stringPtr = linphone_content_get_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_content_set_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_get_parts(IntPtr thiz);

		/// <summary>
		/// Get all the parts from a multipart content. 
		/// </summary>
		public IEnumerable<Linphone.Content> Parts
		{
			get
			{
				return MarshalBctbxList<Linphone.Content>(linphone_content_get_parts(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_content_get_size(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_content_set_size(IntPtr thiz, long size);

		/// <summary>
		/// Get the content data buffer size, excluding null character despite null
		/// character is always set for convenience. 
		/// </summary>
		public long Size
		{
			get
			{
				return linphone_content_get_size(nativePtr);
			}
			set
			{
				linphone_content_set_size(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_get_string_buffer(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_content_set_string_buffer(IntPtr thiz, string buffer);

		/// <summary>
		/// Get the string content data buffer. 
		/// </summary>
		public string StringBuffer
		{
			get
			{
				IntPtr stringPtr = linphone_content_get_string_buffer(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_content_set_string_buffer(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_get_subtype(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_content_set_subtype(IntPtr thiz, string subtype);

		/// <summary>
		/// Get the mime subtype of the content data. 
		/// </summary>
		public string Subtype
		{
			get
			{
				IntPtr stringPtr = linphone_content_get_subtype(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_content_set_subtype(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_get_type(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_content_set_type(IntPtr thiz, string type);

		/// <summary>
		/// Get the mime type of the content data. 
		/// </summary>
		public string Type
		{
			get
			{
				IntPtr stringPtr = linphone_content_get_type(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_content_set_type(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_content_add_content_type_parameter(IntPtr thiz, string name, string val);

		/// <summary>
		/// Adds a parameter to the ContentType header. 
		/// </summary>
		public void AddContentTypeParameter(string name, string val)
		{
			linphone_content_add_content_type_parameter(nativePtr, name, val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_find_part_by_header(IntPtr thiz, string headerName, string headerValue);

		/// <summary>
		/// Find a part from a multipart content looking for a part header with a specified
		/// value. 
		/// </summary>
		public Linphone.Content FindPartByHeader(string headerName, string headerValue)
		{
			IntPtr ptr = linphone_content_find_part_by_header(nativePtr, headerName, headerValue);
			Linphone.Content returnVal = fromNativePtr<Linphone.Content>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_get_custom_header(IntPtr thiz, string headerName);

		/// <summary>
		/// Get a custom header value of a content. 
		/// </summary>
		public string GetCustomHeader(string headerName)
		{
			IntPtr stringPtr = linphone_content_get_custom_header(nativePtr, headerName);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_content_get_part(IntPtr thiz, int idx);

		/// <summary>
		/// Get a part from a multipart content according to its index. 
		/// </summary>
		public Linphone.Content GetPart(int idx)
		{
			IntPtr ptr = linphone_content_get_part(nativePtr, idx);
			Linphone.Content returnVal = fromNativePtr<Linphone.Content>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_content_set_buffer(IntPtr thiz, uint buffer, long size);

		/// <summary>
		/// Set the content data buffer, usually a string. 
		/// </summary>
		public void SetBuffer(uint buffer, long size)
		{
			linphone_content_set_buffer(nativePtr, buffer, size);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_content_set_key(IntPtr thiz, string key, long keyLength);

		/// <summary>
		/// Set the key associated with a RCS file transfer message if encrypted. 
		/// </summary>
		public void SetKey(string key, long keyLength)
		{
			linphone_content_set_key(nativePtr, key, keyLength);
			
			
			
		}
	}
	/// <summary>
	/// Linphone core main object created by function linphone_core_new . 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Core : LinphoneObject
	{
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_ms_factory(IntPtr thiz);

		public MediastreamerFactory MsFactory {
			get
			{
				IntPtr ptr = linphone_core_get_ms_factory(nativePtr);
				MediastreamerFactory factory = new MediastreamerFactory();
				factory.nativePtr = ptr;
				return factory;
			}
		}

		/// Get the native window handle of the video window.
		public string NativeVideoWindowIdString
		{
			get
			{
				return Marshal.PtrToStringUni(linphone_core_get_native_video_window_id(nativePtr));
			}
			set
			{
				IntPtr string_ptr = Marshal.StringToHGlobalUni(value);
				linphone_core_set_native_video_window_id(nativePtr, string_ptr);
				Marshal.FreeHGlobal(string_ptr);
			}
		}

		/// Get the native window handle of the video preview window.
		public string NativePreviewWindowIdString
		{
			get
			{
				return Marshal.PtrToStringUni(linphone_core_get_native_preview_window_id(nativePtr));
			}
			set
			{
				IntPtr string_ptr = Marshal.StringToHGlobalUni(value);
				linphone_core_set_native_preview_window_id(nativePtr, string_ptr);
				Marshal.FreeHGlobal(string_ptr);
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_core_cbs(IntPtr factory);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_add_callbacks(IntPtr thiz, IntPtr cbs);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_remove_callbacks(IntPtr thiz, IntPtr cbs);

		~Core() 
		{
			if (listener != null)
			{
				linphone_core_remove_callbacks(nativePtr, listener.nativePtr);
			}
		}

		private CoreListener listener;

		public CoreListener Listener
		{
			get {
				if (listener == null)
				{
					IntPtr nativeListener = linphone_factory_create_core_cbs(IntPtr.Zero);
					listener = fromNativePtr<CoreListener>(nativeListener, false);
					linphone_core_add_callbacks(nativePtr, nativeListener);
					listener.register();
				}
				return listener;
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_compress_log_collection();

		/// <summary>
		/// Compress the log collection in a single file. 
		/// </summary>
		public static string CompressLogCollection()
		{
			IntPtr stringPtr = linphone_core_compress_log_collection();
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_log_collection(int state);

		/// <summary>
		/// Enable the linphone core log collection to upload logs on a server. 
		/// </summary>
		public static void EnableLogCollection(Linphone.LogCollectionState state)
		{
			linphone_core_enable_log_collection((int)state);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_core_get_log_collection_max_file_size();

		/// <summary>
		/// Get the max file size in bytes of the files used for log collection. 
		/// </summary>
		static public long LogCollectionMaxFileSize
		{
			get
			{
				return linphone_core_get_log_collection_max_file_size();
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_log_collection_path();

		/// <summary>
		/// Get the path where the log files will be written for log collection. 
		/// </summary>
		static public string LogCollectionPath
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_log_collection_path();
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_log_collection_prefix();

		/// <summary>
		/// Get the prefix of the filenames that will be used for log collection. 
		/// </summary>
		static public string LogCollectionPrefix
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_log_collection_prefix();
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_core_get_log_level_mask();

		/// <summary>
		/// Get defined log level mask. 
		/// </summary>
		static public uint LogLevelMask
		{
			get
			{
				return linphone_core_get_log_level_mask();
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_version();

		/// <summary>
		/// Returns liblinphone's version as a string. 
		/// </summary>
		static public string Version
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_version();
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.LogCollectionState linphone_core_log_collection_enabled();

		/// <summary>
		/// Tells whether the linphone core log collection is enabled. 
		/// </summary>
		public static Linphone.LogCollectionState LogCollectionEnabled()
		{
			Linphone.LogCollectionState returnVal = linphone_core_log_collection_enabled();
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_reset_log_collection();

		/// <summary>
		/// Reset the log collection by removing the log files. 
		/// </summary>
		public static void ResetLogCollection()
		{
			linphone_core_reset_log_collection();
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_serialize_logs();

		/// <summary>
		/// Enable logs serialization (output logs from either the thread that creates the
		/// linphone core or the thread that calls <see cref="Linphone.Core.Iterate()" />). 
		/// </summary>
		public static void SerializeLogs()
		{
			linphone_core_serialize_logs();
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_log_collection_max_file_size(long size);

		/// <summary>
		/// Set the max file size in bytes of the files used for log collection. 
		/// </summary>
		public static void SetLogCollectionMaxFileSize(long size)
		{
			linphone_core_set_log_collection_max_file_size(size);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_log_collection_path(string path);

		/// <summary>
		/// Set the path of a directory where the log files will be written for log
		/// collection. 
		/// </summary>
		public static void SetLogCollectionPath(string path)
		{
			linphone_core_set_log_collection_path(path);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_log_collection_prefix(string prefix);

		/// <summary>
		/// Set the prefix of the filenames that will be used for log collection. 
		/// </summary>
		public static void SetLogCollectionPrefix(string prefix)
		{
			linphone_core_set_log_collection_prefix(prefix);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_log_level_mask(uint mask);

		/// <summary>
		/// Define the log level using mask. 
		/// </summary>
		public static void SetLogLevelMask(uint mask)
		{
			linphone_core_set_log_level_mask(mask);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_tunnel_available();

		/// <summary>
		/// True if tunnel support was compiled. 
		/// </summary>
		public static bool TunnelAvailable()
		{
			bool returnVal = linphone_core_tunnel_available() == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_upnp_available();

		/// <summary>
		/// Return the availability of uPnP. 
		/// </summary>
		public static bool UpnpAvailable()
		{
			bool returnVal = linphone_core_upnp_available() == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_vcard_supported();

		/// <summary>
		/// Tells whether VCARD support is builtin. 
		/// </summary>
		public static bool VcardSupported()
		{
			bool returnVal = linphone_core_vcard_supported() == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_adaptive_rate_algorithm(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_adaptive_rate_algorithm(IntPtr thiz, string algorithm);

		/// <summary>
		/// Returns which adaptive rate algorithm is currently configured for future calls. 
		/// </summary>
		public string AdaptiveRateAlgorithm
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_adaptive_rate_algorithm(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_adaptive_rate_algorithm(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_adaptive_rate_control_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_adaptive_rate_control(IntPtr thiz, char enabled);

		/// <summary>
		/// Returns whether adaptive rate control is enabled. 
		/// </summary>
		public bool AdaptiveRateControlEnabled
		{
			get
			{
				return linphone_core_adaptive_rate_control_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_adaptive_rate_control(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_audio_adaptive_jittcomp_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_audio_adaptive_jittcomp(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether the audio adaptive jitter compensation is enabled. 
		/// </summary>
		public bool AudioAdaptiveJittcompEnabled
		{
			get
			{
				return linphone_core_audio_adaptive_jittcomp_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_audio_adaptive_jittcomp(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_audio_devices(IntPtr thiz);

		/// <summary>
		/// Returns a list of audio devices, with only the first device for each type To
		/// have the list of all audio devices, use
		/// linphone_core_get_extended_audio_devices. 
		/// </summary>
		public IEnumerable<Linphone.AudioDevice> AudioDevices
		{
			get
			{
				return MarshalBctbxList<Linphone.AudioDevice>(linphone_core_get_audio_devices(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_audio_dscp(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_audio_dscp(IntPtr thiz, int dscp);

		/// <summary>
		/// Get the DSCP field for outgoing audio streams. 
		/// </summary>
		public int AudioDscp
		{
			get
			{
				return linphone_core_get_audio_dscp(nativePtr);
			}
			set
			{
				linphone_core_set_audio_dscp(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_audio_jittcomp(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_audio_jittcomp(IntPtr thiz, int milliseconds);

		/// <summary>
		/// Returns the nominal audio jitter buffer size in milliseconds. 
		/// </summary>
		public int AudioJittcomp
		{
			get
			{
				return linphone_core_get_audio_jittcomp(nativePtr);
			}
			set
			{
				linphone_core_set_audio_jittcomp(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_audio_multicast_addr(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_audio_multicast_addr(IntPtr thiz, string ip);

		/// <summary>
		/// Use to get multicast address to be used for audio stream. 
		/// </summary>
		public string AudioMulticastAddr
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_audio_multicast_addr(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_core_set_audio_multicast_addr(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("AudioMulticastAddr setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_audio_multicast_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_audio_multicast(IntPtr thiz, char yesno);

		/// <summary>
		/// Use to get multicast state of audio stream. 
		/// </summary>
		public bool AudioMulticastEnabled
		{
			get
			{
				return linphone_core_audio_multicast_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_audio_multicast(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_audio_multicast_ttl(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_audio_multicast_ttl(IntPtr thiz, int ttl);

		/// <summary>
		/// Use to get multicast ttl to be used for audio stream. 
		/// </summary>
		public int AudioMulticastTtl
		{
			get
			{
				return linphone_core_get_audio_multicast_ttl(nativePtr);
			}
			set
			{
				int exception_result = linphone_core_set_audio_multicast_ttl(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("AudioMulticastTtl setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_audio_payload_types(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_audio_payload_types(IntPtr thiz, IntPtr payloadTypes);

		/// <summary>
		/// Return the list of the available audio payload types. 
		/// </summary>
		public IEnumerable<Linphone.PayloadType> AudioPayloadTypes
		{
			get
			{
				return MarshalBctbxList<Linphone.PayloadType>(linphone_core_get_audio_payload_types(nativePtr));
			}
			set
			{
				linphone_core_set_audio_payload_types(nativePtr, ObjectArrayToBctbxList<Linphone.PayloadType>(value));
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_audio_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_audio_port(IntPtr thiz, int port);

		/// <summary>
		/// Gets the UDP port used for audio streaming. 
		/// </summary>
		public int AudioPort
		{
			get
			{
				return linphone_core_get_audio_port(nativePtr);
			}
			set
			{
				linphone_core_set_audio_port(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_audio_ports_range(IntPtr thiz);

		/// <summary>
		/// Get the audio port range from which is randomly chosen the UDP port used for
		/// audio streaming. 
		/// </summary>
		public Linphone.Range AudioPortsRange
		{
			get
			{
				IntPtr ptr = linphone_core_get_audio_ports_range(nativePtr);
				Linphone.Range obj = fromNativePtr<Linphone.Range>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_auth_info_list(IntPtr thiz);

		/// <summary>
		/// Returns an unmodifiable list of currently entered <see cref="Linphone.AuthInfo"
		/// />. 
		/// </summary>
		public IEnumerable<Linphone.AuthInfo> AuthInfoList
		{
			get
			{
				return MarshalBctbxList<Linphone.AuthInfo>(linphone_core_get_auth_info_list(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_auto_iterate_enabled(IntPtr thiz, char enable);

		/// <summary>
		/// Enable or disable the automatic schedule of <see cref="Linphone.Core.Iterate()"
		/// /> method on Android & iOS. 
		/// </summary>
		public bool AutoIterateEnabled
		{
			set
			{
				linphone_core_set_auto_iterate_enabled(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AVPFMode linphone_core_get_avpf_mode(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_avpf_mode(IntPtr thiz, int mode);

		/// <summary>
		/// Return AVPF enablement. 
		/// </summary>
		public Linphone.AVPFMode AvpfMode
		{
			get
			{
				return linphone_core_get_avpf_mode(nativePtr);
			}
			set
			{
				linphone_core_set_avpf_mode(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_avpf_rr_interval(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_avpf_rr_interval(IntPtr thiz, int interval);

		/// <summary>
		/// Return the avpf report interval in seconds. 
		/// </summary>
		public int AvpfRrInterval
		{
			get
			{
				return linphone_core_get_avpf_rr_interval(nativePtr);
			}
			set
			{
				linphone_core_set_avpf_rr_interval(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_call_logs(IntPtr thiz);

		/// <summary>
		/// Get the list of call logs (past calls). 
		/// </summary>
		public IEnumerable<Linphone.CallLog> CallLogs
		{
			get
			{
				return MarshalBctbxList<Linphone.CallLog>(linphone_core_get_call_logs(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_call_logs_database_path(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_call_logs_database_path(IntPtr thiz, string path);

		/// <summary>
		/// Gets the database filename where call logs will be stored. 
		/// </summary>
		public string CallLogsDatabasePath
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_call_logs_database_path(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_call_logs_database_path(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_callkit_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_callkit(IntPtr thiz, char enabled);

		/// <summary>
		/// Special function to check if the callkit is enabled, False by default. 
		/// </summary>
		public bool CallkitEnabled
		{
			get
			{
				return linphone_core_callkit_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_callkit(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_calls(IntPtr thiz);

		/// <summary>
		/// Gets the current list of calls. 
		/// </summary>
		public IEnumerable<Linphone.Call> Calls
		{
			get
			{
				return MarshalBctbxList<Linphone.Call>(linphone_core_get_calls(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_calls_nb(IntPtr thiz);

		/// <summary>
		/// Get the number of Call. 
		/// </summary>
		public int CallsNb
		{
			get
			{
				return linphone_core_get_calls_nb(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_camera_sensor_rotation(IntPtr thiz);

		/// <summary>
		/// Get the camera sensor rotation. 
		/// </summary>
		public int CameraSensorRotation
		{
			get
			{
				return linphone_core_get_camera_sensor_rotation(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_capture_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_capture_device(IntPtr thiz, string devid);

		/// <summary>
		/// Gets the name of the currently assigned sound device for capture. 
		/// </summary>
		public string CaptureDevice
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_capture_device(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_core_set_capture_device(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("CaptureDevice setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_chat_database_path(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_chat_database_path(IntPtr thiz, string path);

		/// <summary>
		/// Get path to the database file used for storing chat messages. 
		/// </summary>
		public string ChatDatabasePath
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_chat_database_path(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_chat_database_path(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_chat_enabled(IntPtr thiz);

		/// <summary>
		/// Returns whether chat is enabled. 
		/// </summary>
		public bool ChatEnabled
		{
			get
			{
				return linphone_core_chat_enabled(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_chat_rooms(IntPtr thiz);

		/// <summary>
		/// Returns an list of chat rooms. 
		/// </summary>
		public IEnumerable<Linphone.ChatRoom> ChatRooms
		{
			get
			{
				return MarshalBctbxList<Linphone.ChatRoom>(linphone_core_get_chat_rooms(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_conference(IntPtr thiz);

		/// <summary>
		/// Get a pointer on the internal conference object. 
		/// </summary>
		public Linphone.Conference Conference
		{
			get
			{
				IntPtr ptr = linphone_core_get_conference(nativePtr);
				Linphone.Conference obj = fromNativePtr<Linphone.Conference>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_core_get_conference_local_input_volume(IntPtr thiz);

		/// <summary>
		/// Get the set input volume of the local participant. 
		/// </summary>
		public float ConferenceLocalInputVolume
		{
			get
			{
				return linphone_core_get_conference_local_input_volume(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_conference_server_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_conference_server(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether the conference server feature is enabled. 
		/// </summary>
		public bool ConferenceServerEnabled
		{
			get
			{
				return linphone_core_conference_server_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_conference_server(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_conference_size(IntPtr thiz);

		/// <summary>
		/// Get the number of participant in the running conference. 
		/// </summary>
		public int ConferenceSize
		{
			get
			{
				return linphone_core_get_conference_size(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_config(IntPtr thiz);

		/// <summary>
		/// Returns the LpConfig object used to manage the storage (config) file. 
		/// </summary>
		public Linphone.Config Config
		{
			get
			{
				IntPtr ptr = linphone_core_get_config(nativePtr);
				Linphone.Config obj = fromNativePtr<Linphone.Config>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ConsolidatedPresence linphone_core_get_consolidated_presence(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_consolidated_presence(IntPtr thiz, int presence);

		/// <summary>
		/// Get my consolidated presence. 
		/// </summary>
		public Linphone.ConsolidatedPresence ConsolidatedPresence
		{
			get
			{
				return linphone_core_get_consolidated_presence(nativePtr);
			}
			set
			{
				linphone_core_set_consolidated_presence(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_current_call(IntPtr thiz);

		/// <summary>
		/// Gets the current call. 
		/// </summary>
		public Linphone.Call CurrentCall
		{
			get
			{
				IntPtr ptr = linphone_core_get_current_call(nativePtr);
				Linphone.Call obj = fromNativePtr<Linphone.Call>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_current_call_remote_address(IntPtr thiz);

		/// <summary>
		/// Get the remote address of the current call. 
		/// </summary>
		public Linphone.Address CurrentCallRemoteAddress
		{
			get
			{
				IntPtr ptr = linphone_core_get_current_call_remote_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_current_callbacks(IntPtr thiz);

		/// <summary>
		/// Gets the current LinphoneCoreCbs. 
		/// </summary>
		public Linphone.CoreListener CurrentCallbacks
		{
			get
			{
				IntPtr ptr = linphone_core_get_current_callbacks(nativePtr);
				Linphone.CoreListener obj = fromNativePtr<Linphone.CoreListener>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_current_preview_video_definition(IntPtr thiz);

		/// <summary>
		/// Get the effective video definition provided by the camera for the captured
		/// video. 
		/// </summary>
		public Linphone.VideoDefinition CurrentPreviewVideoDefinition
		{
			get
			{
				IntPtr ptr = linphone_core_get_current_preview_video_definition(nativePtr);
				Linphone.VideoDefinition obj = fromNativePtr<Linphone.VideoDefinition>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_default_friend_list(IntPtr thiz);

		/// <summary>
		/// Retrieves the first list of <see cref="Linphone.Friend" /> from the core. 
		/// </summary>
		public Linphone.FriendList DefaultFriendList
		{
			get
			{
				IntPtr ptr = linphone_core_get_default_friend_list(nativePtr);
				Linphone.FriendList obj = fromNativePtr<Linphone.FriendList>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_default_input_audio_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_default_input_audio_device(IntPtr thiz, IntPtr audioDevice);

		/// <summary>
		/// Gets the default input audio device. 
		/// </summary>
		public Linphone.AudioDevice DefaultInputAudioDevice
		{
			get
			{
				IntPtr ptr = linphone_core_get_default_input_audio_device(nativePtr);
				Linphone.AudioDevice obj = fromNativePtr<Linphone.AudioDevice>(ptr, true);
				return obj;
			}
			set
			{
				linphone_core_set_default_input_audio_device(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_default_output_audio_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_default_output_audio_device(IntPtr thiz, IntPtr audioDevice);

		/// <summary>
		/// Gets the default output audio device. 
		/// </summary>
		public Linphone.AudioDevice DefaultOutputAudioDevice
		{
			get
			{
				IntPtr ptr = linphone_core_get_default_output_audio_device(nativePtr);
				Linphone.AudioDevice obj = fromNativePtr<Linphone.AudioDevice>(ptr, true);
				return obj;
			}
			set
			{
				linphone_core_set_default_output_audio_device(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_default_proxy_config(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_default_proxy_config(IntPtr thiz, IntPtr config);

		public Linphone.ProxyConfig DefaultProxyConfig
		{
			get
			{
				IntPtr ptr = linphone_core_get_default_proxy_config(nativePtr);
				Linphone.ProxyConfig obj = fromNativePtr<Linphone.ProxyConfig>(ptr, true);
				return obj;
			}
			set
			{
				linphone_core_set_default_proxy_config(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_default_video_display_filter(IntPtr thiz);

		/// <summary>
		/// Get the name of the default mediastreamer2 filter used for rendering video on
		/// the current platform. 
		/// </summary>
		public string DefaultVideoDisplayFilter
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_default_video_display_filter(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_delayed_timeout(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_delayed_timeout(IntPtr thiz, int seconds);

		/// <summary>
		/// Gets the delayed timeout See <see cref="Linphone.Core.SetDelayedTimeout()" />
		/// for details. 
		/// </summary>
		public int DelayedTimeout
		{
			get
			{
				return linphone_core_get_delayed_timeout(nativePtr);
			}
			set
			{
				linphone_core_set_delayed_timeout(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_device_rotation(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_device_rotation(IntPtr thiz, int rotation);

		/// <summary>
		/// Gets the current device orientation. 
		/// </summary>
		public int DeviceRotation
		{
			get
			{
				return linphone_core_get_device_rotation(nativePtr);
			}
			set
			{
				linphone_core_set_device_rotation(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_dns_search_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_dns_search(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether DNS search (use of local domain if the fully qualified name did
		/// return results) is enabled. 
		/// </summary>
		public bool DnsSearchEnabled
		{
			get
			{
				return linphone_core_dns_search_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_dns_search(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_dns_servers(IntPtr thiz, IntPtr servers);

		/// <summary>
		/// Forces liblinphone to use the supplied list of dns servers, instead of system's
		/// ones. 
		/// </summary>
		public IEnumerable<string> DnsServers
		{
			set
			{
				linphone_core_set_dns_servers(nativePtr, StringArrayToBctbxList(value));
				CleanStringArrayPtrs();
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_dns_servers_app(IntPtr thiz, IntPtr servers);

		/// <summary>
		/// Forces liblinphone to use the supplied list of dns servers, instead of system's
		/// ones and set dns_set_by_app at true or false according to value of servers
		/// list. 
		/// </summary>
		public IEnumerable<string> DnsServersApp
		{
			set
			{
				linphone_core_set_dns_servers_app(nativePtr, StringArrayToBctbxList(value));
				CleanStringArrayPtrs();
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_get_dns_set_by_app(IntPtr thiz);

		/// <summary>
		/// Tells if the DNS was set by an application. 
		/// </summary>
		public bool DnsSetByApp
		{
			get
			{
				return linphone_core_get_dns_set_by_app(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_dns_srv_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_dns_srv(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether DNS SRV resolution is enabled. 
		/// </summary>
		public bool DnsSrvEnabled
		{
			get
			{
				return linphone_core_dns_srv_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_dns_srv(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_download_bandwidth(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_download_bandwidth(IntPtr thiz, int bw);

		/// <summary>
		/// Retrieve the maximum available download bandwidth. 
		/// </summary>
		public int DownloadBandwidth
		{
			get
			{
				return linphone_core_get_download_bandwidth(nativePtr);
			}
			set
			{
				linphone_core_set_download_bandwidth(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_download_ptime(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_download_ptime(IntPtr thiz, int ptime);

		/// <summary>
		/// Get audio packetization time linphone expects to receive from peer. 
		/// </summary>
		public int DownloadPtime
		{
			get
			{
				return linphone_core_get_download_ptime(nativePtr);
			}
			set
			{
				linphone_core_set_download_ptime(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_echo_cancellation_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_echo_cancellation(IntPtr thiz, char val);

		/// <summary>
		/// Returns true if echo cancellation is enabled. 
		/// </summary>
		public bool EchoCancellationEnabled
		{
			get
			{
				return linphone_core_echo_cancellation_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_echo_cancellation(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_echo_canceller_filter_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_echo_canceller_filter_name(IntPtr thiz, string filtername);

		/// <summary>
		/// Get the name of the mediastreamer2 filter used for echo cancelling. 
		/// </summary>
		public string EchoCancellerFilterName
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_echo_canceller_filter_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_echo_canceller_filter_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_echo_limiter_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_echo_limiter(IntPtr thiz, char val);

		/// <summary>
		/// Tells whether echo limiter is enabled. 
		/// </summary>
		public bool EchoLimiterEnabled
		{
			get
			{
				return linphone_core_echo_limiter_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_echo_limiter(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_enable_sip_update(IntPtr thiz, int val);

		/// <summary>
		/// Enable or disable the UPDATE method support. 
		/// </summary>
		public int EnableSipUpdate
		{
			set
			{
				linphone_core_set_enable_sip_update(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_expected_bandwidth(IntPtr thiz, int bw);

		/// <summary>
		/// Sets expected available upload bandwidth This is IP bandwidth, in kbit/s. 
		/// </summary>
		public int ExpectedBandwidth
		{
			set
			{
				linphone_core_set_expected_bandwidth(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_extended_audio_devices(IntPtr thiz);

		/// <summary>
		/// Returns the list of all audio devices. 
		/// </summary>
		public IEnumerable<Linphone.AudioDevice> ExtendedAudioDevices
		{
			get
			{
				return MarshalBctbxList<Linphone.AudioDevice>(linphone_core_get_extended_audio_devices(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_file_transfer_server(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_file_transfer_server(IntPtr thiz, string serverUrl);

		/// <summary>
		/// Get the globaly set http file transfer server to be used for content type
		/// application/vnd.gsma.rcs-ft-http+xml. 
		/// </summary>
		public string FileTransferServer
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_file_transfer_server(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_file_transfer_server(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_friend_list_subscription(IntPtr thiz, char enable);

		/// <summary>
		/// Sets whether or not to start friend lists subscription when in foreground. 
		/// </summary>
		public bool FriendListSubscriptionEnabled
		{
			set
			{
				linphone_core_enable_friend_list_subscription(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_friends_database_path(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_friends_database_path(IntPtr thiz, string path);

		/// <summary>
		/// Gets the database filename where friends will be stored. 
		/// </summary>
		public string FriendsDatabasePath
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_friends_database_path(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_friends_database_path(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_friends_lists(IntPtr thiz);

		/// <summary>
		/// Retrieves the list of <see cref="Linphone.FriendList" /> from the core. 
		/// </summary>
		public IEnumerable<Linphone.FriendList> FriendsLists
		{
			get
			{
				return MarshalBctbxList<Linphone.FriendList>(linphone_core_get_friends_lists(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_get_guess_hostname(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_guess_hostname(IntPtr thiz, char val);

		/// <summary>
		/// Returns true if hostname part of primary contact is guessed automatically. 
		/// </summary>
		public bool GuessHostname
		{
			get
			{
				return linphone_core_get_guess_hostname(nativePtr) != 0;
			}
			set
			{
				linphone_core_set_guess_hostname(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_http_proxy_host(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_http_proxy_host(IntPtr thiz, string host);

		/// <summary>
		/// Get http proxy address to be used for signaling. 
		/// </summary>
		public string HttpProxyHost
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_http_proxy_host(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_http_proxy_host(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_http_proxy_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_http_proxy_port(IntPtr thiz, int port);

		/// <summary>
		/// Get http proxy port to be used for signaling. 
		/// </summary>
		public int HttpProxyPort
		{
			get
			{
				return linphone_core_get_http_proxy_port(nativePtr);
			}
			set
			{
				linphone_core_set_http_proxy_port(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_identity(IntPtr thiz);

		/// <summary>
		/// Gets the default identity SIP address. 
		/// </summary>
		public string Identity
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_identity(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_im_notif_policy(IntPtr thiz);

		/// <summary>
		/// Get the <see cref="Linphone.ImNotifPolicy" /> object controlling the instant
		/// messaging notifications. 
		/// </summary>
		public Linphone.ImNotifPolicy ImNotifPolicy
		{
			get
			{
				IntPtr ptr = linphone_core_get_im_notif_policy(nativePtr);
				Linphone.ImNotifPolicy obj = fromNativePtr<Linphone.ImNotifPolicy>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_in_call_timeout(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_in_call_timeout(IntPtr thiz, int seconds);

		/// <summary>
		/// Gets the in call timeout See <see cref="Linphone.Core.SetInCallTimeout()" />
		/// for details. 
		/// </summary>
		public int InCallTimeout
		{
			get
			{
				return linphone_core_get_in_call_timeout(nativePtr);
			}
			set
			{
				linphone_core_set_in_call_timeout(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_inc_timeout(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_inc_timeout(IntPtr thiz, int seconds);

		/// <summary>
		/// Returns the incoming call timeout See <see cref="Linphone.Core.SetIncTimeout()"
		/// /> for details. 
		/// </summary>
		public int IncTimeout
		{
			get
			{
				return linphone_core_get_inc_timeout(nativePtr);
			}
			set
			{
				linphone_core_set_inc_timeout(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_input_audio_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_input_audio_device(IntPtr thiz, IntPtr audioDevice);

		/// <summary>
		/// Gets the input audio device for the current call. 
		/// </summary>
		public Linphone.AudioDevice InputAudioDevice
		{
			get
			{
				IntPtr ptr = linphone_core_get_input_audio_device(nativePtr);
				Linphone.AudioDevice obj = fromNativePtr<Linphone.AudioDevice>(ptr, true);
				return obj;
			}
			set
			{
				linphone_core_set_input_audio_device(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_ipv6_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_ipv6(IntPtr thiz, char val);

		/// <summary>
		/// Tells whether IPv6 is enabled or not. 
		/// </summary>
		public bool Ipv6Enabled
		{
			get
			{
				return linphone_core_ipv6_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_ipv6(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_auto_iterate_enabled(IntPtr thiz);

		/// <summary>
		/// Gets whether auto iterate is enabled or not (Android & iOS only). 
		/// </summary>
		public bool IsAutoIterateEnabled
		{
			get
			{
				return linphone_core_is_auto_iterate_enabled(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_echo_canceller_calibration_required(IntPtr thiz);

		/// <summary>
		/// Check whether the device is echo canceller calibration is required. 
		/// </summary>
		public bool IsEchoCancellerCalibrationRequired
		{
			get
			{
				return linphone_core_is_echo_canceller_calibration_required(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_friend_list_subscription_enabled(IntPtr thiz);

		/// <summary>
		/// Returns whether or not friend lists subscription are enabled. 
		/// </summary>
		public bool IsFriendListSubscriptionEnabled
		{
			get
			{
				return linphone_core_is_friend_list_subscription_enabled(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_in_conference(IntPtr thiz);

		/// <summary>
		/// Indicates whether the local participant is part of a conference. 
		/// </summary>
		public bool IsInConference
		{
			get
			{
				return linphone_core_is_in_conference(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_incoming_invite_pending(IntPtr thiz);

		/// <summary>
		/// Tells whether there is an incoming invite pending. 
		/// </summary>
		public bool IsIncomingInvitePending
		{
			get
			{
				return linphone_core_is_incoming_invite_pending(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_media_encryption_mandatory(IntPtr thiz);

		/// <summary>
		/// Check if the configured media encryption is mandatory or not. 
		/// </summary>
		public bool IsMediaEncryptionMandatory
		{
			get
			{
				return linphone_core_is_media_encryption_mandatory(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_native_ringing_enabled(IntPtr thiz);

		/// <summary>
		/// Returns whether the native ringing is enabled or not. 
		/// </summary>
		public bool IsNativeRingingEnabled
		{
			get
			{
				return linphone_core_is_native_ringing_enabled(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_network_reachable(IntPtr thiz);

		/// <summary>
		/// return network state either as positioned by the application or by linphone
		/// itself. 
		/// </summary>
		public bool IsNetworkReachable
		{
			get
			{
				return linphone_core_is_network_reachable(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_push_notification_enabled(IntPtr thiz);

		/// <summary>
		/// Gets whether push notifications are enabled or not (Android & iOS only). 
		/// </summary>
		public bool IsPushNotificationEnabled
		{
			get
			{
				return linphone_core_is_push_notification_enabled(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_sender_name_hidden_in_forward_message(IntPtr thiz);

		/// <summary>
		/// Returns whether or not sender name is hidden in forward message. 
		/// </summary>
		public bool IsSenderNameHiddenInForwardMessage
		{
			get
			{
				return linphone_core_is_sender_name_hidden_in_forward_message(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_keep_alive_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_keep_alive(IntPtr thiz, char enable);

		/// <summary>
		/// Is signaling keep alive enabled. 
		/// </summary>
		public bool KeepAliveEnabled
		{
			get
			{
				return linphone_core_keep_alive_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_keep_alive(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_last_outgoing_call_log(IntPtr thiz);

		/// <summary>
		/// Get the latest outgoing call log. 
		/// </summary>
		public Linphone.CallLog LastOutgoingCallLog
		{
			get
			{
				IntPtr ptr = linphone_core_get_last_outgoing_call_log(nativePtr);
				Linphone.CallLog obj = fromNativePtr<Linphone.CallLog>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_lime_x3dh_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_lime_x3dh(IntPtr thiz, char enable);

		/// <summary>
		/// Tells wether LIME X3DH is enabled or not. 
		/// </summary>
		public bool LimeX3DhEnabled
		{
			get
			{
				return linphone_core_lime_x3dh_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_lime_x3dh(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_lime_x3dh_server_url(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_lime_x3dh_server_url(IntPtr thiz, string url);

		/// <summary>
		/// Get the x3dh server url. 
		/// </summary>
		public string LimeX3DhServerUrl
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_lime_x3dh_server_url(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_lime_x3dh_server_url(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_linphone_specs(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_linphone_specs(IntPtr thiz, string specs);

		/// <summary>
		/// Get the linphone specs value telling what functionalities the linphone client
		/// supports. 
		/// </summary>
		public string LinphoneSpecs
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_linphone_specs(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_linphone_specs(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_linphone_specs_list(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_linphone_specs_list(IntPtr thiz, IntPtr specs);

		/// <summary>
		/// Get the list of linphone specs string values representing what functionalities
		/// the linphone client supports. 
		/// </summary>
		public IEnumerable<string> LinphoneSpecsList
		{
			get
			{
				return MarshalStringArray(linphone_core_get_linphone_specs_list(nativePtr));
			}
			set
			{
				linphone_core_set_linphone_specs_list(nativePtr, StringArrayToBctbxList(value));
				CleanStringArrayPtrs();
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_log_collection_upload_server_url(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_log_collection_upload_server_url(IntPtr thiz, string serverUrl);

		/// <summary>
		/// Gets the url of the server where to upload the collected log files. 
		/// </summary>
		public string LogCollectionUploadServerUrl
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_log_collection_upload_server_url(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_log_collection_upload_server_url(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_max_calls(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_max_calls(IntPtr thiz, int max);

		/// <summary>
		/// Get the maximum number of simultaneous calls Linphone core can manage at a
		/// time. 
		/// </summary>
		public int MaxCalls
		{
			get
			{
				return linphone_core_get_max_calls(nativePtr);
			}
			set
			{
				linphone_core_set_max_calls(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_max_size_for_auto_download_incoming_files(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_max_size_for_auto_download_incoming_files(IntPtr thiz, int size);

		/// <summary>
		/// Gets the size under which incoming files in chat messages will be downloaded
		/// automatically. 
		/// </summary>
		public int MaxSizeForAutoDownloadIncomingFiles
		{
			get
			{
				return linphone_core_get_max_size_for_auto_download_incoming_files(nativePtr);
			}
			set
			{
				linphone_core_set_max_size_for_auto_download_incoming_files(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_media_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_media_device(IntPtr thiz, string devid);

		/// <summary>
		/// Gets the name of the currently assigned sound device for media. 
		/// </summary>
		public string MediaDevice
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_media_device(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_core_set_media_device(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("MediaDevice setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.MediaEncryption linphone_core_get_media_encryption(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_media_encryption(IntPtr thiz, int menc);

		/// <summary>
		/// Get the media encryption policy being used for RTP packets. 
		/// </summary>
		public Linphone.MediaEncryption MediaEncryption
		{
			get
			{
				return linphone_core_get_media_encryption(nativePtr);
			}
			set
			{
				int exception_result = linphone_core_set_media_encryption(nativePtr, (int)value);
				if (exception_result != 0) throw new LinphoneException("MediaEncryption setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_media_encryption_mandatory(IntPtr thiz, char m);

		/// <summary>
		/// Define whether the configured media encryption is mandatory, if it is and the
		/// negotation cannot result in the desired media encryption then the call will
		/// fail. 
		/// </summary>
		public bool MediaEncryptionMandatory
		{
			set
			{
				linphone_core_set_media_encryption_mandatory(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_media_network_reachable(IntPtr thiz, char val);

		/// <summary>
		/// This method is called by the application to notify the linphone core library
		/// when the media (RTP) network is reachable. 
		/// </summary>
		public bool MediaNetworkReachable
		{
			set
			{
				linphone_core_set_media_network_reachable(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_mic_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_mic(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether the microphone is enabled. 
		/// </summary>
		public bool MicEnabled
		{
			get
			{
				return linphone_core_mic_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_mic(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_core_get_mic_gain_db(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_mic_gain_db(IntPtr thiz, float level);

		/// <summary>
		/// Get microphone gain in db. 
		/// </summary>
		public float MicGainDb
		{
			get
			{
				return linphone_core_get_mic_gain_db(nativePtr);
			}
			set
			{
				linphone_core_set_mic_gain_db(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_missed_calls_count(IntPtr thiz);

		/// <summary>
		/// Get the number of missed calls. 
		/// </summary>
		public int MissedCallsCount
		{
			get
			{
				return linphone_core_get_missed_calls_count(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_mtu(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_mtu(IntPtr thiz, int mtu);

		/// <summary>
		/// Returns the maximum transmission unit size in bytes. 
		/// </summary>
		public int Mtu
		{
			get
			{
				return linphone_core_get_mtu(nativePtr);
			}
			set
			{
				linphone_core_set_mtu(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_nat_address(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_nat_address(IntPtr thiz, string addr);

		/// <summary>
		/// Get the public IP address of NAT being used. 
		/// </summary>
		public string NatAddress
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_nat_address(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_nat_address(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_nat_policy(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_nat_policy(IntPtr thiz, IntPtr policy);

		/// <summary>
		/// Get The policy that is used to pass through NATs/firewalls. 
		/// </summary>
		public Linphone.NatPolicy NatPolicy
		{
			get
			{
				IntPtr ptr = linphone_core_get_nat_policy(nativePtr);
				Linphone.NatPolicy obj = fromNativePtr<Linphone.NatPolicy>(ptr, true);
				return obj;
			}
			set
			{
				linphone_core_set_nat_policy(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_native_preview_window_id(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_native_preview_window_id(IntPtr thiz, IntPtr id);

		/// <summary>
		/// Get the native window handle of the video preview window. 
		/// </summary>
		public IntPtr NativePreviewWindowId
		{
			get
			{
				return linphone_core_get_native_preview_window_id(nativePtr);
			}
			set
			{
				linphone_core_set_native_preview_window_id(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_native_ringing_enabled(IntPtr thiz, char enable);

		/// <summary>
		/// Sets whether to use the native ringing (Android only). 
		/// </summary>
		public bool NativeRingingEnabled
		{
			set
			{
				linphone_core_set_native_ringing_enabled(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_native_video_window_id(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_native_video_window_id(IntPtr thiz, IntPtr id);

		/// <summary>
		/// Get the native window handle of the video window. 
		/// </summary>
		public IntPtr NativeVideoWindowId
		{
			get
			{
				return linphone_core_get_native_video_window_id(nativePtr);
			}
			set
			{
				linphone_core_set_native_video_window_id(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_network_reachable(IntPtr thiz, char val);

		/// <summary>
		/// This method is called by the application to notify the linphone core library
		/// when network is reachable. 
		/// </summary>
		public bool NetworkReachable
		{
			set
			{
				linphone_core_set_network_reachable(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_nortp_timeout(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_nortp_timeout(IntPtr thiz, int seconds);

		/// <summary>
		/// Gets the value of the no-rtp timeout. 
		/// </summary>
		public int NortpTimeout
		{
			get
			{
				return linphone_core_get_nortp_timeout(nativePtr);
			}
			set
			{
				linphone_core_set_nortp_timeout(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_output_audio_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_output_audio_device(IntPtr thiz, IntPtr audioDevice);

		/// <summary>
		/// Gets the output audio device for the current call. 
		/// </summary>
		public Linphone.AudioDevice OutputAudioDevice
		{
			get
			{
				IntPtr ptr = linphone_core_get_output_audio_device(nativePtr);
				Linphone.AudioDevice obj = fromNativePtr<Linphone.AudioDevice>(ptr, true);
				return obj;
			}
			set
			{
				linphone_core_set_output_audio_device(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_play_file(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_play_file(IntPtr thiz, string file);

		/// <summary>
		/// Get the wav file that is played when putting somebody on hold, or when files
		/// are used instead of soundcards (see <see cref="Linphone.Core.SetUseFiles()"
		/// />). 
		/// </summary>
		public string PlayFile
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_play_file(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_play_file(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_playback_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_playback_device(IntPtr thiz, string devid);

		/// <summary>
		/// Gets the name of the currently assigned sound device for playback. 
		/// </summary>
		public string PlaybackDevice
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_playback_device(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_core_set_playback_device(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("PlaybackDevice setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_core_get_playback_gain_db(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_playback_gain_db(IntPtr thiz, float level);

		/// <summary>
		/// Get playback gain in db before entering sound card. 
		/// </summary>
		public float PlaybackGainDb
		{
			get
			{
				return linphone_core_get_playback_gain_db(nativePtr);
			}
			set
			{
				linphone_core_set_playback_gain_db(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_core_get_preferred_framerate(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_preferred_framerate(IntPtr thiz, float fps);

		/// <summary>
		/// Returns the preferred video framerate, previously set by <see
		/// cref="Linphone.Core.SetPreferredFramerate()" />. 
		/// </summary>
		public float PreferredFramerate
		{
			get
			{
				return linphone_core_get_preferred_framerate(nativePtr);
			}
			set
			{
				linphone_core_set_preferred_framerate(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_preferred_video_definition(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_preferred_video_definition(IntPtr thiz, IntPtr vdef);

		/// <summary>
		/// Get the preferred video definition for the stream that is captured and sent to
		/// the remote party. 
		/// </summary>
		public Linphone.VideoDefinition PreferredVideoDefinition
		{
			get
			{
				IntPtr ptr = linphone_core_get_preferred_video_definition(nativePtr);
				Linphone.VideoDefinition obj = fromNativePtr<Linphone.VideoDefinition>(ptr, true);
				return obj;
			}
			set
			{
				linphone_core_set_preferred_video_definition(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_preferred_video_size_by_name(IntPtr thiz, string name);

		/// <summary>
		/// Sets the preferred video size by its name. 
		/// </summary>
		public string PreferredVideoSizeByName
		{
			set
			{
				linphone_core_set_preferred_video_size_by_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_presence_model(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_presence_model(IntPtr thiz, IntPtr presence);

		/// <summary>
		/// Get my presence model. 
		/// </summary>
		public Linphone.PresenceModel PresenceModel
		{
			get
			{
				IntPtr ptr = linphone_core_get_presence_model(nativePtr);
				Linphone.PresenceModel obj = fromNativePtr<Linphone.PresenceModel>(ptr, true);
				return obj;
			}
			set
			{
				linphone_core_set_presence_model(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_preview_video_definition(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_preview_video_definition(IntPtr thiz, IntPtr vdef);

		/// <summary>
		/// Get the definition of the captured video. 
		/// </summary>
		public Linphone.VideoDefinition PreviewVideoDefinition
		{
			get
			{
				IntPtr ptr = linphone_core_get_preview_video_definition(nativePtr);
				Linphone.VideoDefinition obj = fromNativePtr<Linphone.VideoDefinition>(ptr, true);
				return obj;
			}
			set
			{
				linphone_core_set_preview_video_definition(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_preview_video_size_by_name(IntPtr thiz, string name);

		/// <summary>
		/// Sets the preview video size by its name. 
		/// </summary>
		public string PreviewVideoSizeByName
		{
			set
			{
				linphone_core_set_preview_video_size_by_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_primary_contact(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_primary_contact(IntPtr thiz, string contact);

		/// <summary>
		/// Returns the default identity when no proxy configuration is used. 
		/// </summary>
		public string PrimaryContact
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_primary_contact(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_core_set_primary_contact(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("PrimaryContact setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_primary_contact_parsed(IntPtr thiz);

		/// <summary>
		/// Same as <see cref="Linphone.Core.GetPrimaryContact()" /> but the result is a
		/// <see cref="Linphone.Address" /> object instead of const char *. 
		/// </summary>
		public Linphone.Address PrimaryContactParsed
		{
			get
			{
				IntPtr ptr = linphone_core_get_primary_contact_parsed(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_provisioning_uri(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_provisioning_uri(IntPtr thiz, string uri);

		/// <summary>
		/// Get provisioning URI. 
		/// </summary>
		public string ProvisioningUri
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_provisioning_uri(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_core_set_provisioning_uri(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("ProvisioningUri setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_proxy_config_list(IntPtr thiz);

		/// <summary>
		/// Returns an unmodifiable list of entered proxy configurations. 
		/// </summary>
		public IEnumerable<Linphone.ProxyConfig> ProxyConfigList
		{
			get
			{
				return MarshalBctbxList<Linphone.ProxyConfig>(linphone_core_get_proxy_config_list(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_push_notification_enabled(IntPtr thiz, char enable);

		/// <summary>
		/// Enable or disable push notifications on Android & iOS. 
		/// </summary>
		public bool PushNotificationEnabled
		{
			set
			{
				linphone_core_set_push_notification_enabled(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_qrcode_video_preview_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_qrcode_video_preview(IntPtr thiz, char val);

		/// <summary>
		/// Tells whether QRCode is enabled in the preview. 
		/// </summary>
		public bool QrcodeVideoPreviewEnabled
		{
			get
			{
				return linphone_core_qrcode_video_preview_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_qrcode_video_preview(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_realtime_text_enabled(IntPtr thiz);

		/// <summary>
		/// Gets if realtime text is enabled or not. 
		/// </summary>
		public bool RealtimeTextEnabled
		{
			get
			{
				return linphone_core_realtime_text_enabled(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_record_file(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_record_file(IntPtr thiz, string file);

		/// <summary>
		/// Get the wav file where incoming stream is recorded, when files are used instead
		/// of soundcards (see <see cref="Linphone.Core.SetUseFiles()" />). 
		/// </summary>
		public string RecordFile
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_record_file(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_record_file(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_remote_ringback_tone(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_remote_ringback_tone(IntPtr thiz, string ring);

		/// <summary>
		/// Get the ring back tone played to far end during incoming calls. 
		/// </summary>
		public string RemoteRingbackTone
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_remote_ringback_tone(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_remote_ringback_tone(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_retransmission_on_nack_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_retransmission_on_nack(IntPtr thiz, char val);

		/// <summary>
		/// Tells whether NACK context is enabled or not. 
		/// </summary>
		public bool RetransmissionOnNackEnabled
		{
			get
			{
				return linphone_core_retransmission_on_nack_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_retransmission_on_nack(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_ring(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_ring(IntPtr thiz, string path);

		/// <summary>
		/// Returns the path to the wav file used for ringing. 
		/// </summary>
		public string Ring
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_ring(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_ring(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_get_ring_during_incoming_early_media(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_ring_during_incoming_early_media(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether the ring play is enabled during an incoming early media call. 
		/// </summary>
		public bool RingDuringIncomingEarlyMedia
		{
			get
			{
				return linphone_core_get_ring_during_incoming_early_media(nativePtr) != 0;
			}
			set
			{
				linphone_core_set_ring_during_incoming_early_media(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_ringback(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_ringback(IntPtr thiz, string path);

		/// <summary>
		/// Returns the path to the wav file used for ringing back. 
		/// </summary>
		public string Ringback
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_ringback(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_ringback(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_ringer_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_ringer_device(IntPtr thiz, string devid);

		/// <summary>
		/// Gets the name of the currently assigned sound device for ringing. 
		/// </summary>
		public string RingerDevice
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_ringer_device(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_core_set_ringer_device(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("RingerDevice setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_root_ca(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_root_ca(IntPtr thiz, string path);

		/// <summary>
		/// Gets the path to a file or folder containing the trusted root CAs (PEM format) 
		/// </summary>
		public string RootCa
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_root_ca(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_root_ca(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_root_ca_data(IntPtr thiz, string data);

		/// <summary>
		/// Sets the trusted root CAs (PEM format) 
		/// </summary>
		public string RootCaData
		{
			set
			{
				linphone_core_set_root_ca_data(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_rtp_bundle_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_rtp_bundle(IntPtr thiz, char val);

		/// <summary>
		/// Returns whether RTP bundle mode (also known as Media Multiplexing) is enabled. 
		/// </summary>
		public bool RtpBundleEnabled
		{
			get
			{
				return linphone_core_rtp_bundle_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_rtp_bundle(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_sdp_200_ack_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_sdp_200_ack(IntPtr thiz, char enable);

		/// <summary>
		/// Media offer control param for SIP INVITE. 
		/// </summary>
		public bool Sdp200AckEnabled
		{
			get
			{
				return linphone_core_sdp_200_ack_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_sdp_200_ack(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_self_view_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_self_view(IntPtr thiz, char val);

		/// <summary>
		/// Tells whether video self view during call is enabled or not. 
		/// </summary>
		public bool SelfViewEnabled
		{
			get
			{
				return linphone_core_self_view_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_self_view(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_sender_name_hidden_in_forward_message(IntPtr thiz, char enable);

		/// <summary>
		/// Enable whether or not to hide sender name in forward message. 
		/// </summary>
		public bool SenderNameHiddenInForwardMessageEnabled
		{
			set
			{
				linphone_core_enable_sender_name_hidden_in_forward_message(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_get_session_expires_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_session_expires_enabled(IntPtr thiz, char enabled);

		/// <summary>
		/// Check if the Session Timers feature is enabled. 
		/// </summary>
		public bool SessionExpiresEnabled
		{
			get
			{
				return linphone_core_get_session_expires_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_set_session_expires_enabled(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_session_expires_min_value(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_session_expires_min_value(IntPtr thiz, int min);

		/// <summary>
		/// Returns the session expires min value, 90 by default. 
		/// </summary>
		public int SessionExpiresMinValue
		{
			get
			{
				return linphone_core_get_session_expires_min_value(nativePtr);
			}
			set
			{
				linphone_core_set_session_expires_min_value(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.SessionExpiresRefresher linphone_core_get_session_expires_refresher_value(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_session_expires_refresher_value(IntPtr thiz, int refresher);

		/// <summary>
		/// Returns the session expires refresher value. 
		/// </summary>
		public Linphone.SessionExpiresRefresher SessionExpiresRefresherValue
		{
			get
			{
				return linphone_core_get_session_expires_refresher_value(nativePtr);
			}
			set
			{
				linphone_core_set_session_expires_refresher_value(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_session_expires_value(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_session_expires_value(IntPtr thiz, int expires);

		/// <summary>
		/// Returns the session expires value. 
		/// </summary>
		public int SessionExpiresValue
		{
			get
			{
				return linphone_core_get_session_expires_value(nativePtr);
			}
			set
			{
				linphone_core_set_session_expires_value(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_sip_dscp(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_sip_dscp(IntPtr thiz, int dscp);

		/// <summary>
		/// Get the DSCP field for SIP signaling channel. 
		/// </summary>
		public int SipDscp
		{
			get
			{
				return linphone_core_get_sip_dscp(nativePtr);
			}
			set
			{
				linphone_core_set_sip_dscp(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_sip_network_reachable(IntPtr thiz, char val);

		/// <summary>
		/// This method is called by the application to notify the linphone core library
		/// when the SIP network is reachable. 
		/// </summary>
		public bool SipNetworkReachable
		{
			set
			{
				linphone_core_set_sip_network_reachable(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_sip_transport_timeout(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_sip_transport_timeout(IntPtr thiz, int timeoutMs);

		/// <summary>
		/// Get the SIP transport timeout. 
		/// </summary>
		public int SipTransportTimeout
		{
			get
			{
				return linphone_core_get_sip_transport_timeout(nativePtr);
			}
			set
			{
				linphone_core_set_sip_transport_timeout(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_sound_devices_list(IntPtr thiz);

		/// <summary>
		/// Gets the list of the available sound devices. 
		/// </summary>
		public IEnumerable<string> SoundDevicesList
		{
			get
			{
				return MarshalStringArray(linphone_core_get_sound_devices_list(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_static_picture(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_static_picture(IntPtr thiz, string path);

		/// <summary>
		/// Get the path to the image file streamed when "Static picture" is set as the
		/// video device. 
		/// </summary>
		public string StaticPicture
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_static_picture(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_core_set_static_picture(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("StaticPicture setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_core_get_static_picture_fps(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_static_picture_fps(IntPtr thiz, float fps);

		/// <summary>
		/// Get the frame rate for static picture. 
		/// </summary>
		public float StaticPictureFps
		{
			get
			{
				return linphone_core_get_static_picture_fps(nativePtr);
			}
			set
			{
				int exception_result = linphone_core_set_static_picture_fps(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("StaticPictureFps setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_stun_server(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_stun_server(IntPtr thiz, string server);

		/// <summary>
		/// Get the STUN server address being used. 
		/// </summary>
		public string StunServer
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_stun_server(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_stun_server(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_supported_file_formats_list(IntPtr thiz);

		/// <summary>
		/// Returns a null terminated table of strings containing the file format extension
		/// supported for call recording. 
		/// </summary>
		public IEnumerable<string> SupportedFileFormatsList
		{
			get
			{
				return MarshalStringArray(linphone_core_get_supported_file_formats_list(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_supported_tag(IntPtr thiz, string tags);

		/// <summary>
		/// Set the supported tags. 
		/// </summary>
		public string SupportedTag
		{
			set
			{
				linphone_core_set_supported_tag(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_text_payload_types(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_text_payload_types(IntPtr thiz, IntPtr payloadTypes);

		/// <summary>
		/// Return the list of the available text payload types. 
		/// </summary>
		public IEnumerable<Linphone.PayloadType> TextPayloadTypes
		{
			get
			{
				return MarshalBctbxList<Linphone.PayloadType>(linphone_core_get_text_payload_types(nativePtr));
			}
			set
			{
				linphone_core_set_text_payload_types(nativePtr, ObjectArrayToBctbxList<Linphone.PayloadType>(value));
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_text_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_text_port(IntPtr thiz, int port);

		/// <summary>
		/// Gets the UDP port used for text streaming. 
		/// </summary>
		public int TextPort
		{
			get
			{
				return linphone_core_get_text_port(nativePtr);
			}
			set
			{
				linphone_core_set_text_port(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_text_ports_range(IntPtr thiz);

		/// <summary>
		/// Get the text port range from which is randomly chosen the UDP port used for
		/// text streaming. 
		/// </summary>
		public Linphone.Range TextPortsRange
		{
			get
			{
				IntPtr ptr = linphone_core_get_text_ports_range(nativePtr);
				Linphone.Range obj = fromNativePtr<Linphone.Range>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_tls_cert(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_tls_cert(IntPtr thiz, string tlsCert);

		/// <summary>
		/// Gets the TLS certificate. 
		/// </summary>
		public string TlsCert
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_tls_cert(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_tls_cert(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_tls_cert_path(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_tls_cert_path(IntPtr thiz, string tlsCertPath);

		/// <summary>
		/// Gets the path to the TLS certificate file. 
		/// </summary>
		public string TlsCertPath
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_tls_cert_path(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_tls_cert_path(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_tls_key(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_tls_key(IntPtr thiz, string tlsKey);

		/// <summary>
		/// Gets the TLS key. 
		/// </summary>
		public string TlsKey
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_tls_key(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_tls_key(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_tls_key_path(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_tls_key_path(IntPtr thiz, string tlsKeyPath);

		/// <summary>
		/// Gets the path to the TLS key file. 
		/// </summary>
		public string TlsKeyPath
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_tls_key_path(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_tls_key_path(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_transports(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_transports(IntPtr thiz, IntPtr transports);

		/// <summary>
		/// Retrieves the port configuration used for each transport (udp, tcp, tls). 
		/// </summary>
		public Linphone.Transports Transports
		{
			get
			{
				IntPtr ptr = linphone_core_get_transports(nativePtr);
				Linphone.Transports obj = fromNativePtr<Linphone.Transports>(ptr, true);
				return obj;
			}
			set
			{
				int exception_result = linphone_core_set_transports(nativePtr, value.nativePtr);
				if (exception_result != 0) throw new LinphoneException("Transports setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_transports_used(IntPtr thiz);

		/// <summary>
		/// Retrieves the real port number assigned for each sip transport (udp, tcp, tls). 
		/// </summary>
		public Linphone.Transports TransportsUsed
		{
			get
			{
				IntPtr ptr = linphone_core_get_transports_used(nativePtr);
				Linphone.Transports obj = fromNativePtr<Linphone.Transports>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_tunnel(IntPtr thiz);

		/// <summary>
		/// get tunnel instance if available 
		/// </summary>
		public Linphone.Tunnel Tunnel
		{
			get
			{
				IntPtr ptr = linphone_core_get_tunnel(nativePtr);
				Linphone.Tunnel obj = fromNativePtr<Linphone.Tunnel>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_unread_chat_message_count(IntPtr thiz);

		/// <summary>
		/// Return the global unread chat message count. 
		/// </summary>
		public int UnreadChatMessageCount
		{
			get
			{
				return linphone_core_get_unread_chat_message_count(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_unread_chat_message_count_from_active_locals(IntPtr thiz);

		/// <summary>
		/// Return the unread chat message count for all active local address. 
		/// </summary>
		public int UnreadChatMessageCountFromActiveLocals
		{
			get
			{
				return linphone_core_get_unread_chat_message_count_from_active_locals(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_upload_bandwidth(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_upload_bandwidth(IntPtr thiz, int bw);

		/// <summary>
		/// Retrieve the maximum available upload bandwidth. 
		/// </summary>
		public int UploadBandwidth
		{
			get
			{
				return linphone_core_get_upload_bandwidth(nativePtr);
			}
			set
			{
				linphone_core_set_upload_bandwidth(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_upload_ptime(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_upload_ptime(IntPtr thiz, int ptime);

		/// <summary>
		/// Set audio packetization time linphone will send (in absence of requirement from
		/// peer) A value of 0 stands for the current codec default packetization time. 
		/// </summary>
		public int UploadPtime
		{
			get
			{
				return linphone_core_get_upload_ptime(nativePtr);
			}
			set
			{
				linphone_core_set_upload_ptime(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_upnp_external_ipaddress(IntPtr thiz);

		/// <summary>
		/// Return the external ip address of router. 
		/// </summary>
		public string UpnpExternalIpaddress
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_upnp_external_ipaddress(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.UpnpState linphone_core_get_upnp_state(IntPtr thiz);

		/// <summary>
		/// Return the internal state of uPnP. 
		/// </summary>
		public Linphone.UpnpState UpnpState
		{
			get
			{
				return linphone_core_get_upnp_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_get_use_files(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_use_files(IntPtr thiz, char yesno);

		/// <summary>
		/// Gets whether linphone is currently streaming audio from and to files, rather
		/// than using the soundcard. 
		/// </summary>
		public bool UseFiles
		{
			get
			{
				return linphone_core_get_use_files(nativePtr) != 0;
			}
			set
			{
				linphone_core_set_use_files(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_get_use_info_for_dtmf(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_use_info_for_dtmf(IntPtr thiz, char useInfo);

		/// <summary>
		/// Indicates whether SIP INFO is used to send digits. 
		/// </summary>
		public bool UseInfoForDtmf
		{
			get
			{
				return linphone_core_get_use_info_for_dtmf(nativePtr) != 0;
			}
			set
			{
				linphone_core_set_use_info_for_dtmf(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_get_use_rfc2833_for_dtmf(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_use_rfc2833_for_dtmf(IntPtr thiz, char useRfc2833);

		/// <summary>
		/// Indicates whether RFC2833 is used to send digits. 
		/// </summary>
		public bool UseRfc2833ForDtmf
		{
			get
			{
				return linphone_core_get_use_rfc2833_for_dtmf(nativePtr) != 0;
			}
			set
			{
				linphone_core_set_use_rfc2833_for_dtmf(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_user_agent(IntPtr thiz);

		public string UserAgent
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_user_agent(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_user_certificates_path(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_user_certificates_path(IntPtr thiz, string path);

		/// <summary>
		/// Get the path to the directory storing the user's certificates. 
		/// </summary>
		public string UserCertificatesPath
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_user_certificates_path(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_user_certificates_path(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_video_activation_policy(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_video_activation_policy(IntPtr thiz, IntPtr policy);

		/// <summary>
		/// Get the default policy for video. 
		/// </summary>
		public Linphone.VideoActivationPolicy VideoActivationPolicy
		{
			get
			{
				IntPtr ptr = linphone_core_get_video_activation_policy(nativePtr);
				Linphone.VideoActivationPolicy obj = fromNativePtr<Linphone.VideoActivationPolicy>(ptr, true);
				return obj;
			}
			set
			{
				linphone_core_set_video_activation_policy(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_video_adaptive_jittcomp_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_video_adaptive_jittcomp(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether the video adaptive jitter compensation is enabled. 
		/// </summary>
		public bool VideoAdaptiveJittcompEnabled
		{
			get
			{
				return linphone_core_video_adaptive_jittcomp_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_video_adaptive_jittcomp(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_video_capture_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_video_capture(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether video capture is enabled. 
		/// </summary>
		public bool VideoCaptureEnabled
		{
			get
			{
				return linphone_core_video_capture_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_video_capture(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_video_device(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_video_device(IntPtr thiz, string id);

		/// <summary>
		/// Returns the name of the currently active video device. 
		/// </summary>
		public string VideoDevice
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_video_device(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_core_set_video_device(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("VideoDevice setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_video_devices_list(IntPtr thiz);

		/// <summary>
		/// Gets the list of the available video capture devices. 
		/// </summary>
		public IEnumerable<string> VideoDevicesList
		{
			get
			{
				return MarshalStringArray(linphone_core_get_video_devices_list(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_video_display_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_video_display(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether video display is enabled. 
		/// </summary>
		public bool VideoDisplayEnabled
		{
			get
			{
				return linphone_core_video_display_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_video_display(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_video_display_filter(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_video_display_filter(IntPtr thiz, string filtername);

		/// <summary>
		/// Get the name of the mediastreamer2 filter used for rendering video. 
		/// </summary>
		public string VideoDisplayFilter
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_video_display_filter(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_video_display_filter(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_video_dscp(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_video_dscp(IntPtr thiz, int dscp);

		/// <summary>
		/// Get the DSCP field for outgoing video streams. 
		/// </summary>
		public int VideoDscp
		{
			get
			{
				return linphone_core_get_video_dscp(nativePtr);
			}
			set
			{
				linphone_core_set_video_dscp(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_video_enabled(IntPtr thiz);

		/// <summary>
		/// Returns true if either capture or display is enabled, true otherwise. 
		/// </summary>
		public bool VideoEnabled
		{
			get
			{
				return linphone_core_video_enabled(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_video_jittcomp(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_video_jittcomp(IntPtr thiz, int milliseconds);

		/// <summary>
		/// Returns the nominal video jitter buffer size in milliseconds. 
		/// </summary>
		public int VideoJittcomp
		{
			get
			{
				return linphone_core_get_video_jittcomp(nativePtr);
			}
			set
			{
				linphone_core_set_video_jittcomp(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_video_multicast_addr(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_video_multicast_addr(IntPtr thiz, string ip);

		/// <summary>
		/// Use to get multicast address to be used for video stream. 
		/// </summary>
		public string VideoMulticastAddr
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_video_multicast_addr(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_core_set_video_multicast_addr(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("VideoMulticastAddr setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_video_multicast_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_video_multicast(IntPtr thiz, char yesno);

		/// <summary>
		/// Use to get multicast state of video stream. 
		/// </summary>
		public bool VideoMulticastEnabled
		{
			get
			{
				return linphone_core_video_multicast_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_video_multicast(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_video_multicast_ttl(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_set_video_multicast_ttl(IntPtr thiz, int ttl);

		/// <summary>
		/// Use to get multicast ttl to be used for video stream. 
		/// </summary>
		public int VideoMulticastTtl
		{
			get
			{
				return linphone_core_get_video_multicast_ttl(nativePtr);
			}
			set
			{
				int exception_result = linphone_core_set_video_multicast_ttl(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("VideoMulticastTtl setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_video_payload_types(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_video_payload_types(IntPtr thiz, IntPtr payloadTypes);

		/// <summary>
		/// Return the list of the available video payload types. 
		/// </summary>
		public IEnumerable<Linphone.PayloadType> VideoPayloadTypes
		{
			get
			{
				return MarshalBctbxList<Linphone.PayloadType>(linphone_core_get_video_payload_types(nativePtr));
			}
			set
			{
				linphone_core_set_video_payload_types(nativePtr, ObjectArrayToBctbxList<Linphone.PayloadType>(value));
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_video_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_video_port(IntPtr thiz, int port);

		/// <summary>
		/// Gets the UDP port used for video streaming. 
		/// </summary>
		public int VideoPort
		{
			get
			{
				return linphone_core_get_video_port(nativePtr);
			}
			set
			{
				linphone_core_set_video_port(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_video_ports_range(IntPtr thiz);

		/// <summary>
		/// Get the video port range from which is randomly chosen the UDP port used for
		/// video streaming. 
		/// </summary>
		public Linphone.Range VideoPortsRange
		{
			get
			{
				IntPtr ptr = linphone_core_get_video_ports_range(nativePtr);
				Linphone.Range obj = fromNativePtr<Linphone.Range>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_video_preset(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_video_preset(IntPtr thiz, string preset);

		/// <summary>
		/// Get the video preset used for video calls. 
		/// </summary>
		public string VideoPreset
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_video_preset(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_video_preset(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_video_preview_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_video_preview(IntPtr thiz, char val);

		/// <summary>
		/// Tells whether video preview is enabled. 
		/// </summary>
		public bool VideoPreviewEnabled
		{
			get
			{
				return linphone_core_video_preview_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_video_preview(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_video_source_reuse(IntPtr thiz, char enable);

		/// <summary>
		/// Enable or disable video source reuse when switching from preview to actual
		/// video call. 
		/// </summary>
		public bool VideoSourceReuseEnabled
		{
			set
			{
				linphone_core_enable_video_source_reuse(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_wifi_only_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_wifi_only(IntPtr thiz, char val);

		/// <summary>
		/// Tells whether Wifi only mode is enabled or not. 
		/// </summary>
		public bool WifiOnlyEnabled
		{
			get
			{
				return linphone_core_wifi_only_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_core_enable_wifi_only(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_zrtp_cache_db(IntPtr thiz);

		/// <summary>
		/// Get a pointer to the sqlite db holding zrtp/lime cache. 
		/// </summary>
		public IntPtr ZrtpCacheDb
		{
			get
			{
				return linphone_core_get_zrtp_cache_db(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_zrtp_secrets_file(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_zrtp_secrets_file(IntPtr thiz, string file);

		/// <summary>
		/// Get the path to the file storing the zrtp secrets cache. 
		/// </summary>
		public string ZrtpSecretsFile
		{
			get
			{
				IntPtr stringPtr = linphone_core_get_zrtp_secrets_file(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_core_set_zrtp_secrets_file(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_accept_call(IntPtr thiz, IntPtr call);

		/// <summary>
		/// Accept an incoming call. 
		/// </summary>
		public void AcceptCall(Linphone.Call call)
		{
			int exception_result = linphone_core_accept_call(nativePtr, call != null ? call.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AcceptCall returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_accept_call_update(IntPtr thiz, IntPtr call, IntPtr parameters);

		/// <summary>
		/// Accept call modifications initiated by other end. 
		/// </summary>
		public void AcceptCallUpdate(Linphone.Call call, Linphone.CallParams parameters)
		{
			int exception_result = linphone_core_accept_call_update(nativePtr, call != null ? call.nativePtr : IntPtr.Zero, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AcceptCallUpdate returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_accept_call_with_params(IntPtr thiz, IntPtr call, IntPtr parameters);

		/// <summary>
		/// Accept an incoming call, with parameters. 
		/// </summary>
		public void AcceptCallWithParams(Linphone.Call call, Linphone.CallParams parameters)
		{
			int exception_result = linphone_core_accept_call_with_params(nativePtr, call != null ? call.nativePtr : IntPtr.Zero, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AcceptCallWithParams returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_accept_early_media(IntPtr thiz, IntPtr call);

		/// <summary>
		/// Accept an early media session for an incoming call. 
		/// </summary>
		public void AcceptEarlyMedia(Linphone.Call call)
		{
			int exception_result = linphone_core_accept_early_media(nativePtr, call != null ? call.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AcceptEarlyMedia returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_accept_early_media_with_params(IntPtr thiz, IntPtr call, IntPtr parameters);

		/// <summary>
		/// When receiving an incoming, accept to start a media session as early-media. 
		/// </summary>
		public void AcceptEarlyMediaWithParams(Linphone.Call call, Linphone.CallParams parameters)
		{
			int exception_result = linphone_core_accept_early_media_with_params(nativePtr, call != null ? call.nativePtr : IntPtr.Zero, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AcceptEarlyMediaWithParams returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_activate_audio_session(IntPtr thiz, char actived);

		/// <summary>
		/// Special function to indicate if the audio session is activated. 
		/// </summary>
		public void ActivateAudioSession(bool actived)
		{
			linphone_core_activate_audio_session(nativePtr, actived ? (char)1 : (char)0);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_add_all_to_conference(IntPtr thiz);

		/// <summary>
		/// Add all current calls into the conference. 
		/// </summary>
		public void AddAllToConference()
		{
			int exception_result = linphone_core_add_all_to_conference(nativePtr);
			if (exception_result != 0) throw new LinphoneException("AddAllToConference returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_add_auth_info(IntPtr thiz, IntPtr info);

		/// <summary>
		/// Adds authentication information to the <see cref="Linphone.Core" />. 
		/// </summary>
		public void AddAuthInfo(Linphone.AuthInfo info)
		{
			linphone_core_add_auth_info(nativePtr, info != null ? info.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_add_friend(IntPtr thiz, IntPtr fr);

		/// <summary>
		/// Add a friend to the current buddy list, if subscription attribute  is set, a
		/// SIP SUBSCRIBE message is sent. 
		/// </summary>
		public void AddFriend(Linphone.Friend fr)
		{
			linphone_core_add_friend(nativePtr, fr != null ? fr.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_add_friend_list(IntPtr thiz, IntPtr list);

		/// <summary>
		/// Add a friend list. 
		/// </summary>
		public void AddFriendList(Linphone.FriendList list)
		{
			linphone_core_add_friend_list(nativePtr, list != null ? list.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_add_linphone_spec(IntPtr thiz, string spec);

		/// <summary>
		/// Add the given linphone specs to the list of functionalities the linphone client
		/// supports. 
		/// </summary>
		public void AddLinphoneSpec(string spec)
		{
			linphone_core_add_linphone_spec(nativePtr, spec);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_add_proxy_config(IntPtr thiz, IntPtr config);

		/// <summary>
		/// Add a proxy configuration. 
		/// </summary>
		public void AddProxyConfig(Linphone.ProxyConfig config)
		{
			int exception_result = linphone_core_add_proxy_config(nativePtr, config != null ? config.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AddProxyConfig returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_add_supported_tag(IntPtr thiz, string tag);

		/// <summary>
		/// This function controls signaling features supported by the core. 
		/// </summary>
		public void AddSupportedTag(string tag)
		{
			linphone_core_add_supported_tag(nativePtr, tag);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_add_to_conference(IntPtr thiz, IntPtr call);

		/// <summary>
		/// Add a participant to the conference. 
		/// </summary>
		public void AddToConference(Linphone.Call call)
		{
			int exception_result = linphone_core_add_to_conference(nativePtr, call != null ? call.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AddToConference returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_check_for_update(IntPtr thiz, string currentVersion);

		/// <summary>
		/// Checks if a new version of the application is available. 
		/// </summary>
		public void CheckForUpdate(string currentVersion)
		{
			linphone_core_check_for_update(nativePtr, currentVersion);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_clear_all_auth_info(IntPtr thiz);

		/// <summary>
		/// Clear all authentication information. 
		/// </summary>
		public void ClearAllAuthInfo()
		{
			linphone_core_clear_all_auth_info(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_clear_call_logs(IntPtr thiz);

		/// <summary>
		/// Erase the call log. 
		/// </summary>
		public void ClearCallLogs()
		{
			linphone_core_clear_call_logs(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_clear_proxy_config(IntPtr thiz);

		/// <summary>
		/// Erase all proxies from config. 
		/// </summary>
		public void ClearProxyConfig()
		{
			linphone_core_clear_proxy_config(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_account_creator(IntPtr thiz, string xmlrpcUrl);

		/// <summary>
		/// Create a <see cref="Linphone.AccountCreator" /> and set Linphone Request
		/// callbacks. 
		/// </summary>
		public Linphone.AccountCreator CreateAccountCreator(string xmlrpcUrl)
		{
			IntPtr ptr = linphone_core_create_account_creator(nativePtr, xmlrpcUrl);
			Linphone.AccountCreator returnVal = fromNativePtr<Linphone.AccountCreator>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_address(IntPtr thiz, string address);

		/// <summary>
		/// Create a <see cref="Linphone.Address" /> object by parsing the user supplied
		/// address, given as a string. 
		/// </summary>
		public Linphone.Address CreateAddress(string address)
		{
			IntPtr ptr = linphone_core_create_address(nativePtr, address);
			Linphone.Address returnVal = fromNativePtr<Linphone.Address>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_auth_info(IntPtr thiz, string username, string userid, string passwd, string ha1, string realm, string domain);

		/// <summary>
		/// Create an authentication information with default values from Linphone core. 
		/// </summary>
		public Linphone.AuthInfo CreateAuthInfo(string username, string userid, string passwd, string ha1, string realm, string domain)
		{
			IntPtr ptr = linphone_core_create_auth_info(nativePtr, username, userid, passwd, ha1, realm, domain);
			Linphone.AuthInfo returnVal = fromNativePtr<Linphone.AuthInfo>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_call_log(IntPtr thiz, IntPtr from, IntPtr to, int dir, int duration, long startTime, long connectedTime, int status, char videoEnabled, float quality);

		/// <summary>
		/// Creates a fake LinphoneCallLog. 
		/// </summary>
		public Linphone.CallLog CreateCallLog(Linphone.Address from, Linphone.Address to, Linphone.CallDir dir, int duration, long startTime, long connectedTime, Linphone.CallStatus status, bool videoEnabled, float quality)
		{
			IntPtr ptr = linphone_core_create_call_log(nativePtr, from != null ? from.nativePtr : IntPtr.Zero, to != null ? to.nativePtr : IntPtr.Zero, (int)dir, duration, startTime, connectedTime, (int)status, videoEnabled ? (char)1 : (char)0, quality);
			Linphone.CallLog returnVal = fromNativePtr<Linphone.CallLog>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_call_params(IntPtr thiz, IntPtr call);

		/// <summary>
		/// Create a <see cref="Linphone.CallParams" /> suitable for <see
		/// cref="Linphone.Core.InviteWithParams()" />, <see
		/// cref="Linphone.Core.AcceptCallWithParams()" />, <see
		/// cref="Linphone.Core.AcceptEarlyMediaWithParams()" />, <see
		/// cref="Linphone.Core.AcceptCallUpdate()" />. 
		/// </summary>
		public Linphone.CallParams CreateCallParams(Linphone.Call call)
		{
			IntPtr ptr = linphone_core_create_call_params(nativePtr, call != null ? call.nativePtr : IntPtr.Zero);
			Linphone.CallParams returnVal = fromNativePtr<Linphone.CallParams>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_chat_room_2(IntPtr thiz, IntPtr parameters, string subject, IntPtr participants);

		/// <summary>
		/// Create a chat room. 
		/// </summary>
		public Linphone.ChatRoom CreateChatRoom(Linphone.ChatRoomParams parameters, string subject, IEnumerable<Linphone.Address> participants)
		{
			IntPtr ptr = linphone_core_create_chat_room_2(nativePtr, parameters != null ? parameters.nativePtr : IntPtr.Zero, subject, ObjectArrayToBctbxList<Linphone.Address>(participants));
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_chat_room_3(IntPtr thiz, string subject, IntPtr participants);

		public Linphone.ChatRoom CreateChatRoom(string subject, IEnumerable<Linphone.Address> participants)
		{
			IntPtr ptr = linphone_core_create_chat_room_3(nativePtr, subject, ObjectArrayToBctbxList<Linphone.Address>(participants));
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_chat_room_4(IntPtr thiz, IntPtr parameters, IntPtr localAddr, IntPtr participant);

		public Linphone.ChatRoom CreateChatRoom(Linphone.ChatRoomParams parameters, Linphone.Address localAddr, Linphone.Address participant)
		{
			IntPtr ptr = linphone_core_create_chat_room_4(nativePtr, parameters != null ? parameters.nativePtr : IntPtr.Zero, localAddr != null ? localAddr.nativePtr : IntPtr.Zero, participant != null ? participant.nativePtr : IntPtr.Zero);
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_chat_room_5(IntPtr thiz, IntPtr participant);

		public Linphone.ChatRoom CreateChatRoom(Linphone.Address participant)
		{
			IntPtr ptr = linphone_core_create_chat_room_5(nativePtr, participant != null ? participant.nativePtr : IntPtr.Zero);
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_chat_room(IntPtr thiz, IntPtr parameters, IntPtr localAddr, string subject, IntPtr participants);

		/// <summary>
		/// Create a chat room. 
		/// </summary>
		public Linphone.ChatRoom CreateChatRoom(Linphone.ChatRoomParams parameters, Linphone.Address localAddr, string subject, IEnumerable<Linphone.Address> participants)
		{
			IntPtr ptr = linphone_core_create_chat_room(nativePtr, parameters != null ? parameters.nativePtr : IntPtr.Zero, localAddr != null ? localAddr.nativePtr : IntPtr.Zero, subject, ObjectArrayToBctbxList<Linphone.Address>(participants));
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_client_group_chat_room(IntPtr thiz, string subject, char fallback);

		/// <summary>
		/// Create a client-side group chat room. 
		/// </summary>
		public Linphone.ChatRoom CreateClientGroupChatRoom(string subject, bool fallback)
		{
			IntPtr ptr = linphone_core_create_client_group_chat_room(nativePtr, subject, fallback ? (char)1 : (char)0);
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_client_group_chat_room_2(IntPtr thiz, string subject, char fallback, char encrypted);

		/// <summary>
		/// Create a client-side group chat room. 
		/// </summary>
		public Linphone.ChatRoom CreateClientGroupChatRoom(string subject, bool fallback, bool encrypted)
		{
			IntPtr ptr = linphone_core_create_client_group_chat_room_2(nativePtr, subject, fallback ? (char)1 : (char)0, encrypted ? (char)1 : (char)0);
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_conference_params(IntPtr thiz);

		/// <summary>
		/// Create some default conference parameters for instanciating a a conference with
		/// <see cref="Linphone.Core.CreateConferenceWithParams()" />. 
		/// </summary>
		public Linphone.ConferenceParams CreateConferenceParams()
		{
			IntPtr ptr = linphone_core_create_conference_params(nativePtr);
			Linphone.ConferenceParams returnVal = fromNativePtr<Linphone.ConferenceParams>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_conference_with_params(IntPtr thiz, IntPtr parameters);

		/// <summary>
		/// Create a conference. 
		/// </summary>
		public Linphone.Conference CreateConferenceWithParams(Linphone.ConferenceParams parameters)
		{
			IntPtr ptr = linphone_core_create_conference_with_params(nativePtr, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			Linphone.Conference returnVal = fromNativePtr<Linphone.Conference>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_config(IntPtr thiz, string filename);

		/// <summary>
		/// Create a <see cref="Linphone.Config" /> object from a user config file. 
		/// </summary>
		public Linphone.Config CreateConfig(string filename)
		{
			IntPtr ptr = linphone_core_create_config(nativePtr, filename);
			Linphone.Config returnVal = fromNativePtr<Linphone.Config>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_content(IntPtr thiz);

		/// <summary>
		/// Create a content with default values from Linphone core. 
		/// </summary>
		public Linphone.Content CreateContent()
		{
			IntPtr ptr = linphone_core_create_content(nativePtr);
			Linphone.Content returnVal = fromNativePtr<Linphone.Content>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_default_chat_room_params(IntPtr thiz);

		/// <summary>
		/// Creates and returns the default chat room parameters. 
		/// </summary>
		public Linphone.ChatRoomParams CreateDefaultChatRoomParams()
		{
			IntPtr ptr = linphone_core_create_default_chat_room_params(nativePtr);
			Linphone.ChatRoomParams returnVal = fromNativePtr<Linphone.ChatRoomParams>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_friend(IntPtr thiz);

		/// <summary>
		/// Create a default LinphoneFriend. 
		/// </summary>
		public Linphone.Friend CreateFriend()
		{
			IntPtr ptr = linphone_core_create_friend(nativePtr);
			Linphone.Friend returnVal = fromNativePtr<Linphone.Friend>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_friend_list(IntPtr thiz);

		/// <summary>
		/// Create a new empty <see cref="Linphone.FriendList" /> object. 
		/// </summary>
		public Linphone.FriendList CreateFriendList()
		{
			IntPtr ptr = linphone_core_create_friend_list(nativePtr);
			Linphone.FriendList returnVal = fromNativePtr<Linphone.FriendList>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_friend_with_address(IntPtr thiz, string address);

		/// <summary>
		/// Create a <see cref="Linphone.Friend" /> from the given address. 
		/// </summary>
		public Linphone.Friend CreateFriendWithAddress(string address)
		{
			IntPtr ptr = linphone_core_create_friend_with_address(nativePtr, address);
			Linphone.Friend returnVal = fromNativePtr<Linphone.Friend>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_info_message(IntPtr thiz);

		/// <summary>
		/// Creates an empty info message. 
		/// </summary>
		public Linphone.InfoMessage CreateInfoMessage()
		{
			IntPtr ptr = linphone_core_create_info_message(nativePtr);
			Linphone.InfoMessage returnVal = fromNativePtr<Linphone.InfoMessage>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_local_player(IntPtr thiz, string soundCardName, string videoDisplayName, IntPtr windowId);

		/// <summary>
		/// Create an independent media file player. 
		/// </summary>
		public Linphone.Player CreateLocalPlayer(string soundCardName, string videoDisplayName, IntPtr windowId)
		{
			IntPtr ptr = linphone_core_create_local_player(nativePtr, soundCardName, videoDisplayName, windowId);
			Linphone.Player returnVal = fromNativePtr<Linphone.Player>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_magic_search(IntPtr thiz);

		/// <summary>
		/// Create a <see cref="Linphone.MagicSearch" /> object. 
		/// </summary>
		public Linphone.MagicSearch CreateMagicSearch()
		{
			IntPtr ptr = linphone_core_create_magic_search(nativePtr);
			Linphone.MagicSearch returnVal = fromNativePtr<Linphone.MagicSearch>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_nat_policy(IntPtr thiz);

		/// <summary>
		/// Create a new <see cref="Linphone.NatPolicy" /> object with every policies being
		/// disabled. 
		/// </summary>
		public Linphone.NatPolicy CreateNatPolicy()
		{
			IntPtr ptr = linphone_core_create_nat_policy(nativePtr);
			Linphone.NatPolicy returnVal = fromNativePtr<Linphone.NatPolicy>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_nat_policy_from_config(IntPtr thiz, string reference);

		/// <summary>
		/// Create a new <see cref="Linphone.NatPolicy" /> by reading the config of a <see
		/// cref="Linphone.Core" /> according to the passed ref. 
		/// </summary>
		public Linphone.NatPolicy CreateNatPolicyFromConfig(string reference)
		{
			IntPtr ptr = linphone_core_create_nat_policy_from_config(nativePtr, reference);
			Linphone.NatPolicy returnVal = fromNativePtr<Linphone.NatPolicy>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_notify(IntPtr thiz, IntPtr resource, string ev);

		/// <summary>
		/// Create an out-of-dialog notification, specifying the destination resource, the
		/// event name. 
		/// </summary>
		public Linphone.Event CreateNotify(Linphone.Address resource, string ev)
		{
			IntPtr ptr = linphone_core_create_notify(nativePtr, resource != null ? resource.nativePtr : IntPtr.Zero, ev);
			Linphone.Event returnVal = fromNativePtr<Linphone.Event>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_one_shot_publish(IntPtr thiz, IntPtr resource, string ev);

		/// <summary>
		/// Create a publish context for a one-shot publish. 
		/// </summary>
		public Linphone.Event CreateOneShotPublish(Linphone.Address resource, string ev)
		{
			IntPtr ptr = linphone_core_create_one_shot_publish(nativePtr, resource != null ? resource.nativePtr : IntPtr.Zero, ev);
			Linphone.Event returnVal = fromNativePtr<Linphone.Event>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_presence_activity(IntPtr thiz, int acttype, string description);

		/// <summary>
		/// Create a <see cref="Linphone.PresenceActivity" /> with the given type and
		/// description. 
		/// </summary>
		public Linphone.PresenceActivity CreatePresenceActivity(Linphone.PresenceActivityType acttype, string description)
		{
			IntPtr ptr = linphone_core_create_presence_activity(nativePtr, (int)acttype, description);
			Linphone.PresenceActivity returnVal = fromNativePtr<Linphone.PresenceActivity>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_presence_model(IntPtr thiz);

		/// <summary>
		/// Create a default LinphonePresenceModel. 
		/// </summary>
		public Linphone.PresenceModel CreatePresenceModel()
		{
			IntPtr ptr = linphone_core_create_presence_model(nativePtr);
			Linphone.PresenceModel returnVal = fromNativePtr<Linphone.PresenceModel>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_presence_model_with_activity(IntPtr thiz, int acttype, string description);

		/// <summary>
		/// Create a <see cref="Linphone.PresenceModel" /> with the given activity type and
		/// activity description. 
		/// </summary>
		public Linphone.PresenceModel CreatePresenceModelWithActivity(Linphone.PresenceActivityType acttype, string description)
		{
			IntPtr ptr = linphone_core_create_presence_model_with_activity(nativePtr, (int)acttype, description);
			Linphone.PresenceModel returnVal = fromNativePtr<Linphone.PresenceModel>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_presence_model_with_activity_and_note(IntPtr thiz, int acttype, string description, string note, string lang);

		/// <summary>
		/// Create a <see cref="Linphone.PresenceModel" /> with the given activity type,
		/// activity description, note content and note language. 
		/// </summary>
		public Linphone.PresenceModel CreatePresenceModelWithActivityAndNote(Linphone.PresenceActivityType acttype, string description, string note, string lang)
		{
			IntPtr ptr = linphone_core_create_presence_model_with_activity_and_note(nativePtr, (int)acttype, description, note, lang);
			Linphone.PresenceModel returnVal = fromNativePtr<Linphone.PresenceModel>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_presence_note(IntPtr thiz, string content, string lang);

		/// <summary>
		/// Create a <see cref="Linphone.PresenceNote" /> with the given content and
		/// language. 
		/// </summary>
		public Linphone.PresenceNote CreatePresenceNote(string content, string lang)
		{
			IntPtr ptr = linphone_core_create_presence_note(nativePtr, content, lang);
			Linphone.PresenceNote returnVal = fromNativePtr<Linphone.PresenceNote>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_presence_person(IntPtr thiz, string id);

		/// <summary>
		/// Create a <see cref="Linphone.PresencePerson" /> with the given id. 
		/// </summary>
		public Linphone.PresencePerson CreatePresencePerson(string id)
		{
			IntPtr ptr = linphone_core_create_presence_person(nativePtr, id);
			Linphone.PresencePerson returnVal = fromNativePtr<Linphone.PresencePerson>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_presence_service(IntPtr thiz, string id, int basicStatus, string contact);

		/// <summary>
		/// Create a <see cref="Linphone.PresenceService" /> with the given id, basic
		/// status and contact. 
		/// </summary>
		public Linphone.PresenceService CreatePresenceService(string id, Linphone.PresenceBasicStatus basicStatus, string contact)
		{
			IntPtr ptr = linphone_core_create_presence_service(nativePtr, id, (int)basicStatus, contact);
			Linphone.PresenceService returnVal = fromNativePtr<Linphone.PresenceService>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_primary_contact_parsed(IntPtr thiz);

		/// <summary>
		/// Same as <see cref="Linphone.Core.GetPrimaryContact()" /> but the result is a
		/// <see cref="Linphone.Address" /> object instead of const char *. 
		/// </summary>
		public Linphone.Address CreatePrimaryContactParsed()
		{
			IntPtr ptr = linphone_core_create_primary_contact_parsed(nativePtr);
			Linphone.Address returnVal = fromNativePtr<Linphone.Address>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_proxy_config(IntPtr thiz);

		/// <summary>
		/// Create a proxy config with default values from Linphone core. 
		/// </summary>
		public Linphone.ProxyConfig CreateProxyConfig()
		{
			IntPtr ptr = linphone_core_create_proxy_config(nativePtr);
			Linphone.ProxyConfig returnVal = fromNativePtr<Linphone.ProxyConfig>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_publish(IntPtr thiz, IntPtr resource, string ev, int expires);

		/// <summary>
		/// Create a publish context for an event state. 
		/// </summary>
		public Linphone.Event CreatePublish(Linphone.Address resource, string ev, int expires)
		{
			IntPtr ptr = linphone_core_create_publish(nativePtr, resource != null ? resource.nativePtr : IntPtr.Zero, ev, expires);
			Linphone.Event returnVal = fromNativePtr<Linphone.Event>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_subscribe_2(IntPtr thiz, IntPtr resource, IntPtr proxy, string ev, int expires);

		/// <summary>
		/// Create an outgoing subscription, specifying the destination resource, the event
		/// name, and an optional content body. 
		/// </summary>
		public Linphone.Event CreateSubscribe(Linphone.Address resource, Linphone.ProxyConfig proxy, string ev, int expires)
		{
			IntPtr ptr = linphone_core_create_subscribe_2(nativePtr, resource != null ? resource.nativePtr : IntPtr.Zero, proxy != null ? proxy.nativePtr : IntPtr.Zero, ev, expires);
			Linphone.Event returnVal = fromNativePtr<Linphone.Event>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_subscribe(IntPtr thiz, IntPtr resource, string ev, int expires);

		/// <summary>
		/// Create an outgoing subscription, specifying the destination resource, the event
		/// name, and an optional content body. 
		/// </summary>
		public Linphone.Event CreateSubscribe(Linphone.Address resource, string ev, int expires)
		{
			IntPtr ptr = linphone_core_create_subscribe(nativePtr, resource != null ? resource.nativePtr : IntPtr.Zero, ev, expires);
			Linphone.Event returnVal = fromNativePtr<Linphone.Event>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_create_xml_rpc_session(IntPtr thiz, string url);

		/// <summary>
		/// Create a <see cref="Linphone.XmlRpcSession" /> for a given url. 
		/// </summary>
		public Linphone.XmlRpcSession CreateXmlRpcSession(string url)
		{
			IntPtr ptr = linphone_core_create_xml_rpc_session(nativePtr, url);
			Linphone.XmlRpcSession returnVal = fromNativePtr<Linphone.XmlRpcSession>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_decline_call(IntPtr thiz, IntPtr call, int reason);

		/// <summary>
		/// Decline a pending incoming call, with a reason. 
		/// </summary>
		public void DeclineCall(Linphone.Call call, Linphone.Reason reason)
		{
			int exception_result = linphone_core_decline_call(nativePtr, call != null ? call.nativePtr : IntPtr.Zero, (int)reason);
			if (exception_result != 0) throw new LinphoneException("DeclineCall returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_defer_call_update(IntPtr thiz, IntPtr call);

		/// <summary>
		/// When receiving a #LinphoneCallUpdatedByRemote state notification, prevent <see
		/// cref="Linphone.Core" /> from performing an automatic answer. 
		/// </summary>
		public void DeferCallUpdate(Linphone.Call call)
		{
			int exception_result = linphone_core_defer_call_update(nativePtr, call != null ? call.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("DeferCallUpdate returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_delete_chat_room(IntPtr thiz, IntPtr cr);

		/// <summary>
		/// Removes a chatroom including all message history from the LinphoneCore. 
		/// </summary>
		public void DeleteChatRoom(Linphone.ChatRoom cr)
		{
			linphone_core_delete_chat_room(nativePtr, cr != null ? cr.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_disable_chat(IntPtr thiz, int denyReason);

		/// <summary>
		/// Inconditionnaly disable incoming chat messages. 
		/// </summary>
		public void DisableChat(Linphone.Reason denyReason)
		{
			linphone_core_disable_chat(nativePtr, (int)denyReason);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_chat(IntPtr thiz);

		/// <summary>
		/// Enable reception of incoming chat messages. 
		/// </summary>
		public void EnableChat()
		{
			linphone_core_enable_chat(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enable_lime(IntPtr thiz, int val);

		/// <summary>
		/// Tells to <see cref="Linphone.Core" /> to use Linphone Instant Messaging
		/// encryption. 
		/// </summary>
		public void EnableLime(Linphone.LimeState val)
		{
			linphone_core_enable_lime(nativePtr, (int)val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_ensure_registered(IntPtr thiz);

		/// <summary>
		/// Call this method when you receive a push notification (if you handle push
		/// notifications manually). 
		/// </summary>
		public void EnsureRegistered()
		{
			linphone_core_ensure_registered(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enter_background(IntPtr thiz);

		/// <summary>
		/// This method is called by the application to notify the linphone core library
		/// when it enters background mode. 
		/// </summary>
		public void EnterBackground()
		{
			linphone_core_enter_background(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_enter_conference(IntPtr thiz);

		/// <summary>
		/// Join the local participant to the running conference. 
		/// </summary>
		public void EnterConference()
		{
			int exception_result = linphone_core_enter_conference(nativePtr);
			if (exception_result != 0) throw new LinphoneException("EnterConference returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_enter_foreground(IntPtr thiz);

		/// <summary>
		/// This method is called by the application to notify the linphone core library
		/// when it enters foreground mode. 
		/// </summary>
		public void EnterForeground()
		{
			linphone_core_enter_foreground(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_file_format_supported(IntPtr thiz, string fmt);

		/// <summary>
		/// Returns whether a specific file format is supported. 
		/// </summary>
		public bool FileFormatSupported(string fmt)
		{
			bool returnVal = linphone_core_file_format_supported(nativePtr, fmt) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_find_auth_info(IntPtr thiz, string realm, string username, string sipDomain);

		/// <summary>
		/// Find authentication info matching realm, username, domain criteria. 
		/// </summary>
		public Linphone.AuthInfo FindAuthInfo(string realm, string username, string sipDomain)
		{
			IntPtr ptr = linphone_core_find_auth_info(nativePtr, realm, username, sipDomain);
			Linphone.AuthInfo returnVal = fromNativePtr<Linphone.AuthInfo>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_find_call_from_uri(IntPtr thiz, string uri);

		/// <summary>
		/// Search from the list of current calls if a remote address match uri. 
		/// </summary>
		public Linphone.Call FindCallFromUri(string uri)
		{
			IntPtr ptr = linphone_core_find_call_from_uri(nativePtr, uri);
			Linphone.Call returnVal = fromNativePtr<Linphone.Call>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_find_call_log_from_call_id(IntPtr thiz, string callId);

		/// <summary>
		/// Get the call log matching the call id, or null if can't be found. 
		/// </summary>
		public Linphone.CallLog FindCallLogFromCallId(string callId)
		{
			IntPtr ptr = linphone_core_find_call_log_from_call_id(nativePtr, callId);
			Linphone.CallLog returnVal = fromNativePtr<Linphone.CallLog>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_find_chat_room(IntPtr thiz, IntPtr peerAddr, IntPtr localAddr);

		/// <summary>
		/// Find a chat room. 
		/// </summary>
		public Linphone.ChatRoom FindChatRoom(Linphone.Address peerAddr, Linphone.Address localAddr)
		{
			IntPtr ptr = linphone_core_find_chat_room(nativePtr, peerAddr != null ? peerAddr.nativePtr : IntPtr.Zero, localAddr != null ? localAddr.nativePtr : IntPtr.Zero);
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_find_contacts_by_char(IntPtr thiz, string filter, char sipOnly);

		/// <summary>
		/// Retrieves a list of <see cref="Linphone.Address" /> sort and filter. 
		/// </summary>
		public IEnumerable<Linphone.Address> FindContactsByChar(string filter, bool sipOnly)
		{
			IEnumerable<Linphone.Address> returnVal = MarshalBctbxList<Linphone.Address>(linphone_core_find_contacts_by_char(nativePtr, filter, sipOnly ? (char)1 : (char)0));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_find_friend(IntPtr thiz, IntPtr addr);

		/// <summary>
		/// Search a <see cref="Linphone.Friend" /> by its address. 
		/// </summary>
		public Linphone.Friend FindFriend(Linphone.Address addr)
		{
			IntPtr ptr = linphone_core_find_friend(nativePtr, addr != null ? addr.nativePtr : IntPtr.Zero);
			Linphone.Friend returnVal = fromNativePtr<Linphone.Friend>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_find_friends(IntPtr thiz, IntPtr addr);

		/// <summary>
		/// Search all <see cref="Linphone.Friend" /> matching an address. 
		/// </summary>
		public IEnumerable<Linphone.Friend> FindFriends(Linphone.Address addr)
		{
			IEnumerable<Linphone.Friend> returnVal = MarshalBctbxList<Linphone.Friend>(linphone_core_find_friends(nativePtr, addr != null ? addr.nativePtr : IntPtr.Zero));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_find_one_to_one_chat_room_2(IntPtr thiz, IntPtr localAddr, IntPtr participantAddr, char encrypted);

		/// <summary>
		/// Find a one to one chat room. 
		/// </summary>
		public Linphone.ChatRoom FindOneToOneChatRoom(Linphone.Address localAddr, Linphone.Address participantAddr, bool encrypted)
		{
			IntPtr ptr = linphone_core_find_one_to_one_chat_room_2(nativePtr, localAddr != null ? localAddr.nativePtr : IntPtr.Zero, participantAddr != null ? participantAddr.nativePtr : IntPtr.Zero, encrypted ? (char)1 : (char)0);
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_find_one_to_one_chat_room(IntPtr thiz, IntPtr localAddr, IntPtr participantAddr);

		/// <summary>
		/// Find a one to one chat room. 
		/// </summary>
		public Linphone.ChatRoom FindOneToOneChatRoom(Linphone.Address localAddr, Linphone.Address participantAddr)
		{
			IntPtr ptr = linphone_core_find_one_to_one_chat_room(nativePtr, localAddr != null ? localAddr.nativePtr : IntPtr.Zero, participantAddr != null ? participantAddr.nativePtr : IntPtr.Zero);
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_call_by_remote_address(IntPtr thiz, string remoteAddress);

		/// <summary>
		/// Get the call with the remote_address specified. 
		/// </summary>
		public Linphone.Call GetCallByRemoteAddress(string remoteAddress)
		{
			IntPtr ptr = linphone_core_get_call_by_remote_address(nativePtr, remoteAddress);
			Linphone.Call returnVal = fromNativePtr<Linphone.Call>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_call_by_remote_address2(IntPtr thiz, IntPtr remoteAddress);

		/// <summary>
		/// Get the call with the remote_address specified. 
		/// </summary>
		public Linphone.Call GetCallByRemoteAddress2(Linphone.Address remoteAddress)
		{
			IntPtr ptr = linphone_core_get_call_by_remote_address2(nativePtr, remoteAddress != null ? remoteAddress.nativePtr : IntPtr.Zero);
			Linphone.Call returnVal = fromNativePtr<Linphone.Call>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_call_history_2(IntPtr thiz, IntPtr peerAddr, IntPtr localAddr);

		/// <summary>
		/// Get the list of call logs (past calls). 
		/// </summary>
		public IEnumerable<Linphone.CallLog> GetCallHistory(Linphone.Address peerAddr, Linphone.Address localAddr)
		{
			IEnumerable<Linphone.CallLog> returnVal = MarshalBctbxList<Linphone.CallLog>(linphone_core_get_call_history_2(nativePtr, peerAddr != null ? peerAddr.nativePtr : IntPtr.Zero, localAddr != null ? localAddr.nativePtr : IntPtr.Zero));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_call_history_for_address(IntPtr thiz, IntPtr addr);

		/// <summary>
		/// Get the list of call logs (past calls) that matches the given <see
		/// cref="Linphone.Address" />. 
		/// </summary>
		public IEnumerable<Linphone.CallLog> GetCallHistoryForAddress(Linphone.Address addr)
		{
			IEnumerable<Linphone.CallLog> returnVal = MarshalBctbxList<Linphone.CallLog>(linphone_core_get_call_history_for_address(nativePtr, addr != null ? addr.nativePtr : IntPtr.Zero));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_chat_room_2(IntPtr thiz, IntPtr peerAddr, IntPtr localAddr);

		/// <summary>
		/// Get a basic chat room. 
		/// </summary>
		public Linphone.ChatRoom GetChatRoom(Linphone.Address peerAddr, Linphone.Address localAddr)
		{
			IntPtr ptr = linphone_core_get_chat_room_2(nativePtr, peerAddr != null ? peerAddr.nativePtr : IntPtr.Zero, localAddr != null ? localAddr.nativePtr : IntPtr.Zero);
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_chat_room(IntPtr thiz, IntPtr addr);

		/// <summary>
		/// Get a basic chat room whose peer is the supplied address. 
		/// </summary>
		public Linphone.ChatRoom GetChatRoom(Linphone.Address addr)
		{
			IntPtr ptr = linphone_core_get_chat_room(nativePtr, addr != null ? addr.nativePtr : IntPtr.Zero);
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_chat_room_from_uri(IntPtr thiz, string to);

		/// <summary>
		/// Get a basic chat room for messaging from a sip uri like
		/// sip:joe@sip.linphone.org. 
		/// </summary>
		public Linphone.ChatRoom GetChatRoomFromUri(string to)
		{
			IntPtr ptr = linphone_core_get_chat_room_from_uri(nativePtr, to);
			Linphone.ChatRoom returnVal = fromNativePtr<Linphone.ChatRoom>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_friend_by_ref_key(IntPtr thiz, string key);

		/// <summary>
		/// Search a <see cref="Linphone.Friend" /> by its reference key. 
		/// </summary>
		public Linphone.Friend GetFriendByRefKey(string key)
		{
			IntPtr ptr = linphone_core_get_friend_by_ref_key(nativePtr, key);
			Linphone.Friend returnVal = fromNativePtr<Linphone.Friend>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_friend_list_by_name(IntPtr thiz, string name);

		/// <summary>
		/// Retrieves the list of <see cref="Linphone.Friend" /> from the core that has the
		/// given display name. 
		/// </summary>
		public Linphone.FriendList GetFriendListByName(string name)
		{
			IntPtr ptr = linphone_core_get_friend_list_by_name(nativePtr, name);
			Linphone.FriendList returnVal = fromNativePtr<Linphone.FriendList>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_payload_type(IntPtr thiz, string type, int rate, int channels);

		/// <summary>
		/// Get payload type from mime type and clock rate. 
		/// </summary>
		public Linphone.PayloadType GetPayloadType(string type, int rate, int channels)
		{
			IntPtr ptr = linphone_core_get_payload_type(nativePtr, type, rate, channels);
			Linphone.PayloadType returnVal = fromNativePtr<Linphone.PayloadType>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_get_proxy_config_by_idkey(IntPtr thiz, string idkey);

		public Linphone.ProxyConfig GetProxyConfigByIdkey(string idkey)
		{
			IntPtr ptr = linphone_core_get_proxy_config_by_idkey(nativePtr, idkey);
			Linphone.ProxyConfig returnVal = fromNativePtr<Linphone.ProxyConfig>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_get_unread_chat_message_count_from_local(IntPtr thiz, IntPtr address);

		/// <summary>
		/// Return the unread chat message count for a given local address. 
		/// </summary>
		public int GetUnreadChatMessageCountFromLocal(Linphone.Address address)
		{
			int returnVal = linphone_core_get_unread_chat_message_count_from_local(nativePtr, address != null ? address.nativePtr : IntPtr.Zero);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ZrtpPeerStatus linphone_core_get_zrtp_status(IntPtr thiz, string addr);

		/// <summary>
		/// Get the zrtp sas validation status for a peer uri. 
		/// </summary>
		public Linphone.ZrtpPeerStatus GetZrtpStatus(string addr)
		{
			Linphone.ZrtpPeerStatus returnVal = linphone_core_get_zrtp_status(nativePtr, addr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_has_builtin_echo_canceller(IntPtr thiz);

		/// <summary>
		/// Check whether the device has a hardware echo canceller. 
		/// </summary>
		public bool HasBuiltinEchoCanceller()
		{
			bool returnVal = linphone_core_has_builtin_echo_canceller(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_has_crappy_opengl(IntPtr thiz);

		/// <summary>
		/// Check whether the device is flagged has crappy opengl. 
		/// </summary>
		public bool HasCrappyOpengl()
		{
			bool returnVal = linphone_core_has_crappy_opengl(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_in_call(IntPtr thiz);

		/// <summary>
		/// Tells whether there is a call running. 
		/// </summary>
		public bool InCall()
		{
			bool returnVal = linphone_core_in_call(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_interpret_url(IntPtr thiz, string url);

		/// <summary>
		/// See linphone_proxy_config_normalize_sip_uri for documentation. 
		/// </summary>
		public Linphone.Address InterpretUrl(string url)
		{
			IntPtr ptr = linphone_core_interpret_url(nativePtr, url);
			Linphone.Address returnVal = fromNativePtr<Linphone.Address>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_invite(IntPtr thiz, string url);

		/// <summary>
		/// Initiates an outgoing call. 
		/// </summary>
		public Linphone.Call Invite(string url)
		{
			IntPtr ptr = linphone_core_invite(nativePtr, url);
			Linphone.Call returnVal = fromNativePtr<Linphone.Call>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_invite_address(IntPtr thiz, IntPtr addr);

		/// <summary>
		/// Initiates an outgoing call given a destination <see cref="Linphone.Address" />
		/// The <see cref="Linphone.Address" /> can be constructed directly using
		/// linphone_address_new, or created by <see cref="Linphone.Core.InterpretUrl()"
		/// />. 
		/// </summary>
		public Linphone.Call InviteAddress(Linphone.Address addr)
		{
			IntPtr ptr = linphone_core_invite_address(nativePtr, addr != null ? addr.nativePtr : IntPtr.Zero);
			Linphone.Call returnVal = fromNativePtr<Linphone.Call>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_invite_address_with_params(IntPtr thiz, IntPtr addr, IntPtr parameters);

		/// <summary>
		/// Initiates an outgoing call given a destination <see cref="Linphone.Address" />
		/// The <see cref="Linphone.Address" /> can be constructed directly using
		/// linphone_address_new, or created by <see cref="Linphone.Core.InterpretUrl()"
		/// />. 
		/// </summary>
		public Linphone.Call InviteAddressWithParams(Linphone.Address addr, Linphone.CallParams parameters)
		{
			IntPtr ptr = linphone_core_invite_address_with_params(nativePtr, addr != null ? addr.nativePtr : IntPtr.Zero, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			Linphone.Call returnVal = fromNativePtr<Linphone.Call>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_invite_with_params(IntPtr thiz, string url, IntPtr parameters);

		/// <summary>
		/// Initiates an outgoing call according to supplied call parameters The
		/// application doesn't own a reference to the returned <see cref="Linphone.Call"
		/// /> object. 
		/// </summary>
		public Linphone.Call InviteWithParams(string url, Linphone.CallParams parameters)
		{
			IntPtr ptr = linphone_core_invite_with_params(nativePtr, url, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			Linphone.Call returnVal = fromNativePtr<Linphone.Call>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_is_media_filter_supported(IntPtr thiz, string filtername);

		/// <summary>
		/// Checks if the given media filter is loaded and usable. 
		/// </summary>
		public bool IsMediaFilterSupported(string filtername)
		{
			bool returnVal = linphone_core_is_media_filter_supported(nativePtr, filtername) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_iterate(IntPtr thiz);

		/// <summary>
		/// Main loop function. 
		/// </summary>
		public void Iterate()
		{
			linphone_core_iterate(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_leave_conference(IntPtr thiz);

		/// <summary>
		/// Make the local participant leave the running conference. 
		/// </summary>
		public void LeaveConference()
		{
			int exception_result = linphone_core_leave_conference(nativePtr);
			if (exception_result != 0) throw new LinphoneException("LeaveConference returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_lime_available(IntPtr thiz);

		/// <summary>
		/// Tells if lime is available. 
		/// </summary>
		public bool LimeAvailable()
		{
			bool returnVal = linphone_core_lime_available(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.LimeState linphone_core_lime_enabled(IntPtr thiz);

		/// <summary>
		/// Returns the lime state. 
		/// </summary>
		public Linphone.LimeState LimeEnabled()
		{
			Linphone.LimeState returnVal = linphone_core_lime_enabled(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_lime_x3dh_available(IntPtr thiz);

		/// <summary>
		/// Tells if LIME X3DH is available. 
		/// </summary>
		public bool LimeX3DhAvailable()
		{
			bool returnVal = linphone_core_lime_x3dh_available(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_load_config_from_xml(IntPtr thiz, string xmlUri);

		/// <summary>
		/// Update current config with the content of a xml config file. 
		/// </summary>
		public void LoadConfigFromXml(string xmlUri)
		{
			linphone_core_load_config_from_xml(nativePtr, xmlUri);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_media_encryption_supported(IntPtr thiz, int menc);

		/// <summary>
		/// Check if a media encryption type is supported. 
		/// </summary>
		public bool MediaEncryptionSupported(Linphone.MediaEncryption menc)
		{
			bool returnVal = linphone_core_media_encryption_supported(nativePtr, (int)menc) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_migrate_logs_from_rc_to_db(IntPtr thiz);

		/// <summary>
		/// Migrates the call logs from the linphonerc to the database if not done yet. 
		/// </summary>
		public void MigrateLogsFromRcToDb()
		{
			linphone_core_migrate_logs_from_rc_to_db(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_migrate_to_multi_transport(IntPtr thiz);

		/// <summary>
		/// Migrate configuration so that all SIP transports are enabled. 
		/// </summary>
		public void MigrateToMultiTransport()
		{
			int exception_result = linphone_core_migrate_to_multi_transport(nativePtr);
			if (exception_result != 0) throw new LinphoneException("MigrateToMultiTransport returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_notify_all_friends(IntPtr thiz, IntPtr presence);

		/// <summary>
		/// Notify all friends that have subscribed. 
		/// </summary>
		public void NotifyAllFriends(Linphone.PresenceModel presence)
		{
			linphone_core_notify_all_friends(nativePtr, presence != null ? presence.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_notify_notify_presence_received(IntPtr thiz, IntPtr lf);

		/// <summary>
		/// Notifies the upper layer that a presence status has been received by calling
		/// the appropriate callback if one has been set. 
		/// </summary>
		public void NotifyNotifyPresenceReceived(Linphone.Friend lf)
		{
			linphone_core_notify_notify_presence_received(nativePtr, lf != null ? lf.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_notify_notify_presence_received_for_uri_or_tel(IntPtr thiz, IntPtr lf, string uriOrTel, IntPtr presenceModel);

		/// <summary>
		/// Notifies the upper layer that a presence model change has been received for the
		/// uri or telephone number given as a parameter, by calling the appropriate
		/// callback if one has been set. 
		/// </summary>
		public void NotifyNotifyPresenceReceivedForUriOrTel(Linphone.Friend lf, string uriOrTel, Linphone.PresenceModel presenceModel)
		{
			linphone_core_notify_notify_presence_received_for_uri_or_tel(nativePtr, lf != null ? lf.nativePtr : IntPtr.Zero, uriOrTel, presenceModel != null ? presenceModel.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_pause_all_calls(IntPtr thiz);

		/// <summary>
		/// Pause all currently running calls. 
		/// </summary>
		public void PauseAllCalls()
		{
			int exception_result = linphone_core_pause_all_calls(nativePtr);
			if (exception_result != 0) throw new LinphoneException("PauseAllCalls returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_pause_call(IntPtr thiz, IntPtr call);

		/// <summary>
		/// Pauses the call. 
		/// </summary>
		public void PauseCall(Linphone.Call call)
		{
			int exception_result = linphone_core_pause_call(nativePtr, call != null ? call.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("PauseCall returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_play_dtmf(IntPtr thiz, sbyte dtmf, int durationMs);

		/// <summary>
		/// Plays a dtmf sound to the local user. 
		/// </summary>
		public void PlayDtmf(sbyte dtmf, int durationMs)
		{
			linphone_core_play_dtmf(nativePtr, dtmf, durationMs);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_play_local(IntPtr thiz, string audiofile);

		/// <summary>
		/// Plays an audio file to the local user. 
		/// </summary>
		public void PlayLocal(string audiofile)
		{
			int exception_result = linphone_core_play_local(nativePtr, audiofile);
			if (exception_result != 0) throw new LinphoneException("PlayLocal returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_preview_ogl_render(IntPtr thiz);

		/// <summary>
		/// Call generic OpenGL render for a given core. 
		/// </summary>
		public void PreviewOglRender()
		{
			linphone_core_preview_ogl_render(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_publish(IntPtr thiz, IntPtr resource, string ev, int expires, IntPtr body);

		/// <summary>
		/// Publish an event state. 
		/// </summary>
		public Linphone.Event Publish(Linphone.Address resource, string ev, int expires, Linphone.Content body)
		{
			IntPtr ptr = linphone_core_publish(nativePtr, resource != null ? resource.nativePtr : IntPtr.Zero, ev, expires, body != null ? body.nativePtr : IntPtr.Zero);
			Linphone.Event returnVal = fromNativePtr<Linphone.Event>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_core_realtime_text_get_keepalive_interval(IntPtr thiz);

		/// <summary>
		/// Gets keep alive interval of real time text. 
		/// </summary>
		public uint RealtimeTextGetKeepaliveInterval()
		{
			uint returnVal = linphone_core_realtime_text_get_keepalive_interval(nativePtr);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_realtime_text_set_keepalive_interval(IntPtr thiz, uint interval);

		/// <summary>
		/// Set keep alive interval for real time text. 
		/// </summary>
		public void RealtimeTextSetKeepaliveInterval(uint interval)
		{
			linphone_core_realtime_text_set_keepalive_interval(nativePtr, interval);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_redirect_call(IntPtr thiz, IntPtr call, string redirectUri);

		/// <summary>
		/// Redirect the specified call to the given redirect URI. 
		/// </summary>
		public void RedirectCall(Linphone.Call call, string redirectUri)
		{
			int exception_result = linphone_core_redirect_call(nativePtr, call != null ? call.nativePtr : IntPtr.Zero, redirectUri);
			if (exception_result != 0) throw new LinphoneException("RedirectCall returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_refresh_registers(IntPtr thiz);

		/// <summary>
		/// force registration refresh to be initiated upon next iterate 
		/// </summary>
		public void RefreshRegisters()
		{
			linphone_core_refresh_registers(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_reject_subscriber(IntPtr thiz, IntPtr lf);

		/// <summary>
		/// Black list a friend. 
		/// </summary>
		public void RejectSubscriber(Linphone.Friend lf)
		{
			linphone_core_reject_subscriber(nativePtr, lf != null ? lf.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_reload_ms_plugins(IntPtr thiz, string path);

		/// <summary>
		/// Reload mediastreamer2 plugins from specified directory. 
		/// </summary>
		public void ReloadMsPlugins(string path)
		{
			linphone_core_reload_ms_plugins(nativePtr, path);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_reload_sound_devices(IntPtr thiz);

		/// <summary>
		/// Update detection of sound devices. 
		/// </summary>
		public void ReloadSoundDevices()
		{
			linphone_core_reload_sound_devices(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_reload_video_devices(IntPtr thiz);

		/// <summary>
		/// Update detection of camera devices. 
		/// </summary>
		public void ReloadVideoDevices()
		{
			linphone_core_reload_video_devices(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_remove_auth_info(IntPtr thiz, IntPtr info);

		/// <summary>
		/// Removes an authentication information object. 
		/// </summary>
		public void RemoveAuthInfo(Linphone.AuthInfo info)
		{
			linphone_core_remove_auth_info(nativePtr, info != null ? info.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_remove_call_log(IntPtr thiz, IntPtr callLog);

		/// <summary>
		/// Remove a specific call log from call history list. 
		/// </summary>
		public void RemoveCallLog(Linphone.CallLog callLog)
		{
			linphone_core_remove_call_log(nativePtr, callLog != null ? callLog.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_remove_friend_list(IntPtr thiz, IntPtr list);

		/// <summary>
		/// Removes a friend list. 
		/// </summary>
		public void RemoveFriendList(Linphone.FriendList list)
		{
			linphone_core_remove_friend_list(nativePtr, list != null ? list.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_remove_from_conference(IntPtr thiz, IntPtr call);

		/// <summary>
		/// Remove a call from the conference. 
		/// </summary>
		public void RemoveFromConference(Linphone.Call call)
		{
			int exception_result = linphone_core_remove_from_conference(nativePtr, call != null ? call.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("RemoveFromConference returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_remove_linphone_spec(IntPtr thiz, string spec);

		/// <summary>
		/// Remove the given linphone specs from the list of functionalities the linphone
		/// client supports. 
		/// </summary>
		public void RemoveLinphoneSpec(string spec)
		{
			linphone_core_remove_linphone_spec(nativePtr, spec);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_remove_proxy_config(IntPtr thiz, IntPtr config);

		/// <summary>
		/// Removes a proxy configuration. 
		/// </summary>
		public void RemoveProxyConfig(Linphone.ProxyConfig config)
		{
			linphone_core_remove_proxy_config(nativePtr, config != null ? config.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_remove_supported_tag(IntPtr thiz, string tag);

		/// <summary>
		/// Remove a supported tag. 
		/// </summary>
		public void RemoveSupportedTag(string tag)
		{
			linphone_core_remove_supported_tag(nativePtr, tag);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_reset_missed_calls_count(IntPtr thiz);

		/// <summary>
		/// Reset the counter of missed calls. 
		/// </summary>
		public void ResetMissedCallsCount()
		{
			linphone_core_reset_missed_calls_count(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_resume_call(IntPtr thiz, IntPtr call);

		/// <summary>
		/// Resumes a call. 
		/// </summary>
		public void ResumeCall(Linphone.Call call)
		{
			int exception_result = linphone_core_resume_call(nativePtr, call != null ? call.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("ResumeCall returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_audio_port_range(IntPtr thiz, int minPort, int maxPort);

		/// <summary>
		/// Sets the UDP port range from which to randomly select the port used for audio
		/// streaming. 
		/// </summary>
		public void SetAudioPortRange(int minPort, int maxPort)
		{
			linphone_core_set_audio_port_range(nativePtr, minPort, maxPort);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_call_error_tone(IntPtr thiz, int reason, string audiofile);

		/// <summary>
		/// Assign an audio file to be played locally upon call failure, for a given
		/// reason. 
		/// </summary>
		public void SetCallErrorTone(Linphone.Reason reason, string audiofile)
		{
			linphone_core_set_call_error_tone(nativePtr, (int)reason, audiofile);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_qrcode_decode_rect(IntPtr thiz, int x, int y, int w, int h);

		/// <summary>
		/// Set the rectangle where the decoder will search a QRCode. 
		/// </summary>
		public void SetQrcodeDecodeRect(int x, int y, int w, int h)
		{
			linphone_core_set_qrcode_decode_rect(nativePtr, x, y, w, h);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_text_port_range(IntPtr thiz, int minPort, int maxPort);

		/// <summary>
		/// Sets the UDP port range from which to randomly select the port used for text
		/// streaming. 
		/// </summary>
		public void SetTextPortRange(int minPort, int maxPort)
		{
			linphone_core_set_text_port_range(nativePtr, minPort, maxPort);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_user_agent(IntPtr thiz, string uaName, string version);

		/// <summary>
		/// Set the user agent string used in SIP messages. 
		/// </summary>
		public void SetUserAgent(string uaName, string version)
		{
			linphone_core_set_user_agent(nativePtr, uaName, version);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_video_port_range(IntPtr thiz, int minPort, int maxPort);

		/// <summary>
		/// Sets the UDP port range from which to randomly select the port used for video
		/// streaming. 
		/// </summary>
		public void SetVideoPortRange(int minPort, int maxPort)
		{
			linphone_core_set_video_port_range(nativePtr, minPort, maxPort);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_sound_device_can_capture(IntPtr thiz, string device);

		/// <summary>
		/// Tells whether a specified sound device can capture sound. 
		/// </summary>
		public bool SoundDeviceCanCapture(string device)
		{
			bool returnVal = linphone_core_sound_device_can_capture(nativePtr, device) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_sound_device_can_playback(IntPtr thiz, string device);

		/// <summary>
		/// Tells whether a specified sound device can play sound. 
		/// </summary>
		public bool SoundDeviceCanPlayback(string device)
		{
			bool returnVal = linphone_core_sound_device_can_playback(nativePtr, device) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_sound_resources_locked(IntPtr thiz);

		/// <summary>
		/// Check if a call will need the sound resources in near future (typically an
		/// outgoing call that is awaiting response). 
		/// </summary>
		public bool SoundResourcesLocked()
		{
			bool returnVal = linphone_core_sound_resources_locked(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_start(IntPtr thiz);

		/// <summary>
		/// Start a <see cref="Linphone.Core" /> object after it has been instantiated and
		/// not automatically started. 
		/// </summary>
		public void Start()
		{
			int exception_result = linphone_core_start(nativePtr);
			if (exception_result != 0) throw new LinphoneException("Start returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_start_conference_recording(IntPtr thiz, string path);

		/// <summary>
		/// Start recording the running conference. 
		/// </summary>
		public void StartConferenceRecording(string path)
		{
			int exception_result = linphone_core_start_conference_recording(nativePtr, path);
			if (exception_result != 0) throw new LinphoneException("StartConferenceRecording returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_start_dtmf_stream(IntPtr thiz);

		/// <summary>
		/// Special function to warm up dtmf feeback stream. 
		/// </summary>
		public void StartDtmfStream()
		{
			linphone_core_start_dtmf_stream(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_start_echo_canceller_calibration(IntPtr thiz);

		/// <summary>
		/// Starts an echo calibration of the sound devices, in order to find adequate
		/// settings for the echo canceler automatically. 
		/// </summary>
		public void StartEchoCancellerCalibration()
		{
			int exception_result = linphone_core_start_echo_canceller_calibration(nativePtr);
			if (exception_result != 0) throw new LinphoneException("StartEchoCancellerCalibration returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_start_echo_tester(IntPtr thiz, uint rate);

		/// <summary>
		/// 停止
		/// Start the simulation of call to test the latency with an external device. 
		/// </summary>
		public void StartEchoTester(uint rate)
		{
			int exception_result = linphone_core_start_echo_tester(nativePtr, rate);
			if (exception_result != 0) throw new LinphoneException("StartEchoTester returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_stop(IntPtr thiz);

		/// <summary>
		/// 停止
		/// Stop a <see cref="Linphone.Core" /> object after it has been instantiated and
		/// started. 
		/// </summary>
		public void Stop()
		{
			linphone_core_stop(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_stop_async(IntPtr thiz);

		/// <summary>
		/// Stop asynchronously a <see cref="Linphone.Core" /> object after it has been
		/// instantiated and started. 
		/// </summary>
		public void StopAsync()
		{
			linphone_core_stop_async(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_stop_conference_recording(IntPtr thiz);

		/// <summary>
		/// Stop recording the running conference. 
		/// </summary>
		public void StopConferenceRecording()
		{
			int exception_result = linphone_core_stop_conference_recording(nativePtr);
			if (exception_result != 0) throw new LinphoneException("StopConferenceRecording returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_stop_dtmf(IntPtr thiz);

		/// <summary>
		/// Stops playing a dtmf started by <see cref="Linphone.Core.PlayDtmf()" />. 
		/// </summary>
		public void StopDtmf()
		{
			linphone_core_stop_dtmf(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_stop_dtmf_stream(IntPtr thiz);

		/// <summary>
		/// Special function to stop dtmf feed back function. 
		/// </summary>
		public void StopDtmfStream()
		{
			linphone_core_stop_dtmf_stream(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_stop_echo_tester(IntPtr thiz);

		/// <summary>
		/// Stop the simulation of call. 
		/// </summary>
		public void StopEchoTester()
		{
			int exception_result = linphone_core_stop_echo_tester(nativePtr);
			if (exception_result != 0) throw new LinphoneException("StopEchoTester returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_stop_ringing(IntPtr thiz);

		/// <summary>
		/// Whenever the liblinphone is playing a ring to advertise an incoming call or
		/// ringback of an outgoing call, this function stops the ringing. 
		/// </summary>
		public void StopRinging()
		{
			linphone_core_stop_ringing(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_core_subscribe(IntPtr thiz, IntPtr resource, string ev, int expires, IntPtr body);

		/// <summary>
		/// Create an outgoing subscription, specifying the destination resource, the event
		/// name, and an optional content body. 
		/// </summary>
		public Linphone.Event Subscribe(Linphone.Address resource, string ev, int expires, Linphone.Content body)
		{
			IntPtr ptr = linphone_core_subscribe(nativePtr, resource != null ? resource.nativePtr : IntPtr.Zero, ev, expires, body != null ? body.nativePtr : IntPtr.Zero);
			Linphone.Event returnVal = fromNativePtr<Linphone.Event>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_take_preview_snapshot(IntPtr thiz, string file);

		/// <summary>
		/// Take a photo of currently from capture device and write it into a jpeg file. 
		/// </summary>
		public void TakePreviewSnapshot(string file)
		{
			int exception_result = linphone_core_take_preview_snapshot(nativePtr, file);
			if (exception_result != 0) throw new LinphoneException("TakePreviewSnapshot returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_terminate_all_calls(IntPtr thiz);

		/// <summary>
		/// 挂断
		/// Terminates all the calls. 
		/// </summary>
		public void TerminateAllCalls()
		{
			int exception_result = linphone_core_terminate_all_calls(nativePtr);
			if (exception_result != 0) throw new LinphoneException("TerminateAllCalls returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_terminate_call(IntPtr thiz, IntPtr call);

		/// <summary>
		/// Terminates a call. 
		/// </summary>
		public void TerminateCall(Linphone.Call call)
		{
			int exception_result = linphone_core_terminate_call(nativePtr, call != null ? call.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("TerminateCall returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_terminate_conference(IntPtr thiz);

		/// <summary>
		/// Terminate the running conference. 
		/// </summary>
		public void TerminateConference()
		{
			int exception_result = linphone_core_terminate_conference(nativePtr);
			if (exception_result != 0) throw new LinphoneException("TerminateConference returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_transfer_call(IntPtr thiz, IntPtr call, string referTo);

		/// <summary>
		/// Performs a simple call transfer to the specified destination. 
		/// </summary>
		public void TransferCall(Linphone.Call call, string referTo)
		{
			int exception_result = linphone_core_transfer_call(nativePtr, call != null ? call.nativePtr : IntPtr.Zero, referTo);
			if (exception_result != 0) throw new LinphoneException("TransferCall returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_transfer_call_to_another(IntPtr thiz, IntPtr call, IntPtr dest);

		/// <summary>
		/// Transfers a call to destination of another running call. 
		/// </summary>
		public void TransferCallToAnother(Linphone.Call call, Linphone.Call dest)
		{
			int exception_result = linphone_core_transfer_call_to_another(nativePtr, call != null ? call.nativePtr : IntPtr.Zero, dest != null ? dest.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("TransferCallToAnother returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_core_update_call(IntPtr thiz, IntPtr call, IntPtr parameters);

		/// <summary>
		/// Updates a running call according to supplied call parameters or parameters
		/// changed in the LinphoneCore. 
		/// </summary>
		public void UpdateCall(Linphone.Call call, Linphone.CallParams parameters)
		{
			int exception_result = linphone_core_update_call(nativePtr, call != null ? call.nativePtr : IntPtr.Zero, parameters != null ? parameters.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("UpdateCall returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_upload_log_collection(IntPtr thiz);

		/// <summary>
		/// Upload the log collection to the configured server url. 
		/// </summary>
		public void UploadLogCollection()
		{
			linphone_core_upload_log_collection(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_use_preview_window(IntPtr thiz, char yesno);

		/// <summary>
		/// Tells the core to use a separate window for local camera preview video, instead
		/// of inserting local view within the remote video window. 
		/// </summary>
		public void UsePreviewWindow(bool yesno)
		{
			linphone_core_use_preview_window(nativePtr, yesno ? (char)1 : (char)0);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_verify_server_certificates(IntPtr thiz, char yesno);

		/// <summary>
		/// Specify whether the tls server certificate must be verified when connecting to
		/// a SIP/TLS server. 
		/// </summary>
		public void VerifyServerCertificates(bool yesno)
		{
			linphone_core_verify_server_certificates(nativePtr, yesno ? (char)1 : (char)0);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_verify_server_cn(IntPtr thiz, char yesno);

		/// <summary>
		/// Specify whether the tls server certificate common name must be verified when
		/// connecting to a SIP/TLS server. 
		/// </summary>
		public void VerifyServerCn(bool yesno)
		{
			linphone_core_verify_server_cn(nativePtr, yesno ? (char)1 : (char)0);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_core_video_supported(IntPtr thiz);

		/// <summary>
		/// Test if video is supported. 
		/// </summary>
		public bool VideoSupported()
		{
			bool returnVal = linphone_core_video_supported(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
	}
	/// <summary>
	/// Represents a dial plan. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class DialPlan : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_dial_plan_by_ccc(string ccc);

		/// <summary>
		/// Find best match for given CCC. 
		/// </summary>
		public static Linphone.DialPlan ByCcc(string ccc)
		{
			IntPtr ptr = linphone_dial_plan_by_ccc(ccc);
			Linphone.DialPlan returnVal = fromNativePtr<Linphone.DialPlan>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_dial_plan_by_ccc_as_int(int ccc);

		/// <summary>
		/// Find best match for given CCC. 
		/// </summary>
		public static Linphone.DialPlan ByCccAsInt(int ccc)
		{
			IntPtr ptr = linphone_dial_plan_by_ccc_as_int(ccc);
			Linphone.DialPlan returnVal = fromNativePtr<Linphone.DialPlan>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_dial_plan_get_all_list();

		static public IEnumerable<Linphone.DialPlan> AllList
		{
			get
			{
				return MarshalBctbxList<Linphone.DialPlan>(linphone_dial_plan_get_all_list());
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_dial_plan_lookup_ccc_from_e164(string e164);

		/// <summary>
		/// Function to get call country code from an e164 number, ex: +33952650121 will
		/// return 33. 
		/// </summary>
		public static int LookupCccFromE164(string e164)
		{
			int returnVal = linphone_dial_plan_lookup_ccc_from_e164(e164);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_dial_plan_lookup_ccc_from_iso(string iso);

		/// <summary>
		/// Function to get call country code from ISO 3166-1 alpha-2 code, ex: FR returns
		/// 33. 
		/// </summary>
		public static int LookupCccFromIso(string iso)
		{
			int returnVal = linphone_dial_plan_lookup_ccc_from_iso(iso);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_dial_plan_get_country(IntPtr thiz);

		/// <summary>
		/// Returns the country name of the dialplan. 
		/// </summary>
		public string Country
		{
			get
			{
				IntPtr stringPtr = linphone_dial_plan_get_country(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_dial_plan_get_country_calling_code(IntPtr thiz);

		/// <summary>
		/// Returns the country calling code of the dialplan. 
		/// </summary>
		public string CountryCallingCode
		{
			get
			{
				IntPtr stringPtr = linphone_dial_plan_get_country_calling_code(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_dial_plan_get_international_call_prefix(IntPtr thiz);

		/// <summary>
		/// Returns the international call prefix of the dialplan. 
		/// </summary>
		public string InternationalCallPrefix
		{
			get
			{
				IntPtr stringPtr = linphone_dial_plan_get_international_call_prefix(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_dial_plan_is_generic(IntPtr thiz);

		/// <summary>
		/// Return if given plan is generic. 
		/// </summary>
		public bool IsGeneric
		{
			get
			{
				return linphone_dial_plan_is_generic(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_dial_plan_get_iso_country_code(IntPtr thiz);

		/// <summary>
		/// Returns the iso country code of the dialplan. 
		/// </summary>
		public string IsoCountryCode
		{
			get
			{
				IntPtr stringPtr = linphone_dial_plan_get_iso_country_code(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_dial_plan_get_national_number_length(IntPtr thiz);

		/// <summary>
		/// Returns the national number length of the dialplan. 
		/// </summary>
		public int NationalNumberLength
		{
			get
			{
				return linphone_dial_plan_get_national_number_length(nativePtr);
			}
		}
	}
	/// <summary>
	/// Object representing full details about a signaling error or status. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class ErrorInfo : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_error_info_get_phrase(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_error_info_set_phrase(IntPtr thiz, string phrase);

		/// <summary>
		/// Get textual phrase from the error info. 
		/// </summary>
		public string Phrase
		{
			get
			{
				IntPtr stringPtr = linphone_error_info_get_phrase(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_error_info_set_phrase(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_error_info_get_protocol(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_error_info_set_protocol(IntPtr thiz, string proto);

		/// <summary>
		/// Get protocol from the error info. 
		/// </summary>
		public string Protocol
		{
			get
			{
				IntPtr stringPtr = linphone_error_info_get_protocol(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_error_info_set_protocol(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_error_info_get_protocol_code(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_error_info_set_protocol_code(IntPtr thiz, int code);

		/// <summary>
		/// Get the status code from the low level protocol (ex a SIP status code). 
		/// </summary>
		public int ProtocolCode
		{
			get
			{
				return linphone_error_info_get_protocol_code(nativePtr);
			}
			set
			{
				linphone_error_info_set_protocol_code(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.Reason linphone_error_info_get_reason(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_error_info_set_reason(IntPtr thiz, int reason);

		/// <summary>
		/// Get reason code from the error info. 
		/// </summary>
		public Linphone.Reason Reason
		{
			get
			{
				return linphone_error_info_get_reason(nativePtr);
			}
			set
			{
				linphone_error_info_set_reason(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_error_info_get_retry_after(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_error_info_set_retry_after(IntPtr thiz, int retryAfter);

		/// <summary>
		/// Get Retry-After delay second from the error info. 
		/// </summary>
		public int RetryAfter
		{
			get
			{
				return linphone_error_info_get_retry_after(nativePtr);
			}
			set
			{
				linphone_error_info_set_retry_after(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_error_info_get_sub_error_info(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_error_info_set_sub_error_info(IntPtr thiz, IntPtr appendedEi);

		/// <summary>
		/// Get pointer to chained <see cref="Linphone.ErrorInfo" /> set in sub_ei. 
		/// </summary>
		public Linphone.ErrorInfo SubErrorInfo
		{
			get
			{
				IntPtr ptr = linphone_error_info_get_sub_error_info(nativePtr);
				Linphone.ErrorInfo obj = fromNativePtr<Linphone.ErrorInfo>(ptr, true);
				return obj;
			}
			set
			{
				linphone_error_info_set_sub_error_info(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_error_info_get_warnings(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_error_info_set_warnings(IntPtr thiz, string warnings);

		/// <summary>
		/// Provides additional information regarding the failure. 
		/// </summary>
		public string Warnings
		{
			get
			{
				IntPtr stringPtr = linphone_error_info_get_warnings(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_error_info_set_warnings(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_error_info_set(IntPtr thiz, string protocol, int reason, int code, string statusString, string warning);

		/// <summary>
		/// Assign information to a <see cref="Linphone.ErrorInfo" /> object. 
		/// </summary>
		public void Set(string protocol, Linphone.Reason reason, int code, string statusString, string warning)
		{
			linphone_error_info_set(nativePtr, protocol, (int)reason, code, statusString, warning);
			
			
			
		}
	}
	/// <summary>
	/// Object representing an event state, which is subcribed or published. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Event : LinphoneObject
	{

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_event_cbs(IntPtr factory);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_add_callbacks(IntPtr thiz, IntPtr cbs);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_remove_callbacks(IntPtr thiz, IntPtr cbs);

		~Event() 
		{
			if (listener != null)
			{
				linphone_event_remove_callbacks(nativePtr, listener.nativePtr);
			}
		}

		private EventListener listener;

		public EventListener Listener
		{
			get {
				if (listener == null)
				{
					IntPtr nativeListener = linphone_factory_create_event_cbs(IntPtr.Zero);
					listener = fromNativePtr<EventListener>(nativeListener, false);
					linphone_event_add_callbacks(nativePtr, nativeListener);
					listener.register();
				}
				return listener;
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_get_core(IntPtr thiz);

		/// <summary>
		/// Returns back pointer to the <see cref="Linphone.Core" /> that created this <see
		/// cref="Linphone.Event" />. 
		/// </summary>
		public Linphone.Core Core
		{
			get
			{
				IntPtr ptr = linphone_event_get_core(nativePtr);
				Linphone.Core obj = fromNativePtr<Linphone.Core>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_get_current_callbacks(IntPtr thiz);

		/// <summary>
		/// Get the current LinphoneEventCbs object associated with a LinphoneEvent. 
		/// </summary>
		public Linphone.EventListener CurrentCallbacks
		{
			get
			{
				IntPtr ptr = linphone_event_get_current_callbacks(nativePtr);
				Linphone.EventListener obj = fromNativePtr<Linphone.EventListener>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_get_error_info(IntPtr thiz);

		/// <summary>
		/// Get full details about an error occured. 
		/// </summary>
		public Linphone.ErrorInfo ErrorInfo
		{
			get
			{
				IntPtr ptr = linphone_event_get_error_info(nativePtr);
				Linphone.ErrorInfo obj = fromNativePtr<Linphone.ErrorInfo>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_get_from(IntPtr thiz);

		/// <summary>
		/// Get the "from" address of the subscription. 
		/// </summary>
		public Linphone.Address From
		{
			get
			{
				IntPtr ptr = linphone_event_get_from(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_get_name(IntPtr thiz);

		/// <summary>
		/// Get the name of the event as specified in the event package RFC. 
		/// </summary>
		public string Name
		{
			get
			{
				IntPtr stringPtr = linphone_event_get_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.PublishState linphone_event_get_publish_state(IntPtr thiz);

		/// <summary>
		/// Get publish state. 
		/// </summary>
		public Linphone.PublishState PublishState
		{
			get
			{
				return linphone_event_get_publish_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.Reason linphone_event_get_reason(IntPtr thiz);

		/// <summary>
		/// Return reason code (in case of error state reached). 
		/// </summary>
		public Linphone.Reason Reason
		{
			get
			{
				return linphone_event_get_reason(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_get_remote_contact(IntPtr thiz);

		/// <summary>
		/// Get the "contact" address of the subscription. 
		/// </summary>
		public Linphone.Address RemoteContact
		{
			get
			{
				IntPtr ptr = linphone_event_get_remote_contact(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_get_resource(IntPtr thiz);

		/// <summary>
		/// Get the resource address of the subscription or publish. 
		/// </summary>
		public Linphone.Address Resource
		{
			get
			{
				IntPtr ptr = linphone_event_get_resource(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.SubscriptionDir linphone_event_get_subscription_dir(IntPtr thiz);

		/// <summary>
		/// Get subscription direction. 
		/// </summary>
		public Linphone.SubscriptionDir SubscriptionDir
		{
			get
			{
				return linphone_event_get_subscription_dir(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.SubscriptionState linphone_event_get_subscription_state(IntPtr thiz);

		/// <summary>
		/// Get subscription state. 
		/// </summary>
		public Linphone.SubscriptionState SubscriptionState
		{
			get
			{
				return linphone_event_get_subscription_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_event_accept_subscription(IntPtr thiz);

		/// <summary>
		/// Accept an incoming subcription. 
		/// </summary>
		public void AcceptSubscription()
		{
			int exception_result = linphone_event_accept_subscription(nativePtr);
			if (exception_result != 0) throw new LinphoneException("AcceptSubscription returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_event_add_custom_header(IntPtr thiz, string name, string val);

		/// <summary>
		/// Add a custom header to an outgoing susbscription or publish. 
		/// </summary>
		public void AddCustomHeader(string name, string val)
		{
			linphone_event_add_custom_header(nativePtr, name, val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_event_deny_subscription(IntPtr thiz, int reason);

		/// <summary>
		/// Deny an incoming subscription with given reason. 
		/// </summary>
		public void DenySubscription(Linphone.Reason reason)
		{
			int exception_result = linphone_event_deny_subscription(nativePtr, (int)reason);
			if (exception_result != 0) throw new LinphoneException("DenySubscription returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_get_custom_header(IntPtr thiz, string name);

		/// <summary>
		/// Obtain the value of a given header for an incoming subscription. 
		/// </summary>
		public string GetCustomHeader(string name)
		{
			IntPtr stringPtr = linphone_event_get_custom_header(nativePtr, name);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_event_notify(IntPtr thiz, IntPtr body);

		/// <summary>
		/// Send a notification. 
		/// </summary>
		public void Notify(Linphone.Content body)
		{
			int exception_result = linphone_event_notify(nativePtr, body != null ? body.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("Notify returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_event_pause_publish(IntPtr thiz);

		/// <summary>
		/// Prevent an event from refreshing its publish. 
		/// </summary>
		public void PausePublish()
		{
			linphone_event_pause_publish(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_event_refresh_publish(IntPtr thiz);

		/// <summary>
		/// Refresh an outgoing publish keeping the same body. 
		/// </summary>
		public void RefreshPublish()
		{
			int exception_result = linphone_event_refresh_publish(nativePtr);
			if (exception_result != 0) throw new LinphoneException("RefreshPublish returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_event_refresh_subscribe(IntPtr thiz);

		/// <summary>
		/// Refresh an outgoing subscription keeping the same body. 
		/// </summary>
		public void RefreshSubscribe()
		{
			int exception_result = linphone_event_refresh_subscribe(nativePtr);
			if (exception_result != 0) throw new LinphoneException("RefreshSubscribe returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_event_send_publish(IntPtr thiz, IntPtr body);

		/// <summary>
		/// Send a publish created by <see cref="Linphone.Core.CreatePublish()" />. 
		/// </summary>
		public void SendPublish(Linphone.Content body)
		{
			int exception_result = linphone_event_send_publish(nativePtr, body != null ? body.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("SendPublish returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_event_send_subscribe(IntPtr thiz, IntPtr body);

		/// <summary>
		/// Send a subscription previously created by <see
		/// cref="Linphone.Core.CreateSubscribe()" />. 
		/// </summary>
		public void SendSubscribe(Linphone.Content body)
		{
			int exception_result = linphone_event_send_subscribe(nativePtr, body != null ? body.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("SendSubscribe returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_event_terminate(IntPtr thiz);

		/// <summary>
		/// Terminate an incoming or outgoing subscription that was previously acccepted,
		/// or a previous publication. 
		/// </summary>
		public void Terminate()
		{
			linphone_event_terminate(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_event_update_publish(IntPtr thiz, IntPtr body);

		/// <summary>
		/// Update (refresh) a publish. 
		/// </summary>
		public void UpdatePublish(Linphone.Content body)
		{
			int exception_result = linphone_event_update_publish(nativePtr, body != null ? body.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("UpdatePublish returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_event_update_subscribe(IntPtr thiz, IntPtr body);

		/// <summary>
		/// Update (refresh) an outgoing subscription, changing the body. 
		/// </summary>
		public void UpdateSubscribe(Linphone.Content body)
		{
			int exception_result = linphone_event_update_subscribe(nativePtr, body != null ? body.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("UpdateSubscribe returned value" + exception_result);
			
			
		}
	}
	/// <summary>
	/// Base object of events. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class EventLog : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_log_get_call(IntPtr thiz);

		/// <summary>
		/// Returns the call of a conference call event. 
		/// </summary>
		public Linphone.Call Call
		{
			get
			{
				IntPtr ptr = linphone_event_log_get_call(nativePtr);
				Linphone.Call obj = fromNativePtr<Linphone.Call>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_log_get_chat_message(IntPtr thiz);

		/// <summary>
		/// Returns the chat message of a conference chat message event. 
		/// </summary>
		public Linphone.ChatMessage ChatMessage
		{
			get
			{
				IntPtr ptr = linphone_event_log_get_chat_message(nativePtr);
				Linphone.ChatMessage obj = fromNativePtr<Linphone.ChatMessage>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_event_log_get_creation_time(IntPtr thiz);

		/// <summary>
		/// Returns the creation time of a event log. 
		/// </summary>
		public long CreationTime
		{
			get
			{
				return linphone_event_log_get_creation_time(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_log_get_device_address(IntPtr thiz);

		/// <summary>
		/// Returns the device address of a conference participant device event. 
		/// </summary>
		public Linphone.Address DeviceAddress
		{
			get
			{
				IntPtr ptr = linphone_event_log_get_device_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_event_log_get_ephemeral_message_lifetime(IntPtr thiz);

		/// <summary>
		/// Returns the ephemeral message lifetime of a conference ephemeral message event. 
		/// </summary>
		public int EphemeralMessageLifetime
		{
			get
			{
				return linphone_event_log_get_ephemeral_message_lifetime(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_log_get_local_address(IntPtr thiz);

		/// <summary>
		/// Returns the local address of a conference event. 
		/// </summary>
		public Linphone.Address LocalAddress
		{
			get
			{
				IntPtr ptr = linphone_event_log_get_local_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_event_log_get_notify_id(IntPtr thiz);

		/// <summary>
		/// Returns the notify id of a conference notified event. 
		/// </summary>
		public uint NotifyId
		{
			get
			{
				return linphone_event_log_get_notify_id(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_log_get_participant_address(IntPtr thiz);

		/// <summary>
		/// Returns the participant address of a conference participant event. 
		/// </summary>
		public Linphone.Address ParticipantAddress
		{
			get
			{
				IntPtr ptr = linphone_event_log_get_participant_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_log_get_peer_address(IntPtr thiz);

		/// <summary>
		/// Returns the peer address of a conference event. 
		/// </summary>
		public Linphone.Address PeerAddress
		{
			get
			{
				IntPtr ptr = linphone_event_log_get_peer_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_log_get_security_event_faulty_device_address(IntPtr thiz);

		/// <summary>
		/// Returns the faulty device address of a conference security event. 
		/// </summary>
		public Linphone.Address SecurityEventFaultyDeviceAddress
		{
			get
			{
				IntPtr ptr = linphone_event_log_get_security_event_faulty_device_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.SecurityEventType linphone_event_log_get_security_event_type(IntPtr thiz);

		/// <summary>
		/// Returns the type of security event. 
		/// </summary>
		public Linphone.SecurityEventType SecurityEventType
		{
			get
			{
				return linphone_event_log_get_security_event_type(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_event_log_get_subject(IntPtr thiz);

		/// <summary>
		/// Returns the subject of a conference subject event. 
		/// </summary>
		public string Subject
		{
			get
			{
				IntPtr stringPtr = linphone_event_log_get_subject(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.EventLogType linphone_event_log_get_type(IntPtr thiz);

		/// <summary>
		/// Returns the type of a event log. 
		/// </summary>
		public Linphone.EventLogType Type
		{
			get
			{
				return linphone_event_log_get_type(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_event_log_delete_from_database(IntPtr thiz);

		/// <summary>
		/// Delete event log from database. 
		/// </summary>
		public void DeleteFromDatabase()
		{
			linphone_event_log_delete_from_database(nativePtr);
			
			
			
		}
	}
	/// <summary>
	/// <see cref="Linphone.Factory" /> is a singleton object devoted to the creation
	/// of all the object of Liblinphone that cannot be created by <see
	/// cref="Linphone.Core" /> itself. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Factory : LinphoneObject
	{
#if  __ANDROID__
        static Factory()
        {
            Java.Lang.JavaSystem.LoadLibrary("c++_shared");
            Java.Lang.JavaSystem.LoadLibrary("bctoolbox");
            Java.Lang.JavaSystem.LoadLibrary("ortp");
            Java.Lang.JavaSystem.LoadLibrary("mediastreamer");
            Java.Lang.JavaSystem.LoadLibrary("linphone");
        }
#endif


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_factory_clean();

		/// <summary>
		/// Clean the factory. 
		/// </summary>
		public static void Clean()
		{
			linphone_factory_clean();
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get();

		/// <summary>
		/// Create the <see cref="Linphone.Factory" /> if that has not been done and return
		/// a pointer on it. 
		/// </summary>
		static public Linphone.Factory Instance
		{
			get
			{
				IntPtr ptr = linphone_factory_get();
				Linphone.Factory obj = fromNativePtr<Linphone.Factory>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_data_resources_dir(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_factory_set_data_resources_dir(IntPtr thiz, string path);

		/// <summary>
		/// Get the directory where the data resources are located. 
		/// </summary>
		public string DataResourcesDir
		{
			get
			{
				IntPtr stringPtr = linphone_factory_get_data_resources_dir(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_factory_set_data_resources_dir(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_dial_plans(IntPtr thiz);

		/// <summary>
		/// Returns a bctbx_list_t of all DialPlans. 
		/// </summary>
		public IEnumerable<Linphone.DialPlan> DialPlans
		{
			get
			{
				return MarshalBctbxList<Linphone.DialPlan>(linphone_factory_get_dial_plans(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_image_resources_dir(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_factory_set_image_resources_dir(IntPtr thiz, string path);

		/// <summary>
		/// Get the directory where the image resources are located. 
		/// </summary>
		public string ImageResourcesDir
		{
			get
			{
				IntPtr stringPtr = linphone_factory_get_image_resources_dir(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_factory_set_image_resources_dir(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_factory_is_database_storage_available(IntPtr thiz);

		/// <summary>
		/// Indicates if the storage in database is available. 
		/// </summary>
		public bool IsDatabaseStorageAvailable
		{
			get
			{
				return linphone_factory_is_database_storage_available(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_factory_is_imdn_available(IntPtr thiz);

		/// <summary>
		/// Indicates if IMDN are available. 
		/// </summary>
		public bool IsImdnAvailable
		{
			get
			{
				return linphone_factory_is_imdn_available(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_factory_set_log_collection_path(IntPtr thiz, string path);

		/// <summary>
		/// Sets the log collection path. 
		/// </summary>
		public string LogCollectionPath
		{
			set
			{
				linphone_factory_set_log_collection_path(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_msplugins_dir(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_factory_set_msplugins_dir(IntPtr thiz, string path);

		/// <summary>
		/// Get the directory where the mediastreamer2 plugins are located. 
		/// </summary>
		public string MspluginsDir
		{
			get
			{
				IntPtr stringPtr = linphone_factory_get_msplugins_dir(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_factory_set_msplugins_dir(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_ring_resources_dir(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_factory_set_ring_resources_dir(IntPtr thiz, string path);

		/// <summary>
		/// Get the directory where the ring resources are located. 
		/// </summary>
		public string RingResourcesDir
		{
			get
			{
				IntPtr stringPtr = linphone_factory_get_ring_resources_dir(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_factory_set_ring_resources_dir(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_sound_resources_dir(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_factory_set_sound_resources_dir(IntPtr thiz, string path);

		/// <summary>
		/// Get the directory where the sound resources are located. 
		/// </summary>
		public string SoundResourcesDir
		{
			get
			{
				IntPtr stringPtr = linphone_factory_get_sound_resources_dir(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_factory_set_sound_resources_dir(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_supported_video_definitions(IntPtr thiz);

		/// <summary>
		/// Get the list of standard video definitions supported by Linphone. 
		/// </summary>
		public IEnumerable<Linphone.VideoDefinition> SupportedVideoDefinitions
		{
			get
			{
				return MarshalBctbxList<Linphone.VideoDefinition>(linphone_factory_get_supported_video_definitions(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_top_resources_dir(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_factory_set_top_resources_dir(IntPtr thiz, string path);

		/// <summary>
		/// Get the top directory where the resources are located. 
		/// </summary>
		public string TopResourcesDir
		{
			get
			{
				IntPtr stringPtr = linphone_factory_get_top_resources_dir(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_factory_set_top_resources_dir(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_address(IntPtr thiz, string addr);

		/// <summary>
		/// Parse a string holding a SIP URI and create the according <see
		/// cref="Linphone.Address" /> object. 
		/// </summary>
		public Linphone.Address CreateAddress(string addr)
		{
			IntPtr ptr = linphone_factory_create_address(nativePtr, addr);
			Linphone.Address returnVal = fromNativePtr<Linphone.Address>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_auth_info_2(IntPtr thiz, string username, string userid, string passwd, string ha1, string realm, string domain, string algorithm);

		/// <summary>
		/// Creates a <see cref="Linphone.AuthInfo" /> object. 
		/// </summary>
		public Linphone.AuthInfo CreateAuthInfo(string username, string userid, string passwd, string ha1, string realm, string domain, string algorithm)
		{
			IntPtr ptr = linphone_factory_create_auth_info_2(nativePtr, username, userid, passwd, ha1, realm, domain, algorithm);
			Linphone.AuthInfo returnVal = fromNativePtr<Linphone.AuthInfo>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_auth_info(IntPtr thiz, string username, string userid, string passwd, string ha1, string realm, string domain);

		/// <summary>
		/// Creates a <see cref="Linphone.AuthInfo" /> object. 
		/// </summary>
		public Linphone.AuthInfo CreateAuthInfo(string username, string userid, string passwd, string ha1, string realm, string domain)
		{
			IntPtr ptr = linphone_factory_create_auth_info(nativePtr, username, userid, passwd, ha1, realm, domain);
			Linphone.AuthInfo returnVal = fromNativePtr<Linphone.AuthInfo>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_buffer(IntPtr thiz);

		/// <summary>
		/// Creates an object <see cref="Linphone.Buffer" />. 
		/// </summary>
		public Linphone.Buffer CreateBuffer()
		{
			IntPtr ptr = linphone_factory_create_buffer(nativePtr);
			Linphone.Buffer returnVal = fromNativePtr<Linphone.Buffer>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_buffer_from_data(IntPtr thiz, uint data, long size);

		/// <summary>
		/// Creates an object <see cref="Linphone.Buffer" />. 
		/// </summary>
		public Linphone.Buffer CreateBufferFromData(uint data, long size)
		{
			IntPtr ptr = linphone_factory_create_buffer_from_data(nativePtr, data, size);
			Linphone.Buffer returnVal = fromNativePtr<Linphone.Buffer>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_buffer_from_string(IntPtr thiz, string data);

		/// <summary>
		/// Creates an object <see cref="Linphone.Buffer" />. 
		/// </summary>
		public Linphone.Buffer CreateBufferFromString(string data)
		{
			IntPtr ptr = linphone_factory_create_buffer_from_string(nativePtr, data);
			Linphone.Buffer returnVal = fromNativePtr<Linphone.Buffer>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_config(IntPtr thiz, string path);

		/// <summary>
		/// Creates an object <see cref="Linphone.Config" />. 
		/// </summary>
		public Linphone.Config CreateConfig(string path)
		{
			IntPtr ptr = linphone_factory_create_config(nativePtr, path);
			Linphone.Config returnVal = fromNativePtr<Linphone.Config>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_config_from_string(IntPtr thiz, string data);

		/// <summary>
		/// Creates an object <see cref="Linphone.Config" />. 
		/// </summary>
		public Linphone.Config CreateConfigFromString(string data)
		{
			IntPtr ptr = linphone_factory_create_config_from_string(nativePtr, data);
			Linphone.Config returnVal = fromNativePtr<Linphone.Config>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_config_with_factory(IntPtr thiz, string path, string factoryPath);

		/// <summary>
		/// Creates an object <see cref="Linphone.Config" />. 
		/// </summary>
		public Linphone.Config CreateConfigWithFactory(string path, string factoryPath)
		{
			IntPtr ptr = linphone_factory_create_config_with_factory(nativePtr, path, factoryPath);
			Linphone.Config returnVal = fromNativePtr<Linphone.Config>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_content(IntPtr thiz);

		/// <summary>
		/// Creates an object <see cref="Linphone.Content" />. 
		/// </summary>
		public Linphone.Content CreateContent()
		{
			IntPtr ptr = linphone_factory_create_content(nativePtr);
			Linphone.Content returnVal = fromNativePtr<Linphone.Content>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_core(IntPtr thiz, IntPtr cbs, string configPath, string factoryConfigPath);

		/// <summary>
		/// Instanciate a <see cref="Linphone.Core" /> object. 
		/// </summary>
		public Linphone.Core CreateCore(Linphone.CoreListener cbs, string configPath, string factoryConfigPath)
		{
			IntPtr ptr = linphone_factory_create_core(nativePtr, cbs != null ? cbs.nativePtr : IntPtr.Zero, configPath, factoryConfigPath);
			Linphone.Core returnVal = fromNativePtr<Linphone.Core>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_core_2(IntPtr thiz, IntPtr cbs, string configPath, string factoryConfigPath, IntPtr userData, IntPtr systemContext);

		/// <summary>
		/// Instanciate a <see cref="Linphone.Core" /> object. 
		/// </summary>
		public Linphone.Core CreateCore(Linphone.CoreListener cbs, string configPath, string factoryConfigPath, IntPtr userData, IntPtr systemContext)
		{
			IntPtr ptr = linphone_factory_create_core_2(nativePtr, cbs != null ? cbs.nativePtr : IntPtr.Zero, configPath, factoryConfigPath, userData, systemContext);
			Linphone.Core returnVal = fromNativePtr<Linphone.Core>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_core_3(IntPtr thiz, string configPath, string factoryConfigPath, IntPtr systemContext);

		/// <summary>
		/// Instantiate a <see cref="Linphone.Core" /> object. 
		/// </summary>
		public Linphone.Core CreateCore(string configPath, string factoryConfigPath, IntPtr systemContext)
		{
			IntPtr ptr = linphone_factory_create_core_3(nativePtr, configPath, factoryConfigPath, systemContext);
			Linphone.Core returnVal = fromNativePtr<Linphone.Core>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_core_with_config_2(IntPtr thiz, IntPtr cbs, IntPtr config, IntPtr userData, IntPtr systemContext);

		/// <summary>
		/// Instantiates a <see cref="Linphone.Core" /> object with a given LpConfig. 
		/// </summary>
		public Linphone.Core CreateCoreWithConfig(Linphone.CoreListener cbs, Linphone.Config config, IntPtr userData, IntPtr systemContext)
		{
			IntPtr ptr = linphone_factory_create_core_with_config_2(nativePtr, cbs != null ? cbs.nativePtr : IntPtr.Zero, config != null ? config.nativePtr : IntPtr.Zero, userData, systemContext);
			Linphone.Core returnVal = fromNativePtr<Linphone.Core>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_core_with_config_3(IntPtr thiz, IntPtr config, IntPtr systemContext);

		/// <summary>
		/// Instantiate a <see cref="Linphone.Core" /> object with a given LinphoneConfig. 
		/// </summary>
		public Linphone.Core CreateCoreWithConfig(Linphone.Config config, IntPtr systemContext)
		{
			IntPtr ptr = linphone_factory_create_core_with_config_3(nativePtr, config != null ? config.nativePtr : IntPtr.Zero, systemContext);
			Linphone.Core returnVal = fromNativePtr<Linphone.Core>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_core_with_config(IntPtr thiz, IntPtr cbs, IntPtr config);

		/// <summary>
		/// Instantiates a <see cref="Linphone.Core" /> object with a given LpConfig. 
		/// </summary>
		public Linphone.Core CreateCoreWithConfig(Linphone.CoreListener cbs, Linphone.Config config)
		{
			IntPtr ptr = linphone_factory_create_core_with_config(nativePtr, cbs != null ? cbs.nativePtr : IntPtr.Zero, config != null ? config.nativePtr : IntPtr.Zero);
			Linphone.Core returnVal = fromNativePtr<Linphone.Core>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_error_info(IntPtr thiz);

		/// <summary>
		/// Creates an object LinphoneErrorInfo. 
		/// </summary>
		public Linphone.ErrorInfo CreateErrorInfo()
		{
			IntPtr ptr = linphone_factory_create_error_info(nativePtr);
			Linphone.ErrorInfo returnVal = fromNativePtr<Linphone.ErrorInfo>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_participant_device_identity(IntPtr thiz, IntPtr address, string name);

		/// <summary>
		/// Create a #LinphoneParticipantDeviceIdentity object. 
		/// </summary>
		public Linphone.ParticipantDeviceIdentity CreateParticipantDeviceIdentity(Linphone.Address address, string name)
		{
			IntPtr ptr = linphone_factory_create_participant_device_identity(nativePtr, address != null ? address.nativePtr : IntPtr.Zero, name);
			Linphone.ParticipantDeviceIdentity returnVal = fromNativePtr<Linphone.ParticipantDeviceIdentity>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_range(IntPtr thiz);

		/// <summary>
		/// Creates an object LinphoneRange. 
		/// </summary>
		public Linphone.Range CreateRange()
		{
			IntPtr ptr = linphone_factory_create_range(nativePtr);
			Linphone.Range returnVal = fromNativePtr<Linphone.Range>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_transports(IntPtr thiz);

		/// <summary>
		/// Creates an object LinphoneTransports. 
		/// </summary>
		public Linphone.Transports CreateTransports()
		{
			IntPtr ptr = linphone_factory_create_transports(nativePtr);
			Linphone.Transports returnVal = fromNativePtr<Linphone.Transports>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_tunnel_config(IntPtr thiz);

		/// <summary>
		/// Creates an object <see cref="Linphone.TunnelConfig" />. 
		/// </summary>
		public Linphone.TunnelConfig CreateTunnelConfig()
		{
			IntPtr ptr = linphone_factory_create_tunnel_config(nativePtr);
			Linphone.TunnelConfig returnVal = fromNativePtr<Linphone.TunnelConfig>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_vcard(IntPtr thiz);

		/// <summary>
		/// Create an empty <see cref="Linphone.Vcard" />. 
		/// </summary>
		public Linphone.Vcard CreateVcard()
		{
			IntPtr ptr = linphone_factory_create_vcard(nativePtr);
			Linphone.Vcard returnVal = fromNativePtr<Linphone.Vcard>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_video_activation_policy(IntPtr thiz);

		/// <summary>
		/// Creates an object LinphoneVideoActivationPolicy. 
		/// </summary>
		public Linphone.VideoActivationPolicy CreateVideoActivationPolicy()
		{
			IntPtr ptr = linphone_factory_create_video_activation_policy(nativePtr);
			Linphone.VideoActivationPolicy returnVal = fromNativePtr<Linphone.VideoActivationPolicy>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_video_definition(IntPtr thiz, uint width, uint height);

		/// <summary>
		/// Create a <see cref="Linphone.VideoDefinition" /> from a given width and height. 
		/// </summary>
		public Linphone.VideoDefinition CreateVideoDefinition(uint width, uint height)
		{
			IntPtr ptr = linphone_factory_create_video_definition(nativePtr, width, height);
			Linphone.VideoDefinition returnVal = fromNativePtr<Linphone.VideoDefinition>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_video_definition_from_name(IntPtr thiz, string name);

		/// <summary>
		/// Create a <see cref="Linphone.VideoDefinition" /> from a given standard
		/// definition name. 
		/// </summary>
		public Linphone.VideoDefinition CreateVideoDefinitionFromName(string name)
		{
			IntPtr ptr = linphone_factory_create_video_definition_from_name(nativePtr, name);
			Linphone.VideoDefinition returnVal = fromNativePtr<Linphone.VideoDefinition>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_factory_enable_log_collection(IntPtr thiz, int state);

		/// <summary>
		/// Enables or disables log collection. 
		/// </summary>
		public void EnableLogCollection(Linphone.LogCollectionState state)
		{
			linphone_factory_enable_log_collection(nativePtr, (int)state);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_config_dir(IntPtr thiz, IntPtr context);

		/// <summary>
		/// Get the config path. 
		/// </summary>
		public string GetConfigDir(IntPtr context)
		{
			IntPtr stringPtr = linphone_factory_get_config_dir(nativePtr, context);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_data_dir(IntPtr thiz, IntPtr context);

		/// <summary>
		/// Get the data path. 
		/// </summary>
		public string GetDataDir(IntPtr context)
		{
			IntPtr stringPtr = linphone_factory_get_data_dir(nativePtr, context);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_get_download_dir(IntPtr thiz, IntPtr context);

		/// <summary>
		/// Get the download path. 
		/// </summary>
		public string GetDownloadDir(IntPtr context)
		{
			IntPtr stringPtr = linphone_factory_get_download_dir(nativePtr, context);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_factory_is_chatroom_backend_available(IntPtr thiz, int chatroomBackend);

		/// <summary>
		/// Indicates if the given LinphoneChatRoomBackend is available. 
		/// </summary>
		public bool IsChatroomBackendAvailable(Linphone.ChatRoomBackend chatroomBackend)
		{
			bool returnVal = linphone_factory_is_chatroom_backend_available(nativePtr, (int)chatroomBackend) == (char)0 ? false : true;
			
			return returnVal;
		}
	}
	/// <summary>
	/// Represents a buddy, all presence actions like subscription and status change
	/// notification are performed on this object. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Friend : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_new_from_vcard(IntPtr vcard);

		/// <summary>
		/// Contructor same as linphone_friend_new + <see
		/// cref="Linphone.Friend.SetAddress()" /> 
		/// </summary>
		public static Linphone.Friend NewFromVcard(Linphone.Vcard vcard)
		{
			IntPtr ptr = linphone_friend_new_from_vcard(vcard != null ? vcard.nativePtr : IntPtr.Zero);
			Linphone.Friend returnVal = fromNativePtr<Linphone.Friend>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_get_address(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_friend_set_address(IntPtr thiz, IntPtr address);

		/// <summary>
		/// Get address of this friend. 
		/// </summary>
		public Linphone.Address Address
		{
			get
			{
				IntPtr ptr = linphone_friend_get_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
			set
			{
				int exception_result = linphone_friend_set_address(nativePtr, value.nativePtr);
				if (exception_result != 0) throw new LinphoneException("Address setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_get_addresses(IntPtr thiz);

		/// <summary>
		/// Returns a list of <see cref="Linphone.Address" /> for this friend. 
		/// </summary>
		public IEnumerable<Linphone.Address> Addresses
		{
			get
			{
				return MarshalBctbxList<Linphone.Address>(linphone_friend_get_addresses(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_friend_get_capabilities(IntPtr thiz);

		/// <summary>
		/// Returns the capabilities associated to this friend. 
		/// </summary>
		public int Capabilities
		{
			get
			{
				return linphone_friend_get_capabilities(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ConsolidatedPresence linphone_friend_get_consolidated_presence(IntPtr thiz);

		/// <summary>
		/// Get the consolidated presence of a friend. 
		/// </summary>
		public Linphone.ConsolidatedPresence ConsolidatedPresence
		{
			get
			{
				return linphone_friend_get_consolidated_presence(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_get_core(IntPtr thiz);

		/// <summary>
		/// Returns the <see cref="Linphone.Core" /> object managing this friend, if any. 
		/// </summary>
		public Linphone.Core Core
		{
			get
			{
				IntPtr ptr = linphone_friend_get_core(nativePtr);
				Linphone.Core obj = fromNativePtr<Linphone.Core>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.SubscribePolicy linphone_friend_get_inc_subscribe_policy(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_friend_set_inc_subscribe_policy(IntPtr thiz, int pol);

		/// <summary>
		/// get current subscription policy for this <see cref="Linphone.Friend" /> 
		/// </summary>
		public Linphone.SubscribePolicy IncSubscribePolicy
		{
			get
			{
				return linphone_friend_get_inc_subscribe_policy(nativePtr);
			}
			set
			{
				int exception_result = linphone_friend_set_inc_subscribe_policy(nativePtr, (int)value);
				if (exception_result != 0) throw new LinphoneException("IncSubscribePolicy setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_friend_is_presence_received(IntPtr thiz);

		/// <summary>
		/// Tells whether we already received presence information for a friend. 
		/// </summary>
		public bool IsPresenceReceived
		{
			get
			{
				return linphone_friend_is_presence_received(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_get_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_friend_set_name(IntPtr thiz, string name);

		/// <summary>
		/// Get the display name for this friend. 
		/// </summary>
		public string Name
		{
			get
			{
				IntPtr stringPtr = linphone_friend_get_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_friend_set_name(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Name setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_get_phone_numbers(IntPtr thiz);

		/// <summary>
		/// Returns a list of phone numbers for this friend. 
		/// </summary>
		public IEnumerable<string> PhoneNumbers
		{
			get
			{
				return MarshalStringArray(linphone_friend_get_phone_numbers(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_get_presence_model(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_set_presence_model(IntPtr thiz, IntPtr presence);

		/// <summary>
		/// Get the presence model of a friend. 
		/// </summary>
		public Linphone.PresenceModel PresenceModel
		{
			get
			{
				IntPtr ptr = linphone_friend_get_presence_model(nativePtr);
				Linphone.PresenceModel obj = fromNativePtr<Linphone.PresenceModel>(ptr, true);
				return obj;
			}
			set
			{
				linphone_friend_set_presence_model(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_get_ref_key(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_set_ref_key(IntPtr thiz, string key);

		/// <summary>
		/// Get the reference key of a friend. 
		/// </summary>
		public string RefKey
		{
			get
			{
				IntPtr stringPtr = linphone_friend_get_ref_key(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_friend_set_ref_key(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_friend_subscribes_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_friend_enable_subscribes(IntPtr thiz, char val);

		/// <summary>
		/// get subscription flag value 
		/// </summary>
		public bool SubscribesEnabled
		{
			get
			{
				return linphone_friend_subscribes_enabled(nativePtr) != 0;
			}
			set
			{
				int exception_result = linphone_friend_enable_subscribes(nativePtr, value ? (char)1 : (char)0);
				if (exception_result != 0) throw new LinphoneException("SubscribesEnabled setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.SubscriptionState linphone_friend_get_subscription_state(IntPtr thiz);

		/// <summary>
		/// Get subscription state of a friend. 
		/// </summary>
		public Linphone.SubscriptionState SubscriptionState
		{
			get
			{
				return linphone_friend_get_subscription_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_get_vcard(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_set_vcard(IntPtr thiz, IntPtr vcard);

		/// <summary>
		/// Returns the vCard object associated to this friend, if any. 
		/// </summary>
		public Linphone.Vcard Vcard
		{
			get
			{
				IntPtr ptr = linphone_friend_get_vcard(nativePtr);
				Linphone.Vcard obj = fromNativePtr<Linphone.Vcard>(ptr, true);
				return obj;
			}
			set
			{
				linphone_friend_set_vcard(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_add_address(IntPtr thiz, IntPtr addr);

		/// <summary>
		/// Adds an address in this friend. 
		/// </summary>
		public void AddAddress(Linphone.Address addr)
		{
			linphone_friend_add_address(nativePtr, addr != null ? addr.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_add_phone_number(IntPtr thiz, string phone);

		/// <summary>
		/// Adds a phone number in this friend. 
		/// </summary>
		public void AddPhoneNumber(string phone)
		{
			linphone_friend_add_phone_number(nativePtr, phone);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_friend_create_vcard(IntPtr thiz, string name);

		/// <summary>
		/// Creates a vCard object associated to this friend if there isn't one yet and if
		/// the full name is available, either by the parameter or the one in the friend's
		/// SIP URI. 
		/// </summary>
		public bool CreateVcard(string name)
		{
			bool returnVal = linphone_friend_create_vcard(nativePtr, name) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_done(IntPtr thiz);

		/// <summary>
		/// Commits modification made to the friend configuration. 
		/// </summary>
		public void Done()
		{
			linphone_friend_done(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_edit(IntPtr thiz);

		/// <summary>
		/// Starts editing a friend configuration. 
		/// </summary>
		public void Edit()
		{
			linphone_friend_edit(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_friend_get_capability_version(IntPtr thiz, int capability);

		/// <summary>
		/// Returns the version of a friend's capbility. 
		/// </summary>
		public float GetCapabilityVersion(Linphone.FriendCapability capability)
		{
			float returnVal = linphone_friend_get_capability_version(nativePtr, (int)capability);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_get_presence_model_for_uri_or_tel(IntPtr thiz, string uriOrTel);

		/// <summary>
		/// Get the presence model for a specific SIP URI or phone number of a friend. 
		/// </summary>
		public Linphone.PresenceModel GetPresenceModelForUriOrTel(string uriOrTel)
		{
			IntPtr ptr = linphone_friend_get_presence_model_for_uri_or_tel(nativePtr, uriOrTel);
			Linphone.PresenceModel returnVal = fromNativePtr<Linphone.PresenceModel>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_friend_has_capability(IntPtr thiz, int capability);

		/// <summary>
		/// Returns whether or not a friend has a capbility. 
		/// </summary>
		public bool HasCapability(Linphone.FriendCapability capability)
		{
			bool returnVal = linphone_friend_has_capability(nativePtr, (int)capability) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_friend_has_capability_with_version(IntPtr thiz, int capability, float version);

		/// <summary>
		/// Returns whether or not a friend has a capbility with a given version. 
		/// </summary>
		public bool HasCapabilityWithVersion(Linphone.FriendCapability capability, float version)
		{
			bool returnVal = linphone_friend_has_capability_with_version(nativePtr, (int)capability, version) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_friend_has_capability_with_version_or_more(IntPtr thiz, int capability, float version);

		/// <summary>
		/// Returns whether or not a friend has a capbility with a given version or more. 
		/// </summary>
		public bool HasCapabilityWithVersionOrMore(Linphone.FriendCapability capability, float version)
		{
			bool returnVal = linphone_friend_has_capability_with_version_or_more(nativePtr, (int)capability, version) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_friend_in_list(IntPtr thiz);

		/// <summary>
		/// Check that the given friend is in a friend list. 
		/// </summary>
		public bool InList()
		{
			bool returnVal = linphone_friend_in_list(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_remove(IntPtr thiz);

		/// <summary>
		/// Removes a friend from it's friend list and from the rc if exists. 
		/// </summary>
		public void Remove()
		{
			linphone_friend_remove(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_remove_address(IntPtr thiz, IntPtr addr);

		/// <summary>
		/// Removes an address in this friend. 
		/// </summary>
		public void RemoveAddress(Linphone.Address addr)
		{
			linphone_friend_remove_address(nativePtr, addr != null ? addr.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_remove_phone_number(IntPtr thiz, string phone);

		/// <summary>
		/// Removes a phone number in this friend. 
		/// </summary>
		public void RemovePhoneNumber(string phone)
		{
			linphone_friend_remove_phone_number(nativePtr, phone);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_save(IntPtr thiz, IntPtr lc);

		/// <summary>
		/// Saves a friend either in database if configured, otherwise in linphonerc. 
		/// </summary>
		public void Save(Linphone.Core lc)
		{
			linphone_friend_save(nativePtr, lc != null ? lc.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_set_presence_model_for_uri_or_tel(IntPtr thiz, string uriOrTel, IntPtr presence);

		/// <summary>
		/// Set the presence model for a specific SIP URI or phone number of a friend. 
		/// </summary>
		public void SetPresenceModelForUriOrTel(string uriOrTel, Linphone.PresenceModel presence)
		{
			linphone_friend_set_presence_model_for_uri_or_tel(nativePtr, uriOrTel, presence != null ? presence.nativePtr : IntPtr.Zero);
			
			
			
		}
	}
	/// <summary>
	/// The <see cref="Linphone.FriendList" /> object representing a list of friends. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class FriendList : LinphoneObject
	{

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_friend_list_cbs(IntPtr factory);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_add_callbacks(IntPtr thiz, IntPtr cbs);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_remove_callbacks(IntPtr thiz, IntPtr cbs);

		~FriendList() 
		{
			if (listener != null)
			{
				linphone_friend_list_remove_callbacks(nativePtr, listener.nativePtr);
			}
		}

		private FriendListListener listener;

		public FriendListListener Listener
		{
			get {
				if (listener == null)
				{
					IntPtr nativeListener = linphone_factory_create_friend_list_cbs(IntPtr.Zero);
					listener = fromNativePtr<FriendListListener>(nativeListener, false);
					linphone_friend_list_add_callbacks(nativePtr, nativeListener);
					listener.register();
				}
				return listener;
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_get_core(IntPtr thiz);

		/// <summary>
		/// Returns the <see cref="Linphone.Core" /> object attached to this
		/// LinphoneFriendList. 
		/// </summary>
		public Linphone.Core Core
		{
			get
			{
				IntPtr ptr = linphone_friend_list_get_core(nativePtr);
				Linphone.Core obj = fromNativePtr<Linphone.Core>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_get_current_callbacks(IntPtr thiz);

		/// <summary>
		/// Get the current LinphoneFriendListCbs object associated with a
		/// LinphoneFriendList. 
		/// </summary>
		public Linphone.FriendListListener CurrentCallbacks
		{
			get
			{
				IntPtr ptr = linphone_friend_list_get_current_callbacks(nativePtr);
				Linphone.FriendListListener obj = fromNativePtr<Linphone.FriendListListener>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_get_display_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_set_display_name(IntPtr thiz, string displayName);

		/// <summary>
		/// Get the display name of the friend list. 
		/// </summary>
		public string DisplayName
		{
			get
			{
				IntPtr stringPtr = linphone_friend_list_get_display_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_friend_list_set_display_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_get_friends(IntPtr thiz);

		/// <summary>
		/// Retrieves the list of <see cref="Linphone.Friend" /> from this
		/// LinphoneFriendList. 
		/// </summary>
		public IEnumerable<Linphone.Friend> Friends
		{
			get
			{
				return MarshalBctbxList<Linphone.Friend>(linphone_friend_list_get_friends(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_friend_list_is_subscription_bodyless(IntPtr thiz);

		/// <summary>
		/// Get wheter the subscription of the friend list is bodyless or not. 
		/// </summary>
		public bool IsSubscriptionBodyless
		{
			get
			{
				return linphone_friend_list_is_subscription_bodyless(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_get_rls_address(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_set_rls_address(IntPtr thiz, IntPtr rlsAddr);

		/// <summary>
		/// Get the RLS (Resource List Server) URI associated with the friend list to
		/// subscribe to these friends presence. 
		/// </summary>
		public Linphone.Address RlsAddress
		{
			get
			{
				IntPtr ptr = linphone_friend_list_get_rls_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
			set
			{
				linphone_friend_list_set_rls_address(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_get_rls_uri(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_set_rls_uri(IntPtr thiz, string rlsUri);

		/// <summary>
		/// Get the RLS (Resource List Server) URI associated with the friend list to
		/// subscribe to these friends presence. 
		/// </summary>
		public string RlsUri
		{
			get
			{
				IntPtr stringPtr = linphone_friend_list_get_rls_uri(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_friend_list_set_rls_uri(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_set_subscription_bodyless(IntPtr thiz, char bodyless);

		/// <summary>
		/// Set wheter the subscription of the friend list is bodyless or not. 
		/// </summary>
		public bool SubscriptionBodyless
		{
			set
			{
				linphone_friend_list_set_subscription_bodyless(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_friend_list_subscriptions_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_enable_subscriptions(IntPtr thiz, char enabled);

		/// <summary>
		/// Gets whether subscription to NOTIFYes of all friends list are enabled or not. 
		/// </summary>
		public bool SubscriptionsEnabled
		{
			get
			{
				return linphone_friend_list_subscriptions_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_friend_list_enable_subscriptions(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_get_uri(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_set_uri(IntPtr thiz, string uri);

		/// <summary>
		/// Get the URI associated with the friend list. 
		/// </summary>
		public string Uri
		{
			get
			{
				IntPtr stringPtr = linphone_friend_list_get_uri(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_friend_list_set_uri(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.FriendListStatus linphone_friend_list_add_friend(IntPtr thiz, IntPtr lf);

		/// <summary>
		/// Add a friend to a friend list. 
		/// </summary>
		public Linphone.FriendListStatus AddFriend(Linphone.Friend lf)
		{
			Linphone.FriendListStatus returnVal = linphone_friend_list_add_friend(nativePtr, lf != null ? lf.nativePtr : IntPtr.Zero);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.FriendListStatus linphone_friend_list_add_local_friend(IntPtr thiz, IntPtr lf);

		/// <summary>
		/// Add a friend to a friend list. 
		/// </summary>
		public Linphone.FriendListStatus AddLocalFriend(Linphone.Friend lf)
		{
			Linphone.FriendListStatus returnVal = linphone_friend_list_add_local_friend(nativePtr, lf != null ? lf.nativePtr : IntPtr.Zero);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_export_friends_as_vcard4_file(IntPtr thiz, string vcardFile);

		/// <summary>
		/// Creates and export <see cref="Linphone.Friend" /> objects from <see
		/// cref="Linphone.FriendList" /> to a file using vCard 4 format. 
		/// </summary>
		public void ExportFriendsAsVcard4File(string vcardFile)
		{
			linphone_friend_list_export_friends_as_vcard4_file(nativePtr, vcardFile);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_find_friend_by_address(IntPtr thiz, IntPtr address);

		/// <summary>
		/// Find a friend in the friend list using a LinphoneAddress. 
		/// </summary>
		public Linphone.Friend FindFriendByAddress(Linphone.Address address)
		{
			IntPtr ptr = linphone_friend_list_find_friend_by_address(nativePtr, address != null ? address.nativePtr : IntPtr.Zero);
			Linphone.Friend returnVal = fromNativePtr<Linphone.Friend>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_find_friend_by_ref_key(IntPtr thiz, string refKey);

		/// <summary>
		/// Find a friend in the friend list using a ref key. 
		/// </summary>
		public Linphone.Friend FindFriendByRefKey(string refKey)
		{
			IntPtr ptr = linphone_friend_list_find_friend_by_ref_key(nativePtr, refKey);
			Linphone.Friend returnVal = fromNativePtr<Linphone.Friend>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_find_friend_by_uri(IntPtr thiz, string uri);

		/// <summary>
		/// Find a friend in the friend list using an URI string. 
		/// </summary>
		public Linphone.Friend FindFriendByUri(string uri)
		{
			IntPtr ptr = linphone_friend_list_find_friend_by_uri(nativePtr, uri);
			Linphone.Friend returnVal = fromNativePtr<Linphone.Friend>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_find_friends_by_address(IntPtr thiz, IntPtr address);

		/// <summary>
		/// Find all friends in the friend list using a LinphoneAddress. 
		/// </summary>
		public IEnumerable<Linphone.Friend> FindFriendsByAddress(Linphone.Address address)
		{
			IEnumerable<Linphone.Friend> returnVal = MarshalBctbxList<Linphone.Friend>(linphone_friend_list_find_friends_by_address(nativePtr, address != null ? address.nativePtr : IntPtr.Zero));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_friend_list_find_friends_by_uri(IntPtr thiz, string uri);

		/// <summary>
		/// Find all friends in the friend list using an URI string. 
		/// </summary>
		public IEnumerable<Linphone.Friend> FindFriendsByUri(string uri)
		{
			IEnumerable<Linphone.Friend> returnVal = MarshalBctbxList<Linphone.Friend>(linphone_friend_list_find_friends_by_uri(nativePtr, uri));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_friend_list_import_friends_from_vcard4_buffer(IntPtr thiz, string vcardBuffer);

		/// <summary>
		/// Creates and adds <see cref="Linphone.Friend" /> objects to <see
		/// cref="Linphone.FriendList" /> from a buffer that contains the vCard(s) to
		/// parse. 
		/// </summary>
		public void ImportFriendsFromVcard4Buffer(string vcardBuffer)
		{
			int exception_result = linphone_friend_list_import_friends_from_vcard4_buffer(nativePtr, vcardBuffer);
			if (exception_result != 0) throw new LinphoneException("ImportFriendsFromVcard4Buffer returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_friend_list_import_friends_from_vcard4_file(IntPtr thiz, string vcardFile);

		/// <summary>
		/// Creates and adds <see cref="Linphone.Friend" /> objects to <see
		/// cref="Linphone.FriendList" /> from a file that contains the vCard(s) to parse. 
		/// </summary>
		public void ImportFriendsFromVcard4File(string vcardFile)
		{
			int exception_result = linphone_friend_list_import_friends_from_vcard4_file(nativePtr, vcardFile);
			if (exception_result != 0) throw new LinphoneException("ImportFriendsFromVcard4File returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_notify_presence(IntPtr thiz, IntPtr presence);

		/// <summary>
		/// Notify our presence to all the friends in the friend list that have subscribed
		/// to our presence directly (not using a RLS). 
		/// </summary>
		public void NotifyPresence(Linphone.PresenceModel presence)
		{
			linphone_friend_list_notify_presence(nativePtr, presence != null ? presence.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.FriendListStatus linphone_friend_list_remove_friend(IntPtr thiz, IntPtr lf);

		/// <summary>
		/// Remove a friend from a friend list. 
		/// </summary>
		public Linphone.FriendListStatus RemoveFriend(Linphone.Friend lf)
		{
			Linphone.FriendListStatus returnVal = linphone_friend_list_remove_friend(nativePtr, lf != null ? lf.nativePtr : IntPtr.Zero);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_synchronize_friends_from_server(IntPtr thiz);

		/// <summary>
		/// Starts a CardDAV synchronization using value set using
		/// linphone_friend_list_set_uri. 
		/// </summary>
		public void SynchronizeFriendsFromServer()
		{
			linphone_friend_list_synchronize_friends_from_server(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_update_dirty_friends(IntPtr thiz);

		/// <summary>
		/// Goes through all the <see cref="Linphone.Friend" /> that are dirty and does a
		/// CardDAV PUT to update the server. 
		/// </summary>
		public void UpdateDirtyFriends()
		{
			linphone_friend_list_update_dirty_friends(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_update_revision(IntPtr thiz, int rev);

		/// <summary>
		/// Sets the revision from the last synchronization. 
		/// </summary>
		public void UpdateRevision(int rev)
		{
			linphone_friend_list_update_revision(nativePtr, rev);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_friend_list_update_subscriptions(IntPtr thiz);

		/// <summary>
		/// Update presence subscriptions for the entire list. 
		/// </summary>
		public void UpdateSubscriptions()
		{
			linphone_friend_list_update_subscriptions(nativePtr);
			
			
			
		}
	}
	/// <summary>
	/// Object representing a chain of protocol headers. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Headers : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_headers_add(IntPtr thiz, string name, string val);

		/// <summary>
		/// Add given header name and corresponding value. 
		/// </summary>
		public void Add(string name, string val)
		{
			linphone_headers_add(nativePtr, name, val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_headers_get_value(IntPtr thiz, string headerName);

		/// <summary>
		/// Search for a given header name and return its value. 
		/// </summary>
		public string GetValue(string headerName)
		{
			IntPtr stringPtr = linphone_headers_get_value(nativePtr, headerName);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_headers_remove(IntPtr thiz, string name);

		/// <summary>
		/// Add given header name and corresponding value. 
		/// </summary>
		public void Remove(string name)
		{
			linphone_headers_remove(nativePtr, name);
			
			
			
		}
	}
	/// <summary>
	/// Policy to use to send/receive instant messaging composing/delivery/display
	/// notifications. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class ImNotifPolicy : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_im_notif_policy_get_recv_imdn_delivered(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_im_notif_policy_set_recv_imdn_delivered(IntPtr thiz, char enable);

		/// <summary>
		/// Tell whether imdn delivered notifications are being notified when received. 
		/// </summary>
		public bool RecvImdnDelivered
		{
			get
			{
				return linphone_im_notif_policy_get_recv_imdn_delivered(nativePtr) != 0;
			}
			set
			{
				linphone_im_notif_policy_set_recv_imdn_delivered(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_im_notif_policy_get_recv_imdn_displayed(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_im_notif_policy_set_recv_imdn_displayed(IntPtr thiz, char enable);

		/// <summary>
		/// Tell whether imdn displayed notifications are being notified when received. 
		/// </summary>
		public bool RecvImdnDisplayed
		{
			get
			{
				return linphone_im_notif_policy_get_recv_imdn_displayed(nativePtr) != 0;
			}
			set
			{
				linphone_im_notif_policy_set_recv_imdn_displayed(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_im_notif_policy_get_recv_is_composing(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_im_notif_policy_set_recv_is_composing(IntPtr thiz, char enable);

		/// <summary>
		/// Tell whether is_composing notifications are being notified when received. 
		/// </summary>
		public bool RecvIsComposing
		{
			get
			{
				return linphone_im_notif_policy_get_recv_is_composing(nativePtr) != 0;
			}
			set
			{
				linphone_im_notif_policy_set_recv_is_composing(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_im_notif_policy_get_send_imdn_delivered(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_im_notif_policy_set_send_imdn_delivered(IntPtr thiz, char enable);

		/// <summary>
		/// Tell whether imdn delivered notifications are being sent. 
		/// </summary>
		public bool SendImdnDelivered
		{
			get
			{
				return linphone_im_notif_policy_get_send_imdn_delivered(nativePtr) != 0;
			}
			set
			{
				linphone_im_notif_policy_set_send_imdn_delivered(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_im_notif_policy_get_send_imdn_displayed(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_im_notif_policy_set_send_imdn_displayed(IntPtr thiz, char enable);

		/// <summary>
		/// Tell whether imdn displayed notifications are being sent. 
		/// </summary>
		public bool SendImdnDisplayed
		{
			get
			{
				return linphone_im_notif_policy_get_send_imdn_displayed(nativePtr) != 0;
			}
			set
			{
				linphone_im_notif_policy_set_send_imdn_displayed(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_im_notif_policy_get_send_is_composing(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_im_notif_policy_set_send_is_composing(IntPtr thiz, char enable);

		/// <summary>
		/// Tell whether is_composing notifications are being sent. 
		/// </summary>
		public bool SendIsComposing
		{
			get
			{
				return linphone_im_notif_policy_get_send_is_composing(nativePtr) != 0;
			}
			set
			{
				linphone_im_notif_policy_set_send_is_composing(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_im_notif_policy_clear(IntPtr thiz);

		/// <summary>
		/// Clear an IM notif policy (deactivate all receiving and sending of
		/// notifications). 
		/// </summary>
		public void Clear()
		{
			linphone_im_notif_policy_clear(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_im_notif_policy_enable_all(IntPtr thiz);

		/// <summary>
		/// Enable all receiving and sending of notifications. 
		/// </summary>
		public void EnableAll()
		{
			linphone_im_notif_policy_enable_all(nativePtr);
			
			
			
		}
	}
	/// <summary>
	/// The <see cref="Linphone.InfoMessage" /> is an object representing an
	/// informational message sent or received by the core. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class InfoMessage : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_info_message_get_content(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_info_message_set_content(IntPtr thiz, IntPtr content);

		/// <summary>
		/// Returns the info message's content as a <see cref="Linphone.Content" />
		/// structure. 
		/// </summary>
		public Linphone.Content Content
		{
			get
			{
				IntPtr ptr = linphone_info_message_get_content(nativePtr);
				Linphone.Content obj = fromNativePtr<Linphone.Content>(ptr, true);
				return obj;
			}
			set
			{
				linphone_info_message_set_content(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_info_message_add_header(IntPtr thiz, string name, string val);

		/// <summary>
		/// Add a header to an info message to be sent. 
		/// </summary>
		public void AddHeader(string name, string val)
		{
			linphone_info_message_add_header(nativePtr, name, val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_info_message_get_header(IntPtr thiz, string name);

		/// <summary>
		/// Obtain a header value from a received info message. 
		/// </summary>
		public string GetHeader(string name)
		{
			IntPtr stringPtr = linphone_info_message_get_header(nativePtr, name);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
	}
	/// <summary>
	/// Singleton class giving access to logging features. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class LoggingService : LinphoneObject
	{

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_logging_service_cbs(IntPtr factory);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_logging_service_add_callbacks(IntPtr thiz, IntPtr cbs);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_logging_service_remove_callbacks(IntPtr thiz, IntPtr cbs);

		~LoggingService() 
		{
			if (listener != null)
			{
				linphone_logging_service_remove_callbacks(nativePtr, listener.nativePtr);
			}
		}

		private LoggingServiceListener listener;

		public LoggingServiceListener Listener
		{
			get {
				if (listener == null)
				{
					IntPtr nativeListener = linphone_factory_create_logging_service_cbs(IntPtr.Zero);
					listener = fromNativePtr<LoggingServiceListener>(nativeListener, false);
					linphone_logging_service_add_callbacks(nativePtr, nativeListener);
					listener.register();
				}
				return listener;
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_logging_service_get();

		/// <summary>
		/// Gets the singleton logging service object. 
		/// </summary>
		static public Linphone.LoggingService Instance
		{
			get
			{
				IntPtr ptr = linphone_logging_service_get();
				Linphone.LoggingService obj = fromNativePtr<Linphone.LoggingService>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_logging_service_get_current_callbacks(IntPtr thiz);

		/// <summary>
		/// Returns the current callbacks being called while iterating on callbacks. 
		/// </summary>
		public Linphone.LoggingServiceListener CurrentCallbacks
		{
			get
			{
				IntPtr ptr = linphone_logging_service_get_current_callbacks(nativePtr);
				Linphone.LoggingServiceListener obj = fromNativePtr<Linphone.LoggingServiceListener>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_logging_service_get_domain(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_logging_service_set_domain(IntPtr thiz, string domain);

		/// <summary>
		/// Get the domain where application logs are written (for example with <see
		/// cref="Linphone.LoggingService.Message()" />). 
		/// </summary>
		public string Domain
		{
			get
			{
				IntPtr stringPtr = linphone_logging_service_get_domain(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_logging_service_set_domain(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_logging_service_set_log_level(IntPtr thiz, int level);

		/// <summary>
		/// Set the verbosity of the log. 
		/// </summary>
		public Linphone.LogLevel LogLevel
		{
			set
			{
				linphone_logging_service_set_log_level(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_logging_service_get_log_level_mask(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_logging_service_set_log_level_mask(IntPtr thiz, uint mask);

		/// <summary>
		/// Gets the log level mask. 
		/// </summary>
		public uint LogLevelMask
		{
			get
			{
				return linphone_logging_service_get_log_level_mask(nativePtr);
			}
			set
			{
				linphone_logging_service_set_log_level_mask(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_logging_service_debug(IntPtr thiz, string msg);

		/// <summary>
		/// Write a LinphoneLogLevelDebug message to the logs. 
		/// </summary>
		public void Debug(string msg)
		{
			linphone_logging_service_debug(nativePtr, msg);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_logging_service_error(IntPtr thiz, string msg);

		/// <summary>
		/// Write a LinphoneLogLevelError message to the logs. 
		/// </summary>
		public void Error(string msg)
		{
			linphone_logging_service_error(nativePtr, msg);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_logging_service_fatal(IntPtr thiz, string msg);

		/// <summary>
		/// Write a LinphoneLogLevelFatal message to the logs. 
		/// </summary>
		public void Fatal(string msg)
		{
			linphone_logging_service_fatal(nativePtr, msg);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_logging_service_message(IntPtr thiz, string msg);

		/// <summary>
		/// Write a LinphoneLogLevelMessage message to the logs. 
		/// </summary>
		public void Message(string msg)
		{
			linphone_logging_service_message(nativePtr, msg);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_logging_service_set_log_file(IntPtr thiz, string dir, string filename, long maxSize);

		/// <summary>
		/// Enables logging in a file. 
		/// </summary>
		public void SetLogFile(string dir, string filename, long maxSize)
		{
			linphone_logging_service_set_log_file(nativePtr, dir, filename, maxSize);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_logging_service_trace(IntPtr thiz, string msg);

		/// <summary>
		/// Write a LinphoneLogLevelTrace message to the logs. 
		/// </summary>
		public void Trace(string msg)
		{
			linphone_logging_service_trace(nativePtr, msg);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_logging_service_warning(IntPtr thiz, string msg);

		/// <summary>
		/// Write a LinphoneLogLevelWarning message to the logs. 
		/// </summary>
		public void Warning(string msg)
		{
			linphone_logging_service_warning(nativePtr, msg);
			
			
			
		}
	}
	/// <summary>
	/// A <see cref="Linphone.MagicSearch" /> is used to do specifics searchs. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class MagicSearch : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_magic_search_get_delimiter(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_magic_search_set_delimiter(IntPtr thiz, string delimiter);

		public string Delimiter
		{
			get
			{
				IntPtr stringPtr = linphone_magic_search_get_delimiter(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_magic_search_set_delimiter(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_magic_search_get_limited_search(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_magic_search_set_limited_search(IntPtr thiz, char limited);

		public bool LimitedSearch
		{
			get
			{
				return linphone_magic_search_get_limited_search(nativePtr) != 0;
			}
			set
			{
				linphone_magic_search_set_limited_search(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_magic_search_get_max_weight(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_magic_search_set_max_weight(IntPtr thiz, uint weight);

		public uint MaxWeight
		{
			get
			{
				return linphone_magic_search_get_max_weight(nativePtr);
			}
			set
			{
				linphone_magic_search_set_max_weight(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_magic_search_get_min_weight(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_magic_search_set_min_weight(IntPtr thiz, uint weight);

		public uint MinWeight
		{
			get
			{
				return linphone_magic_search_get_min_weight(nativePtr);
			}
			set
			{
				linphone_magic_search_set_min_weight(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_magic_search_get_search_limit(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_magic_search_set_search_limit(IntPtr thiz, uint limit);

		public uint SearchLimit
		{
			get
			{
				return linphone_magic_search_get_search_limit(nativePtr);
			}
			set
			{
				linphone_magic_search_set_search_limit(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_magic_search_get_use_delimiter(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_magic_search_set_use_delimiter(IntPtr thiz, char enable);

		public bool UseDelimiter
		{
			get
			{
				return linphone_magic_search_get_use_delimiter(nativePtr) != 0;
			}
			set
			{
				linphone_magic_search_set_use_delimiter(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_magic_search_get_contact_list_from_filter(IntPtr thiz, string filter, string domain);

		/// <summary>
		/// Create a sorted list of SearchResult from SipUri, Contact name, Contact
		/// displayname, Contact phone number, which match with a filter word The last item
		/// list will be an address formed with "filter" if a proxy config exist During the
		/// first search, a cache is created and used for the next search Use <see
		/// cref="Linphone.MagicSearch.ResetSearchCache()" /> to begin a new search. 
		/// </summary>
		public IEnumerable<Linphone.SearchResult> GetContactListFromFilter(string filter, string domain)
		{
			IEnumerable<Linphone.SearchResult> returnVal = MarshalBctbxList<Linphone.SearchResult>(linphone_magic_search_get_contact_list_from_filter(nativePtr, filter, domain));
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_magic_search_reset_search_cache(IntPtr thiz);

		/// <summary>
		/// Reset the cache to begin a new search. 
		/// </summary>
		public void ResetSearchCache()
		{
			linphone_magic_search_reset_search_cache(nativePtr);
			
			
			
		}
	}
	/// <summary>
	/// Policy to use to pass through NATs/firewalls. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class NatPolicy : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_nat_policy_get_core(IntPtr thiz);

		/// <summary>
		/// Returns the <see cref="Linphone.Core" /> object managing this nat policy, if
		/// any. 
		/// </summary>
		public Linphone.Core Core
		{
			get
			{
				IntPtr ptr = linphone_nat_policy_get_core(nativePtr);
				Linphone.Core obj = fromNativePtr<Linphone.Core>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_nat_policy_ice_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_enable_ice(IntPtr thiz, char enable);

		/// <summary>
		/// Tell whether ICE is enabled. 
		/// </summary>
		public bool IceEnabled
		{
			get
			{
				return linphone_nat_policy_ice_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_nat_policy_enable_ice(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_nat_policy_stun_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_enable_stun(IntPtr thiz, char enable);

		/// <summary>
		/// Tell whether STUN is enabled. 
		/// </summary>
		public bool StunEnabled
		{
			get
			{
				return linphone_nat_policy_stun_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_nat_policy_enable_stun(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_nat_policy_get_stun_server(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_set_stun_server(IntPtr thiz, string stunServer);

		/// <summary>
		/// Get the STUN/TURN server to use with this NAT policy. 
		/// </summary>
		public string StunServer
		{
			get
			{
				IntPtr stringPtr = linphone_nat_policy_get_stun_server(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_nat_policy_set_stun_server(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_nat_policy_get_stun_server_username(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_set_stun_server_username(IntPtr thiz, string username);

		/// <summary>
		/// Get the username used to authenticate with the STUN/TURN server. 
		/// </summary>
		public string StunServerUsername
		{
			get
			{
				IntPtr stringPtr = linphone_nat_policy_get_stun_server_username(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_nat_policy_set_stun_server_username(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_nat_policy_tcp_turn_transport_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_enable_tcp_turn_transport(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether TCP TURN transport is enabled. 
		/// </summary>
		public bool TcpTurnTransportEnabled
		{
			get
			{
				return linphone_nat_policy_tcp_turn_transport_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_nat_policy_enable_tcp_turn_transport(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_nat_policy_tls_turn_transport_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_enable_tls_turn_transport(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether TLS TURN transport is enabled. 
		/// </summary>
		public bool TlsTurnTransportEnabled
		{
			get
			{
				return linphone_nat_policy_tls_turn_transport_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_nat_policy_enable_tls_turn_transport(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_nat_policy_turn_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_enable_turn(IntPtr thiz, char enable);

		/// <summary>
		/// Tell whether TURN is enabled. 
		/// </summary>
		public bool TurnEnabled
		{
			get
			{
				return linphone_nat_policy_turn_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_nat_policy_enable_turn(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_nat_policy_udp_turn_transport_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_enable_udp_turn_transport(IntPtr thiz, char enable);

		/// <summary>
		/// Tells whether UDP TURN transport is enabled. 
		/// </summary>
		public bool UdpTurnTransportEnabled
		{
			get
			{
				return linphone_nat_policy_udp_turn_transport_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_nat_policy_enable_udp_turn_transport(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_nat_policy_upnp_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_enable_upnp(IntPtr thiz, char enable);

		/// <summary>
		/// Tell whether uPnP is enabled. 
		/// </summary>
		public bool UpnpEnabled
		{
			get
			{
				return linphone_nat_policy_upnp_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_nat_policy_enable_upnp(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_clear(IntPtr thiz);

		/// <summary>
		/// Clear a NAT policy (deactivate all protocols and unset the STUN server). 
		/// </summary>
		public void Clear()
		{
			linphone_nat_policy_clear(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_nat_policy_resolve_stun_server(IntPtr thiz);

		/// <summary>
		/// Start a STUN server DNS resolution. 
		/// </summary>
		public void ResolveStunServer()
		{
			linphone_nat_policy_resolve_stun_server(nativePtr);
			
			
			
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class Participant : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_participant_get_address(IntPtr thiz);

		/// <summary>
		/// Get the address of a conference participant. 
		/// </summary>
		public Linphone.Address Address
		{
			get
			{
				IntPtr ptr = linphone_participant_get_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_participant_get_devices(IntPtr thiz);

		/// <summary>
		/// Gets the list of devices from a chat room's participant. 
		/// </summary>
		public IEnumerable<Linphone.ParticipantDevice> Devices
		{
			get
			{
				return MarshalBctbxList<Linphone.ParticipantDevice>(linphone_participant_get_devices(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_participant_is_admin(IntPtr thiz);

		/// <summary>
		/// Tells whether a conference participant is an administrator of the conference. 
		/// </summary>
		public bool IsAdmin
		{
			get
			{
				return linphone_participant_is_admin(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ChatRoomSecurityLevel linphone_participant_get_security_level(IntPtr thiz);

		/// <summary>
		/// Get the security level of a chat room. 
		/// </summary>
		public Linphone.ChatRoomSecurityLevel SecurityLevel
		{
			get
			{
				return linphone_participant_get_security_level(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_participant_find_device(IntPtr thiz, IntPtr address);

		/// <summary>
		/// Find a device in the list of devices from a chat room's participant. 
		/// </summary>
		public Linphone.ParticipantDevice FindDevice(Linphone.Address address)
		{
			IntPtr ptr = linphone_participant_find_device(nativePtr, address != null ? address.nativePtr : IntPtr.Zero);
			Linphone.ParticipantDevice returnVal = fromNativePtr<Linphone.ParticipantDevice>(ptr, true);
			
			return returnVal;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class ParticipantDevice : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_participant_device_get_address(IntPtr thiz);

		/// <summary>
		/// Get the address of a participant's device. 
		/// </summary>
		public Linphone.Address Address
		{
			get
			{
				IntPtr ptr = linphone_participant_device_get_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_participant_device_get_name(IntPtr thiz);

		/// <summary>
		/// Return the name of the device or null. 
		/// </summary>
		public string Name
		{
			get
			{
				IntPtr stringPtr = linphone_participant_device_get_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ChatRoomSecurityLevel linphone_participant_device_get_security_level(IntPtr thiz);

		/// <summary>
		/// Get the security level of a participant's device. 
		/// </summary>
		public Linphone.ChatRoomSecurityLevel SecurityLevel
		{
			get
			{
				return linphone_participant_device_get_security_level(nativePtr);
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class ParticipantDeviceIdentity : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_participant_device_identity_clone(IntPtr thiz);

		/// <summary>
		/// Clones a #LinphoneParticipantDeviceIdentity object. 
		/// </summary>
		public Linphone.ParticipantDeviceIdentity Clone()
		{
			IntPtr ptr = linphone_participant_device_identity_clone(nativePtr);
			Linphone.ParticipantDeviceIdentity returnVal = fromNativePtr<Linphone.ParticipantDeviceIdentity>(ptr, true);
			
			return returnVal;
		}
	}
	/// <summary>
	/// The LinphoneParticipantImdnState object represents the state of chat message
	/// for a participant of a conference chat room. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class ParticipantImdnState : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_participant_imdn_state_get_participant(IntPtr thiz);

		/// <summary>
		/// Get the participant concerned by a LinphoneParticipantImdnState. 
		/// </summary>
		public Linphone.Participant Participant
		{
			get
			{
				IntPtr ptr = linphone_participant_imdn_state_get_participant(nativePtr);
				Linphone.Participant obj = fromNativePtr<Linphone.Participant>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ChatMessageState linphone_participant_imdn_state_get_state(IntPtr thiz);

		/// <summary>
		/// Get the chat message state the participant is in. 
		/// </summary>
		public Linphone.ChatMessageState State
		{
			get
			{
				return linphone_participant_imdn_state_get_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_participant_imdn_state_get_state_change_time(IntPtr thiz);

		/// <summary>
		/// Get the timestamp at which a participant has reached the state described by a
		/// LinphoneParticipantImdnState. 
		/// </summary>
		public long StateChangeTime
		{
			get
			{
				return linphone_participant_imdn_state_get_state_change_time(nativePtr);
			}
		}
	}
	/// <summary>
	/// Object representing an RTP payload type. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class PayloadType : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_payload_type_get_channels(IntPtr thiz);

		/// <summary>
		/// Get the number of channels. 
		/// </summary>
		public int Channels
		{
			get
			{
				return linphone_payload_type_get_channels(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_payload_type_get_clock_rate(IntPtr thiz);

		/// <summary>
		/// Get the clock rate of a payload type. 
		/// </summary>
		public int ClockRate
		{
			get
			{
				return linphone_payload_type_get_clock_rate(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_payload_type_get_description(IntPtr thiz);

		/// <summary>
		/// Return a string describing a payload type. 
		/// </summary>
		public string Description
		{
			get
			{
				IntPtr stringPtr = linphone_payload_type_get_description(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_payload_type_get_encoder_description(IntPtr thiz);

		/// <summary>
		/// Get a description of the encoder used to provide a payload type. 
		/// </summary>
		public string EncoderDescription
		{
			get
			{
				IntPtr stringPtr = linphone_payload_type_get_encoder_description(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_payload_type_is_usable(IntPtr thiz);

		/// <summary>
		/// Check whether the payload is usable according the bandwidth targets set in the
		/// core. 
		/// </summary>
		public bool IsUsable
		{
			get
			{
				return linphone_payload_type_is_usable(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_payload_type_is_vbr(IntPtr thiz);

		/// <summary>
		/// Tells whether the specified payload type represents a variable bitrate codec. 
		/// </summary>
		public bool IsVbr
		{
			get
			{
				return linphone_payload_type_is_vbr(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_payload_type_get_mime_type(IntPtr thiz);

		/// <summary>
		/// Get the mime type. 
		/// </summary>
		public string MimeType
		{
			get
			{
				IntPtr stringPtr = linphone_payload_type_get_mime_type(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_payload_type_get_normal_bitrate(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_payload_type_set_normal_bitrate(IntPtr thiz, int bitrate);

		/// <summary>
		/// Get the normal bitrate in bits/s. 
		/// </summary>
		public int NormalBitrate
		{
			get
			{
				return linphone_payload_type_get_normal_bitrate(nativePtr);
			}
			set
			{
				linphone_payload_type_set_normal_bitrate(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_payload_type_get_number(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_payload_type_set_number(IntPtr thiz, int number);

		/// <summary>
		/// Returns the payload type number assigned for this codec. 
		/// </summary>
		public int Number
		{
			get
			{
				return linphone_payload_type_get_number(nativePtr);
			}
			set
			{
				linphone_payload_type_set_number(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_payload_type_get_recv_fmtp(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_payload_type_set_recv_fmtp(IntPtr thiz, string recvFmtp);

		/// <summary>
		/// Get the format parameters for incoming streams. 
		/// </summary>
		public string RecvFmtp
		{
			get
			{
				IntPtr stringPtr = linphone_payload_type_get_recv_fmtp(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_payload_type_set_recv_fmtp(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_payload_type_get_send_fmtp(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_payload_type_set_send_fmtp(IntPtr thiz, string sendFmtp);

		/// <summary>
		/// Get the format parameters for outgoing streams. 
		/// </summary>
		public string SendFmtp
		{
			get
			{
				IntPtr stringPtr = linphone_payload_type_get_send_fmtp(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_payload_type_set_send_fmtp(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_payload_type_get_type(IntPtr thiz);

		/// <summary>
		/// Get the type of a payload type. 
		/// </summary>
		public int Type
		{
			get
			{
				return linphone_payload_type_get_type(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_payload_type_enable(IntPtr thiz, char enabled);

		/// <summary>
		/// Enable/disable a payload type. 
		/// </summary>
		public int Enable(bool enabled)
		{
			int returnVal = linphone_payload_type_enable(nativePtr, enabled ? (char)1 : (char)0);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_payload_type_enabled(IntPtr thiz);

		/// <summary>
		/// Check whether a palyoad type is enabled. 
		/// </summary>
		public bool Enabled()
		{
			bool returnVal = linphone_payload_type_enabled(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
	}
	/// <summary>
	/// Player interface. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Player : LinphoneObject
	{

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_player_cbs(IntPtr factory);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_player_add_callbacks(IntPtr thiz, IntPtr cbs);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_player_remove_callbacks(IntPtr thiz, IntPtr cbs);

		~Player() 
		{
			if (listener != null)
			{
				linphone_player_remove_callbacks(nativePtr, listener.nativePtr);
			}
		}

		private PlayerListener listener;

		public PlayerListener Listener
		{
			get {
				if (listener == null)
				{
					IntPtr nativeListener = linphone_factory_create_player_cbs(IntPtr.Zero);
					listener = fromNativePtr<PlayerListener>(nativeListener, false);
					linphone_player_add_callbacks(nativePtr, nativeListener);
					listener.register();
				}
				return listener;
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_player_get_core(IntPtr thiz);

		/// <summary>
		/// Returns the <see cref="Linphone.Core" /> object managing this player's call, if
		/// any. 
		/// </summary>
		public Linphone.Core Core
		{
			get
			{
				IntPtr ptr = linphone_player_get_core(nativePtr);
				Linphone.Core obj = fromNativePtr<Linphone.Core>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_player_get_current_callbacks(IntPtr thiz);

		/// <summary>
		/// Returns the current LinphonePlayerCbsCbs object. 
		/// </summary>
		public Linphone.PlayerListener CurrentCallbacks
		{
			get
			{
				IntPtr ptr = linphone_player_get_current_callbacks(nativePtr);
				Linphone.PlayerListener obj = fromNativePtr<Linphone.PlayerListener>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_player_get_current_position(IntPtr thiz);

		/// <summary>
		/// Get the current position in the opened file. 
		/// </summary>
		public int CurrentPosition
		{
			get
			{
				return linphone_player_get_current_position(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_player_get_duration(IntPtr thiz);

		/// <summary>
		/// Get the duration of the opened file. 
		/// </summary>
		public int Duration
		{
			get
			{
				return linphone_player_get_duration(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.PlayerState linphone_player_get_state(IntPtr thiz);

		/// <summary>
		/// Get the current state of a player. 
		/// </summary>
		public Linphone.PlayerState State
		{
			get
			{
				return linphone_player_get_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_player_close(IntPtr thiz);

		/// <summary>
		/// Close the opened file. 
		/// </summary>
		public void Close()
		{
			linphone_player_close(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_player_open(IntPtr thiz, string filename);

		/// <summary>
		/// Open a file for playing. 
		/// </summary>
		public void Open(string filename)
		{
			int exception_result = linphone_player_open(nativePtr, filename);
			if (exception_result != 0) throw new LinphoneException("Open returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_player_pause(IntPtr thiz);

		/// <summary>
		/// Pause the playing of a file. 
		/// </summary>
		public void Pause()
		{
			int exception_result = linphone_player_pause(nativePtr);
			if (exception_result != 0) throw new LinphoneException("Pause returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_player_seek(IntPtr thiz, int timeMs);

		/// <summary>
		/// Seek in an opened file. 
		/// </summary>
		public void Seek(int timeMs)
		{
			int exception_result = linphone_player_seek(nativePtr, timeMs);
			if (exception_result != 0) throw new LinphoneException("Seek returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_player_start(IntPtr thiz);

		/// <summary>
		/// Start playing a file that has been opened with <see
		/// cref="Linphone.Player.Open()" />. 
		/// </summary>
		public void Start()
		{
			int exception_result = linphone_player_start(nativePtr);
			if (exception_result != 0) throw new LinphoneException("Start returned value" + exception_result);
			
			
		}
	}
	/// <summary>
	/// Presence activity type holding information about a presence activity. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class PresenceActivity : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_activity_get_description(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_activity_set_description(IntPtr thiz, string description);

		/// <summary>
		/// Gets the description of a presence activity. 
		/// </summary>
		public string Description
		{
			get
			{
				IntPtr stringPtr = linphone_presence_activity_get_description(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_presence_activity_set_description(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Description setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.PresenceActivityType linphone_presence_activity_get_type(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_activity_set_type(IntPtr thiz, int acttype);

		/// <summary>
		/// Gets the activity type of a presence activity. 
		/// </summary>
		public Linphone.PresenceActivityType Type
		{
			get
			{
				return linphone_presence_activity_get_type(nativePtr);
			}
			set
			{
				int exception_result = linphone_presence_activity_set_type(nativePtr, (int)value);
				if (exception_result != 0) throw new LinphoneException("Type setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_activity_to_string(IntPtr thiz);

		/// <summary>
		/// Gets the string representation of a presence activity. 
		/// </summary>
		public override string ToString()
		{
			IntPtr stringPtr = linphone_presence_activity_to_string(nativePtr);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
	}
	/// <summary>
	/// Presence model type holding information about the presence of a person. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class PresenceModel : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_model_new_with_activity(int activity, string description);

		/// <summary>
		/// Creates a presence model specifying an activity. 
		/// </summary>
		public static Linphone.PresenceModel NewWithActivity(Linphone.PresenceActivityType activity, string description)
		{
			IntPtr ptr = linphone_presence_model_new_with_activity((int)activity, description);
			Linphone.PresenceModel returnVal = fromNativePtr<Linphone.PresenceModel>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_model_new_with_activity_and_note(int activity, string description, string note, string lang);

		/// <summary>
		/// Creates a presence model specifying an activity and adding a note. 
		/// </summary>
		public static Linphone.PresenceModel NewWithActivityAndNote(Linphone.PresenceActivityType activity, string description, string note, string lang)
		{
			IntPtr ptr = linphone_presence_model_new_with_activity_and_note((int)activity, description, note, lang);
			Linphone.PresenceModel returnVal = fromNativePtr<Linphone.PresenceModel>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_model_get_activity(IntPtr thiz);

		/// <summary>
		/// Gets the first activity of a presence model (there is usually only one). 
		/// </summary>
		public Linphone.PresenceActivity Activity
		{
			get
			{
				IntPtr ptr = linphone_presence_model_get_activity(nativePtr);
				Linphone.PresenceActivity obj = fromNativePtr<Linphone.PresenceActivity>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.PresenceBasicStatus linphone_presence_model_get_basic_status(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_set_basic_status(IntPtr thiz, int basicStatus);

		/// <summary>
		/// Gets the basic status of a presence model. 
		/// </summary>
		public Linphone.PresenceBasicStatus BasicStatus
		{
			get
			{
				return linphone_presence_model_get_basic_status(nativePtr);
			}
			set
			{
				int exception_result = linphone_presence_model_set_basic_status(nativePtr, (int)value);
				if (exception_result != 0) throw new LinphoneException("BasicStatus setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_get_capabilities(IntPtr thiz);

		/// <summary>
		/// Gets the capabilities of a <see cref="Linphone.PresenceModel" /> object. 
		/// </summary>
		public int Capabilities
		{
			get
			{
				return linphone_presence_model_get_capabilities(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.ConsolidatedPresence linphone_presence_model_get_consolidated_presence(IntPtr thiz);

		/// <summary>
		/// Get the consolidated presence from a presence model. 
		/// </summary>
		public Linphone.ConsolidatedPresence ConsolidatedPresence
		{
			get
			{
				return linphone_presence_model_get_consolidated_presence(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_model_get_contact(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_set_contact(IntPtr thiz, string contact);

		/// <summary>
		/// Gets the contact of a presence model. 
		/// </summary>
		public string Contact
		{
			get
			{
				IntPtr stringPtr = linphone_presence_model_get_contact(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_presence_model_set_contact(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Contact setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_presence_model_is_online(IntPtr thiz);

		/// <summary>
		/// Tells whether a presence model is considered online. 
		/// </summary>
		public bool IsOnline
		{
			get
			{
				return linphone_presence_model_is_online(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_presence_model_get_nb_activities(IntPtr thiz);

		/// <summary>
		/// Gets the number of activities included in the presence model. 
		/// </summary>
		public uint NbActivities
		{
			get
			{
				return linphone_presence_model_get_nb_activities(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_presence_model_get_nb_persons(IntPtr thiz);

		/// <summary>
		/// Gets the number of persons included in the presence model. 
		/// </summary>
		public uint NbPersons
		{
			get
			{
				return linphone_presence_model_get_nb_persons(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_presence_model_get_nb_services(IntPtr thiz);

		/// <summary>
		/// Gets the number of services included in the presence model. 
		/// </summary>
		public uint NbServices
		{
			get
			{
				return linphone_presence_model_get_nb_services(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_model_get_presentity(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_set_presentity(IntPtr thiz, IntPtr presentity);

		/// <summary>
		/// Gets the presentity of a presence model. 
		/// </summary>
		public Linphone.Address Presentity
		{
			get
			{
				IntPtr ptr = linphone_presence_model_get_presentity(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
			set
			{
				int exception_result = linphone_presence_model_set_presentity(nativePtr, value.nativePtr);
				if (exception_result != 0) throw new LinphoneException("Presentity setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern long linphone_presence_model_get_timestamp(IntPtr thiz);

		/// <summary>
		/// Gets the timestamp of a presence model. 
		/// </summary>
		public long Timestamp
		{
			get
			{
				return linphone_presence_model_get_timestamp(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_add_activity(IntPtr thiz, IntPtr activity);

		/// <summary>
		/// Adds an activity to a presence model. 
		/// </summary>
		public void AddActivity(Linphone.PresenceActivity activity)
		{
			int exception_result = linphone_presence_model_add_activity(nativePtr, activity != null ? activity.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AddActivity returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_add_note(IntPtr thiz, string noteContent, string lang);

		/// <summary>
		/// Adds a note to a presence model. 
		/// </summary>
		public void AddNote(string noteContent, string lang)
		{
			int exception_result = linphone_presence_model_add_note(nativePtr, noteContent, lang);
			if (exception_result != 0) throw new LinphoneException("AddNote returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_add_person(IntPtr thiz, IntPtr person);

		/// <summary>
		/// Adds a person to a presence model. 
		/// </summary>
		public void AddPerson(Linphone.PresencePerson person)
		{
			int exception_result = linphone_presence_model_add_person(nativePtr, person != null ? person.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AddPerson returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_add_service(IntPtr thiz, IntPtr service);

		/// <summary>
		/// Adds a service to a presence model. 
		/// </summary>
		public void AddService(Linphone.PresenceService service)
		{
			int exception_result = linphone_presence_model_add_service(nativePtr, service != null ? service.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AddService returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_clear_activities(IntPtr thiz);

		/// <summary>
		/// Clears the activities of a presence model. 
		/// </summary>
		public void ClearActivities()
		{
			int exception_result = linphone_presence_model_clear_activities(nativePtr);
			if (exception_result != 0) throw new LinphoneException("ClearActivities returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_clear_notes(IntPtr thiz);

		/// <summary>
		/// Clears all the notes of a presence model. 
		/// </summary>
		public void ClearNotes()
		{
			int exception_result = linphone_presence_model_clear_notes(nativePtr);
			if (exception_result != 0) throw new LinphoneException("ClearNotes returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_clear_persons(IntPtr thiz);

		/// <summary>
		/// Clears the persons of a presence model. 
		/// </summary>
		public void ClearPersons()
		{
			int exception_result = linphone_presence_model_clear_persons(nativePtr);
			if (exception_result != 0) throw new LinphoneException("ClearPersons returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_clear_services(IntPtr thiz);

		/// <summary>
		/// Clears the services of a presence model. 
		/// </summary>
		public void ClearServices()
		{
			int exception_result = linphone_presence_model_clear_services(nativePtr);
			if (exception_result != 0) throw new LinphoneException("ClearServices returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern float linphone_presence_model_get_capability_version(IntPtr thiz, int capability);

		/// <summary>
		/// Returns the version of the capability of a <see cref="Linphone.PresenceModel"
		/// />. 
		/// </summary>
		public float GetCapabilityVersion(Linphone.FriendCapability capability)
		{
			float returnVal = linphone_presence_model_get_capability_version(nativePtr, (int)capability);
			
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_model_get_note(IntPtr thiz, string lang);

		/// <summary>
		/// Gets the first note of a presence model (there is usually only one). 
		/// </summary>
		public Linphone.PresenceNote GetNote(string lang)
		{
			IntPtr ptr = linphone_presence_model_get_note(nativePtr, lang);
			Linphone.PresenceNote returnVal = fromNativePtr<Linphone.PresenceNote>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_model_get_nth_activity(IntPtr thiz, uint idx);

		/// <summary>
		/// Gets the nth activity of a presence model. 
		/// </summary>
		public Linphone.PresenceActivity GetNthActivity(uint idx)
		{
			IntPtr ptr = linphone_presence_model_get_nth_activity(nativePtr, idx);
			Linphone.PresenceActivity returnVal = fromNativePtr<Linphone.PresenceActivity>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_model_get_nth_person(IntPtr thiz, uint idx);

		/// <summary>
		/// Gets the nth person of a presence model. 
		/// </summary>
		public Linphone.PresencePerson GetNthPerson(uint idx)
		{
			IntPtr ptr = linphone_presence_model_get_nth_person(nativePtr, idx);
			Linphone.PresencePerson returnVal = fromNativePtr<Linphone.PresencePerson>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_model_get_nth_service(IntPtr thiz, uint idx);

		/// <summary>
		/// Gets the nth service of a presence model. 
		/// </summary>
		public Linphone.PresenceService GetNthService(uint idx)
		{
			IntPtr ptr = linphone_presence_model_get_nth_service(nativePtr, idx);
			Linphone.PresenceService returnVal = fromNativePtr<Linphone.PresenceService>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_presence_model_has_capability(IntPtr thiz, int capability);

		/// <summary>
		/// Returns whether or not the <see cref="Linphone.PresenceModel" /> object has a
		/// given capability. 
		/// </summary>
		public bool HasCapability(Linphone.FriendCapability capability)
		{
			bool returnVal = linphone_presence_model_has_capability(nativePtr, (int)capability) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_presence_model_has_capability_with_version(IntPtr thiz, int capability, float version);

		/// <summary>
		/// Returns whether or not the <see cref="Linphone.PresenceModel" /> object has a
		/// given capability with a certain version. 
		/// </summary>
		public bool HasCapabilityWithVersion(Linphone.FriendCapability capability, float version)
		{
			bool returnVal = linphone_presence_model_has_capability_with_version(nativePtr, (int)capability, version) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_presence_model_has_capability_with_version_or_more(IntPtr thiz, int capability, float version);

		/// <summary>
		/// Returns whether or not the <see cref="Linphone.PresenceModel" /> object has a
		/// given capability with a certain version or more. 
		/// </summary>
		public bool HasCapabilityWithVersionOrMore(Linphone.FriendCapability capability, float version)
		{
			bool returnVal = linphone_presence_model_has_capability_with_version_or_more(nativePtr, (int)capability, version) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_model_set_activity(IntPtr thiz, int activity, string description);

		/// <summary>
		/// Sets the activity of a presence model (limits to only one activity). 
		/// </summary>
		public void SetActivity(Linphone.PresenceActivityType activity, string description)
		{
			int exception_result = linphone_presence_model_set_activity(nativePtr, (int)activity, description);
			if (exception_result != 0) throw new LinphoneException("SetActivity returned value" + exception_result);
			
			
		}
	}
	/// <summary>
	/// Presence note type holding information about a presence note. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class PresenceNote : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_note_get_content(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_note_set_content(IntPtr thiz, string content);

		/// <summary>
		/// Gets the content of a presence note. 
		/// </summary>
		public string Content
		{
			get
			{
				IntPtr stringPtr = linphone_presence_note_get_content(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_presence_note_set_content(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Content setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_note_get_lang(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_note_set_lang(IntPtr thiz, string lang);

		/// <summary>
		/// Gets the language of a presence note. 
		/// </summary>
		public string Lang
		{
			get
			{
				IntPtr stringPtr = linphone_presence_note_get_lang(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_presence_note_set_lang(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Lang setter returned value " + exception_result);
			}
		}
	}
	/// <summary>
	/// Presence person holding information about a presence person. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class PresencePerson : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_person_get_id(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_person_set_id(IntPtr thiz, string id);

		/// <summary>
		/// Gets the id of a presence person. 
		/// </summary>
		public string Id
		{
			get
			{
				IntPtr stringPtr = linphone_presence_person_get_id(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_presence_person_set_id(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Id setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_presence_person_get_nb_activities(IntPtr thiz);

		/// <summary>
		/// Gets the number of activities included in the presence person. 
		/// </summary>
		public uint NbActivities
		{
			get
			{
				return linphone_presence_person_get_nb_activities(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_presence_person_get_nb_activities_notes(IntPtr thiz);

		/// <summary>
		/// Gets the number of activities notes included in the presence person. 
		/// </summary>
		public uint NbActivitiesNotes
		{
			get
			{
				return linphone_presence_person_get_nb_activities_notes(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_presence_person_get_nb_notes(IntPtr thiz);

		/// <summary>
		/// Gets the number of notes included in the presence person. 
		/// </summary>
		public uint NbNotes
		{
			get
			{
				return linphone_presence_person_get_nb_notes(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_person_add_activities_note(IntPtr thiz, IntPtr note);

		/// <summary>
		/// Adds an activities note to a presence person. 
		/// </summary>
		public void AddActivitiesNote(Linphone.PresenceNote note)
		{
			int exception_result = linphone_presence_person_add_activities_note(nativePtr, note != null ? note.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AddActivitiesNote returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_person_add_activity(IntPtr thiz, IntPtr activity);

		/// <summary>
		/// Adds an activity to a presence person. 
		/// </summary>
		public void AddActivity(Linphone.PresenceActivity activity)
		{
			int exception_result = linphone_presence_person_add_activity(nativePtr, activity != null ? activity.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AddActivity returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_person_add_note(IntPtr thiz, IntPtr note);

		/// <summary>
		/// Adds a note to a presence person. 
		/// </summary>
		public void AddNote(Linphone.PresenceNote note)
		{
			int exception_result = linphone_presence_person_add_note(nativePtr, note != null ? note.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AddNote returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_person_clear_activities(IntPtr thiz);

		/// <summary>
		/// Clears the activities of a presence person. 
		/// </summary>
		public void ClearActivities()
		{
			int exception_result = linphone_presence_person_clear_activities(nativePtr);
			if (exception_result != 0) throw new LinphoneException("ClearActivities returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_person_clear_activities_notes(IntPtr thiz);

		/// <summary>
		/// Clears the activities notes of a presence person. 
		/// </summary>
		public void ClearActivitiesNotes()
		{
			int exception_result = linphone_presence_person_clear_activities_notes(nativePtr);
			if (exception_result != 0) throw new LinphoneException("ClearActivitiesNotes returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_person_clear_notes(IntPtr thiz);

		/// <summary>
		/// Clears the notes of a presence person. 
		/// </summary>
		public void ClearNotes()
		{
			int exception_result = linphone_presence_person_clear_notes(nativePtr);
			if (exception_result != 0) throw new LinphoneException("ClearNotes returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_person_get_nth_activities_note(IntPtr thiz, uint idx);

		/// <summary>
		/// Gets the nth activities note of a presence person. 
		/// </summary>
		public Linphone.PresenceNote GetNthActivitiesNote(uint idx)
		{
			IntPtr ptr = linphone_presence_person_get_nth_activities_note(nativePtr, idx);
			Linphone.PresenceNote returnVal = fromNativePtr<Linphone.PresenceNote>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_person_get_nth_activity(IntPtr thiz, uint idx);

		/// <summary>
		/// Gets the nth activity of a presence person. 
		/// </summary>
		public Linphone.PresenceActivity GetNthActivity(uint idx)
		{
			IntPtr ptr = linphone_presence_person_get_nth_activity(nativePtr, idx);
			Linphone.PresenceActivity returnVal = fromNativePtr<Linphone.PresenceActivity>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_person_get_nth_note(IntPtr thiz, uint idx);

		/// <summary>
		/// Gets the nth note of a presence person. 
		/// </summary>
		public Linphone.PresenceNote GetNthNote(uint idx)
		{
			IntPtr ptr = linphone_presence_person_get_nth_note(nativePtr, idx);
			Linphone.PresenceNote returnVal = fromNativePtr<Linphone.PresenceNote>(ptr, true);
			
			return returnVal;
		}
	}
	/// <summary>
	/// Presence service type holding information about a presence service. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class PresenceService : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.PresenceBasicStatus linphone_presence_service_get_basic_status(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_service_set_basic_status(IntPtr thiz, int basicStatus);

		/// <summary>
		/// Gets the basic status of a presence service. 
		/// </summary>
		public Linphone.PresenceBasicStatus BasicStatus
		{
			get
			{
				return linphone_presence_service_get_basic_status(nativePtr);
			}
			set
			{
				int exception_result = linphone_presence_service_set_basic_status(nativePtr, (int)value);
				if (exception_result != 0) throw new LinphoneException("BasicStatus setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_service_get_contact(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_service_set_contact(IntPtr thiz, string contact);

		/// <summary>
		/// Gets the contact of a presence service. 
		/// </summary>
		public string Contact
		{
			get
			{
				IntPtr stringPtr = linphone_presence_service_get_contact(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_presence_service_set_contact(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Contact setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_service_get_id(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_service_set_id(IntPtr thiz, string id);

		/// <summary>
		/// Gets the id of a presence service. 
		/// </summary>
		public string Id
		{
			get
			{
				IntPtr stringPtr = linphone_presence_service_get_id(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_presence_service_set_id(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Id setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_presence_service_get_nb_notes(IntPtr thiz);

		/// <summary>
		/// Gets the number of notes included in the presence service. 
		/// </summary>
		public uint NbNotes
		{
			get
			{
				return linphone_presence_service_get_nb_notes(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_service_get_service_descriptions(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_service_set_service_descriptions(IntPtr thiz, IntPtr descriptions);

		/// <summary>
		/// Gets the service descriptions of a presence service. 
		/// </summary>
		public IEnumerable<string> ServiceDescriptions
		{
			get
			{
				return MarshalStringArray(linphone_presence_service_get_service_descriptions(nativePtr));
			}
			set
			{
				int exception_result = linphone_presence_service_set_service_descriptions(nativePtr, StringArrayToBctbxList(value));
				CleanStringArrayPtrs();
				if (exception_result != 0) throw new LinphoneException("ServiceDescriptions setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_service_add_note(IntPtr thiz, IntPtr note);

		/// <summary>
		/// Adds a note to a presence service. 
		/// </summary>
		public void AddNote(Linphone.PresenceNote note)
		{
			int exception_result = linphone_presence_service_add_note(nativePtr, note != null ? note.nativePtr : IntPtr.Zero);
			if (exception_result != 0) throw new LinphoneException("AddNote returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_presence_service_clear_notes(IntPtr thiz);

		/// <summary>
		/// Clears the notes of a presence service. 
		/// </summary>
		public void ClearNotes()
		{
			int exception_result = linphone_presence_service_clear_notes(nativePtr);
			if (exception_result != 0) throw new LinphoneException("ClearNotes returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_presence_service_get_nth_note(IntPtr thiz, uint idx);

		/// <summary>
		/// Gets the nth note of a presence service. 
		/// </summary>
		public Linphone.PresenceNote GetNthNote(uint idx)
		{
			IntPtr ptr = linphone_presence_service_get_nth_note(nativePtr, idx);
			Linphone.PresenceNote returnVal = fromNativePtr<Linphone.PresenceNote>(ptr, true);
			
			return returnVal;
		}
	}
	/// <summary>
	/// The <see cref="Linphone.ProxyConfig" /> object represents a proxy configuration
	/// to be used by the <see cref="Linphone.Core" /> object. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class ProxyConfig : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_proxy_config_avpf_enabled(IntPtr thiz);

		/// <summary>
		/// Indicates whether AVPF/SAVPF is being used for calls using this proxy config. 
		/// </summary>
		public bool AvpfEnabled
		{
			get
			{
				return linphone_proxy_config_avpf_enabled(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.AVPFMode linphone_proxy_config_get_avpf_mode(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_avpf_mode(IntPtr thiz, int mode);

		/// <summary>
		/// Get enablement status of RTCP feedback (also known as AVPF profile). 
		/// </summary>
		public Linphone.AVPFMode AvpfMode
		{
			get
			{
				return linphone_proxy_config_get_avpf_mode(nativePtr);
			}
			set
			{
				linphone_proxy_config_set_avpf_mode(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_proxy_config_get_avpf_rr_interval(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_avpf_rr_interval(IntPtr thiz, uint interval);

		/// <summary>
		/// Get the interval between regular RTCP reports when using AVPF/SAVPF. 
		/// </summary>
		public uint AvpfRrInterval
		{
			get
			{
				return linphone_proxy_config_get_avpf_rr_interval(nativePtr);
			}
			set
			{
				linphone_proxy_config_set_avpf_rr_interval(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_conference_factory_uri(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_conference_factory_uri(IntPtr thiz, string uri);

		/// <summary>
		/// Get the conference factory uri. 
		/// </summary>
		public string ConferenceFactoryUri
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_conference_factory_uri(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_proxy_config_set_conference_factory_uri(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_contact(IntPtr thiz);

		/// <summary>
		/// Return the contact address of the proxy config. 
		/// </summary>
		public Linphone.Address Contact
		{
			get
			{
				IntPtr ptr = linphone_proxy_config_get_contact(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_contact_parameters(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_contact_parameters(IntPtr thiz, string contactParams);

		public string ContactParameters
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_contact_parameters(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_proxy_config_set_contact_parameters(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_contact_uri_parameters(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_contact_uri_parameters(IntPtr thiz, string contactUriParams);

		public string ContactUriParameters
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_contact_uri_parameters(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_proxy_config_set_contact_uri_parameters(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_core(IntPtr thiz);

		/// <summary>
		/// Get the <see cref="Linphone.Core" /> object to which is associated the <see
		/// cref="Linphone.ProxyConfig" />. 
		/// </summary>
		public Linphone.Core Core
		{
			get
			{
				IntPtr ptr = linphone_proxy_config_get_core(nativePtr);
				Linphone.Core obj = fromNativePtr<Linphone.Core>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_dependency(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_dependency(IntPtr thiz, IntPtr dependency);

		/// <summary>
		/// Get the dependency of a <see cref="Linphone.ProxyConfig" />. 
		/// </summary>
		public Linphone.ProxyConfig Dependency
		{
			get
			{
				IntPtr ptr = linphone_proxy_config_get_dependency(nativePtr);
				Linphone.ProxyConfig obj = fromNativePtr<Linphone.ProxyConfig>(ptr, true);
				return obj;
			}
			set
			{
				linphone_proxy_config_set_dependency(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_proxy_config_get_dial_escape_plus(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_dial_escape_plus(IntPtr thiz, char val);

		public bool DialEscapePlus
		{
			get
			{
				return linphone_proxy_config_get_dial_escape_plus(nativePtr) != 0;
			}
			set
			{
				linphone_proxy_config_set_dial_escape_plus(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_dial_prefix(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_dial_prefix(IntPtr thiz, string prefix);

		public string DialPrefix
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_dial_prefix(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_proxy_config_set_dial_prefix(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_domain(IntPtr thiz);

		/// <summary>
		/// Get the domain name of the given proxy config. 
		/// </summary>
		public string Domain
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_domain(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.Reason linphone_proxy_config_get_error(IntPtr thiz);

		/// <summary>
		/// Get the reason why registration failed when the proxy config state is
		/// LinphoneRegistrationFailed. 
		/// </summary>
		public Linphone.Reason Error
		{
			get
			{
				return linphone_proxy_config_get_error(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_error_info(IntPtr thiz);

		/// <summary>
		/// Get detailed information why registration failed when the proxy config state is
		/// LinphoneRegistrationFailed. 
		/// </summary>
		public Linphone.ErrorInfo ErrorInfo
		{
			get
			{
				IntPtr ptr = linphone_proxy_config_get_error_info(nativePtr);
				Linphone.ErrorInfo obj = fromNativePtr<Linphone.ErrorInfo>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_proxy_config_get_expires(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_expires(IntPtr thiz, int expires);

		public int Expires
		{
			get
			{
				return linphone_proxy_config_get_expires(nativePtr);
			}
			set
			{
				linphone_proxy_config_set_expires(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_identity_address(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_proxy_config_set_identity_address(IntPtr thiz, IntPtr identity);

		public Linphone.Address IdentityAddress
		{
			get
			{
				IntPtr ptr = linphone_proxy_config_get_identity_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
			set
			{
				int exception_result = linphone_proxy_config_set_identity_address(nativePtr, value.nativePtr);
				if (exception_result != 0) throw new LinphoneException("IdentityAddress setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_idkey(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_idkey(IntPtr thiz, string idkey);

		/// <summary>
		/// Get the idkey property of a <see cref="Linphone.ProxyConfig" />. 
		/// </summary>
		public string Idkey
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_idkey(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_proxy_config_set_idkey(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_proxy_config_is_push_notification_allowed(IntPtr thiz);

		/// <summary>
		/// Indicates whether to add to the contact parameters the push notification
		/// information. 
		/// </summary>
		public bool IsPushNotificationAllowed
		{
			get
			{
				return linphone_proxy_config_is_push_notification_allowed(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_nat_policy(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_nat_policy(IntPtr thiz, IntPtr policy);

		/// <summary>
		/// Get The policy that is used to pass through NATs/firewalls when using this
		/// proxy config. 
		/// </summary>
		public Linphone.NatPolicy NatPolicy
		{
			get
			{
				IntPtr ptr = linphone_proxy_config_get_nat_policy(nativePtr);
				Linphone.NatPolicy obj = fromNativePtr<Linphone.NatPolicy>(ptr, true);
				return obj;
			}
			set
			{
				linphone_proxy_config_set_nat_policy(nativePtr, value.nativePtr);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_proxy_config_get_privacy(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_privacy(IntPtr thiz, uint privacy);

		/// <summary>
		/// Get default privacy policy for all calls routed through this proxy. 
		/// </summary>
		public uint Privacy
		{
			get
			{
				return linphone_proxy_config_get_privacy(nativePtr);
			}
			set
			{
				linphone_proxy_config_set_privacy(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_proxy_config_publish_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_enable_publish(IntPtr thiz, char val);

		public bool PublishEnabled
		{
			get
			{
				return linphone_proxy_config_publish_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_proxy_config_enable_publish(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_proxy_config_get_publish_expires(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_publish_expires(IntPtr thiz, int expires);

		/// <summary>
		/// get the publish expiration time in second. 
		/// </summary>
		public int PublishExpires
		{
			get
			{
				return linphone_proxy_config_get_publish_expires(nativePtr);
			}
			set
			{
				linphone_proxy_config_set_publish_expires(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_push_notification_allowed(IntPtr thiz, char allow);

		/// <summary>
		/// Indicates whether to add to the contact parameters the push notification
		/// information. 
		/// </summary>
		public bool PushNotificationAllowed
		{
			set
			{
				linphone_proxy_config_set_push_notification_allowed(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_quality_reporting_collector(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_quality_reporting_collector(IntPtr thiz, string collector);

		/// <summary>
		/// Get the route of the collector end-point when using quality reporting. 
		/// </summary>
		public string QualityReportingCollector
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_quality_reporting_collector(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_proxy_config_set_quality_reporting_collector(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_proxy_config_quality_reporting_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_enable_quality_reporting(IntPtr thiz, char enable);

		/// <summary>
		/// Indicates whether quality statistics during call should be stored and sent to a
		/// collector according to RFC 6035. 
		/// </summary>
		public bool QualityReportingEnabled
		{
			get
			{
				return linphone_proxy_config_quality_reporting_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_proxy_config_enable_quality_reporting(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_proxy_config_get_quality_reporting_interval(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_quality_reporting_interval(IntPtr thiz, int interval);

		/// <summary>
		/// Get the interval between interval reports when using quality reporting. 
		/// </summary>
		public int QualityReportingInterval
		{
			get
			{
				return linphone_proxy_config_get_quality_reporting_interval(nativePtr);
			}
			set
			{
				linphone_proxy_config_set_quality_reporting_interval(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_realm(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_realm(IntPtr thiz, string realm);

		/// <summary>
		/// Get the realm of the given proxy config. 
		/// </summary>
		public string Realm
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_realm(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_proxy_config_set_realm(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_ref_key(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_ref_key(IntPtr thiz, string refkey);

		/// <summary>
		/// Get the persistent reference key associated to the proxy config. 
		/// </summary>
		public string RefKey
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_ref_key(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_proxy_config_set_ref_key(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_proxy_config_register_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_enable_register(IntPtr thiz, char val);

		public bool RegisterEnabled
		{
			get
			{
				return linphone_proxy_config_register_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_proxy_config_enable_register(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_route(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_proxy_config_set_route(IntPtr thiz, string route);

		public string Route
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_route(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_proxy_config_set_route(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("Route setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_routes(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_proxy_config_set_routes(IntPtr thiz, IntPtr routes);

		/// <summary>
		/// Gets the list of the routes set for this proxy config. 
		/// </summary>
		public IEnumerable<string> Routes
		{
			get
			{
				return MarshalStringArray(linphone_proxy_config_get_routes(nativePtr));
			}
			set
			{
				int exception_result = linphone_proxy_config_set_routes(nativePtr, StringArrayToBctbxList(value));
				CleanStringArrayPtrs();
				if (exception_result != 0) throw new LinphoneException("Routes setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_server_addr(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_proxy_config_set_server_addr(IntPtr thiz, string serverAddr);

		public string ServerAddr
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_server_addr(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				int exception_result = linphone_proxy_config_set_server_addr(nativePtr, value);
				if (exception_result != 0) throw new LinphoneException("ServerAddr setter returned value " + exception_result);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.RegistrationState linphone_proxy_config_get_state(IntPtr thiz);

		/// <summary>
		/// Get the registration state of the given proxy config. 
		/// </summary>
		public Linphone.RegistrationState State
		{
			get
			{
				return linphone_proxy_config_get_state(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_transport(IntPtr thiz);

		/// <summary>
		/// Get the transport from either service route, route or addr. 
		/// </summary>
		public string Transport
		{
			get
			{
				IntPtr stringPtr = linphone_proxy_config_get_transport(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_proxy_config_get_unread_chat_message_count(IntPtr thiz);

		/// <summary>
		/// Return the unread chat message count for a given proxy config. 
		/// </summary>
		public int UnreadChatMessageCount
		{
			get
			{
				return linphone_proxy_config_get_unread_chat_message_count(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_proxy_config_done(IntPtr thiz);

		/// <summary>
		/// Commits modification made to the proxy configuration. 
		/// </summary>
		public void Done()
		{
			int exception_result = linphone_proxy_config_done(nativePtr);
			if (exception_result != 0) throw new LinphoneException("Done returned value" + exception_result);
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_edit(IntPtr thiz);

		/// <summary>
		/// Starts editing a proxy configuration. 
		/// </summary>
		public void Edit()
		{
			linphone_proxy_config_edit(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_find_auth_info(IntPtr thiz);

		/// <summary>
		/// Find authentication info matching proxy config, if any, similarly to
		/// linphone_core_find_auth_info. 
		/// </summary>
		public Linphone.AuthInfo FindAuthInfo()
		{
			IntPtr ptr = linphone_proxy_config_find_auth_info(nativePtr);
			Linphone.AuthInfo returnVal = fromNativePtr<Linphone.AuthInfo>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_get_custom_header(IntPtr thiz, string headerName);

		/// <summary>
		/// Obtain the value of a header sent by the server in last answer to REGISTER. 
		/// </summary>
		public string GetCustomHeader(string headerName)
		{
			IntPtr stringPtr = linphone_proxy_config_get_custom_header(nativePtr, headerName);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_proxy_config_is_phone_number(IntPtr thiz, string username);

		/// <summary>
		/// Detect if the given input is a phone number or not. 
		/// </summary>
		public bool IsPhoneNumber(string username)
		{
			bool returnVal = linphone_proxy_config_is_phone_number(nativePtr, username) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_normalize_phone_number(IntPtr thiz, string username);

		/// <summary>
		/// Normalize a human readable phone number into a basic string. 
		/// </summary>
		public string NormalizePhoneNumber(string username)
		{
			IntPtr stringPtr = linphone_proxy_config_normalize_phone_number(nativePtr, username);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_proxy_config_normalize_sip_uri(IntPtr thiz, string username);

		/// <summary>
		/// Normalize a human readable sip uri into a fully qualified LinphoneAddress. 
		/// </summary>
		public Linphone.Address NormalizeSipUri(string username)
		{
			IntPtr ptr = linphone_proxy_config_normalize_sip_uri(nativePtr, username);
			Linphone.Address returnVal = fromNativePtr<Linphone.Address>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_pause_register(IntPtr thiz);

		/// <summary>
		/// Prevent a proxy config from refreshing its registration. 
		/// </summary>
		public void PauseRegister()
		{
			linphone_proxy_config_pause_register(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_refresh_register(IntPtr thiz);

		/// <summary>
		/// Refresh a proxy registration. 
		/// </summary>
		public void RefreshRegister()
		{
			linphone_proxy_config_refresh_register(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_proxy_config_set_custom_header(IntPtr thiz, string headerName, string headerValue);

		/// <summary>
		/// Set the value of a custom header sent to the server in REGISTERs request. 
		/// </summary>
		public void SetCustomHeader(string headerName, string headerValue)
		{
			linphone_proxy_config_set_custom_header(nativePtr, headerName, headerValue);
			
			
			
		}
	}
	/// <summary>
	/// Object holding chat message data received by a push notification. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class PushNotificationMessage : LinphoneObject
	{


	}
	/// <summary>
	/// Structure describing a range of integers. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Range : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_range_get_max(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_range_set_max(IntPtr thiz, int max);

		/// <summary>
		/// Gets the higher value of the range. 
		/// </summary>
		public int Max
		{
			get
			{
				return linphone_range_get_max(nativePtr);
			}
			set
			{
				linphone_range_set_max(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_range_get_min(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_range_set_min(IntPtr thiz, int min);

		/// <summary>
		/// Gets the lower value of the range. 
		/// </summary>
		public int Min
		{
			get
			{
				return linphone_range_get_min(nativePtr);
			}
			set
			{
				linphone_range_set_min(nativePtr, value);
				
			}
		}
	}
	/// <summary>
	/// The LinphoneSearchResult object represents a result of a search. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class SearchResult : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_search_result_get_address(IntPtr thiz);

		public Linphone.Address Address
		{
			get
			{
				IntPtr ptr = linphone_search_result_get_address(nativePtr);
				Linphone.Address obj = fromNativePtr<Linphone.Address>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_search_result_get_capabilities(IntPtr thiz);

		public int Capabilities
		{
			get
			{
				return linphone_search_result_get_capabilities(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_search_result_get_friend(IntPtr thiz);

		public Linphone.Friend Friend
		{
			get
			{
				IntPtr ptr = linphone_search_result_get_friend(nativePtr);
				Linphone.Friend obj = fromNativePtr<Linphone.Friend>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_search_result_get_phone_number(IntPtr thiz);

		public string PhoneNumber
		{
			get
			{
				IntPtr stringPtr = linphone_search_result_get_phone_number(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_search_result_get_weight(IntPtr thiz);

		public uint Weight
		{
			get
			{
				return linphone_search_result_get_weight(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_search_result_has_capability(IntPtr thiz, int capability);

		public bool HasCapability(Linphone.FriendCapability capability)
		{
			bool returnVal = linphone_search_result_has_capability(nativePtr, (int)capability) == (char)0 ? false : true;
			
			return returnVal;
		}
	}
	/// <summary>
	/// Linphone core SIP transport ports. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Transports : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_transports_get_dtls_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_transports_set_dtls_port(IntPtr thiz, int port);

		/// <summary>
		/// Gets the DTLS port in the <see cref="Linphone.Transports" /> object. 
		/// </summary>
		public int DtlsPort
		{
			get
			{
				return linphone_transports_get_dtls_port(nativePtr);
			}
			set
			{
				linphone_transports_set_dtls_port(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_transports_get_tcp_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_transports_set_tcp_port(IntPtr thiz, int port);

		/// <summary>
		/// Gets the TCP port in the <see cref="Linphone.Transports" /> object. 
		/// </summary>
		public int TcpPort
		{
			get
			{
				return linphone_transports_get_tcp_port(nativePtr);
			}
			set
			{
				linphone_transports_set_tcp_port(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_transports_get_tls_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_transports_set_tls_port(IntPtr thiz, int port);

		/// <summary>
		/// Gets the TLS port in the <see cref="Linphone.Transports" /> object. 
		/// </summary>
		public int TlsPort
		{
			get
			{
				return linphone_transports_get_tls_port(nativePtr);
			}
			set
			{
				linphone_transports_set_tls_port(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_transports_get_udp_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_transports_set_udp_port(IntPtr thiz, int port);

		/// <summary>
		/// Gets the UDP port in the <see cref="Linphone.Transports" /> object. 
		/// </summary>
		public int UdpPort
		{
			get
			{
				return linphone_transports_get_udp_port(nativePtr);
			}
			set
			{
				linphone_transports_set_udp_port(nativePtr, value);
				
			}
		}
	}
	/// <summary>
	/// Linphone tunnel object. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Tunnel : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_tunnel_get_activated(IntPtr thiz);

		/// <summary>
		/// Returns whether the tunnel is activated. 
		/// </summary>
		public bool Activated
		{
			get
			{
				return linphone_tunnel_get_activated(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_tunnel_dual_mode_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_enable_dual_mode(IntPtr thiz, char dualModeEnabled);

		/// <summary>
		/// Get the dual tunnel client mode. 
		/// </summary>
		public bool DualModeEnabled
		{
			get
			{
				return linphone_tunnel_dual_mode_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_tunnel_enable_dual_mode(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.TunnelMode linphone_tunnel_get_mode(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_set_mode(IntPtr thiz, int mode);

		/// <summary>
		/// Get the tunnel mode. 
		/// </summary>
		public Linphone.TunnelMode Mode
		{
			get
			{
				return linphone_tunnel_get_mode(nativePtr);
			}
			set
			{
				linphone_tunnel_set_mode(nativePtr, (int)value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_tunnel_get_servers(IntPtr thiz);

		/// <summary>
		/// Get added servers. 
		/// </summary>
		public IEnumerable<Linphone.TunnelConfig> Servers
		{
			get
			{
				return MarshalBctbxList<Linphone.TunnelConfig>(linphone_tunnel_get_servers(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_tunnel_sip_enabled(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_enable_sip(IntPtr thiz, char enable);

		/// <summary>
		/// Check whether tunnel is set to transport SIP packets. 
		/// </summary>
		public bool SipEnabled
		{
			get
			{
				return linphone_tunnel_sip_enabled(nativePtr) != 0;
			}
			set
			{
				linphone_tunnel_enable_sip(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_add_server(IntPtr thiz, IntPtr tunnelConfig);

		/// <summary>
		/// Add a tunnel server configuration. 
		/// </summary>
		public void AddServer(Linphone.TunnelConfig tunnelConfig)
		{
			linphone_tunnel_add_server(nativePtr, tunnelConfig != null ? tunnelConfig.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_clean_servers(IntPtr thiz);

		/// <summary>
		/// Remove all tunnel server addresses previously entered with <see
		/// cref="Linphone.Tunnel.AddServer()" /> 
		/// </summary>
		public void CleanServers()
		{
			linphone_tunnel_clean_servers(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_tunnel_connected(IntPtr thiz);

		/// <summary>
		/// Check whether the tunnel is connected. 
		/// </summary>
		public bool Connected()
		{
			bool returnVal = linphone_tunnel_connected(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_reconnect(IntPtr thiz);

		/// <summary>
		/// Force reconnection to the tunnel server. 
		/// </summary>
		public void Reconnect()
		{
			linphone_tunnel_reconnect(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_remove_server(IntPtr thiz, IntPtr tunnelConfig);

		/// <summary>
		/// Remove a tunnel server configuration. 
		/// </summary>
		public void RemoveServer(Linphone.TunnelConfig tunnelConfig)
		{
			linphone_tunnel_remove_server(nativePtr, tunnelConfig != null ? tunnelConfig.nativePtr : IntPtr.Zero);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_set_http_proxy(IntPtr thiz, string host, int port, string username, string passwd);

		/// <summary>
		/// Set an optional http proxy to go through when connecting to tunnel server. 
		/// </summary>
		public void SetHttpProxy(string host, int port, string username, string passwd)
		{
			linphone_tunnel_set_http_proxy(nativePtr, host, port, username, passwd);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_set_http_proxy_auth_info(IntPtr thiz, string username, string passwd);

		/// <summary>
		/// Set authentication info for the http proxy. 
		/// </summary>
		public void SetHttpProxyAuthInfo(string username, string passwd)
		{
			linphone_tunnel_set_http_proxy_auth_info(nativePtr, username, passwd);
			
			
			
		}
	}
	/// <summary>
	/// Tunnel settings. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class TunnelConfig : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_tunnel_config_get_delay(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_config_set_delay(IntPtr thiz, int delay);

		/// <summary>
		/// Get the UDP packet round trip delay in ms for a tunnel configuration. 
		/// </summary>
		public int Delay
		{
			get
			{
				return linphone_tunnel_config_get_delay(nativePtr);
			}
			set
			{
				linphone_tunnel_config_set_delay(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_tunnel_config_get_host(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_config_set_host(IntPtr thiz, string host);

		/// <summary>
		/// Get the IP address or hostname of the tunnel server. 
		/// </summary>
		public string Host
		{
			get
			{
				IntPtr stringPtr = linphone_tunnel_config_get_host(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_tunnel_config_set_host(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_tunnel_config_get_host2(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_config_set_host2(IntPtr thiz, string host);

		/// <summary>
		/// Get the IP address or hostname of the second tunnel server when using dual
		/// tunnel client. 
		/// </summary>
		public string Host2
		{
			get
			{
				IntPtr stringPtr = linphone_tunnel_config_get_host2(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_tunnel_config_set_host2(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_tunnel_config_get_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_config_set_port(IntPtr thiz, int port);

		/// <summary>
		/// Get the TLS port of the tunnel server. 
		/// </summary>
		public int Port
		{
			get
			{
				return linphone_tunnel_config_get_port(nativePtr);
			}
			set
			{
				linphone_tunnel_config_set_port(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_tunnel_config_get_port2(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_config_set_port2(IntPtr thiz, int port);

		/// <summary>
		/// Get the TLS port of the second tunnel server when using dual tunnel client. 
		/// </summary>
		public int Port2
		{
			get
			{
				return linphone_tunnel_config_get_port2(nativePtr);
			}
			set
			{
				linphone_tunnel_config_set_port2(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_tunnel_config_get_remote_udp_mirror_port(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_tunnel_config_set_remote_udp_mirror_port(IntPtr thiz, int remoteUdpMirrorPort);

		/// <summary>
		/// Get the remote port on the tunnel server side used to test UDP reachability. 
		/// </summary>
		public int RemoteUdpMirrorPort
		{
			get
			{
				return linphone_tunnel_config_get_remote_udp_mirror_port(nativePtr);
			}
			set
			{
				linphone_tunnel_config_set_remote_udp_mirror_port(nativePtr, value);
				
			}
		}
	}
	/// <summary>
	/// The <see cref="Linphone.Vcard" /> object. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class Vcard : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_get_etag(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_set_etag(IntPtr thiz, string etag);

		/// <summary>
		/// Gets the eTag of the vCard. 
		/// </summary>
		public string Etag
		{
			get
			{
				IntPtr stringPtr = linphone_vcard_get_etag(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_vcard_set_etag(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_get_family_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_set_family_name(IntPtr thiz, string name);

		/// <summary>
		/// Returns the family name in the N attribute of the vCard, or null if it isn't
		/// set yet. 
		/// </summary>
		public string FamilyName
		{
			get
			{
				IntPtr stringPtr = linphone_vcard_get_family_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_vcard_set_family_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_get_full_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_set_full_name(IntPtr thiz, string name);

		/// <summary>
		/// Returns the FN attribute of the vCard, or null if it isn't set yet. 
		/// </summary>
		public string FullName
		{
			get
			{
				IntPtr stringPtr = linphone_vcard_get_full_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_vcard_set_full_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_get_given_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_set_given_name(IntPtr thiz, string name);

		/// <summary>
		/// Returns the given name in the N attribute of the vCard, or null if it isn't set
		/// yet. 
		/// </summary>
		public string GivenName
		{
			get
			{
				IntPtr stringPtr = linphone_vcard_get_given_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_vcard_set_given_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_get_organization(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_set_organization(IntPtr thiz, string organization);

		/// <summary>
		/// Gets the Organization of the vCard. 
		/// </summary>
		public string Organization
		{
			get
			{
				IntPtr stringPtr = linphone_vcard_get_organization(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_vcard_set_organization(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_get_phone_numbers(IntPtr thiz);

		/// <summary>
		/// Returns the list of phone numbers (as string) in the vCard (all the TEL
		/// attributes) or null. 
		/// </summary>
		public IEnumerable<string> PhoneNumbers
		{
			get
			{
				return MarshalStringArray(linphone_vcard_get_phone_numbers(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_get_sip_addresses(IntPtr thiz);

		/// <summary>
		/// Returns the list of SIP addresses (as LinphoneAddress) in the vCard (all the
		/// IMPP attributes that has an URI value starting by "sip:") or null. 
		/// </summary>
		public IEnumerable<Linphone.Address> SipAddresses
		{
			get
			{
				return MarshalBctbxList<Linphone.Address>(linphone_vcard_get_sip_addresses(nativePtr));
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_vcard_get_skip_validation(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_set_skip_validation(IntPtr thiz, char skip);

		/// <summary>
		/// Returns the skipFieldValidation property of the vcard. 
		/// </summary>
		public bool SkipValidation
		{
			get
			{
				return linphone_vcard_get_skip_validation(nativePtr) != 0;
			}
			set
			{
				linphone_vcard_set_skip_validation(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_get_uid(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_set_uid(IntPtr thiz, string uid);

		/// <summary>
		/// Gets the UID of the vCard. 
		/// </summary>
		public string Uid
		{
			get
			{
				IntPtr stringPtr = linphone_vcard_get_uid(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_vcard_set_uid(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_get_url(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_set_url(IntPtr thiz, string url);

		/// <summary>
		/// Gets the URL of the vCard. 
		/// </summary>
		public string Url
		{
			get
			{
				IntPtr stringPtr = linphone_vcard_get_url(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_vcard_set_url(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_add_phone_number(IntPtr thiz, string phone);

		/// <summary>
		/// Adds a phone number in the vCard, using the TEL property. 
		/// </summary>
		public void AddPhoneNumber(string phone)
		{
			linphone_vcard_add_phone_number(nativePtr, phone);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_add_sip_address(IntPtr thiz, string sipAddress);

		/// <summary>
		/// Adds a SIP address in the vCard, using the IMPP property. 
		/// </summary>
		public void AddSipAddress(string sipAddress)
		{
			linphone_vcard_add_sip_address(nativePtr, sipAddress);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_as_vcard4_string(IntPtr thiz);

		/// <summary>
		/// Returns the vCard4 representation of the LinphoneVcard. 
		/// </summary>
		public string AsVcard4String()
		{
			IntPtr stringPtr = linphone_vcard_as_vcard4_string(nativePtr);
			string returnVal = Marshal.PtrToStringAnsi(stringPtr);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_vcard_clone(IntPtr thiz);

		/// <summary>
		/// Clone a <see cref="Linphone.Vcard" />. 
		/// </summary>
		public Linphone.Vcard Clone()
		{
			IntPtr ptr = linphone_vcard_clone(nativePtr);
			Linphone.Vcard returnVal = fromNativePtr<Linphone.Vcard>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_edit_main_sip_address(IntPtr thiz, string sipAddress);

		/// <summary>
		/// Edits the preferred SIP address in the vCard (or the first one), using the IMPP
		/// property. 
		/// </summary>
		public void EditMainSipAddress(string sipAddress)
		{
			linphone_vcard_edit_main_sip_address(nativePtr, sipAddress);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_vcard_generate_unique_id(IntPtr thiz);

		/// <summary>
		/// Generates a random unique id for the vCard. 
		/// </summary>
		public bool GenerateUniqueId()
		{
			bool returnVal = linphone_vcard_generate_unique_id(nativePtr) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_remove_phone_number(IntPtr thiz, string phone);

		/// <summary>
		/// Removes a phone number in the vCard (if it exists), using the TEL property. 
		/// </summary>
		public void RemovePhoneNumber(string phone)
		{
			linphone_vcard_remove_phone_number(nativePtr, phone);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_vcard_remove_sip_address(IntPtr thiz, string sipAddress);

		/// <summary>
		/// Removes a SIP address in the vCard (if it exists), using the IMPP property. 
		/// </summary>
		public void RemoveSipAddress(string sipAddress)
		{
			linphone_vcard_remove_sip_address(nativePtr, sipAddress);
			
			
			
		}
	}
	/// <summary>
	/// Structure describing policy regarding video streams establishments. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class VideoActivationPolicy : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_video_activation_policy_get_automatically_accept(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_video_activation_policy_set_automatically_accept(IntPtr thiz, char enable);

		/// <summary>
		/// Gets the value for the automatically accept video policy. 
		/// </summary>
		public bool AutomaticallyAccept
		{
			get
			{
				return linphone_video_activation_policy_get_automatically_accept(nativePtr) != 0;
			}
			set
			{
				linphone_video_activation_policy_set_automatically_accept(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_video_activation_policy_get_automatically_initiate(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_video_activation_policy_set_automatically_initiate(IntPtr thiz, char enable);

		/// <summary>
		/// Gets the value for the automatically initiate video policy. 
		/// </summary>
		public bool AutomaticallyInitiate
		{
			get
			{
				return linphone_video_activation_policy_get_automatically_initiate(nativePtr) != 0;
			}
			set
			{
				linphone_video_activation_policy_set_automatically_initiate(nativePtr, value ? (char)1 : (char)0);
				
			}
		}
	}
	/// <summary>
	/// The <see cref="Linphone.VideoDefinition" /> object represents a video
	/// definition, eg. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class VideoDefinition : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_video_definition_get_height(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_video_definition_set_height(IntPtr thiz, uint height);

		/// <summary>
		/// Get the height of the video definition. 
		/// </summary>
		public uint Height
		{
			get
			{
				return linphone_video_definition_get_height(nativePtr);
			}
			set
			{
				linphone_video_definition_set_height(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_video_definition_is_undefined(IntPtr thiz);

		/// <summary>
		/// Tells whether a <see cref="Linphone.VideoDefinition" /> is undefined. 
		/// </summary>
		public bool IsUndefined
		{
			get
			{
				return linphone_video_definition_is_undefined(nativePtr) != 0;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_video_definition_get_name(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_video_definition_set_name(IntPtr thiz, string name);

		/// <summary>
		/// Get the name of the video definition. 
		/// </summary>
		public string Name
		{
			get
			{
				IntPtr stringPtr = linphone_video_definition_get_name(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			set
			{
				linphone_video_definition_set_name(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern uint linphone_video_definition_get_width(IntPtr thiz);
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_video_definition_set_width(IntPtr thiz, uint width);

		/// <summary>
		/// Get the width of the video definition. 
		/// </summary>
		public uint Width
		{
			get
			{
				return linphone_video_definition_get_width(nativePtr);
			}
			set
			{
				linphone_video_definition_set_width(nativePtr, value);
				
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_video_definition_clone(IntPtr thiz);

		/// <summary>
		/// Clone a video definition. 
		/// </summary>
		public Linphone.VideoDefinition Clone()
		{
			IntPtr ptr = linphone_video_definition_clone(nativePtr);
			Linphone.VideoDefinition returnVal = fromNativePtr<Linphone.VideoDefinition>(ptr, true);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_video_definition_equals(IntPtr thiz, IntPtr vdef2);

		/// <summary>
		/// Tells whether two <see cref="Linphone.VideoDefinition" /> objects are equal
		/// (the widths and the heights are the same but can be switched). 
		/// </summary>
		public bool Equals(Linphone.VideoDefinition vdef2)
		{
			bool returnVal = linphone_video_definition_equals(nativePtr, vdef2 != null ? vdef2.nativePtr : IntPtr.Zero) == (char)0 ? false : true;
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_video_definition_set_definition(IntPtr thiz, uint width, uint height);

		/// <summary>
		/// Set the width and the height of the video definition. 
		/// </summary>
		public void SetDefinition(uint width, uint height)
		{
			linphone_video_definition_set_definition(nativePtr, width, height);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern char linphone_video_definition_strict_equals(IntPtr thiz, IntPtr vdef2);

		/// <summary>
		/// Tells whether two <see cref="Linphone.VideoDefinition" /> objects are strictly
		/// equal (the widths are the same and the heights are the same). 
		/// </summary>
		public bool StrictEquals(Linphone.VideoDefinition vdef2)
		{
			bool returnVal = linphone_video_definition_strict_equals(nativePtr, vdef2 != null ? vdef2.nativePtr : IntPtr.Zero) == (char)0 ? false : true;
			
			return returnVal;
		}
	}
	/// <summary>
	/// The <see cref="Linphone.XmlRpcRequest" /> object representing a XML-RPC request
	/// to be sent. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class XmlRpcRequest : LinphoneObject
	{

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_factory_create_xml_rpc_request_cbs(IntPtr factory);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_xml_rpc_request_add_callbacks(IntPtr thiz, IntPtr cbs);

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_xml_rpc_request_remove_callbacks(IntPtr thiz, IntPtr cbs);

		~XmlRpcRequest() 
		{
			if (listener != null)
			{
				linphone_xml_rpc_request_remove_callbacks(nativePtr, listener.nativePtr);
			}
		}

		private XmlRpcRequestListener listener;

		public XmlRpcRequestListener Listener
		{
			get {
				if (listener == null)
				{
					IntPtr nativeListener = linphone_factory_create_xml_rpc_request_cbs(IntPtr.Zero);
					listener = fromNativePtr<XmlRpcRequestListener>(nativeListener, false);
					linphone_xml_rpc_request_add_callbacks(nativePtr, nativeListener);
					listener.register();
				}
				return listener;
			}
		}

		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_xml_rpc_request_get_content(IntPtr thiz);

		/// <summary>
		/// Get the content of the XML-RPC request. 
		/// </summary>
		public string Content
		{
			get
			{
				IntPtr stringPtr = linphone_xml_rpc_request_get_content(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_xml_rpc_request_get_current_callbacks(IntPtr thiz);

		/// <summary>
		/// Get the current LinphoneXmlRpcRequestCbs object associated with a
		/// LinphoneXmlRpcRequest. 
		/// </summary>
		public Linphone.XmlRpcRequestListener CurrentCallbacks
		{
			get
			{
				IntPtr ptr = linphone_xml_rpc_request_get_current_callbacks(nativePtr);
				Linphone.XmlRpcRequestListener obj = fromNativePtr<Linphone.XmlRpcRequestListener>(ptr, true);
				return obj;
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern int linphone_xml_rpc_request_get_int_response(IntPtr thiz);

		/// <summary>
		/// Get the response to an XML-RPC request sent with <see
		/// cref="Linphone.XmlRpcSession.SendRequest()" /> and returning an integer
		/// response. 
		/// </summary>
		public int IntResponse
		{
			get
			{
				return linphone_xml_rpc_request_get_int_response(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_xml_rpc_request_get_raw_response(IntPtr thiz);

		/// <summary>
		/// Get the raw response to an XML-RPC request sent with <see
		/// cref="Linphone.XmlRpcSession.SendRequest()" /> and returning http body as
		/// string. 
		/// </summary>
		public string RawResponse
		{
			get
			{
				IntPtr stringPtr = linphone_xml_rpc_request_get_raw_response(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern Linphone.XmlRpcStatus linphone_xml_rpc_request_get_status(IntPtr thiz);

		/// <summary>
		/// Get the status of the XML-RPC request. 
		/// </summary>
		public Linphone.XmlRpcStatus Status
		{
			get
			{
				return linphone_xml_rpc_request_get_status(nativePtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_xml_rpc_request_get_string_response(IntPtr thiz);

		/// <summary>
		/// Get the response to an XML-RPC request sent with <see
		/// cref="Linphone.XmlRpcSession.SendRequest()" /> and returning a string response. 
		/// </summary>
		public string StringResponse
		{
			get
			{
				IntPtr stringPtr = linphone_xml_rpc_request_get_string_response(nativePtr);
				return Marshal.PtrToStringAnsi(stringPtr);
			}
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_xml_rpc_request_add_int_arg(IntPtr thiz, int val);

		/// <summary>
		/// Add an integer argument to an XML-RPC request. 
		/// </summary>
		public void AddIntArg(int val)
		{
			linphone_xml_rpc_request_add_int_arg(nativePtr, val);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_xml_rpc_request_add_string_arg(IntPtr thiz, string val);

		/// <summary>
		/// Add a string argument to an XML-RPC request. 
		/// </summary>
		public void AddStringArg(string val)
		{
			linphone_xml_rpc_request_add_string_arg(nativePtr, val);
			
			
			
		}
	}
	/// <summary>
	/// The <see cref="Linphone.XmlRpcSession" /> object used to send XML-RPC requests
	/// and handle their responses. 
	/// </summary>

	[StructLayout(LayoutKind.Sequential)]
	public class XmlRpcSession : LinphoneObject
	{


		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_xml_rpc_session_create_request(IntPtr thiz, int returnType, string method);

		/// <summary>
		/// Creates a <see cref="Linphone.XmlRpcRequest" /> from a <see
		/// cref="Linphone.XmlRpcSession" />. 
		/// </summary>
		public Linphone.XmlRpcRequest CreateRequest(Linphone.XmlRpcArgType returnType, string method)
		{
			IntPtr ptr = linphone_xml_rpc_session_create_request(nativePtr, (int)returnType, method);
			Linphone.XmlRpcRequest returnVal = fromNativePtr<Linphone.XmlRpcRequest>(ptr, false);
			
			return returnVal;
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_xml_rpc_session_release(IntPtr thiz);

		/// <summary>
		/// Stop and unref an XML rpc session. 
		/// </summary>
		public void Release()
		{
			linphone_xml_rpc_session_release(nativePtr);
			
			
			
		}
		[DllImport(LinphoneWrapper.LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_xml_rpc_session_send_request(IntPtr thiz, IntPtr request);

		/// <summary>
		/// Send an XML-RPC request. 
		/// </summary>
		public void SendRequest(Linphone.XmlRpcRequest request)
		{
			linphone_xml_rpc_session_send_request(nativePtr, request != null ? request.nativePtr : IntPtr.Zero);
			
			
			
		}
	}
#endregion
}
