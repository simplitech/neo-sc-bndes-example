using Neo.SmartContract.Framework.Services.Neo;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.System;
using BNDES.Application;
using System;

namespace BNDES
{
	class BankProcess : SmartContract
	{
        public static readonly BigInteger MaxMint = 10000000;

        public static object TransferAmount(object[] args)
        {
            if(args.Length != 3)
            {
                Runtime.Log("Invalid parameters");
                return false;
            }

            byte[] owner = (byte[])args[0];

            if (owner.Length != 20)
            {
                return false;
            }

            //if (!Runtime.CheckWitness(owner))
            //{
            //    Runtime.Log("Not authorized");
            //    return false;
            //}

            byte[] accountBytes = BankDao.GetRegularAccount(owner);

            if (accountBytes.Length == 0)
            {
                Runtime.Log("Account not found");
                return false;
            }

            byte[] recipient = (byte[])args[1];

            if (recipient.Length != 20)
            {
                Runtime.Log("Invalid recipient");
                return false;
            }

            byte[] recipientBytes = BankDao.GetRegularAccount(recipient);

            if (recipientBytes.Length == 0)
            {
                Runtime.Log("Account not found");
                return false;
            }


            BigInteger amount = (BigInteger)args[2];
            
            if(amount <= 0)
            {
                Runtime.Log("Invalid amount");
                return false;
            }
            
            Map<string, object> ownerAccount = (Map<string, object>)accountBytes.Deserialize();
            Map<string, object> recipientAccount = (Map<string, object>)recipientBytes.Deserialize();
            Map<byte[], object> senderUnspentTransactions = (Map<byte[], object>)ownerAccount[ModelMap.ACCOUNT_UNSPENT];
            Map<byte[], object> spentTransactions = new Map<byte[], object>();
            BigInteger accumulatedTotal = 0;
            for(int i = 0; i < senderUnspentTransactions.Keys.Length; i++)
            {
                byte[] transactionHash = senderUnspentTransactions.Keys[i];
                byte[] unspentTransaction = (byte[])senderUnspentTransactions[transactionHash];
                if(unspentTransaction.Length > 0)
                {
                    Runtime.Log("Has unspent");
                    Map<string, byte[]> tx = (Map<string, byte[]>)unspentTransaction.Deserialize();
                    spentTransactions[transactionHash] = unspentTransaction;
                    BigInteger transactionAmount = tx[ModelMap.TRANSACTION_AMOUNT].AsBigInteger();

                    accumulatedTotal += transactionAmount;
                    if (accumulatedTotal >= amount)
                    {
                        Runtime.Log("Has funds");
                        object[] transactionsToTransfer = new object[spentTransactions.Keys.Length];
                        for (int j = 0; j < spentTransactions.Keys.Length; j++)
                        {
                            byte[] spentTxHash = spentTransactions.Keys[i];
                            transactionsToTransfer[i] = transactionHash;
                        }

                        return DoTransfer(owner, recipient, ownerAccount, recipientAccount, amount, transactionsToTransfer);
                    }
                    else
                    {
                        Runtime.Log("Not enough funds");
                    }
                }
                else
                {
                    Runtime.Log("No unspent");
                }
            }
            
            return false;
        }

        public static object GetSignature(object[] args)
        {
            if(args.Length != 1)
            {
                return false;
            }

            byte[] scriptHash = (byte[])args[0];
            if(scriptHash.Length != 20)
            {
                return false;
            }

            return BankDao.GetSignature(scriptHash);
        }

        /// <summary>
        /// Method that must be called before using this smart contract.
        /// It will start the storage and also deploy the genesis master account.
        /// </summary>
        /// <returns></returns>
        public static object Deploy()
        {
            //if (Runtime.CheckWitness(AppGlobals.TrustedSourceScriptHash))
            //{
                Runtime.Log("Deploying contract");
                byte[] existingMasterAccounts = BankDao.GetMasterAccounts();
                if(existingMasterAccounts.Length != 0)
                {
                    Runtime.Log("Contract already deployed");
                    return false;
                }

                Map<byte[], object> masterAccountsObjects = new Map<byte[], object>();
                var genesisMasterAccount = CreateMasterAccount("Genesis Master Account", "ricardo.prado@simpli.com.br", "Simpli Tech", "+55 11 3280-5541");
                masterAccountsObjects[AppGlobals.TrustedSourceScriptHash] = genesisMasterAccount;
                BankDao.UpdateMasterAccounts(masterAccountsObjects.Serialize());
                return true;
            //}
            //else{
            //    Runtime.Log("This account is not authorized to deploy this contract.");
            //    return false;
            //}
        }

        /// <summary>
        /// Private method used to transfer values from one account to another.
        /// </summary>
        /// <param name="senderScriptHash"></param>
        /// <param name="recipientScriptHash"></param>
        /// <param name="sender"></param>
        /// <param name="recipient"></param>
        /// <param name="amount"></param>
        /// <param name="transactions"></param>
        /// <returns></returns>
        private static object DoTransfer(byte[] senderScriptHash, byte[] recipientScriptHash, Map<string, object> sender, Map<string, object> recipient, BigInteger amount, object[] transactions)
        {
            BigInteger transactionCount = transactions.Length;
            BigInteger totalInTransfers = 0;
            Map<byte[], object> senderUnspentTransactions = (Map<byte[], object>)sender[ModelMap.ACCOUNT_UNSPENT];
            Map<byte[], object> spentTransactions = new Map<byte[], object>();
            Map<byte[], object> recipientUnspentTransactions = (Map<byte[], object>)recipient[ModelMap.ACCOUNT_UNSPENT];
            for (int i = 0; i < transactionCount; i++)
            {
                byte[] transactionHash = (byte[])transactions[i];

                if (!senderUnspentTransactions.HasKey(transactionHash))
                {
                    Runtime.Log("Not the owner");
                    return false;
                }

                byte[] transactionBytes = (byte[])senderUnspentTransactions[transactionHash];
                Map<string, object> transaction = (Map<string, object>)transactionBytes.Deserialize();

                byte[] amountInTransactionBytes = (byte[])transaction[ModelMap.TRANSACTION_AMOUNT];
                if (amountInTransactionBytes.Length <= 0)
                {
                    return false;
                }

                senderUnspentTransactions.Remove(transactionHash);
                spentTransactions[transactionHash] = transaction;

                BigInteger amountInTransaction = amountInTransactionBytes.AsBigInteger();
                totalInTransfers += amountInTransaction;
                if (totalInTransfers >= amount)
                {
                    Runtime.Log("There is enough in the transactions sent.");
                    if (i != transactionCount - 1)
                    {
                        Runtime.Log("Too many transactions");
                        return false;
                    }

                    BigInteger change = totalInTransfers - amount;
                    uint currentBlockHeight = Blockchain.GetHeight();

                    byte[] changeTransactionHash = new byte[0];
                    if (change > 0)
                    {
                        Runtime.Log("Has change!");
                        object[] previousTransactions = new object[] { transactionHash };
                        Map<string, byte[]> changeTransaction = CreateTransaction(currentBlockHeight, 0, new byte[0], change, previousTransactions);
                        byte[] serializedChangeTransaction = changeTransaction.Serialize();
                        changeTransactionHash = Hash256(serializedChangeTransaction);
                        senderUnspentTransactions[changeTransactionHash] = serializedChangeTransaction;
                    }

                    object[] previousTransactionsNewTransaction = new object[spentTransactions.Keys.Length];

                    for (int j = 0; j < spentTransactions.Keys.Length; j++)
                    {
                        byte[] spentTransactionHash = spentTransactions.Keys[j];
                        previousTransactionsNewTransaction[j] = spentTransactionHash;
                    }

                    Map<string, byte[]> sentTransaction = CreateTransaction(currentBlockHeight, 0, new byte[0], amount, previousTransactionsNewTransaction);
                    byte[] serializedSentTransaction = sentTransaction.Serialize();
                    byte[] sentTransactionHash = Hash256(serializedSentTransaction);
                    recipientUnspentTransactions[sentTransactionHash] = serializedSentTransaction;

                    BankDao.PersistRegularAccount(senderScriptHash, sender.Serialize());
                    BankDao.PersistRegularAccount(recipientScriptHash, recipient.Serialize());
                    Notifier.NotifyTransfer(senderScriptHash, recipientScriptHash, amount, sentTransactionHash, change, changeTransactionHash);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Transfer the amount desired using selected transactions.
        /// </summary>
        /// <param name="args"> [owner, recipient, amount, [txId, txId, ...]]</param>
        /// <returns></returns>
        public static object TransferTransactions(object[] args)
        {
            if (args.Length != 4)
            {
                return false;
            }

            byte[] owner = (byte[])args[0];

            if(owner.Length != 20)
            {
                return false;
            }

            //if(!Runtime.CheckWitness(owner))
            //{
            //    Runtime.Log("Not authorized");
            //    return false;
            //}

            byte[] accountBytes = BankDao.GetRegularAccount(owner);

            if(accountBytes.Length == 0)
            {
                Runtime.Log("Account not found");
                return false;
            }

            byte[] recipient = (byte[])args[1];

            if(recipient.Length != 20)
            {
                Runtime.Log("Invalid recipient");
                return false;
            }

            byte[] recipientAccountBytes = BankDao.GetRegularAccount(owner);
            if(recipientAccountBytes.Length == 0)
            {
                Runtime.Log("Recipient can't be null");
                return false;
            }
            
            BigInteger amountToTransfer = (BigInteger)args[2];

            if(amountToTransfer  <= 0)
            {
                Runtime.Log("Amount can't be zero");
                return false;
            }
            
            Map<string, object> ownerAccount = (Map<string, object>)accountBytes.Deserialize();
            Map<string, object> recipientAccount = (Map<string, object>)recipientAccountBytes.Deserialize();

            //Logic starts here
            object[] transactions = (object[])args[3];
            return DoTransfer(owner, recipient, ownerAccount, recipientAccount, amountToTransfer, transactions);
        }
        
        /// <summary>
        /// Creates tokens and tag it to the master that is creating it.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool MintTokens(object[] args)
        {
            if (args.Length != 3)
            {
                Runtime.Log("Invalid parameters for MintTokens");
                return false;
            }

            byte[] masterAccount = (byte[])args[0];
            if (masterAccount.Length != 20)
            {
                Runtime.Log("Invalid master account");
                return false;
            }

            byte[] masterAccounts = BankDao.GetMasterAccounts();
            if (masterAccounts.Length == 0)
            {
                Runtime.Log("Mint called before Deploy. Contract not deployed");
                return false;
            }

            Map<byte[], BigInteger> masterAccountsObjects = (Map<byte[], BigInteger>)masterAccounts.Deserialize();

            if (!masterAccountsObjects.HasKey(masterAccount))
            {
                Runtime.Log("Master Account not found");
                return false;
            }

            //if (!Runtime.CheckWitness(masterAccount))
            //{
            //    Runtime.Log("Check Witness failed for master account");
            //    return false;
            //}

            byte[] recipient = (byte[])args[1];
            if (recipient.Length != 20)
            {
                Runtime.Log("Invalid recipient to Mint");
                return false;
            }
            

            byte[] amountBytes = (byte[])args[2];
            if (amountBytes.Length == 0)
            {
                Runtime.Log("Invalid amount to Mint");
                return false;
            }

            BigInteger amount = amountBytes.AsBigInteger();
            if (amount > MaxMint)
            {
                Runtime.Log("Amount too large to Mint. ");
                return false;
            }

            byte[] account = BankDao.GetRegularAccount(recipient);
            if (account.Length == 0)
            {
                Runtime.Log("Account not found");
                return false;
            }
            
            Map<string, object> accountMap = (Map<string, object>)account.Deserialize();

            bool accountIsApproved = AccountHasApprovals(accountMap);
            if(!accountIsApproved)
            {
                Runtime.Log("Account not approved");
                return false;
            }
            
            Map<byte[], object> unspentTransactions = (Map<byte[], object>)accountMap[ModelMap.ACCOUNT_UNSPENT];

            Transaction invocationTransaction = (Transaction)ExecutionEngine.ScriptContainer;
            uint currentBlockHeight = Blockchain.GetHeight();
            object[] previousTransactions = new object[] { invocationTransaction.Hash };
            Map<string, byte[]> transaction = CreateTransaction(currentBlockHeight, 1, masterAccount, amount, previousTransactions);

            byte[] serializedTransaction = transaction.Serialize();
            byte[] transactionHash = Hash256(serializedTransaction);

            if (unspentTransactions.HasKey(transactionHash))
            {
                Runtime.Log("Invalid transaction hash to mint");
                return false;
            }

            unspentTransactions[transactionHash] = serializedTransaction;
            accountMap[ModelMap.ACCOUNT_UNSPENT] = unspentTransactions;

            BankDao.PersistRegularAccount(recipient, accountMap.Serialize());
            Notifier.NotifyMint(masterAccount, amount, recipient, transactionHash);

            return true;
        }
        
        /// <summary>
        /// Number of master approvals required to an account be usable
        /// </summary>
        /// <returns></returns>
        public static BigInteger RequiredAuthorizations()
        {
            return 0;
        }

        /// <summary>
        /// Inserts a new account into the database. The account must be inserted prior to being approved.
        /// The account can only be registered by the owner (using the account signature)
        /// </summary>
        /// <param name="args">
        /// Args[0] = Account ScriptHash,
        /// Args[1] = publicKey,
        /// Args[2] = signature,
        /// Args[3] = type (1 for personal 2 for enterprise),
        /// Args[4] = document,
        /// Args[5] = name,
        /// Args[6] = email,
        /// Args[7] = description </param>
        /// <returns></returns>
        public static bool RegisterRegularAccount(object[] args)
        {
            if (args.Length != 8)
            {
                Runtime.Log("Invalid parameters for RegisterRegularAccount");
                return false;
            }

            byte[] newAccount = (byte[])args[0];

            if (newAccount.Length != 20)
            {
                Runtime.Log("Invalid parameters for ApproveRegularAccount");
                return false;
            }
            
            //if (!Runtime.CheckWitness(newAccount))
            //{
            //    Runtime.Log("Invalid signature for account " + newAccount);
            //    return false;
            //}
           
            byte[] currentAccount = BankDao.GetRegularAccount(newAccount);
            if (currentAccount.Length != 0)
            {
                Runtime.Log("Account already exists " + newAccount);
                return false;
            }

            byte[] publicKey = (byte[])args[1];
            byte[] signature = (byte[])args[2];

            if (publicKey.Length == 0)
            {
                Runtime.Log("Invalid public key");
                return false;
            }
            
            if (signature.Length == 0)
            {
                Runtime.Log("Invalid Signature");
                return false;
            }

            BigInteger accountType = (BigInteger)args[3];
            if(accountType != 1 && accountType != 2)
            {
                Runtime.Log("Invalid account type");
                return false;
            }

            string document = (string)args[4];
            if(document.Length < 10 || document.Length > 24)
            {
                Runtime.Log("Invalid document");
                return false;
            }

            string accountName = (string)args[5];
            if (accountName.Length <= 3)
            {
                Runtime.Log("Invalid account name");
                return false;
            }

            string email = (string)args[6];
            if(email.Length < 5)
            {
                Runtime.Log("Invalid e-mail");
                return false;
            }


            string description = (string)args[7];
            if(description.Length > 255)
            {
                Runtime.Log("Remarks too long");
                return false;
            }

            Map<string, object> accountMap = CreateRegularAccount(publicKey, document, email, accountName, accountType, description);
            BankDao.PersistRegularAccount(newAccount, accountMap.Serialize());
            BankDao.PersistSignature(newAccount, signature);
            Notifier.NotifyNewRegularAccount(newAccount);

            return true;
        }

        /// <summary>
        /// Checks if the passed account has enough approvals to receive transactions.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static bool AccountHasApprovals(Map<string, object> account)
        {
            BigInteger requiredApprovals = RequiredAuthorizations();
            Map<byte[], BigInteger> accountApprovals = (Map<byte[], BigInteger>)account[ModelMap.ACCOUNT_MASTER_APPROVALS];
           
            BigInteger approvedCount = 0;

            foreach (byte[] key in accountApprovals.Keys)
            {
                BigInteger masterApprovedStatus = accountApprovals[key];
                approvedCount = approvedCount + 1;
                if (approvedCount >= requiredApprovals)
                {
                    Runtime.Log("Account is approved");
                    return true;
                }
            }

            Runtime.Log("Account not approved");
            return false;
        }
        
        /// <summary>
        /// Get the account status. 1 for approved, 0 for waiting approval / not approved and -1 for account not found
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static BigInteger GetAccountStatus(object[] args)
        {
            if (args.Length != 1)
            {
                Runtime.Log("Invalid parameters for GetAccountStatus");
                return -1;
            }

            byte[] account = (byte[])args[0];

            if (account.Length != 20)
            {
                Runtime.Log("Invalid account for GetAccountStatus");
                return -1;
            }

            byte[] masterAccounts = BankDao.GetMasterAccounts();
            if (masterAccounts.Length != 0)
            {
                Map<byte[], BigInteger> masterAccountsObjects = (Map<byte[], BigInteger>)masterAccounts.Deserialize();
                byte[] accountBytes = BankDao.GetRegularAccount(account);
                if(accountBytes.Length == 0)
                {
                    Runtime.Log("Account not registered");
                    return -1;
                }

                Map<string, object> accountMap = (Map<string, object>)accountBytes.Deserialize();
                if(AccountHasApprovals(accountMap))
                {
                    return 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// Retrieves user unspent transactions.
        /// </summary>
        /// <param name="args">args[0] = Address script hash</param>
        /// <returns></returns>
        public static object GetBalance(object[] args)
        {
            if (args.Length != 1)
            {
                return new object[0];
            }

            byte[] account = (byte[])args[0];
            if (account.Length != 20)
            {
                return new object[0];
            }

            byte[] serializedAccount = BankDao.GetRegularAccount(account);
            if (serializedAccount.Length == 0)
            {
                return new object[0];
            }
            
            Map<string, object> accountMap = (Map<string, object>)serializedAccount.Deserialize();

            if (accountMap.HasKey(ModelMap.ACCOUNT_UNSPENT))
            {
                Map<byte[], byte[]> unspentTransactions = (Map<byte[], byte[]>)accountMap[ModelMap.ACCOUNT_UNSPENT];
                if(unspentTransactions.Keys.Length == 0)
                {
                    Runtime.Log("No unspent found");
                    return new object[0];
                }

                int modelSize = 5;
                object[] readableUnspent = new object[unspentTransactions.Keys.Length * modelSize];
               
                for (int i = 0; i < unspentTransactions.Keys.Length; i++)
                {
                    byte[] txKey = unspentTransactions.Keys[i];
                    Runtime.Log(txKey.AsString());
                    byte[] txBytes = unspentTransactions[txKey];
                    Runtime.Log(txBytes.AsString());
                    if (txBytes.Length > 0)
                    {
                        Map<string, object> tx = (Map<string, object>)txBytes.Deserialize();
                        readableUnspent[i * modelSize] = txKey;
                        readableUnspent[i * modelSize + 1] = tx[ModelMap.TRANSACTION_AMOUNT];
                        readableUnspent[i * modelSize + 2] = tx[ModelMap.TRANSACTION_BLOCK]; 
                        readableUnspent[i * modelSize + 3] = tx[ModelMap.TRANSACTION_MASTER_OWNERS]; //tx[ModelMap.TRANSACTION_AMOUNT];
                        byte[] previousTransactionsBytes = (byte[])tx[ModelMap.TRANSACTION_PREVIOUS_TRANSACTION_HASH];
                        object[] transactions = (object[])previousTransactionsBytes.Deserialize();
                        byte[] transactionsArray = new byte[0];
                        foreach(byte[] transaction in transactions)
                        {
                            transactionsArray = transactionsArray.Concat(transaction);
                        }
                        readableUnspent[i * modelSize + 4] = transactionsArray;
                    }
                }
                return readableUnspent;
            }
            
            
            return new object[0];
        }

        /// <summary>
        /// Adds the master signature into the approved masters of the desired account.
        /// The minimum signatures required to consider an account approved
        /// is defined bythe RequiredAuthorizations method.
        /// </summary>
        /// <param name="args">
        /// args[0] = account to approve,
        /// args[1] = the master account used to approve this request</param>
        /// <returns></returns>
        public static bool ApproveRegularAccount(object[] args)
        {
            Runtime.Log("Approve called");
            if (args.Length != 2)
            {
                Runtime.Log("Invalid parameters for ApproveRegularAccount");
                return false;
            }

            byte[] newAccount = (byte[])args[0];
            byte[] masterAccountKey = (byte[])args[1];

            if(newAccount.Length != 20 || masterAccountKey.Length != 20)
            {
                Runtime.Log("Invalid parameters for ApproveRegularAccount");
                return false;
            }

            byte[] masterAccounts = BankDao.GetMasterAccounts();
            if(masterAccounts.Length == 0)
            {
                Runtime.Log("Master account not found or not approved");
                return false;
            }

            byte[] account = BankDao.GetRegularAccount(newAccount);
            if(account.Length == 0)
            {
                Runtime.Log("Account not registered");
                return false;
            }
            
            Map<byte[], object> masterAccountsObjects = (Map<byte[], object>)masterAccounts.Deserialize();

            if(masterAccountsObjects.HasKey(masterAccountKey))
            {
                Map<string, object> masterAccountMap = (Map<string, object>) masterAccountsObjects[masterAccountKey];
           
                Map<string, object> accountMap = (Map<string, object>)account.Deserialize();
                //if (Runtime.CheckWitness(masterAccountKey))
                if(true)
                {
                    Map<byte[], BigInteger> accountApprovals = (Map<byte[], BigInteger>)accountMap[ModelMap.ACCOUNT_MASTER_APPROVALS];
                    if(accountApprovals.HasKey(masterAccountKey))
                    {
                        BigInteger approvalStatus = accountApprovals[masterAccountKey];
                        if(approvalStatus != 1)
                        {
                            accountApprovals[masterAccountKey] = 1;
                            accountMap[ModelMap.ACCOUNT_MASTER_APPROVALS] = accountApprovals;
                            BankDao.PersistRegularAccount(newAccount, accountMap.Serialize());
                            Notifier.NotifyRegularAccountApproved(masterAccountKey, newAccount);
                            return true;
                        }
                    }
                    else
                    {
                        accountApprovals[masterAccountKey] = 1;
                        BankDao.PersistRegularAccount(newAccount, accountMap.Serialize());
                        Notifier.NotifyRegularAccountApproved(masterAccountKey, newAccount);
                        return true;
                    }
                } 
            }

            return false;
        }

        /// <summary>
        /// Returns account information without balance and master approvals.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object[] GetRegularAccount(object[] args)
        {
            if (args.Length != 1)
            {
                Runtime.Log("Invalid parameters for GetAccount");
                return new object[0];
            }

            byte[] accountAddress = (byte[])args[0];
            if(accountAddress.Length != 20)
            {
                Runtime.Log("Invalid address");
                return new object[0];
            }
            
            byte[] account = BankDao.GetRegularAccount(accountAddress);

            if(account.Length == 0)
            {
                Runtime.Log("Account not found");
                return new object[0];
            }

            Map<string, object> accountMap = (Map<string, object>)account.Deserialize();
            object[] serializedAccount = new object[7];

            serializedAccount[0] = accountMap[ModelMap.ACCOUNT_DOCUMENT];
            serializedAccount[1] = accountMap[ModelMap.ACCOUNT_EMAIL];
            serializedAccount[2] = accountMap[ModelMap.ACCOUNT_NAME];
            serializedAccount[3] = accountMap[ModelMap.ACCOUNT_PUBLIC_KEY];
            serializedAccount[4] = accountMap[ModelMap.ACCOUNT_REMARKS];
            serializedAccount[6] = accountMap[ModelMap.ACCOUNT_TYPE];
            
            return serializedAccount;
        }

        /// <summary>
        /// Removes the master account from the registered MasterAccounts
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object RemoveMasterAccount(object[] args)
        {
            if(args.Length != 1)
            {
                return false;
            }

            byte[] removedAccount = (byte[])args[0];
            if(removedAccount.Length != 20)
            {
                return false;
            }

            //if (!Runtime.CheckWitness(AppGlobals.TrustedSourceScriptHash))
            //{
            //    Runtime.Log("Caller isn't the trusted source");
            //    return false;
            //}

            byte[] masterAccounts = BankDao.GetMasterAccounts();

            if(masterAccounts.Length == 0)
            {
                Runtime.Log("Contract not deployed. Execute Deploy() before using.");
                return false;
            }

            Map<byte[], object> masterAccountsObjects = (Map<byte[], object>)masterAccounts.Deserialize();
           
            if(!masterAccountsObjects.HasKey(removedAccount))
            {
                Runtime.Log("Master Account not found for deletion");
                return false;
            }

            masterAccountsObjects.Remove(removedAccount);
            BankDao.UpdateMasterAccounts(masterAccountsObjects.Serialize());

            return true;
        }

        /// <summary>
        /// Register a Master account and add this information into the storage
        /// </summary>
        /// <param name="args">ScriptHash, EntityName, EntityAddress, EntityPhone, EntityConactEmail</param>
        /// <returns></returns>
        public static object RegisterMasterAccount(object[] args)
        {
            for(int i = 0; i < args.Length; i++)
            {
                string asstr = (string) args[i];
                Runtime.Log(asstr);
            }

            if(args.Length != 5)
            { 
                Runtime.Log("Invalid parameters for RegisterMasterAccount");
                return false;
            }

            byte[] newAccount = (byte[])args[0];

            if(newAccount.Length != 20)
            {
                Runtime.Log("Invalid account for RegisterMasterAccount");
                return false;
            }

            string entityName = (string)args[1];
            if(entityName.Length == 0)
            {
                Runtime.Log("Invalid entity name");
                return false;
            }

            string entityAddress = (string)args[2];
            if (entityAddress.Length == 0)
            {
                Runtime.Log("Invalid entity address");
                return false;
            }

            string entityPhone = (string)args[3];
            if(entityPhone.Length == 0)
            {
                Runtime.Log("Invalid entity phone");
                return false;
            }

            string entityEmail = (string)args[4];
            if(entityEmail.Length == 0)
            {
                Runtime.Log("Invalid entity e-mail");
                return false;
            }
            
            //if (!Runtime.CheckWitness(AppGlobals.TrustedSourceScriptHash))
            //{
            //    Runtime.Log("Caller isn't the trusted source");
            //    return false;
            //}

            byte[] masterAccounts = BankDao.GetMasterAccounts();

            if (masterAccounts.Length == 0)
            {
                Runtime.Log("Contract not deployed. Call Deploy() before using.");
                return false;
            }

            Map<byte[], object> masterAccountsObjects = (Map<byte[], object>)masterAccounts.Deserialize();

            Map<string, object> masterAccount = new Map<string, object>();

            if (masterAccountsObjects.HasKey(newAccount))
            {
                Runtime.Log("Account exists");
                masterAccount = (Map<string, object>)masterAccountsObjects[newAccount];
            }

            //Update the account
            masterAccount = CreateMasterAccount(entityAddress, entityEmail, entityName, entityPhone);
            masterAccountsObjects[newAccount] = masterAccount;

            BankDao.UpdateMasterAccounts(masterAccountsObjects.Serialize());
            Notifier.NotifyNewMasterAccount(newAccount);
            
            return true;
        }
        
        /// <summary>
        /// Returns a list of all Master Accounts registered.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object[] MasterAccounts(object[] args)
        {
            byte[] masterAccounts = BankDao.GetMasterAccounts();
            if(masterAccounts.Length == 0)
            {
                return new object[0];
            }
            int modelSize = 5;
            Map<byte[], object> masterAccountsMap = (Map<byte[], object>)masterAccounts.Deserialize();
            object[] masterAccountsArray = new object[masterAccountsMap.Keys.Length * modelSize];
            for(int i = 0; i < masterAccountsMap.Keys.Length; i++)
            {
                byte[] masterAccountKey = masterAccountsMap.Keys[i];
                Map<string, object> masterAccount = (Map<string, object>) masterAccountsMap[masterAccountKey];
                masterAccountsArray[i * modelSize] = masterAccount[ModelMap.MASTER_NAME];
                masterAccountsArray[i * modelSize + 1] = masterAccount[ModelMap.MASTER_EMAIL];
                masterAccountsArray[i * modelSize + 2] = masterAccount[ModelMap.MASTER_PHONE];
                masterAccountsArray[i * modelSize + 3] = masterAccount[ModelMap.MASTER_ADDRESS];
                masterAccountsArray[i * modelSize + 4] = masterAccountKey;
            }

            return masterAccountsArray;
        }

        /// <summary>
        /// Creates an "Master account object map", initializing each property with the desired values
        /// </summary>
        /// <param name="entityAddress"></param>
        /// <param name="entityEmail"></param>
        /// <param name="entityName"></param>
        /// <param name="entityPhone"></param>
        /// <returns></returns>
        private static Map<string, object> CreateMasterAccount(string entityAddress, string entityEmail, string entityName, string entityPhone)
        {
            Map<string, object> masterAccount = new Map<string, object>();
            masterAccount[ModelMap.MASTER_ADDRESS] = entityAddress;
            masterAccount[ModelMap.MASTER_EMAIL] = entityEmail;
            masterAccount[ModelMap.MASTER_NAME] = entityName;
            masterAccount[ModelMap.MASTER_PHONE] = entityPhone;
            return masterAccount;
        }

        /// <summary>
        /// Creates an "transaction object map" , initializing each property with the desired values
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="isRoot"></param>
        /// <param name="masterAccount"></param>
        /// <param name="amount"></param>
        /// <param name="previousTransactions"></param>
        /// <returns></returns>
        private static Map<string, byte[]> CreateTransaction(uint blockHeight, BigInteger isRoot, byte[] masterAccount, BigInteger amount, object[] previousTransactions)
        {
            BigInteger blockHeightBigInteger = blockHeight;
            Map<string, byte[]> transaction = new Map<string, byte[]>();

            transaction[ModelMap.TRANSACTION_MASTER_OWNERS] = masterAccount;
            transaction[ModelMap.TRANSACTION_AMOUNT] = amount.AsByteArray();
            transaction[ModelMap.TRANSACTION_PREVIOUS_TRANSACTION_HASH] = previousTransactions.Serialize();
            transaction[ModelMap.TRANSACTION_BLOCK] = blockHeightBigInteger.AsByteArray();
            transaction[ModelMap.TRANSACTION_IS_ROOT] = isRoot.AsByteArray();

            return transaction;
        }
        
        /// <summary>
        /// Creates an "account object map", initializing each property with the desired value
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="signature"></param>
        /// <param name="document"></param>
        /// <param name="email"></param>
        /// <param name="accountName"></param>
        /// <param name="accountType"></param>
        /// <param name="remarks"></param>
        /// <returns></returns>
        private static Map<string, object> CreateRegularAccount(byte[] publicKey, string document, string email, string accountName, BigInteger accountType, string remarks)
        {
            Map<string, object> accountMap = new Map<string, object>();
            accountMap[ModelMap.ACCOUNT_PUBLIC_KEY] = publicKey;
            accountMap[ModelMap.ACCOUNT_DOCUMENT] = document;
            accountMap[ModelMap.ACCOUNT_EMAIL] = email;
            accountMap[ModelMap.ACCOUNT_NAME] = accountName;
            accountMap[ModelMap.ACCOUNT_TYPE] = accountType;
            accountMap[ModelMap.ACCOUNT_REMARKS] = remarks;
            accountMap[ModelMap.ACCOUNT_UNSPENT] = new Map<byte[], object>();
            accountMap[ModelMap.ACCOUNT_MASTER_APPROVALS] = new Map<byte, object>();
            return accountMap;
        }
    }
}
