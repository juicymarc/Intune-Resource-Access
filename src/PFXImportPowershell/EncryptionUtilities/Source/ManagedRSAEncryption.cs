﻿// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portionas of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace Microsoft.Intune.EncryptionUtilities
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Pkcs;
    using System.Security.Cryptography.X509Certificates;

    public class ManagedRSAEncryption : ICNGLocalKeyCrypto
    {
        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public ManagedRSAEncryption()
        {
        }

        /// <summary>
        /// Given a KeyProvider and a KeyName, uses that key to encrypt the given data
        /// </summary>
        /// <param name="providerName">Provider where the key is stored</param>
        /// <param name="keyName">Name of the key to use</param>
        /// <param name="toEncrypt">Data to encrypt</param>
        /// <param name="hashAlgorithm">OAEP hash algorithm, Look in PaddingHashAlgorithmNames.cs for values, but supports only SHA1, SHA256, SHA384, SHA512</param>
        /// <param name="paddingFlags">Padding Type, Look in PaddingFlags.cs for values, but supports only PKCS1 amd OAEP</param>
        /// <exception cref="CryptographicException">Cryptographic Exception</exception>
        /// <returns>Encrypted data</returns>
        public byte[] EncryptWithLocalKey(string providerName, string keyName, byte[] toEncrypt, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            CngProvider provider = new CngProvider(providerName);
            CngKey key = null;
            RSACng rsa = null;
            bool keyExists = false;
            byte[] encryptedData = null;

            try
            {
                try
                {
                    if (CngKey.Exists(keyName, provider))
                    {
                        keyExists = true;
                    }
                }
                catch (CryptographicException e)
                {
                    // This happens if the provider isn't a valid one, but we want the exception to have better info
                    throw new CryptographicException(string.Format("The provider {0} does not exist", providerName), e);
                }

                if (keyExists)
                {
                    key = CngKey.Open(keyName, provider);
                }
                else
                {
                    throw new CryptographicException(string.Format("They key {0} does not exist and cannot be used for encryption", keyName));
                }

                rsa = new RSACng(key);
                RSAEncryptionPadding padding = this.GetRSAPadding(hashAlgorithm, paddingFlags);
                encryptedData = rsa.Encrypt(toEncrypt, padding);
            }
            finally
            {
                if (rsa != null)
                {
                    rsa.Dispose();
                }

                if (key != null)
                {
                    key.Dispose();
                }
            }

            return encryptedData;
        }

        /// <summary>
        /// Given a KeyProvider and a KeyName, decrypts the data with the given key
        /// </summary>
        /// <param name="providerName">Provider where the key is stored</param>
        /// <param name="keyName">Name of the key to use</param>
        /// <param name="toDecrypt">Data to decrypt</param>
        /// <param name="hashAlgorithm">OAEP hash algorithm, Look in PaddingHashAlgorithmNames.cs for values, but supports only SHA1, SHA256, SHA384, SHA512</param>
        /// <param name="paddingFlags">Padding Type, Look in PaddingFlags.cs for values, but supports only PKCS1 amd OAEP</param>
        /// <exception cref="CryptographicException">Cryptographic Exception</exception>
        /// <returns>The decrypted data</returns>
        public byte[] DecryptWithLocalKey(string providerName, string keyName, byte[] toDecrypt, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            CngProvider provider = new CngProvider(providerName);
            CngKey key = null;
            RSACng rsa = null;
            bool keyExists = false;
            byte[] decrypted;

            try
            {
                try
                {
                    if (CngKey.Exists(keyName, provider))
                    {
                        keyExists = true;
                    }
                }
                catch (CryptographicException e)
                {
                    // This happens if the provider isn't a valid one, but we want the exception to have better info
                    throw new CryptographicException(string.Format("The provider {0} does not exist", providerName), e);
                }

                if (keyExists)
                {
                    key = CngKey.Open(keyName, provider);
                }
                else
                {
                    throw new CryptographicException(string.Format("They key {0} does not exist and cannot be used for decryption", keyName));
                }

                rsa = new RSACng(key);
                RSAEncryptionPadding padding = this.GetRSAPadding(hashAlgorithm, paddingFlags);
                decrypted = rsa.Decrypt(toDecrypt, padding);
            }
            finally
            {
                if (rsa != null)
                {
                    rsa.Dispose();
                }

                if (key != null)
                {
                    key.Dispose();
                }
            }

            return decrypted;
        }

        /// <summary>
        /// Tries to generate and RSA Key in the given provider with this keyName.  
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        /// <param name="keyName">Name of the key</param>
        /// <param name="keyLength">Length of the key to generate</param>
        /// <returns>true if successful, false if that key already exists.</returns>
        public bool TryGenerateLocalRSAKey(string providerName, string keyName, int keyLength = 2048)
        {
            CngProvider provider = new CngProvider(providerName);
            CngKey key = null;

            try
            {
                if (CngKey.Exists(keyName, provider))
                {
                    return false;
                }
                else
                {
                    CngKeyCreationParameters keyParams = new CngKeyCreationParameters()
                    {
                        ExportPolicy = CngExportPolicies.None,
                        Provider = provider,
                        Parameters = { new CngProperty("Length", BitConverter.GetBytes(keyLength), CngPropertyOptions.None) }
                    };

                    key = CngKey.Create(CngAlgorithm.Rsa, keyName, keyParams);
                    return true;
                }
            }
            finally
            {
                if (key != null)
                {
                    key.Dispose();
                }
            }
        }

        /// <summary>
        /// Will destroy the key with keyname within the given provider with providerName.  Will throw CryptographicException if either Provider or Key don't exist
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        /// <param name="keyName">Name of the key to destroy</param>
        public void DestroyLocalRSAKey(string providerName, string keyName)
        {
            CngProvider provider = new CngProvider(providerName);

            if (CngKey.Exists(keyName, provider))
            {
                using (CngKey key = CngKey.Open(keyName, provider))
                {
                    key.Delete();
                }
            }
        }

        /// <summary>
        /// Encrypts the given data with the given certificate
        /// </summary>
        /// <param name="toEncrypt">Data to encrypt</param>
        /// <param name="encryptionCert">Certificate to encrypt with</param>
        /// <returns>The encrypted data</returns>
        public byte[] EncryptWithCertificate(byte[] toEncrypt, X509Certificate2 encryptionCert)
        {
            if (toEncrypt == null)
            {
                throw new ArgumentNullException(nameof(toEncrypt));
            }

            if (encryptionCert == null)
            {
                throw new ArgumentNullException(nameof(encryptionCert));
            }

            X509Certificate2Collection encryptedCerts = new X509Certificate2Collection(encryptionCert);

            ContentInfo contentInfo = new ContentInfo(toEncrypt);
            EnvelopedCms cms = new EnvelopedCms(contentInfo);
            CmsRecipientCollection recipCollection = new CmsRecipientCollection(SubjectIdentifierType.IssuerAndSerialNumber, encryptedCerts);
            cms.Encrypt(recipCollection);
            byte[] bytes = cms.Encode();

            return bytes;
        }

        /// <summary>
        /// Decrypts the given data with the given certificate
        /// </summary>
        /// <param name="toDecrypt">Data to encrypt</param>
        /// <param name="decryptionCert">Certificate to encrypt with</param>
        /// <returns>The decrypted data</returns>
        public byte[] DecryptWithCertificate(byte[] toDecrypt, X509Certificate2 decryptionCert)
        {
            if (toDecrypt == null)
            {
                throw new ArgumentNullException(nameof(toDecrypt));
            }

            if (decryptionCert == null)
            {
                throw new ArgumentNullException(nameof(decryptionCert));
            }

            X509Certificate2Collection decryptCerts = new X509Certificate2Collection(decryptionCert);

            ContentInfo contentInfo = new ContentInfo(toDecrypt);
            EnvelopedCms cms = new EnvelopedCms(contentInfo);
            cms.Decode(contentInfo.Content);
            cms.Decrypt(decryptCerts);
            return cms.ContentInfo.Content;
        }

        /// <summary>
        /// Takes the encrypted data, decrypts it with the key <paramref name="keyName"/>  found in the provider <paramref name="providerName"/>, and then recrypts it with <paramref name="deviceCertificate"/>
        /// </summary>
        /// <param name="encryptedPassword">Data encrypted with the given key</param>
        /// <param name="deviceCertificate">Certificate to recrypt with</param>
        /// <param name="providerName">Provider that the key is stored in</param>
        /// <param name="keyName">Key used to originally encrypt the data</param>
        /// <param name="hashAlgorithm">OAEP hash algorithm</param>
        /// <param name="paddingFlags">Padding Type</param>
        /// <returns>Data recrypted by the given certificate</returns>
        public byte[] RecryptPfxImportMessage(byte[] encryptedPassword, X509Certificate2 deviceCertificate, string providerName, string keyName, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            byte[] decryptedPassword = null;
            byte[] recryptedPassword;

            decryptedPassword = this.DecryptWithLocalKey(providerName, keyName, encryptedPassword, hashAlgorithm, paddingFlags);
            GCHandle pinnedPasswordHandle = GCHandle.Alloc(decryptedPassword, GCHandleType.Pinned);

            try
            {
                recryptedPassword = this.EncryptWithCertificate(decryptedPassword, deviceCertificate);
                return recryptedPassword;
            }
            finally
            {
                // Need to clean up decrypted password, make sure it's not staying in memory any longer than necessary.
                // The decryptedPassword was pinned after the decryption was done so it is possible that the
                // GC ran and copied the memory around before it could be pinned.  With the current design of the
                // .NET RSA API, pinning right after doing the decryption is the best we can do to minimize proliferation
                // of secrets throughout memory.

                if (decryptedPassword != null)
                {
                    decryptedPassword.ZeroFill();
                }

                if (pinnedPasswordHandle.IsAllocated)
                {
                    pinnedPasswordHandle.Free();
                }
            }
        }

        /// <summary>
        /// Parses thepadding flags into an RSAEncryptionPadding object we can use with the API
        /// Look in PaddingFlags.cs for values, but supports only PKCS1 amd OAEP
        /// </summary>
        /// <param name="hashAlgorithm">Name of the hash algorithm to use, look in the PaddingHashAlgorithmNames enum for values</param>
        /// <param name="paddingFlags">Padding Type, Look in PaddingFlags.cs for values, but supports only PKCS1 amd OAEP</param>
        /// <returns>padding</returns>
        private RSAEncryptionPadding GetRSAPadding(string hashAlgorithm, int paddingFlags)
        {
            RSAEncryptionPadding padding = null;

            switch (paddingFlags)
            {
                case PaddingFlags.PKCS1Padding:
                    padding = RSAEncryptionPadding.Pkcs1;
                    break;
                case PaddingFlags.OAEPPadding:
                    // Need to parse the hash algorithm out of the string
                    HashAlgorithmName hashAlgorithmName = this.GetHashAlgorithmNameFromPlaintext(hashAlgorithm);
                    padding = RSAEncryptionPadding.CreateOaep(hashAlgorithmName);
                    break;
                default:
                    throw new CryptographicException(
                        string.Format(
                            "Attempting to get the RSA padding of type {0} is not supported, only supported types are PKCS1 ({1}) or OAEP ({2})", 
                            paddingFlags, 
                            (int)PaddingFlags.PKCS1Padding, 
                            (int)PaddingFlags.OAEPPadding));
            }

            return padding;
        }

        /// <summary>
        /// Returns a CNG HashAlgorithmName object matching the plaintext string passed in
        /// </summary>
        /// <param name="hashAlgorithm">Name of the hash algorithm to use, look in the PaddingHashAlgorithmNames enum for values</param>
        /// <returns>CNG HashAlgorithmName</returns>
        private HashAlgorithmName GetHashAlgorithmNameFromPlaintext(string hashAlgorithm)
        {
            HashAlgorithmName hashAlgorithmName;
            if (hashAlgorithm.Equals(PaddingHashAlgorithmNames.SHA1))
            {
                hashAlgorithmName = HashAlgorithmName.SHA1;
            }
            else if (hashAlgorithm.Equals(PaddingHashAlgorithmNames.SHA256))
            {
                hashAlgorithmName = HashAlgorithmName.SHA256;
            }
            else if (hashAlgorithm.Equals(PaddingHashAlgorithmNames.SHA384))
            {
                hashAlgorithmName = HashAlgorithmName.SHA384;
            }
            else if (hashAlgorithm.Equals(PaddingHashAlgorithmNames.SHA512))
            {
                hashAlgorithmName = HashAlgorithmName.SHA512;
            }
            else
            {
                throw new CryptographicException(
                    string.Format(
                        "Attempting to find HashAlgorithm for {0} failed, only supported algorithms are {1}, {2}, {3}, {4}",
                        hashAlgorithm, 
                        PaddingHashAlgorithmNames.SHA1, 
                        PaddingHashAlgorithmNames.SHA256, 
                        PaddingHashAlgorithmNames.SHA384, 
                        PaddingHashAlgorithmNames.SHA512));
            }

            return hashAlgorithmName;
        }
    }
}