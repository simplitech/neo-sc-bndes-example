using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace BNDES
{
	/// <summary>
	/// Storage I/O
	/// </summary>
	class GenericDao
	{

		public static byte[] Get(byte[] key)
		{
			byte[] res = Storage.Get(Storage.CurrentContext, key);
			return res;
		}

		public static void Put(byte[] key, byte[] value)
		{
			Storage.Put(Storage.CurrentContext, key, value);
		}
	}
}
