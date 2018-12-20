using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace BNDES
{
	class AppGlobals
	{

		public static readonly byte[] TrustedSourceScriptHash = "Ad83tfsuWxxexhefPzXVpn5vv6oCbLKFEx".ToScriptHash();

		#region Default Assets
		public static readonly BigInteger NeoDecimals = 100000000;
		public static readonly byte[] NeoAssetId = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
		public static readonly byte[] GasAssetId = { 96, 44, 121, 113, 139, 22, 228, 66, 222, 88, 119, 142, 20, 141, 11, 16, 132, 227, 178, 223, 253, 93, 230, 183, 177, 108, 238, 121, 105, 40, 45, 231 };
		#endregion

	}
}
