using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace BNDES
{
	/// <summary>
	/// Responsible for BRC Persistence
	/// </summary>
	class BankDao
	{

		#region KEYS PREFIX
		private static readonly byte[] KpOfWalletOfBankAccount = "WalletOfBankAccount".AsByteArray();
		private static readonly byte[] KpOfWithdrawRequest = "WithdrawRequest".AsByteArray();
        private static readonly byte[] kpOfMasterAccounts = "masterAccounts".AsByteArray();
        private static readonly byte[] kpOfRegularAccounts = "regularAccounts".AsByteArray();
        private static readonly byte[] kpOfSignatures = "signatures".AsByteArray();
        private static readonly byte[] KpOfTransactions = "T".AsByteArray();
        private static readonly byte[] KpOfBlocks = "B".AsByteArray();
        #endregion

        public static void PersistRegularAccount(byte[] account, byte[] serializedData)
        {
            byte[] accountKey = kpOfRegularAccounts.Concat(account);
            GenericDao.Put(accountKey, serializedData);
        }

        public static void PersistSignature(byte[] account, byte[] signature)
        {
            byte[] signatureKey = kpOfSignatures.Concat(account);
            GenericDao.Put(signatureKey, signature);
        }

        public static byte[] GetSignature(byte[] account)
        {
            byte[] signatureKey = kpOfSignatures.Concat(account);
            return GenericDao.Get(signatureKey);
        }

        public static byte[] GetRegularAccount(byte[] newAccount)
        {
            byte[] accountKey = kpOfRegularAccounts.Concat(newAccount);
            return GenericDao.Get(accountKey);
        }

        public static byte[] GetMasterAccounts()
        {
            return GenericDao.Get(kpOfMasterAccounts);
        }

        public static void UpdateMasterAccounts(byte[] serializedMasterAccounts)
        {
            GenericDao.Put(kpOfMasterAccounts, serializedMasterAccounts);
        }

        public static void PersistTransaction(byte[] owner, byte[] txId, byte[] transaction)
        {
            byte[] transactionKey = KpOfTransactions.Concat(owner).Concat(txId);
            GenericDao.Put(transactionKey, transaction);
        }

        public static byte[] GetTransactionsBlock(BigInteger height)
        {
            byte[] blockKey = KpOfBlocks.Concat(height.ToByteArray());
            return GenericDao.Get(blockKey);
        }

        public static void PersistTransactionBlock(BigInteger height, byte[] blockBytes)
        {
            byte[] blockKey = KpOfBlocks.Concat(height.ToByteArray());
            GenericDao.Put(blockKey, blockBytes);
        }
    }
}
