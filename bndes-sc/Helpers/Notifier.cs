using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace BNDES
{
	class Notifier
	{
		
		#region PARAM VARIATION
		
		public delegate void SmartContractNotification<T>(T p0);
		public delegate void SmartContractNotification<T, T1>(T p0, T1 p1);
		public delegate void SmartContractNotification<T, T1, T2>(T p0, T1 p1, T2 p2);
		public delegate void SmartContractNotification<T, T1, T2, T3>(T p0, T1 p1, T2 p2, T3 p3);
        public delegate void SmartContractNotification<T, T1, T2, T3, T4>(T p0, T1 p1, T2 p2, T3 p3, T4 p4);
        public delegate void SmartContractNotification<T, T1, T2, T3, T4, T5>(T p0, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5);

        #endregion

        [DisplayName("mint")] 
		public static event SmartContractNotification<byte[], BigInteger, byte[], byte[]> NotifyMintEvent;

        [DisplayName("transferTransactions")]
        public static event SmartContractNotification<byte[], byte[], BigInteger, byte[], BigInteger, byte[]> NotifyTransferEvent;

        [DisplayName("newRegularAccount")]
        public static event SmartContractNotification<byte[]> NotifyNewRegularAccountEvent;
        
        [DisplayName("newMasterAccount")]
        public static event SmartContractNotification<byte[]> NotifyNewMasterAccountEvent;

        [DisplayName("regularAccountApproved")]
        public static event SmartContractNotification<byte[], byte[]> NotifyRegularAccountApprovedEvent;
        
        public static void NotifyMint(byte[] masterAccount, BigInteger amount, byte[] recipient, byte[] txHash)
        {
            NotifyMintEvent(masterAccount, amount, recipient, txHash);
        }

        public static void NotifyNewRegularAccount(byte[] account)
        {
            NotifyNewRegularAccountEvent(account);
        }

        public static void NotifyRegularAccountApproved(byte[] masterAccount, byte[] regularAccount)
        {
            NotifyRegularAccountApprovedEvent(masterAccount, regularAccount);
        }

        public static void NotifyNewMasterAccount(byte[] masterAccount)
        {
            NotifyNewMasterAccountEvent(masterAccount);
        }

        public static void NotifyTransfer(byte[] from, byte[] to, BigInteger amount, byte[] transactionHash, BigInteger change, byte[] changeTransactionHash)
        {
            NotifyTransferEvent(from, to, amount, transactionHash, change, changeTransactionHash);
        }
        
    }
}
