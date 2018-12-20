using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;

namespace BNDES
{
	/// <summary>
	/// Front Door for the Smart Contract Operations
	/// </summary>
	public class BNDES : SmartContract
	{
        public static object Main(string operation, object[] args)
        {
            BigInteger seed = 0;
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return VerifyContract(operation, args);
            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "symbol")
                {
                    return Symbol();
                }

                if (operation == "name")
                {
                    return Name();
                }
                
                if (operation == "decimals")
                {
                    return Decimals();
                }

                if(operation == "deploy")
                {
                    return BankProcess.Deploy();
                }

                if(operation == "getRegularAccount")
                {
                    return BankProcess.GetRegularAccount(args);
                }

                if(operation == "getAccountStatus")
                {
                    return BankProcess.GetAccountStatus(args);
                }
                
                if (operation == "registerRegularAccount")
                {
                    return BankProcess.RegisterRegularAccount(args);
                }

                if (operation == "approveRegularAccount")
                {
                    return BankProcess.ApproveRegularAccount(args);
                }
                
                if (operation == "registerMasterAccount")
                {
                    return BankProcess.RegisterMasterAccount(args);
                }

				if (operation == "removeMasterAccount")
				{
                    return BankProcess.RemoveMasterAccount(args);
				}

                if(operation == "masterAccounts")
                {
                    return BankProcess.MasterAccounts(args);
                }

                if(operation == "requiredAuthorizations")
                {
                    return BankProcess.RequiredAuthorizations();
                }

                if(operation == "mintTokens")
                {
                    return BankProcess.MintTokens(args);
                }

                if(operation == "getBalance")
                {
                    return BankProcess.GetBalance(args);
                }

                if(operation == "transferTransactions")
                {
                    return BankProcess.TransferTransactions(args);
                }

                if(operation == "transferAmount")
                {
                    return BankProcess.TransferAmount(args);
                }
                if(operation == "getAccountSignature")
                {
                    return BankProcess.GetSignature(args);
                }
                

				return null;
			}

			return false;

		}
		
		public static bool VerifyContract(string signatureString, object[] args)
		{
			bool returnValue = false;
			if (AppGlobals.TrustedSourceScriptHash.Length == 20)
			{
				returnValue = Runtime.CheckWitness(AppGlobals.TrustedSourceScriptHash);
			}

			return returnValue;
		}

		public static object RunContract(string operation, object[] args)
		{
			return true;
		}


		public static string Name()
		{
			return "BNDES Token";
		}

		public static string Symbol()
		{
			var symbol = "BNDT";
			return symbol;
		}

		public static BigInteger Decimals()
		{
			return 2;
		}
	}
}
