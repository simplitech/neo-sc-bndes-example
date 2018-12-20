using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.System;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;
using SimpliPay.Model;

namespace SimpliPay.Helpers
{
	class TransactionHelper
	{


		public static void GetAssetAttachments(byte[] scriptHash)
		{
			TransactionAttachments.NeoSent = 0;
			TransactionAttachments.GasSent = 0;
			Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
			TransactionOutput[] reference = tx.GetReferences();
			TransactionAttachments.ReceiverScriptHash = ExecutionEngine.ExecutingScriptHash;
			if (reference.Length > 0)
			{
				TransactionOutput output = reference[0];
				TransactionAttachments.SenderScriptHash = output.ScriptHash;

				for (int i = 0; i < reference.Length; i++)
				{
					TransactionOutput outputReference = reference[i];
					if (output.ScriptHash == outputReference.ScriptHash)
					{
						if (output.AssetId == AppGlobals.NeoAssetId)
						{
							TransactionAttachments.NeoSent = TransactionAttachments.NeoSent + output.Value;
						}
						else if (output.AssetId == AppGlobals.GasAssetId)
						{
							TransactionAttachments.GasSent = TransactionAttachments.GasSent + output.Value;
						}
					}

				}
			}
			
		}
	}
}
